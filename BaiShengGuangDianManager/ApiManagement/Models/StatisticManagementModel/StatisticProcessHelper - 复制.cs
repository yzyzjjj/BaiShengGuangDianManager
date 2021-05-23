//using ApiManagement.Base.Server;
//using ApiManagement.Models.BaseModel;
//using ApiManagement.Models.DeviceManagementModel;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace ApiManagement.Models.StatisticManagementModel
//{
//    public class StatisticProcessHelper : DataHelper
//    {
//        private StatisticProcessHelper()
//        {
//            Table = "";
//            InsertSql = "";
//            UpdateSql = "";

//            SameField = "";
//            //MenuFields.AddRange(new[] { "Id", "Time", "OpName", "DeviceId", "StartTime", "EndTime" });
//        }
//        public static readonly StatisticProcessHelper Instance = new StatisticProcessHelper();
//        #region Get
//        /// <summary>
//        /// 获取设备数据
//        /// </summary>
//        /// <returns></returns>
//        public static IEnumerable<StatisticProcessDevice> GetDevices(int workshopId, StatisticProcessTimeEnum timeType, DateTime startTime, DateTime endTime, List<int> steps, List<int> deviceIds)
//        {
//            var stepList = DeviceProcessStepHelper.GetDetails(workshopId, steps).ToDictionary(x => x.Id, x => x.StepName);

//            var type = StatisticProcessTypeEnum.设备;
//            var res = new List<StatisticProcessDevice>();
//            if (Configs.ContainsKey(type))
//            {
//                var config = Configs[type];
//                switch (timeType)
//                {
//                    case StatisticProcessTimeEnum.小时:
//                        var data = ServerConfig.ApiDb.Query<StatisticProcessDevice>(
//                            $"SELECT * FROM `{config[0]}` WHERE Time >= @startTime AND Time < @endTime;",
//                            new
//                            {
//                                startTime,
//                                endTime,
//                            }, 1000);
//                        var hours = (endTime - startTime).TotalHours;
//                        for (var i = 0; i < hours; i++)
//                        {
//                            var tPre = startTime.AddHours(i - 1);
//                            var t = startTime.AddHours(i);
//                            foreach (var p in paramList)
//                            {
//                                var tPreData = data.FirstOrDefault(x => x.Time == tPre && x.DeviceId == p.Item1);
//                                var tData = data.FirstOrDefault(x => x.Time == t && x.DeviceId == p.Item1);
//                                if (tData == null)
//                                {
//                                    var nData = new StatisticProcessDevice
//                                    {
//                                        Time = p.Item1,
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
//                        }
//                        break;
//                    case StatisticProcessTimeEnum.日: break;
//                }
//            }
//            return res;
//        }
//        /// <summary>
//        /// 获取计划号数据
//        /// </summary>
//        /// <returns></returns>
//        public static IEnumerable<StatisticProcessProduction> GetProductions(StatisticProcessTimeEnum timeType, DateTime startTime, DateTime endTime)
//        {
//            return Get<StatisticProcessProduction>(StatisticProcessTypeEnum.计划号, timeType, startTime, endTime);
//        }
//        /// <summary>
//        /// 获取操作工数据
//        /// </summary>
//        /// <returns></returns>
//        public static IEnumerable<StatisticProcessProcessor> GetProcessors(StatisticProcessTimeEnum timeType, DateTime startTime, DateTime endTime)
//        {
//            return Get<StatisticProcessProcessor>(StatisticProcessTypeEnum.操作工, timeType, startTime, endTime);
//        }

//        /// <summary>
//        /// 
//        /// </summary>
//        /// <returns></returns>
//        private static IEnumerable<T> Get<T>(StatisticProcessTypeEnum type, StatisticProcessTimeEnum timeType, DateTime startTime, DateTime endTime, List<Tuple<int, string>> paramList)
//        {
//            var res = new List<T>();
//            if (Configs.ContainsKey(type))
//            {
//                var config = Configs[type];
//                switch (timeType)
//                {
//                    case StatisticProcessTimeEnum.小时:
//                        var data = ServerConfig.ApiDb.Query<T>(
//                            $"SELECT * FROM `{config[0]}` WHERE Time >= @startTime AND Time < @endTime;",
//                            new
//                            {
//                                startTime,
//                                endTime,
//                            }, 1000);
//                        var hours = (endTime - startTime).TotalHours;
//                        for (var i = 0; i < hours; i++)
//                        {
//                            var t = startTime.AddHours(i);
//                            switch (type)
//                            {
//                                case StatisticProcessTypeEnum.设备:
//                                    foreach (var p in paramList)
//                                    {
//                                        var tData = data.FirstOrDefault(x => (x as StatisticProcessDevice).DeviceId == p.Item1);
//                                        if (tData == null)
//                                        {
//                                            var nData = new StatisticProcessDevice
//                                            {
//                                                DeviceId = p.Item1,
//                                                Code = p.Item2
//                                            };
//                                            res.Add(nData);
//                                        }
//                                        else
//                                        {
//                                            res.Add(tData);
//                                        }

//                                    }

//                            }
//                        }
//                        break;
//                    case StatisticProcessTimeEnum.日: break;
//                }
//            }
//            return res;
//        }


//        /// <summary>
//        /// 获取最大的统计时间
//        /// </summary>
//        /// <returns></returns>
//        public static IEnumerable<StatisticProcessDevice> GetMaxDevices(Tuple<DateTime, DateTime> workDays)
//        {
//            return GetMax<StatisticProcessDevice>(StatisticProcessTypeEnum.设备, workDays);
//        }
//        /// <summary>
//        /// 获取最大的统计时间
//        /// </summary>
//        /// <returns></returns>
//        public static IEnumerable<StatisticProcessProduction> GetMaxProductions(Tuple<DateTime, DateTime> workDays)
//        {
//            return GetMax<StatisticProcessProduction>(StatisticProcessTypeEnum.计划号, workDays);
//        }
//        /// <summary>
//        /// 获取最大的统计时间
//        /// </summary>
//        /// <returns></returns>
//        public static IEnumerable<StatisticProcessProcessor> GetMaxProcessors(Tuple<DateTime, DateTime> workDays)
//        {
//            return GetMax<StatisticProcessProcessor>(StatisticProcessTypeEnum.操作工, workDays);
//        }

//        private static readonly Dictionary<StatisticProcessTypeEnum, string[]> Configs = new Dictionary<StatisticProcessTypeEnum, string[]>
//        {
//            {  StatisticProcessTypeEnum.设备, new []{ "statistic_process_device", "DeviceId", "Code"}},
//            {  StatisticProcessTypeEnum.计划号, new []{ "statistic_process_production", "ProductionId", "Production"}},
//            {  StatisticProcessTypeEnum.操作工, new []{ "statistic_process_processor", "ProcessorId", "Processor"}},
//        };
//        /// <summary>
//        /// 
//        /// </summary>
//        /// <returns></returns>
//        private static IEnumerable<T> GetMax<T>(StatisticProcessTypeEnum type, Tuple<DateTime, DateTime> workDays)
//        {
//            var res = new List<T>();
//            if (Configs.ContainsKey(type))
//            {
//                var config = Configs[type];
//                //res.AddRange(ServerConfig.ApiDb.Query<T>($"SELECT *, MAX(Time) Time FROM `{config[0]}` WHERE Time >= @st AND Time < @ed GROUP BY WorkshopId, Type, Step, {config[1]};",
//                //res.AddRange(ServerConfig.ApiDb.Query<T>($"SELECT * FROM `{config[0]}` WHERE Time >= @st AND Time < @ed GROUP BY WorkshopId, Type, Step, {config[1]};",
//                res.AddRange(ServerConfig.ApiDb.Query<T>($"SELECT * FROM `{config[0]}` WHERE Time >= @st AND Time < @ed;",
//                    new
//                    {
//                        st = workDays.Item1,
//                        ed = workDays.Item2,
//                    }, 1000));
//            }
//            return res;
//        }
//        #endregion

//        #region Add
//        /// <summary>
//        /// 添加设备统计
//        /// </summary>
//        /// <returns></returns>
//        public static void AddDevices(IEnumerable<StatisticProcessDevice> data)
//        {
//            Add(StatisticProcessTypeEnum.设备, data);
//        }
//        /// <summary>
//        /// 添加计划号统计
//        /// </summary>
//        /// <returns></returns>
//        public static void AddProductions(IEnumerable<StatisticProcessProduction> data)
//        {
//            Add(StatisticProcessTypeEnum.计划号, data);
//        }
//        /// <summary>
//        /// 添加操作工统计
//        /// </summary>
//        /// <returns></returns>
//        public static void AddProcessors(IEnumerable<StatisticProcessProcessor> data)
//        {
//            Add(StatisticProcessTypeEnum.操作工, data);
//        }
//        private static void Add<T>(StatisticProcessTypeEnum type, IEnumerable<T> data)
//        {
//            if (Configs.ContainsKey(type))
//            {
//                var config = Configs[type];
//                ServerConfig.ApiDb.Execute($"INSERT INTO `{config[0]}` (`MarkedDateTime`, `WorkshopId`, `Type`, `Time`, `Step`, `StepName`, `StepAbbrev`, `{config[1]}`, `{config[2]}`, `Total`, `Qualified`, `Unqualified`, `QualifiedRate`, `UnqualifiedRate`) " +
//                                           $"VALUES (@MarkedDateTime, @WorkshopId, @Type, @Time, @Step, @StepName, @StepAbbrev, @{config[1]}, @{config[2]}, @Total, @Qualified, @Unqualified, @QualifiedRate, @UnqualifiedRate);;", data);
//            }
//        }
//        #endregion

//        #region Update
//        /// <summary>
//        /// 更新设备统计
//        /// </summary>
//        /// <returns></returns>
//        public static void UpdateDevices(IEnumerable<StatisticProcessDevice> data)
//        {
//            Update(StatisticProcessTypeEnum.设备, data);
//        }
//        /// <summary>
//        /// 更新计划号统计
//        /// </summary>
//        /// <returns></returns>
//        public static void UpdateProductions(IEnumerable<StatisticProcessProduction> data)
//        {
//            Update(StatisticProcessTypeEnum.计划号, data);
//        }
//        /// <summary>
//        /// 更新操作工统计
//        /// </summary>
//        /// <returns></returns>
//        public static void UpdateProcessors(IEnumerable<StatisticProcessProcessor> data)
//        {
//            Update(StatisticProcessTypeEnum.操作工, data);
//        }
//        private static void Update<T>(StatisticProcessTypeEnum type, IEnumerable<T> data)
//        {
//            if (Configs.ContainsKey(type))
//            {
//                var config = Configs[type];
//                ServerConfig.ApiDb.Execute($"UPDATE `{config[0]}` SET `MarkedDateTime` = @MarkedDateTime, `Total` = @Total, `Qualified` = @Qualified, `Unqualified` = @Unqualified, `QualifiedRate` = @QualifiedRate, `UnqualifiedRate` = @UnqualifiedRate " +
//                                           $"WHERE `WorkshopId` = @WorkshopId AND `Type` = @Type AND `Time` = @Time AND `Step` = @Step AND `{config[1]}` = @{config[1]};", data);
//            }
//        }
//        #endregion

//        #region AddOrUpdate
//        /// <summary>
//        /// 添加或更新设备统计
//        /// </summary>
//        /// <returns></returns>
//        public static void AddOrUpdateDevices(IEnumerable<StatisticProcessDevice> data)
//        {
//            AddOrUpdate(StatisticProcessTypeEnum.设备, data);
//        }
//        /// <summary>
//        /// 添加或更新计划号统计
//        /// </summary>
//        /// <returns></returns>
//        public static void AddOrUpdateProductions(IEnumerable<StatisticProcessProduction> data)
//        {
//            AddOrUpdate(StatisticProcessTypeEnum.计划号, data);
//        }
//        /// <summary>
//        /// 添加或更新操作工统计
//        /// </summary>
//        /// <returns></returns>
//        public static void AddOrUpdateProcessors(IEnumerable<StatisticProcessProcessor> data)
//        {
//            AddOrUpdate(StatisticProcessTypeEnum.操作工, data);
//        }
//        private static void AddOrUpdate<T>(StatisticProcessTypeEnum type, IEnumerable<T> data)
//        {
//            if (Configs.ContainsKey(type))
//            {
//                var config = Configs[type];
//                ServerConfig.ApiDb.Execute($"INSERT INTO `{config[0]}` (`MarkedDateTime`, `WorkshopId`, `Type`, `Time`, `Step`, `StepName`, `StepAbbrev`, `{config[1]}`, `{config[2]}`, `Total`, `Qualified`, `Unqualified`, `QualifiedRate`, `UnqualifiedRate`) " +
//                                           $"VALUES (@MarkedDateTime, @WorkshopId, @Type, @Time, @Step, @StepName, @StepAbbrev, @{config[1]}, @{config[2]}, @Total, @Qualified, @Unqualified, @QualifiedRate, @UnqualifiedRate) " +
//                                           $"ON DUPLICATE KEY UPDATE `MarkedDateTime` = @MarkedDateTime, `Total` = @Total, `Qualified` = @Qualified, `Unqualified` = @Unqualified, `QualifiedRate` = @QualifiedRate, `UnqualifiedRate` = @UnqualifiedRate;", data);
//            }
//        }
//        #endregion

//        #region Delete
//        #endregion
//    }
//}