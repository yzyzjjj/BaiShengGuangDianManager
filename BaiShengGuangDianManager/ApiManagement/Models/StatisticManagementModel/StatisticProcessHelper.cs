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

                var shiftTimes = shift == 0 ? DateTimeExtend.GetDayWorkDayRange(workshop.StatisticTimeList, time)
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
                            startTime = week.Item1.AddWeek(-range).Date;
                        }
                        else
                        {
                            endTime = week.Item2.NoMinute();
                            startTime = week.Item1.AddWeek(-range).NoMinute();
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
                            var t = endTime.AddWeek(-i);
                            var week = DateTimeExtend.GetWeek(0, t);
                            if (isSum)
                            {
                                var tData = data.Where(x =>
                                {
                                    if (x.Time.InSameRange(week))
                                    {
                                        var sfTimes = shift == 0 ? new Tuple<DateTime, DateTime>(t, t.AddWeek(1))
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
                                                var sfTimes = shift == 0 ? new Tuple<DateTime, DateTime>(t, t.AddWeek(1))
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
                                                var sfTimes = shift == 0 ? new Tuple<DateTime, DateTime>(t, t.AddWeek(1))
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
                                                var sfTimes = shift == 0 ? new Tuple<DateTime, DateTime>(t, t.AddWeek(1))
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

        private static void AddProduction(DateTime t, ref List<StatisticProcessAll> res, IEnumerable<StatisticProcessAll> tData,
            int id, string name)
        {
            AddStatisticProcess(t, ref res, tData, id, name);
        }


        private static void AddDevice(DateTime t, ref List<StatisticProcessAll> res, IEnumerable<StatisticProcessAll> tData,
            int id, string name)
        {
            AddStatisticProcess(t, ref res, tData, 0, "", id, name);
        }

        private static void AddProcessor(DateTime t, ref List<StatisticProcessAll> res, IEnumerable<StatisticProcessAll> tData,
            int id, string name)
        {
            AddStatisticProcess(t, ref res, tData, 0, "", 0, "", id, name);
        }

        private static void AddStatisticProcess(DateTime t, ref List<StatisticProcessAll> res, IEnumerable<StatisticProcessAll> tData,
            int productionId = 0, string production = "", int deviceId = 0, string code = "", int processorId = 0, string processor = "")
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