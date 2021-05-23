using ApiManagement.Base.Server;
using ApiManagement.Models.AccountManagementModel;
using ApiManagement.Models.BaseModel;
using ApiManagement.Models.DeviceManagementModel;
using ApiManagement.Models.FlowCardManagementModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.StatisticManagementModel
{
    public class StatisticProcessHelper : DataHelper
    {
        private StatisticProcessHelper()
        {
            Table = "";
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

            return Instance.CommonGet<StatisticProcessAll>(args);
        }

        /// <summary>
        /// 获取设备数据
        /// <param name="shift">班制 0 前天</param>
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<StatisticProcessAll> StatisticProcesses(KanBanItemEnum itemType, Workshop workshop, int shift, StatisticProcessTimeEnum timeType
            , int range, List<int> steps, List<int> deviceIds, List<int> productionIds, List<int> processorIds)
        {
            var workshopId = workshop.Id;
            //DateTime startTime, DateTime endTime
            var stepList = steps != null && steps.Any()
                ? DeviceProcessStepHelper.GetDetails(workshopId, steps).ToDictionary(x => x.Id, x => x.StepName)
                : new Dictionary<int, string>();
            var productionList = productionIds != null && productionIds.Any()
                ? ProductionHelper.GetDetails(workshopId, productionIds).ToDictionary(x => x.Id, x => x.ProductionProcessName)
                : new Dictionary<int, string>();
            var deviceList = processorIds != null && deviceIds.Any()
                ? DeviceLibraryHelper.GetDetails(workshopId, deviceIds).ToDictionary(x => x.Id, x => x.Code)
                : new Dictionary<int, string>();
            var processorList = processorIds != null && processorIds.Any()
                ? AccountInfoHelper.GetAccountInfoByAccountIds(processorIds).ToDictionary(x => x.Id, x => x.Name)
                : new Dictionary<int, string>();

            var res = new List<StatisticProcessAll>();
            //switch (timeType)
            //{
            //    case StatisticProcessTimeEnum.小时:
            //        var data = ServerConfig.ApiDb.Query<StatisticProcessDevice>(
            //            $"SELECT * FROM `{config[0]}` WHERE Time >= @startTime AND Time < @endTime;",
            //            new
            //            {
            //                startTime,
            //                endTime,
            //            }, 1000);
            //        var hours = (endTime - startTime).TotalHours;
            //        for (var i = 0; i < hours; i++)
            //        {
            //            var tPre = startTime.AddHours(i - 1);
            //            var t = startTime.AddHours(i);
            //            foreach (var p in paramList)
            //            {
            //                var tPreData = data.FirstOrDefault(x => x.Time == tPre && x.DeviceId == p.Item1);
            //                var tData = data.FirstOrDefault(x => x.Time == t && x.DeviceId == p.Item1);
            //                if (tData == null)
            //                {
            //                    var nData = new StatisticProcessDevice
            //                    {
            //                        Time = p.Item1,
            //                        DeviceId = p.Item1,
            //                        Code = p.Item2
            //                    };
            //                    res.Add(nData);
            //                }
            //                else
            //                {
            //                    res.Add(tData);
            //                }

            //            }
            //        }
            //        break;
            //    case StatisticProcessTimeEnum.日: break;
            //}
            return res;
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
            res.AddRange(ServerConfig.ApiDb.Query<StatisticProcessAll>($"SELECT * FROM `statistic_process` WHERE Time >= @st AND Time < @ed;",
                new
                {
                    st = workDays.Item1,
                    ed = workDays.Item2,
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
        #endregion
    }
}