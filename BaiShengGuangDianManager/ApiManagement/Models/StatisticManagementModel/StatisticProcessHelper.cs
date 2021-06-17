using ApiManagement.Base.Server;
using ApiManagement.Models.AccountManagementModel;
using ApiManagement.Models.BaseModel;
using ApiManagement.Models.DeviceManagementModel;
using ApiManagement.Models.FlowCardManagementModel;
using ModelBase.Base.Logger;
using ModelBase.Base.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.StatisticManagementModel
{
    public class StatisticProcessHelper : DataHelper
    {
        private StatisticProcessHelper()
        {
            Table = "statistic_process";
            InsertSql = "";
            UpdateSql = "";

            SameField = "";
            //MenuFields.AddRange(new[] { "Id", "Time", "OpName", "DeviceId", "StartTime", "EndTime" });
        }
        public static readonly StatisticProcessHelper Instance = new StatisticProcessHelper();
        #region Get
        public static IEnumerable<StatisticProcessAll> GetDetails(int wId, StatisticProcessTimeEnum timeType,
            DateTime startTime, DateTime endTime, List<int> steps, List<int> deviceIds, List<int> productionIds, List<int> processorIds)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (wId != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("WorkshopId", "=", wId));
            }
            args.Add(new Tuple<string, string, dynamic>("Type", "=", timeType));
            if (startTime != default(DateTime))
            {
                args.Add(new Tuple<string, string, dynamic>("Time", ">=", startTime));
            }
            if (endTime != default(DateTime))
            {
                args.Add(new Tuple<string, string, dynamic>("Time", "<", endTime));
            }
            if (steps != null && steps.Any())
            {
                args.Add(new Tuple<string, string, dynamic>("Step", "IN", steps));
            }
            if (deviceIds != null && deviceIds.Any())
            {
                args.Add(new Tuple<string, string, dynamic>("DeviceId", "IN", deviceIds));
            }
            if (productionIds != null && productionIds.Any())
            {
                args.Add(new Tuple<string, string, dynamic>("ProductionId", "IN", productionIds));
            }
            if (processorIds != null && processorIds.Any())
            {
                args.Add(new Tuple<string, string, dynamic>("ProcessorId", "IN", processorIds));
            }

            return Instance.CommonGet<StatisticProcessAll>(args, false, 1000);
        }

        public static IEnumerable<StatisticProcessAll> GetDetails(int wId, StatisticProcessTimeEnum timeType,
            IEnumerable<DateTime> times, List<int> steps, List<int> deviceIds, List<int> productionIds, List<int> processorIds)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (wId != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("WorkshopId", "=", wId));
            }
            args.Add(new Tuple<string, string, dynamic>("Type", "=", timeType));
            if (times != null && times.Any())
            {
                args.Add(new Tuple<string, string, dynamic>("Time", "IN", times));
            }
            if (steps != null && steps.Any())
            {
                args.Add(new Tuple<string, string, dynamic>("Step", "IN", steps));
            }
            if (deviceIds != null && deviceIds.Any())
            {
                args.Add(new Tuple<string, string, dynamic>("DeviceId", "IN", deviceIds));
            }
            if (productionIds != null && productionIds.Any())
            {
                args.Add(new Tuple<string, string, dynamic>("ProductionId", "IN", productionIds));
            }
            if (processorIds != null && processorIds.Any())
            {
                args.Add(new Tuple<string, string, dynamic>("ProcessorId", "IN", processorIds));
            }

            return Instance.CommonGet<StatisticProcessAll>(args, false, 1000);
        }
        /// <summary>
        /// 获取加工数据  例：班制shift为0, 首班时间为8:00:00，统计延时30分钟时, 5月28日的数据为5月28日(含)至5月29日8:30:00(不含)
        /// <param name="shift">班制 0 全天 1  2  ...  </param>
        /// <param name="isSum">是否分组</param>
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<StatisticProcessAll> StatisticProcesses(KanBanItemEnum itemType, DateTime time,
            Workshop workshop, int shift, StatisticProcessTimeEnum timeType
            , int range, bool isSum, List<int> steps, List<int> deviceIds, List<int> productionIds, List<int> processorIds)
        {
            var res = new List<StatisticProcessAll>();
            try
            {
                var workshopId = workshop.Id;
                //DateTime startTime, DateTime endTime
                var stepList = (steps != null && steps.Any())
                    ? DeviceProcessStepHelper.GetMenu(workshopId, steps).ToDictionary(x => x.Id, x => x.StepName)
                    : new Dictionary<int, string>();
                var productionList = (productionIds != null && productionIds.Any())
                    ? ProductionHelper.GetMenu(workshopId, productionIds).ToDictionary(x => x.Id, x => x.ProductionProcessName)
                    : new Dictionary<int, string>();
                var deviceList = (deviceIds != null && deviceIds.Any())
                    ? DeviceLibraryHelper.GetMenu(workshopId, deviceIds).ToDictionary(x => x.Id, x => x.Code)
                    : new Dictionary<int, string>();
                var processorList = (processorIds != null && processorIds.Any())
                    ? AccountInfoHelper.GetMenu(processorIds).ToDictionary(x => x.Id, x => x.Name)
                    : new Dictionary<int, string>();
                if (!stepList.Any())
                {
                    return res;
                }

                var shiftTimes = shift == 0 ? DateTimeExtend.GetDayWorkDay(workshop.StatisticTimeList, time)
                    : DateTimeExtend.GetDayWorkTimeRange(shift, workshop.StatisticTimeList, time);
                var startTime = default(DateTime);
                var endTime = default(DateTime);
                var dTimeType = timeType == StatisticProcessTimeEnum.小时 ?
                    timeType : shift == 0 ? StatisticProcessTimeEnum.日 : StatisticProcessTimeEnum.小时;
                switch (timeType)
                {
                    case StatisticProcessTimeEnum.小时:
                        var t = time < shiftTimes.Item2 ? time : shiftTimes.Item2;
                        endTime = t > t.NoMinute() ? t.NoMinute().AddHours(1) : t.NoMinute();
                        var th = (int)(endTime - shiftTimes.Item1.NoMinute()).TotalHours;
                        startTime = th < range ? shiftTimes.Item1.NoMinute() : time.NoMinute().AddHours(-range);
                        range = th < range ? th : range;
                        break;
                    case StatisticProcessTimeEnum.日:
                        if (shift == 0)
                        {
                            endTime = shiftTimes.Item2.Date;
                            startTime = shiftTimes.Item1.AddDays(1 - range).Date;
                        }
                        else
                        {
                            endTime = shiftTimes.Item2.NoMinute();
                            startTime = shiftTimes.Item1.AddDays(-range).NoMinute();
                        }
                        break;
                    case StatisticProcessTimeEnum.周:
                        var week = DateTimeExtend.GetWorkWeek(0, time, workshop.StatisticTimeList);
                        if (shift == 0)
                        {
                            endTime = week.Item2.Date;
                            startTime = week.Item1.AddWeeks(-range).Date;
                        }
                        else
                        {
                            endTime = week.Item2.NoMinute();
                            startTime = week.Item1.AddWeeks(-range).NoMinute();
                        }
                        break;
                    case StatisticProcessTimeEnum.月:
                        var month = DateTimeExtend.GetWorkMonth(0, time, workshop.StatisticTimeList);
                        if (shift == 0)
                        {
                            endTime = month.Item2.Date;
                            startTime = month.Item1.AddMonths(-range).Date;
                        }
                        else
                        {
                            endTime = month.Item2.NoMinute();
                            startTime = month.Item1.AddMonths(-range).NoMinute();
                        }
                        break;
                    case StatisticProcessTimeEnum.年:
                        var year = DateTimeExtend.GetWorkYear(0, time, workshop.StatisticTimeList);
                        if (shift == 0)
                        {
                            endTime = year.Item2.Date;
                            startTime = year.Item1.AddYears(-range).Date;
                        }
                        else
                        {
                            endTime = year.Item2.NoMinute();
                            startTime = year.Item1.AddYears(-range).NoMinute();
                        }
                        break;
                }
                if (startTime == default(DateTime) || endTime == default(DateTime))
                {
                    return res;
                }

                var data = GetDetails(workshopId, dTimeType, startTime.NoMinute(), endTime.NextHour(0, 1), steps, deviceIds, productionIds, processorIds);

                switch (timeType)
                {
                    case StatisticProcessTimeEnum.小时:
                        #region 小时
                        for (var i = range; i > 0; i--)
                        {
                            var t = endTime.AddHours(-i).NoMinute();
                            if (t > time)
                            {
                                continue;
                            }

                            if (isSum)
                            {
                                var tData = data.Where(x => x.Time == t);
                                AddStatisticProcess(t, ref res, tData);
                            }
                            else
                            {
                                if (itemType == KanBanItemEnum.计划号工序推移图)
                                {
                                    foreach (var (id, name) in productionList)
                                    {
                                        var tData = data.Where(x => x.Time == t && x.ProductionId == id);
                                        AddProduction(t, ref res, tData, id, name);
                                    }
                                }
                                else if (itemType == KanBanItemEnum.设备工序推移图)
                                {
                                    foreach (var (id, name) in deviceList)
                                    {
                                        var tData = data.Where(x => x.Time == t && x.DeviceId == id);
                                        AddDevice(t, ref res, tData, id, name);
                                    }
                                }
                                else if (itemType == KanBanItemEnum.操作工工序推移图)
                                {
                                    foreach (var (id, name) in processorList)
                                    {
                                        var tData = data.Where(x => x.Time == t && x.ProcessorId == id);
                                        AddProcessor(t, ref res, tData, id, name);
                                    }
                                }
                            }
                        }
                        #endregion
                        break;
                    case StatisticProcessTimeEnum.日:
                        #region 日
                        for (var i = range; i > 0; i--)
                        {
                            var t = endTime.AddDays(-i);
                            Tuple<DateTime, DateTime> sfTimes;
                            if (shift == 0)
                            {
                                sfTimes = new Tuple<DateTime, DateTime>(t, t.AddDays(1));
                            }
                            else
                            {
                                sfTimes = DateTimeExtend.GetDayWorkTimeRange(shift, workshop.StatisticTimeList, t);
                            }

                            if (isSum)
                            {
                                var tData = data.Where(x => x.Time.InSameRange(sfTimes));
                                AddStatisticProcess(sfTimes.Item1, ref res, tData);
                            }
                            else
                            {
                                if (itemType == KanBanItemEnum.计划号工序推移图)
                                {
                                    foreach (var (id, name) in productionList)
                                    {
                                        var tData = data.Where(x => x.Time.InSameRange(sfTimes) && x.ProductionId == id);
                                        AddProduction(sfTimes.Item1, ref res, tData, id, name);
                                    }
                                }
                                else if (itemType == KanBanItemEnum.设备工序推移图)
                                {
                                    foreach (var (id, name) in deviceList)
                                    {
                                        var tData = data.Where(x => x.Time.InSameRange(sfTimes) && x.DeviceId == id);
                                        AddDevice(sfTimes.Item1, ref res, tData, id, name);
                                    }
                                }
                                else if (itemType == KanBanItemEnum.操作工工序推移图)
                                {
                                    foreach (var (id, name) in processorList)
                                    {
                                        var tData = data.Where(x => x.Time.InSameRange(sfTimes) && x.ProcessorId == id);
                                        AddProcessor(sfTimes.Item1, ref res, tData, id, name);
                                    }
                                }
                            }
                        }
                        #endregion
                        break;
                    case StatisticProcessTimeEnum.周:
                        #region 周
                        for (var i = range; i > 0; i--)
                        {
                            var t = endTime.AddWeeks(-i);
                            var week = DateTimeExtend.GetWeek(0, t);
                            if (isSum)
                            {
                                var tData = data.Where(x =>
                                {
                                    if (x.Time.InSameRange(week))
                                    {
                                        var sfTimes = shift == 0 ? new Tuple<DateTime, DateTime>(t, t.AddWeeks(1))
                                            : DateTimeExtend.GetDayWorkTimeRange(shift, workshop.StatisticTimeList, x.Time);
                                        return x.Time.InSameRange(sfTimes);
                                    }
                                    return false;
                                });
                                AddStatisticProcess(week.Item1, ref res, tData);
                            }
                            else
                            {
                                if (itemType == KanBanItemEnum.计划号工序推移图)
                                {
                                    foreach (var (id, name) in productionList)
                                    {
                                        var tData = data.Where(x =>
                                        {
                                            if (x.Time.InSameRange(week) && x.ProductionId == id)
                                            {
                                                var sfTimes = shift == 0 ? new Tuple<DateTime, DateTime>(t, t.AddWeeks(1))
                                                    : DateTimeExtend.GetDayWorkTimeRange(shift, workshop.StatisticTimeList, x.Time);
                                                return x.Time.InSameRange(sfTimes);
                                            }
                                            return false;
                                        });
                                        AddProduction(week.Item1, ref res, tData, id, name);
                                    }
                                }
                                else if (itemType == KanBanItemEnum.设备工序推移图)
                                {
                                    foreach (var (id, name) in deviceList)
                                    {
                                        var tData = data.Where(x =>
                                        {
                                            if (x.Time.InSameRange(week) && x.DeviceId == id)
                                            {
                                                var sfTimes = shift == 0 ? new Tuple<DateTime, DateTime>(t, t.AddWeeks(1))
                                                    : DateTimeExtend.GetDayWorkTimeRange(shift, workshop.StatisticTimeList, x.Time);
                                                return x.Time.InSameRange(sfTimes);
                                            }
                                            return false;
                                        });
                                        AddDevice(week.Item1, ref res, tData, id, name);
                                    }
                                }
                                else if (itemType == KanBanItemEnum.操作工工序推移图)
                                {
                                    foreach (var (id, name) in processorList)
                                    {
                                        var tData = data.Where(x =>
                                        {
                                            if (x.Time.InSameRange(week) && x.ProcessorId == id)
                                            {
                                                var sfTimes = shift == 0 ? new Tuple<DateTime, DateTime>(t, t.AddWeeks(1))
                                                    : DateTimeExtend.GetDayWorkTimeRange(shift, workshop.StatisticTimeList, x.Time);
                                                return x.Time.InSameRange(sfTimes);
                                            }
                                            return false;
                                        });
                                        AddProcessor(week.Item1, ref res, tData, id, name);
                                    }
                                }
                            }
                        }
                        #endregion
                        break;
                    case StatisticProcessTimeEnum.月:
                        #region 月
                        for (var i = range; i > 0; i--)
                        {
                            var t = endTime.AddMonths(-i);
                            var month = DateTimeExtend.GetMonth(0, t);
                            if (isSum)
                            {
                                var tData = data.Where(x =>
                                {
                                    if (x.Time.InSameRange(month))
                                    {
                                        var sfTimes = shift == 0 ? new Tuple<DateTime, DateTime>(t, t.AddMonths(1))
                                            : DateTimeExtend.GetDayWorkTimeRange(shift, workshop.StatisticTimeList, x.Time);
                                        return x.Time.InSameRange(sfTimes);
                                    }
                                    return false;
                                });
                                AddStatisticProcess(month.Item1, ref res, tData);
                            }
                            else
                            {
                                if (itemType == KanBanItemEnum.计划号工序推移图)
                                {
                                    foreach (var (id, name) in productionList)
                                    {
                                        var tData = data.Where(x =>
                                        {
                                            if (x.Time.InSameRange(month) && x.ProductionId == id)
                                            {
                                                var sfTimes = shift == 0 ? new Tuple<DateTime, DateTime>(t, t.AddMonths(1))
                                                    : DateTimeExtend.GetDayWorkTimeRange(shift, workshop.StatisticTimeList, x.Time);
                                                return x.Time.InSameRange(sfTimes);
                                            }
                                            return false;
                                        });
                                        AddProduction(month.Item1, ref res, tData, id, name);
                                    }
                                }
                                else if (itemType == KanBanItemEnum.设备工序推移图)
                                {
                                    foreach (var (id, name) in deviceList)
                                    {
                                        var tData = data.Where(x =>
                                        {
                                            if (x.Time.InSameRange(month) && x.DeviceId == id)
                                            {
                                                var sfTimes = shift == 0 ? new Tuple<DateTime, DateTime>(t, t.AddMonths(1))
                                                    : DateTimeExtend.GetDayWorkTimeRange(shift, workshop.StatisticTimeList, x.Time);
                                                return x.Time.InSameRange(sfTimes);
                                            }
                                            return false;
                                        });
                                        AddDevice(month.Item1, ref res, tData, id, name);
                                    }
                                }
                                else if (itemType == KanBanItemEnum.操作工工序推移图)
                                {
                                    foreach (var (id, name) in processorList)
                                    {
                                        var tData = data.Where(x =>
                                        {
                                            if (x.Time.InSameRange(month) && x.ProcessorId == id)
                                            {
                                                var sfTimes = shift == 0 ? new Tuple<DateTime, DateTime>(t, t.AddMonths(1))
                                                    : DateTimeExtend.GetDayWorkTimeRange(shift, workshop.StatisticTimeList, x.Time);
                                                return x.Time.InSameRange(sfTimes);
                                            }
                                            return false;
                                        });
                                        AddProcessor(month.Item1, ref res, tData, id, name);
                                    }
                                }
                            }
                        }
                        #endregion
                        break;
                    case StatisticProcessTimeEnum.年:
                        #region 年
                        for (var i = range; i > 0; i--)
                        {
                            var t = endTime.AddYears(-i);
                            var year = DateTimeExtend.GetYear(0, t);
                            if (isSum)
                            {
                                var tData = data.Where(x =>
                                {
                                    if (x.Time.InSameRange(year))
                                    {
                                        var sfTimes = shift == 0 ? new Tuple<DateTime, DateTime>(t, t.AddYears(1))
                                            : DateTimeExtend.GetDayWorkTimeRange(shift, workshop.StatisticTimeList, x.Time);
                                        return x.Time.InSameRange(sfTimes);
                                    }
                                    return false;
                                });
                                AddStatisticProcess(year.Item1, ref res, tData);
                            }
                            else
                            {
                                if (itemType == KanBanItemEnum.计划号工序推移图)
                                {
                                    foreach (var (id, name) in productionList)
                                    {
                                        var tData = data.Where(x =>
                                        {
                                            if (x.Time.InSameRange(year) && x.ProductionId == id)
                                            {
                                                var sfTimes = shift == 0 ? new Tuple<DateTime, DateTime>(t, t.AddYears(1))
                                                    : DateTimeExtend.GetDayWorkTimeRange(shift, workshop.StatisticTimeList, x.Time);
                                                return x.Time.InSameRange(sfTimes);
                                            }
                                            return false;
                                        });
                                        AddProduction(year.Item1, ref res, tData, id, name);
                                    }
                                }
                                else if (itemType == KanBanItemEnum.设备工序推移图)
                                {
                                    foreach (var (id, name) in deviceList)
                                    {
                                        var tData = data.Where(x =>
                                        {
                                            if (x.Time.InSameRange(year) && x.DeviceId == id)
                                            {
                                                var sfTimes = shift == 0 ? new Tuple<DateTime, DateTime>(t, t.AddYears(1))
                                                    : DateTimeExtend.GetDayWorkTimeRange(shift, workshop.StatisticTimeList, x.Time);
                                                return x.Time.InSameRange(sfTimes);
                                            }
                                            return false;
                                        });
                                        AddDevice(year.Item1, ref res, tData, id, name);
                                    }
                                }
                                else if (itemType == KanBanItemEnum.操作工工序推移图)
                                {
                                    foreach (var (id, name) in processorList)
                                    {
                                        var tData = data.Where(x =>
                                        {
                                            if (x.Time.InSameRange(year) && x.ProcessorId == id)
                                            {
                                                var sfTimes = shift == 0 ? new Tuple<DateTime, DateTime>(t, t.AddYears(1))
                                                    : DateTimeExtend.GetDayWorkTimeRange(shift, workshop.StatisticTimeList, x.Time);
                                                return x.Time.InSameRange(sfTimes);
                                            }
                                            return false;
                                        });
                                        AddProcessor(year.Item1, ref res, tData, id, name);
                                    }
                                }
                            }
                        }
                        #endregion
                        break;
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
            return res;
        }


        /// <summary>
        /// 获取加工数据  例：班制shift为0, 首班时间为8:00:00，统计延时30分钟时, 5月28日的数据为5月28日(含)至5月29日8:30:00(不含)
        /// <param name="shift">班制 0 全天 1  2  ...  </param>
        /// <param name="isSum">是否分组</param>
        /// <param name="isCompare">是否比较</param>
        /// <param name="compareTimeRange">前多少时间 取[0], 指定时间 取数组, 时间范围 取数组</param>
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<StatisticProcessAll> StatisticProcesses(KanBanItemEnum itemType, DateTime time,
            Workshop workshop, int shift, StatisticProcessTimeEnum timeType, int range, bool isSum,
            bool isCompare, StatisticProcessTimeEnum compareTimeType, StatisticProcessTimeRangeEnum compareTimeRangeType, List<int> compareTimeRange,
            List<int> steps, List<int> deviceIds, List<int> productionIds, List<int> processorIds)
        {
            var res = new List<StatisticProcessAll>();
            try
            {
                var workshopId = workshop.Id;
                //DateTime startTime, DateTime endTime
                var stepList = (steps != null && steps.Any())
                    ? DeviceProcessStepHelper.GetMenu(workshopId, steps).ToDictionary(x => x.Id, x => x.StepName)
                    : new Dictionary<int, string>();
                var productionList = (productionIds != null && productionIds.Any())
                    ? ProductionHelper.GetMenu(workshopId, productionIds).ToDictionary(x => x.Id, x => x.ProductionProcessName)
                    : new Dictionary<int, string>();
                var deviceList = (deviceIds != null && deviceIds.Any())
                    ? DeviceLibraryHelper.GetMenu(workshopId, deviceIds).ToDictionary(x => x.Id, x => x.Code)
                    : new Dictionary<int, string>();
                var processorList = (processorIds != null && processorIds.Any())
                    ? AccountInfoHelper.GetMenu(processorIds).ToDictionary(x => x.Id, x => x.Name)
                    : new Dictionary<int, string>();
                if (!stepList.Any())
                {
                    return res;
                }

                var shiftTime = shift == 0 ? DateTimeExtend.GetDayWorkDay(workshop.StatisticTimeList, time)
                    : DateTimeExtend.GetDayWorkTimeRange(shift, workshop.StatisticTimeList, time);
                var dayTime = DateTimeExtend.GetDayWorkDay(workshop.StatisticTimeList, time);
                var days = new List<DateTime>();
                var nums = new List<int>();
                var times = new List<DateTime>();
                IEnumerable<StatisticProcessAll> data = null;
                var startTime = default(DateTime);
                var endTime = default(DateTime);
                Tuple<DateTime, DateTime> st = null;
                var th = 0;
                var pre = 0;
                var dTimeType = timeType == StatisticProcessTimeEnum.小时 ?
                    StatisticProcessTimeEnum.小时 : shift == 0 ? StatisticProcessTimeEnum.日 : StatisticProcessTimeEnum.小时;
                DateTime ws;
                DateTime we;
                switch (timeType)
                {
                    case StatisticProcessTimeEnum.小时:
                        days.Add(dayTime.Item1.Date);
                        break;
                    case StatisticProcessTimeEnum.日:
                        for (var i = range - 1; i >= 0; i--)
                        {
                            var t = dayTime.Item1.Date.AddDays(-i);
                            days.Add(t);
                        }
                        break;
                    case StatisticProcessTimeEnum.周:
                        var week = DateTimeExtend.GetWorkWeek(0, time, workshop.StatisticTimeList);
                        ws = week.Item1.Date.AddWeeks(1 - range);
                        we = week.Item1.Date.AddWeeks(1);
                        while (ws < we)
                        {
                            days.Add(ws);
                            ws = ws.AddDays(1);
                        }
                        break;
                    case StatisticProcessTimeEnum.月:
                        var month = DateTimeExtend.GetWorkMonth(0, time, workshop.StatisticTimeList);
                        ws = month.Item1.Date.AddMonths(1 - range);
                        we = month.Item1.Date.AddMonths(1);
                        while (ws < we)
                        {
                            days.Add(ws);
                            ws = ws.AddDays(1);
                        }
                        break;
                    case StatisticProcessTimeEnum.年:
                        var year = DateTimeExtend.GetWorkYear(0, time, workshop.StatisticTimeList);
                        ws = year.Item1.Date.AddYears(1 - range);
                        we = year.Item1.Date.AddYears(1);
                        while (ws < we)
                        {
                            days.Add(ws);
                            ws = ws.AddDays(1);
                        }
                        break;
                }

                if (isCompare)
                {
                    dTimeType = compareTimeType == StatisticProcessTimeEnum.小时 ?
                        StatisticProcessTimeEnum.小时 : shift == 0 ? StatisticProcessTimeEnum.日 : StatisticProcessTimeEnum.小时;

                    switch (compareTimeType)
                    {
                        case StatisticProcessTimeEnum.小时:
                            endTime = (time < shiftTime.Item2 ? time : shiftTime.Item2).NoMinute();
                            th = (int)(endTime - shiftTime.Item1.NoMinute()).TotalHours;
                            range = Math.Min(th, range);
                            pre = compareTimeRange.Count > 0 ? compareTimeRange[0] : 0;
                            foreach (var day in days)
                            {
                                if (compareTimeRangeType == StatisticProcessTimeRangeEnum.前多少时间)
                                {
                                    for (var i = pre - 1; i >= 0; i--)
                                    {
                                        var t = day.AddHours(endTime.Hour - i);
                                        times.Add(t);
                                        if (!nums.Contains(t.Hour))
                                        {
                                            nums.Add(t.Hour);
                                        }
                                    }
                                }
                                else if (compareTimeRangeType == StatisticProcessTimeRangeEnum.指定时间)
                                {
                                    foreach (var tr in compareTimeRange)
                                    {
                                        var t = day.AddHours(tr);
                                        times.Add(t);
                                        if (!nums.Contains(tr))
                                        {
                                            nums.Add(tr);
                                        }
                                    }
                                }
                                else if (compareTimeRangeType == StatisticProcessTimeRangeEnum.时间范围)
                                {
                                    var s = compareTimeRange.Count > 0 ? compareTimeRange[0] : 0;
                                    var e = compareTimeRange.Count > 1 ? compareTimeRange[1] : 0;
                                    for (var i = s; i <= e; i++)
                                    {
                                        var t = day.AddHours(i);
                                        times.Add(t);
                                        if (!nums.Contains(i))
                                        {
                                            nums.Add(i);
                                        }
                                    }
                                }
                            }
                            break;
                        case StatisticProcessTimeEnum.日:
                            th = (int)(endTime - shiftTime.Item1.NoMinute()).TotalHours;
                            endTime = (time < shiftTime.Item2 ? time : shiftTime.Item2).NoMinute();
                            range = Math.Min(th, range);
                            pre = compareTimeRange.Count > 0 ? compareTimeRange[0] : 0;
                            var allDay = (new DateTime(2020, 1, 1)).GetAllDayOfYear().ToDictionary(x => x.DayOfYear);
                            foreach (var day in days)
                            {
                                var valid = false;
                                if (compareTimeRangeType == StatisticProcessTimeRangeEnum.前多少时间)
                                {
                                    //前几天
                                    switch (timeType)
                                    {
                                        case StatisticProcessTimeEnum.周:
                                            st = DateTimeExtend.GetWorkWeek(0, day, workshop.StatisticTimeList);
                                            break;
                                        case StatisticProcessTimeEnum.月:
                                            st = DateTimeExtend.GetWorkMonth(0, day, workshop.StatisticTimeList);
                                            break;
                                        case StatisticProcessTimeEnum.年:
                                            st = DateTimeExtend.GetWorkYear(0, day, workshop.StatisticTimeList);
                                            break;
                                    }
                                    if (st != null)
                                    {
                                        valid = (day - st.Item1.Date).TotalDays < pre;
                                    }
                                }
                                else if (compareTimeRangeType == StatisticProcessTimeRangeEnum.指定时间)
                                {
                                    switch (timeType)
                                    {
                                        //周一 ~ 周日， 0开始
                                        case StatisticProcessTimeEnum.周:
                                            valid = compareTimeRange.Contains(day.DayInWeek() - 1);
                                            break;
                                        //1~31日， 0开始
                                        case StatisticProcessTimeEnum.月:
                                            valid = compareTimeRange.Contains(day.Day - 1);
                                            break;
                                        //1月1日~12月31日， 0开始  默认2020年 366天
                                        case StatisticProcessTimeEnum.年:
                                            valid = compareTimeRange.Any(x => allDay.ContainsKey(x + 1) && allDay[x + 1].InSameMonthDay(day));
                                            break;
                                    }
                                }
                                else if (compareTimeRangeType == StatisticProcessTimeRangeEnum.时间范围)
                                {
                                    var s = compareTimeRange.Count > 0 ? compareTimeRange[0] : 0;
                                    var e = compareTimeRange.Count > 1 ? compareTimeRange[1] : 0;
                                    valid = day.Day >= s && day.Day <= e;
                                }

                                if (valid)
                                {
                                    if (shift == 0)
                                    {
                                        times.Add(day);
                                    }
                                    else
                                    {
                                        for (var i = range - 1; i >= 0; i--)
                                        {
                                            var t = day.AddHours(endTime.Hour - i);
                                            times.Add(t);
                                            if (!nums.Contains(t.Hour))
                                            {
                                                nums.Add(t.Hour);
                                            }
                                        }
                                    }
                                }
                            }
                            break;
                        case StatisticProcessTimeEnum.周:
                            th = (int)(endTime - shiftTime.Item1.NoMinute()).TotalHours;
                            endTime = (time < shiftTime.Item2 ? time : shiftTime.Item2).NoMinute();
                            range = Math.Min(th, range);
                            pre = compareTimeRange.Count > 0 ? compareTimeRange[0] : 0;
                            foreach (var day in days)
                            {
                                var valid = false;
                                if (compareTimeRangeType == StatisticProcessTimeRangeEnum.前多少时间)
                                {
                                    switch (timeType)
                                    {
                                        //月的前几周
                                        case StatisticProcessTimeEnum.月:
                                            st = DateTimeExtend.GetWorkMonth(0, day, workshop.StatisticTimeList);
                                            break;
                                        //年的前几周
                                        case StatisticProcessTimeEnum.年:
                                            st = DateTimeExtend.GetWorkYear(0, day, workshop.StatisticTimeList);
                                            break;
                                    }
                                    if (st != null)
                                    {
                                        valid = day.WeekInMonth() <= pre;
                                    }
                                }
                                else if (compareTimeRangeType == StatisticProcessTimeRangeEnum.指定时间)
                                {
                                    switch (timeType)
                                    {
                                        //月 1~5周；  从 0 开始
                                        case StatisticProcessTimeEnum.月:
                                            valid = compareTimeRange.Contains(day.WeekInMonth() - 1);
                                            break;
                                        //年 1~53周； 从 0 开始
                                        case StatisticProcessTimeEnum.年:
                                            valid = compareTimeRange.Contains(day.WeekInYear() - 1);
                                            break;
                                    }
                                }
                                else if (compareTimeRangeType == StatisticProcessTimeRangeEnum.时间范围)
                                {
                                    var s = compareTimeRange.Count > 0 ? compareTimeRange[0] : 0;
                                    var e = compareTimeRange.Count > 1 ? compareTimeRange[1] : 0;
                                    var w = day.DayInWeek();
                                    valid = w >= s && w <= e;
                                }

                                if (valid)
                                {
                                    if (shift == 0)
                                    {
                                        times.Add(day);
                                    }
                                    else
                                    {
                                        for (var i = range - 1; i >= 0; i--)
                                        {
                                            var t = day.AddHours(endTime.Hour - i);
                                            times.Add(t);
                                            if (!nums.Contains(t.Hour))
                                            {
                                                nums.Add(t.Hour);
                                            }
                                        }
                                    }
                                }
                            }
                            break;
                        case StatisticProcessTimeEnum.月:
                            th = (int)(endTime - shiftTime.Item1.NoMinute()).TotalHours;
                            endTime = (time < shiftTime.Item2 ? time : shiftTime.Item2).NoMinute();
                            range = Math.Min(th, range);
                            pre = compareTimeRange.Count > 0 ? compareTimeRange[0] : 0;
                            foreach (var day in days)
                            {
                                var valid = false;
                                if (compareTimeRangeType == StatisticProcessTimeRangeEnum.前多少时间)
                                {
                                    switch (timeType)
                                    {
                                        case StatisticProcessTimeEnum.年:
                                            st = DateTimeExtend.GetWorkYear(0, day, workshop.StatisticTimeList);
                                            break;
                                    }
                                    if (st != null)
                                    {
                                        valid = day.TotalMonths(st.Item1.Date) <= pre;
                                    }
                                }
                                else if (compareTimeRangeType == StatisticProcessTimeRangeEnum.指定时间)
                                {
                                    valid = compareTimeRange.Contains(day.Month - 1);
                                }
                                else if (compareTimeRangeType == StatisticProcessTimeRangeEnum.时间范围)
                                {
                                    var s = compareTimeRange.Count > 0 ? compareTimeRange[0] : 0;
                                    var e = compareTimeRange.Count > 1 ? compareTimeRange[1] : 0;
                                    var w = day.Month;
                                    valid = w >= s && w <= e;
                                }
                                if (valid)
                                {
                                    if (shift == 0)
                                    {
                                        times.Add(day);
                                    }
                                    else
                                    {
                                        for (var i = range - 1; i >= 0; i--)
                                        {
                                            var t = day.AddHours(endTime.Hour - i);
                                            times.Add(t);
                                        }
                                    }
                                }
                            }
                            break;
                    }
                    data = GetDetails(workshopId, dTimeType, times, steps, deviceIds, productionIds, processorIds);
                }
                else
                {
                    switch (dTimeType)
                    {
                        case StatisticProcessTimeEnum.小时:
                            endTime = (time < shiftTime.Item2 ? time : shiftTime.Item2).NoMinute();
                            th = (int)(endTime - shiftTime.Item1.NoMinute()).Hours;
                            startTime = th < range ? shiftTime.Item1.NoMinute() : time.NoMinute().AddHours(-range);
                            range = th < range ? th : range;
                            break;
                        case StatisticProcessTimeEnum.日:
                            if (shift == 0)
                            {
                                endTime = shiftTime.Item2.Date;
                                startTime = shiftTime.Item1.AddDays(1 - range).Date;
                            }
                            else
                            {
                                endTime = shiftTime.Item2.NoMinute();
                                startTime = shiftTime.Item1.AddDays(-range).NoMinute();
                            }
                            break;
                        case StatisticProcessTimeEnum.周:
                            var week = DateTimeExtend.GetWorkWeek(0, time, workshop.StatisticTimeList);
                            if (shift == 0)
                            {
                                endTime = week.Item2.Date;
                                startTime = week.Item1.AddWeeks(-range).Date;
                            }
                            else
                            {
                                endTime = week.Item2.NoMinute();
                                startTime = week.Item1.AddWeeks(-range).NoMinute();
                            }
                            break;
                        case StatisticProcessTimeEnum.月:
                            var month = DateTimeExtend.GetWorkMonth(0, time, workshop.StatisticTimeList);
                            if (shift == 0)
                            {
                                endTime = month.Item2.Date;
                                startTime = month.Item1.AddMonths(-range).Date;
                            }
                            else
                            {
                                endTime = month.Item2.NoMinute();
                                startTime = month.Item1.AddMonths(-range).NoMinute();
                            }
                            break;
                        case StatisticProcessTimeEnum.年:
                            var year = DateTimeExtend.GetWorkYear(0, time, workshop.StatisticTimeList);
                            if (shift == 0)
                            {
                                endTime = year.Item2.Date;
                                startTime = year.Item1.AddYears(-range).Date;
                            }
                            else
                            {
                                endTime = year.Item2.NoMinute();
                                startTime = year.Item1.AddYears(-range).NoMinute();
                            }
                            break;
                    }
                    data = GetDetails(workshopId, dTimeType, startTime.NoMinute(), endTime.NextHour(0, 1), steps, deviceIds, productionIds, processorIds);
                }

                if (isCompare)
                {
                    IEnumerable<IGrouping<dynamic, DateTime>> groups = null;
                    Dictionary<dynamic, IEnumerable<DateTime>> tg = null;
                    switch (compareTimeType)
                    {
                        case StatisticProcessTimeEnum.小时:
                            #region 小时
                            switch (timeType)
                            {
                                case StatisticProcessTimeEnum.日:
                                    groups = times.GroupBy(x => new { T1 = x.Date, T2 = x.Hour });
                                    break;
                                case StatisticProcessTimeEnum.周:
                                    groups = times.GroupBy(x => new { T1 = x.WeekBeginTime(), T2 = x.Hour });
                                    break;
                                case StatisticProcessTimeEnum.月:
                                    groups = times.GroupBy(x => new { T1 = x.StartOfMonth(), T2 = x.Hour });
                                    break;
                                case StatisticProcessTimeEnum.年:
                                    groups = times.GroupBy(x => new { T1 = x.StartOfYear(), T2 = x.Hour });
                                    break;
                            }
                            #endregion
                            if (groups != null)
                            {
                                tg = groups.ToDictionary(x => ((DateTime)x.Key.T1).AddHours(x.Key.T2), x => x.Select(y => y));
                            }
                            break;
                        case StatisticProcessTimeEnum.日:
                            #region 日
                            switch (timeType)
                            {
                                case StatisticProcessTimeEnum.周:
                                    groups = times.GroupBy(x => new { T1 = x.WeekBeginTime(), T2 = x.DayInWeek() - 1 });
                                    break;
                                case StatisticProcessTimeEnum.月:
                                    groups = times.GroupBy(x => new { T1 = x.StartOfMonth(), T2 = x.Day - 1 });
                                    break;
                                case StatisticProcessTimeEnum.年:
                                    groups = times.GroupBy(x => new { T1 = x.StartOfYear(), T2 = x.Day - 1 });
                                    break;
                            }
                            if (groups != null)
                            {
                                tg = groups.ToDictionary(x => ((DateTime)x.Key.T1).AddDays(x.Key.T2), x => x.Select(y => y));
                            }
                            #endregion
                            break;
                        case StatisticProcessTimeEnum.周:
                            #region 周
                            switch (timeType)
                            {
                                case StatisticProcessTimeEnum.月:
                                    groups = times.GroupBy(x => new { T1 = x.StartOfMonth(), T2 = x.WeekInMonth() - 1 });
                                    break;
                                case StatisticProcessTimeEnum.年:
                                    groups = times.GroupBy(x => new { T1 = x.StartOfYear(), T2 = x.WeekInYear() - 1 });
                                    break;
                            }
                            if (groups != null)
                            {
                                tg = groups.ToDictionary(x => DateTimeExtend.AddWeeks(x.Key.T1, x.Key.T2), x => x.Select(y => y));
                            }
                            #endregion
                            break;
                        case StatisticProcessTimeEnum.月:
                            #region 月
                            switch (timeType)
                            {
                                case StatisticProcessTimeEnum.年:
                                    groups = times.GroupBy(x => new { T1 = x.StartOfYear(), T2 = x.Month - 1 });
                                    break;
                            }
                            if (groups != null)
                            {
                                tg = groups.ToDictionary(x => ((DateTime)x.Key.T1).AddMonths(x.Key.T2), x => x.Select(y => y));
                            }
                            #endregion
                            break;
                    }

                    if (tg != null)
                    {
                        foreach (var group in tg)
                        {
                            var t = group.Key;
                            #region MyRegion
                            if (isSum)
                            {
                                var tData = data.Where(x => group.Value.Contains(x.Time));
                                AddStatisticProcess(t, ref res, tData);
                            }
                            else
                            {
                                if (itemType == KanBanItemEnum.计划号工序推移图)
                                {
                                    foreach (var (id, name) in productionList)
                                    {
                                        var tData = data.Where(x => group.Value.Contains(x.Time) && x.ProductionId == id);
                                        AddProduction(t, ref res, tData, id, name);
                                    }
                                }
                                else if (itemType == KanBanItemEnum.设备工序推移图)
                                {
                                    foreach (var (id, name) in deviceList)
                                    {
                                        var tData = data.Where(x => group.Value.Contains(x.Time) && x.DeviceId == id);
                                        AddDevice(t, ref res, tData, id, name);
                                    }
                                }
                                else if (itemType == KanBanItemEnum.操作工工序推移图)
                                {
                                    foreach (var (id, name) in processorList)
                                    {
                                        var tData = data.Where(x => group.Value.Contains(x.Time) && x.ProcessorId == id);
                                        AddProcessor(t, ref res, tData, id, name);
                                    }
                                }
                            }
                            #endregion
                        }
                    }
                }
                else
                {

                    switch (timeType)
                    {
                        case StatisticProcessTimeEnum.小时:
                            #region 小时
                            for (var i = range; i > 0; i--)
                            {
                                var t = endTime.AddHours(-i).NoMinute();
                                if (t > time)
                                {
                                    continue;
                                }

                                if (isSum)
                                {
                                    var tData = data.Where(x => x.Time == t);
                                    AddStatisticProcess(t, ref res, tData);
                                }
                                else
                                {
                                    if (itemType == KanBanItemEnum.计划号工序推移图)
                                    {
                                        foreach (var (id, name) in productionList)
                                        {
                                            var tData = data.Where(x => x.Time == t && x.ProductionId == id);
                                            AddProduction(t, ref res, tData, id, name);
                                        }
                                    }
                                    else if (itemType == KanBanItemEnum.设备工序推移图)
                                    {
                                        foreach (var (id, name) in deviceList)
                                        {
                                            var tData = data.Where(x => x.Time == t && x.DeviceId == id);
                                            AddDevice(t, ref res, tData, id, name);
                                        }
                                    }
                                    else if (itemType == KanBanItemEnum.操作工工序推移图)
                                    {
                                        foreach (var (id, name) in processorList)
                                        {
                                            var tData = data.Where(x => x.Time == t && x.ProcessorId == id);
                                            AddProcessor(t, ref res, tData, id, name);
                                        }
                                    }
                                }
                            }
                            #endregion
                            break;
                        case StatisticProcessTimeEnum.日:
                            #region 日
                            for (var i = range; i > 0; i--)
                            {
                                var t = endTime.AddDays(-i);
                                Tuple<DateTime, DateTime> sfTimes;
                                if (shift == 0)
                                {
                                    sfTimes = new Tuple<DateTime, DateTime>(t, t.AddDays(1));
                                }
                                else
                                {
                                    sfTimes = DateTimeExtend.GetDayWorkTimeRange(shift, workshop.StatisticTimeList, t);
                                }

                                if (isSum)
                                {
                                    var tData = data.Where(x => x.Time.InSameRange(sfTimes));
                                    AddStatisticProcess(sfTimes.Item1, ref res, tData);
                                }
                                else
                                {
                                    if (itemType == KanBanItemEnum.计划号工序推移图)
                                    {
                                        foreach (var (id, name) in productionList)
                                        {
                                            var tData = data.Where(x => x.Time.InSameRange(sfTimes) && x.ProductionId == id);
                                            AddProduction(sfTimes.Item1, ref res, tData, id, name);
                                        }
                                    }
                                    else if (itemType == KanBanItemEnum.设备工序推移图)
                                    {
                                        foreach (var (id, name) in deviceList)
                                        {
                                            var tData = data.Where(x => x.Time.InSameRange(sfTimes) && x.DeviceId == id);
                                            AddDevice(sfTimes.Item1, ref res, tData, id, name);
                                        }
                                    }
                                    else if (itemType == KanBanItemEnum.操作工工序推移图)
                                    {
                                        foreach (var (id, name) in processorList)
                                        {
                                            var tData = data.Where(x => x.Time.InSameRange(sfTimes) && x.ProcessorId == id);
                                            AddProcessor(sfTimes.Item1, ref res, tData, id, name);
                                        }
                                    }
                                }
                            }
                            #endregion
                            break;
                        case StatisticProcessTimeEnum.周:
                            #region 周
                            for (var i = range; i > 0; i--)
                            {
                                var t = endTime.AddWeeks(-i);
                                var week = DateTimeExtend.GetWeek(0, t);
                                if (isSum)
                                {
                                    var tData = data.Where(x =>
                                    {
                                        if (x.Time.InSameRange(week))
                                        {
                                            var sfTimes = shift == 0 ? new Tuple<DateTime, DateTime>(t, t.AddWeeks(1))
                                                : DateTimeExtend.GetDayWorkTimeRange(shift, workshop.StatisticTimeList, x.Time);
                                            return x.Time.InSameRange(sfTimes);
                                        }
                                        return false;
                                    });
                                    AddStatisticProcess(week.Item1, ref res, tData);
                                }
                                else
                                {
                                    if (itemType == KanBanItemEnum.计划号工序推移图)
                                    {
                                        foreach (var (id, name) in productionList)
                                        {
                                            var tData = data.Where(x =>
                                            {
                                                if (x.Time.InSameRange(week) && x.ProductionId == id)
                                                {
                                                    var sfTimes = shift == 0 ? new Tuple<DateTime, DateTime>(t, t.AddWeeks(1))
                                                        : DateTimeExtend.GetDayWorkTimeRange(shift, workshop.StatisticTimeList, x.Time);
                                                    return x.Time.InSameRange(sfTimes);
                                                }
                                                return false;
                                            });
                                            AddProduction(week.Item1, ref res, tData, id, name);
                                        }
                                    }
                                    else if (itemType == KanBanItemEnum.设备工序推移图)
                                    {
                                        foreach (var (id, name) in deviceList)
                                        {
                                            var tData = data.Where(x =>
                                            {
                                                if (x.Time.InSameRange(week) && x.DeviceId == id)
                                                {
                                                    var sfTimes = shift == 0 ? new Tuple<DateTime, DateTime>(t, t.AddWeeks(1))
                                                        : DateTimeExtend.GetDayWorkTimeRange(shift, workshop.StatisticTimeList, x.Time);
                                                    return x.Time.InSameRange(sfTimes);
                                                }
                                                return false;
                                            });
                                            AddDevice(week.Item1, ref res, tData, id, name);
                                        }
                                    }
                                    else if (itemType == KanBanItemEnum.操作工工序推移图)
                                    {
                                        foreach (var (id, name) in processorList)
                                        {
                                            var tData = data.Where(x =>
                                            {
                                                if (x.Time.InSameRange(week) && x.ProcessorId == id)
                                                {
                                                    var sfTimes = shift == 0 ? new Tuple<DateTime, DateTime>(t, t.AddWeeks(1))
                                                        : DateTimeExtend.GetDayWorkTimeRange(shift, workshop.StatisticTimeList, x.Time);
                                                    return x.Time.InSameRange(sfTimes);
                                                }
                                                return false;
                                            });
                                            AddProcessor(week.Item1, ref res, tData, id, name);
                                        }
                                    }
                                }
                            }
                            #endregion
                            break;
                        case StatisticProcessTimeEnum.月:
                            #region 月
                            for (var i = range; i > 0; i--)
                            {
                                var t = endTime.AddMonths(-i);
                                var month = DateTimeExtend.GetMonth(0, t);
                                if (isSum)
                                {
                                    var tData = data.Where(x =>
                                    {
                                        if (x.Time.InSameRange(month))
                                        {
                                            var sfTimes = shift == 0 ? new Tuple<DateTime, DateTime>(t, t.AddMonths(1))
                                                : DateTimeExtend.GetDayWorkTimeRange(shift, workshop.StatisticTimeList, x.Time);
                                            return x.Time.InSameRange(sfTimes);
                                        }
                                        return false;
                                    });
                                    AddStatisticProcess(month.Item1, ref res, tData);
                                }
                                else
                                {
                                    if (itemType == KanBanItemEnum.计划号工序推移图)
                                    {
                                        foreach (var (id, name) in productionList)
                                        {
                                            var tData = data.Where(x =>
                                            {
                                                if (x.Time.InSameRange(month) && x.ProductionId == id)
                                                {
                                                    var sfTimes = shift == 0 ? new Tuple<DateTime, DateTime>(t, t.AddMonths(1))
                                                        : DateTimeExtend.GetDayWorkTimeRange(shift, workshop.StatisticTimeList, x.Time);
                                                    return x.Time.InSameRange(sfTimes);
                                                }
                                                return false;
                                            });
                                            AddProduction(month.Item1, ref res, tData, id, name);
                                        }
                                    }
                                    else if (itemType == KanBanItemEnum.设备工序推移图)
                                    {
                                        foreach (var (id, name) in deviceList)
                                        {
                                            var tData = data.Where(x =>
                                            {
                                                if (x.Time.InSameRange(month) && x.DeviceId == id)
                                                {
                                                    var sfTimes = shift == 0 ? new Tuple<DateTime, DateTime>(t, t.AddMonths(1))
                                                        : DateTimeExtend.GetDayWorkTimeRange(shift, workshop.StatisticTimeList, x.Time);
                                                    return x.Time.InSameRange(sfTimes);
                                                }
                                                return false;
                                            });
                                            AddDevice(month.Item1, ref res, tData, id, name);
                                        }
                                    }
                                    else if (itemType == KanBanItemEnum.操作工工序推移图)
                                    {
                                        foreach (var (id, name) in processorList)
                                        {
                                            var tData = data.Where(x =>
                                            {
                                                if (x.Time.InSameRange(month) && x.ProcessorId == id)
                                                {
                                                    var sfTimes = shift == 0 ? new Tuple<DateTime, DateTime>(t, t.AddMonths(1))
                                                        : DateTimeExtend.GetDayWorkTimeRange(shift, workshop.StatisticTimeList, x.Time);
                                                    return x.Time.InSameRange(sfTimes);
                                                }
                                                return false;
                                            });
                                            AddProcessor(month.Item1, ref res, tData, id, name);
                                        }
                                    }
                                }
                            }
                            #endregion
                            break;
                        case StatisticProcessTimeEnum.年:
                            #region 年
                            for (var i = range; i > 0; i--)
                            {
                                var t = endTime.AddYears(-i);
                                var year = DateTimeExtend.GetYear(0, t);
                                if (isSum)
                                {
                                    var tData = data.Where(x =>
                                    {
                                        if (x.Time.InSameRange(year))
                                        {
                                            var sfTimes = shift == 0 ? new Tuple<DateTime, DateTime>(t, t.AddYears(1))
                                                : DateTimeExtend.GetDayWorkTimeRange(shift, workshop.StatisticTimeList, x.Time);
                                            return x.Time.InSameRange(sfTimes);
                                        }
                                        return false;
                                    });
                                    AddStatisticProcess(year.Item1, ref res, tData);
                                }
                                else
                                {
                                    if (itemType == KanBanItemEnum.计划号工序推移图)
                                    {
                                        foreach (var (id, name) in productionList)
                                        {
                                            var tData = data.Where(x =>
                                            {
                                                if (x.Time.InSameRange(year) && x.ProductionId == id)
                                                {
                                                    var sfTimes = shift == 0 ? new Tuple<DateTime, DateTime>(t, t.AddYears(1))
                                                        : DateTimeExtend.GetDayWorkTimeRange(shift, workshop.StatisticTimeList, x.Time);
                                                    return x.Time.InSameRange(sfTimes);
                                                }
                                                return false;
                                            });
                                            AddProduction(year.Item1, ref res, tData, id, name);
                                        }
                                    }
                                    else if (itemType == KanBanItemEnum.设备工序推移图)
                                    {
                                        foreach (var (id, name) in deviceList)
                                        {
                                            var tData = data.Where(x =>
                                            {
                                                if (x.Time.InSameRange(year) && x.DeviceId == id)
                                                {
                                                    var sfTimes = shift == 0 ? new Tuple<DateTime, DateTime>(t, t.AddYears(1))
                                                        : DateTimeExtend.GetDayWorkTimeRange(shift, workshop.StatisticTimeList, x.Time);
                                                    return x.Time.InSameRange(sfTimes);
                                                }
                                                return false;
                                            });
                                            AddDevice(year.Item1, ref res, tData, id, name);
                                        }
                                    }
                                    else if (itemType == KanBanItemEnum.操作工工序推移图)
                                    {
                                        foreach (var (id, name) in processorList)
                                        {
                                            var tData = data.Where(x =>
                                            {
                                                if (x.Time.InSameRange(year) && x.ProcessorId == id)
                                                {
                                                    var sfTimes = shift == 0 ? new Tuple<DateTime, DateTime>(t, t.AddYears(1))
                                                        : DateTimeExtend.GetDayWorkTimeRange(shift, workshop.StatisticTimeList, x.Time);
                                                    return x.Time.InSameRange(sfTimes);
                                                }
                                                return false;
                                            });
                                            AddProcessor(year.Item1, ref res, tData, id, name);
                                        }
                                    }
                                }
                            }
                            #endregion
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
            return res;
        }

        private static void AddProduction(DateTime t, ref List<StatisticProcessAll> res, IEnumerable<StatisticProcessAll> tData,
            int id, string name)
        {
            AddStatisticProcess(t, ref res, tData, 0, id, name);
        }


        private static void AddDevice(DateTime t, ref List<StatisticProcessAll> res, IEnumerable<StatisticProcessAll> tData,
            int id, string name)
        {
            AddStatisticProcess(t, ref res, tData, 1, 0, "", id, name);
        }

        private static void AddProcessor(DateTime t, ref List<StatisticProcessAll> res, IEnumerable<StatisticProcessAll> tData,
            int id, string name)
        {
            AddStatisticProcess(t, ref res, tData, 2, 0, "", 0, "", id, name);
        }

        private static void AddStatisticProcess(DateTime t, ref List<StatisticProcessAll> res, IEnumerable<StatisticProcessAll> tData,
            int dataType = -1, int productionId = 0, string production = "", int deviceId = 0, string code = "", int processorId = 0, string processor = "")
        {
            var sp = new StatisticProcessAll
            {
                Time = t,
                ProductionId = productionId,
                Production = production,
                DeviceId = deviceId,
                Code = code,
                ProcessorId = processorId,
                Processor = processor,
            };
            if (dataType == 0)
            {
                sp.DataId = productionId;
                sp.Data = production;
            }
            else if (dataType == 1)
            {
                sp.DataId = deviceId;
                sp.Data = code;
            }
            else if (dataType == 2)
            {
                sp.DataId = processorId;
                sp.Data = processor;
            }
            if (tData != null && tData.Any())
            {
                sp.Total = tData.Sum(x => x.Total);
                sp.Qualified = tData.Sum(x => x.Qualified);
                sp.Unqualified = tData.Sum(x => x.Unqualified);
            }
            res.Add(sp);
        }
        ///// <summary>
        ///// 获取计划号数据
        ///// </summary>
        ///// <returns></returns>
        //public static IEnumerable<StatisticProcessProduction> GetProductions(StatisticProcessTimeEnum timeType, DateTime startTime, DateTime endTime)
        //{
        //    return Get<StatisticProcessProduction>(StatisticProcessTypeEnum.计划号, timeType, startTime, endTime);
        //}
        ///// <summary>
        ///// 获取操作工数据
        ///// </summary>
        ///// <returns></returns>
        //public static IEnumerable<StatisticProcessProcessor> GetProcessors(StatisticProcessTimeEnum timeType, DateTime startTime, DateTime endTime)
        //{
        //    return Get<StatisticProcessProcessor>(StatisticProcessTypeEnum.操作工, timeType, startTime, endTime);
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <returns></returns>
        //private static IEnumerable<T> Get<T>(StatisticProcessTypeEnum type, StatisticProcessTimeEnum timeType, DateTime startTime, DateTime endTime, List<Tuple<int, string>> paramList)
        //{
        //    var res = new List<T>();
        //    if (Configs.ContainsKey(type))
        //    {
        //        var config = Configs[type];
        //        switch (timeType)
        //        {
        //            case StatisticProcessTimeEnum.小时:
        //                var data = ServerConfig.ApiDb.Query<T>(
        //                    $"SELECT * FROM `{config[0]}` WHERE Time >= @startTime AND Time < @endTime;",
        //                    new
        //                    {
        //                        startTime,
        //                        endTime,
        //                    }, 1000);
        //                var hours = (endTime - startTime).TotalHours;
        //                for (var i = 0; i < hours; i++)
        //                {
        //                    var t = startTime.AddHours(i);
        //                    switch (type)
        //                    {
        //                        case StatisticProcessTypeEnum.设备:
        //                            foreach (var p in paramList)
        //                            {
        //                                var tData = data.FirstOrDefault(x => (x as StatisticProcessDevice).DeviceId == p.Item1);
        //                                if (tData == null)
        //                                {
        //                                    var nData = new StatisticProcessDevice
        //                                    {
        //                                        DeviceId = p.Item1,
        //                                        Code = p.Item2
        //                                    };
        //                                    res.Add(nData);
        //                                }
        //                                else
        //                                {
        //                                    res.Add(tData);
        //                                }

        //                            }

        //                    }
        //                }
        //                break;
        //            case StatisticProcessTimeEnum.日: break;
        //        }
        //    }
        //    return res;
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<StatisticProcessAll> GetMax(Tuple<DateTime, DateTime> workDays)
        {
            var res = new List<StatisticProcessAll>();
            //res.AddRange(ServerConfig.ApiDb.Query<T>($"SELECT *, MAX(Time) Time FROM `{config[0]}` WHERE Time >= @st AND Time < @ed GROUP BY WorkshopId, Type, Step, {config[1]};",
            //res.AddRange(ServerConfig.ApiDb.Query<T>($"SELECT * FROM `{config[0]}` WHERE Time >= @st AND Time < @ed GROUP BY WorkshopId, Type, Step, {config[1]};",
            res.AddRange(ServerConfig.ApiDb.Query<StatisticProcessAll>(
                $"SELECT * FROM `statistic_process` WHERE (Type = @t AND Time = @st1) OR (Type != @t AND Time >= @st2 AND Time < @ed);",
                new
                {
                    t = StatisticProcessTimeEnum.日,
                    st1 = workDays.Item1.DayBeginTime(),
                    st2 = workDays.Item1.NoMinute(),
                    ed = workDays.Item2.NoMinute().AddHours(1),
                }, 1000));
            return res;
        }
        #endregion

        #region Add
        public static void Add(IEnumerable<StatisticProcessAll> data)
        {
            ServerConfig.ApiDb.Execute($"INSERT INTO `statistic_process` (`MarkedDateTime`, `WorkshopId`, `Type`, `Time`, `Step`, `StepName`, `StepAbbrev`, `DeviceId`, `Code`, `ProcessorId`, `Processor`, `ProductionId`, `Production`, `Total`, `Qualified`, `Unqualified`, `QualifiedRate`, `UnqualifiedRate`) " +
                                       $"VALUES (@MarkedDateTime, @WorkshopId, @Type, @Time, @Step, @StepName, @StepAbbrev, @DeviceId, @Code, @ProcessorId, @Processor, @ProductionId, @Production, @Total, @Qualified, @Unqualified, @QualifiedRate, @UnqualifiedRate);", data);
        }
        #endregion

        #region Update
        public static void Update(IEnumerable<StatisticProcessAll> data)
        {
            ServerConfig.ApiDb.Execute($"UPDATE `statistic_process` SET `MarkedDateTime` = @MarkedDateTime, `Total` = @Total, `Qualified` = @Qualified, `Unqualified` = @Unqualified, `QualifiedRate` = @QualifiedRate, `UnqualifiedRate` = @UnqualifiedRate " +
                                       $"WHERE `WorkshopId` = @WorkshopId AND `Type` = @Type AND `Time` = @Time AND `Step` = @Step AND `DeviceId` = @DeviceId AND `ProcessorId` = @ProcessorId AND `ProductionId` = @ProductionId;", data);
        }
        #endregion

        #region AddOrUpdate
        public static void AddOrUpdate(IEnumerable<StatisticProcessAll> data)
        {
            ServerConfig.ApiDb.Execute($"INSERT INTO `statistic_process` (`MarkedDateTime`, `WorkshopId`, `Type`, `Time`, `Step`, `StepName`, `StepAbbrev`, `DeviceId`, `Code`, `ProcessorId`, `Processor`, `ProductionId`, `Production`, `Total`, `Qualified`, `Unqualified`, `QualifiedRate`, `UnqualifiedRate`) " +
                                       $"VALUES (@MarkedDateTime, @WorkshopId, @Type, @Time, @Step, @StepName, @StepAbbrev, @DeviceId, @Code, @ProcessorId, @Processor, @ProductionId, @Production, @Total, @Qualified, @Unqualified, @QualifiedRate, @UnqualifiedRate) " +
                                       $"ON DUPLICATE KEY UPDATE `MarkedDateTime` = @MarkedDateTime, `Total` = @Total, `Qualified` = @Qualified, `Unqualified` = @Unqualified, `QualifiedRate` = @QualifiedRate, `UnqualifiedRate` = @UnqualifiedRate;", data);
        }
        #endregion

        #region Delete
        public static void Delete(IEnumerable<StatisticProcessAll> data)
        {
            ServerConfig.ApiDb.Execute("DELETE FROM `statistic_process` WHERE WorkshopId = @WorkshopId AND Type = @Type AND Time = @Time AND Step = @Step AND DeviceId = @DeviceId AND ProcessorId = @ProcessorId AND ProductionId = @ProductionId", data);
        }
        #endregion
    }
}