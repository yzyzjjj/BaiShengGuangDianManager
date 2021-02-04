using ApiManagement.Base.Server;
using ApiManagement.Models.StatisticManagementModel;
using ApiManagement.Models.Warning;
using Microsoft.EntityFrameworkCore.Internal;
using ModelBase.Base.Logger;
using ModelBase.Base.Utils;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using WarningInterval = ApiManagement.Models.Warning.WarningInterval;
#if DEBUG
#else
using System.Threading;
#endif

namespace ApiManagement.Base.Helper
{
    /// <summary>
    /// 预警
    /// </summary>
    public class WarningHelper
    {
        private static readonly string Debug = "Debug";
        public static List<string> 生产数据字段 = new List<string> { "加工数", "合格数", "裂片数", "合格率(%)", "次品率(%)" };
        public static Dictionary<string, int> 生产数据字段Dic = 生产数据字段.ToDictionary(x => x, x => 生产数据字段.IndexOf(x));
        public static Dictionary<string, int> 生产数据字段参数数量Dic = new Dictionary<string, int>
        {
            { 生产数据字段[0], 1 },
            { 生产数据字段[1], 1 },
            { 生产数据字段[2], 1 },
            { 生产数据字段[3], 2 },
            { 生产数据字段[4], 2 },
        };
        public static List<string> 生产数据汇总字段 = new List<string> { "总加工数", "总合格数", "总裂片数", "总合格率(%)", "总次品率(%)" };
        public static Dictionary<string, int> 生产数据汇总字段Dic = 生产数据汇总字段.ToDictionary(x => x, x => 生产数据汇总字段.IndexOf(x));
        public static Dictionary<string, int> 生产数据汇总字段参数数量Dic = new Dictionary<string, int>
        {
            { 生产数据汇总字段[0], 1 },
            { 生产数据汇总字段[1], 1 },
            { 生产数据汇总字段[2], 1 },
            { 生产数据汇总字段[3], 2 },
            { 生产数据汇总字段[4], 2 },
        };
        /// <summary>
        /// 预警项Id ItemId, 设备Id DeviceId, 数据类型 DataType, 预警类型， 预警设备分类 1研磨/抛光设备
        /// </summary>
        public static Dictionary<Tuple<int, int, WarningDataType, WarningType, int>, WarningCurrent> CurrentData;
#if DEBUG
#else
        private static Timer _timer5S;
#endif
        private static int _dealLength = 500;
        private static DateTime _startTime = DateTime.Today;
        public static void Init()
        {
            try
            {
#if DEBUG
                if (!RedisHelper.Exists(Debug))
                {
                    RedisHelper.SetForever(Debug, 0);
                }

                var redisPre = "Warning";
                var redisLock = $"{redisPre}:Lock";
                RedisHelper.Remove(redisLock);

                CurrentData = ServerConfig.ApiDb.Query<WarningCurrent>("SELECT a.*, b.VariableName, b.VariableTypeId, b.PointerAddress, b.`Precision`, c.Type, c.ClassId, c.`Name` SetName FROM `warning_current` a " +
                                                                       "JOIN `data_name_dictionary` b ON a.DictionaryId = b.Id " +
                                                                       "JOIN `warning_set` c ON a.SetId = c.Id;")
                    .ToDictionary(x => new Tuple<int, int, WarningDataType, WarningType, int>(x.ItemId, x.DeviceId, x.DataType, x.Type, x.ClassId));
                LoadConfig();

                //_timer5S = new Timer(DoSth_5s, null, 5000, 1000 * 5);
                Console.WriteLine("WarningHelper 调试模式已开启");
#else
                if (!RedisHelper.Exists(Debug))
                {
                    RedisHelper.SetForever(Debug, 0);
                }

                var redisPre = "Warning";
                var redisLock = $"{redisPre}:Lock";
                RedisHelper.Remove(redisLock);

                CurrentData = ServerConfig.ApiDb.Query<WarningCurrent>("SELECT a.*, b.VariableName, b.VariableTypeId, b.PointerAddress, b.`Precision`, c.Type, c.ClassId, c.`Name` SetName FROM `warning_current` a " +
                                                                       "JOIN `data_name_dictionary` b ON a.DictionaryId = b.Id " +
                                                                       "JOIN `warning_set` c ON a.SetId = c.Id;")
                    .ToDictionary(x => new Tuple<int, int, WarningDataType, WarningType, int>(x.ItemId, x.DeviceId, x.DataType, x.Type, x.ClassId));
                LoadConfig();

                _timer5S = new Timer(DoSth_5s, null, 5000, 1000 * 5);
                Console.WriteLine("WarningHelper 发布模式已开启");
#endif
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        public static void LoadConfig()
        {
            try
            {

                #region 设备数据
                var dataType = WarningDataType.设备数据;
                var sql =
                "SELECT a.*, b.DeviceIds, b.ScriptId, c.VariableName, c.VariableTypeId, c.PointerAddress, c.`Precision`, b.Type, b.ClassId, b.`Name` SetName FROM `warning_set_item` a " +
                "JOIN `warning_set` b ON a.SetId = b.Id " +
                "JOIN `data_name_dictionary` c ON a.DictionaryId = c.Id WHERE a.MarkedDelete = 0 AND b.MarkedDelete = 0 AND c.MarkedDelete = 0 AND b.`Enable` = 1 AND b.`DataType` = @DataType;";
                var warningSetItemConfigs = ServerConfig.ApiDb.Query<WarningSetItemConfig>(sql, new { DataType = dataType });
                if (warningSetItemConfigs != null)
                {
                    foreach (var config in warningSetItemConfigs)
                    {
                        foreach (var deviceId in config.DeviceList)
                        {
                            var key = new Tuple<int, int, WarningDataType, WarningType, int>(config.Id, deviceId, config.DataType, config.Type, config.ClassId);
                            var value = ClassExtension.ParentCopyToChild<WarningSetItemDetail, WarningCurrent>(config);
                            value.DeviceId = deviceId;
                            value.DeviceIds = deviceId.ToString();
                            value.ItemId = config.Id;
                            if (!CurrentData.ContainsKey(key))
                            {
                                CurrentData.Add(key, value);
                            }
                            else
                            {
                                if (CurrentData[key].HaveChange(value) && CurrentData[key].DataType == value.DataType)
                                {
                                    CurrentData[key].Update(value);
                                }
                            }
                        }
                    }

                    var removeKeys = CurrentData.Keys
                        .Where(f => f.Item3 == dataType)
                        .Where(x => warningSetItemConfigs.All(y => y.Id != x.Item1)
                                    || !warningSetItemConfigs.First(y => y.Id == x.Item1).DeviceList.Contains(x.Item2)
                                    || warningSetItemConfigs.First(y => y.Id == x.Item1).DataType != x.Item3).ToList();
                    foreach (var key in removeKeys)
                    {
                        CurrentData.Remove(key);
                    }
                }
                #endregion
                #region 生产数据
                dataType = WarningDataType.生产数据;
                sql =
                    "SELECT b.*, a.*  FROM `warning_set_item` a JOIN `warning_set` b ON a.SetId = b.Id WHERE a.MarkedDelete = 0 AND b.MarkedDelete = 0 AND b.`Enable` = 1 AND b.DataType = @DataType;";
                warningSetItemConfigs = ServerConfig.ApiDb.Query<WarningSetItemConfig>(sql, new { DataType = dataType });
                if (warningSetItemConfigs != null)
                {
                    foreach (var config in warningSetItemConfigs)
                    {
                        if (!config.IsSum)
                        {
                            foreach (var deviceId in config.DeviceList)
                            {
                                var key = new Tuple<int, int, WarningDataType, WarningType, int>(config.Id, deviceId, config.DataType, config.Type, config.ClassId);
                                var value = ClassExtension.ParentCopyToChild<WarningSetItemDetail, WarningCurrent>(config);
                                value.DeviceId = deviceId;
                                value.DeviceIds = deviceId.ToString();
                                value.ItemId = config.Id;
                                if (!CurrentData.ContainsKey(key))
                                {
                                    CurrentData.Add(key, value);
                                }
                                else
                                {
                                    if (CurrentData[key].HaveChange(value) && CurrentData[key].DataType == value.DataType)
                                    {
                                        CurrentData[key].Update(value);
                                    }
                                }
                            }
                        }
                        else
                        {
                            var key = new Tuple<int, int, WarningDataType, WarningType, int>(config.Id, 0, config.DataType, config.Type, config.ClassId);
                            var value = ClassExtension.ParentCopyToChild<WarningSetItemDetail, WarningCurrent>(config);
                            value.DeviceId = 0;
                            value.DeviceIds = config.DeviceIds;
                            value.ItemId = config.Id;
                            if (!CurrentData.ContainsKey(key))
                            {
                                CurrentData.Add(key, value);
                            }
                            else
                            {
                                if (CurrentData[key].HaveChange(value) && CurrentData[key].DataType == value.DataType)
                                {
                                    CurrentData[key].Update(value);
                                }
                            }
                        }
                    }

                    var removeKeys = CurrentData.Keys
                        .Where(f => f.Item3 == dataType && f.Item2 != 0)
                        .Where(x => warningSetItemConfigs.All(y => y.Id != x.Item1)
                                    || !warningSetItemConfigs.First(y => y.Id == x.Item1).DeviceList.Contains(x.Item2)
                                    || warningSetItemConfigs.First(y => y.Id == x.Item1).DataType != x.Item3).ToList();
                    foreach (var key in removeKeys)
                    {
                        CurrentData.Remove(key);
                    }

                    removeKeys = CurrentData
                        .Where(f => f.Key.Item3 == dataType && f.Key.Item2 == 0)
                        .Where(x => warningSetItemConfigs.All(y => y.Id != x.Key.Item1)
                                    || warningSetItemConfigs.First(y => y.Id == x.Key.Item1).IsSum != x.Value.IsSum
                                    || warningSetItemConfigs.First(y => y.Id == x.Key.Item1).DataType != x.Key.Item3).Select(z => z.Key).ToList();
                    foreach (var key in removeKeys)
                    {
                        CurrentData.Remove(key);
                    }
                }
                #endregion

                CurrentData = CurrentData.OrderBy(x => x.Value.IsSum).ToDictionary(x => x.Key, x => x.Value);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }


        private static void DoSth_5s(object state)
        {
            设备数据Warning();
            生产数据Warning();
        }

        /// <summary>
        /// 设备数据
        /// </summary>
        private static void 设备数据Warning()
        {
#if !DEBUG
            if (RedisHelper.Get<int>("Debug") != 0)
            {
                return;
            }
#endif
            var redisPre = "Warning";
            var redisLock = $"{redisPre}:设备数据Lock";
            var idKey = $"{redisPre}:设备数据Id";
            if (RedisHelper.SetIfNotExist(redisLock, DateTime.Now.ToStr()))
            {
                try
                {
                    RedisHelper.SetExpireAt(redisLock, DateTime.Now.AddMinutes(5));

                    var startId = RedisHelper.Get<int>(idKey);
                    if (startId == 0)
                    {
                        startId = ServerConfig.ApiDb.Query<int>("SELECT Id FROM `npc_monitoring_analysis` WHERE SendTime <= @_startTime ORDER BY SendTime DESC LIMIT 1;", new { _startTime }).FirstOrDefault();
                        RedisHelper.SetForever(idKey, startId);
                    }
                    var mData = ServerConfig.ApiDb.Query<MonitoringData>(
                        "SELECT * FROM `npc_monitoring_analysis` WHERE Id > @Id AND UserSend = 0 ORDER BY Id LIMIT @limit;", new
                        {
                            Id = startId,
                            limit = _dealLength
                        });

                    var warningLogs = new List<WarningLog>();
                    if (mData.Any())
                    {
                        mData = mData.OrderBy(x => x.SendTime);
                        var endId = mData.Last().Id;
                        if (endId > startId)
                        {
                            #region  预警检验
                            foreach (var data in mData)
                            {
                                var deviceId = data.DeviceId;
                                var endTime = data.SendTime;
                                var analysisData = data.AnalysisData;
                                if (analysisData != null)
                                {
                                    var warningKeys = CurrentData.Keys.Where(x => x.Item2 == deviceId
                                                                                  && x.Item3 == WarningDataType.设备数据
                                                                                  && x.Item4 == WarningType.设备
                                                                                  && x.Item5 == 1).ToList();
                                    foreach (var warningKey in warningKeys)
                                    {
                                        if (CurrentData.ContainsKey(warningKey))
                                        {
                                            var deviceWarning = CurrentData[warningKey];
                                            deviceWarning.CurrentTime = endTime;
                                            if (deviceWarning.StartTime == default(DateTime))
                                            {
                                                deviceWarning.StartTime = endTime;
                                            }

                                            if (deviceWarning.EndTime == default(DateTime))
                                            {
                                                deviceWarning.EndTime = endTime;
                                            }

                                            var totalConfigSeconds = TotalSeconds(deviceWarning.Interval, deviceWarning.Frequency);
                                            var totalSeconds = (int)(endTime - deviceWarning.StartTime).TotalSeconds;
                                            while (totalSeconds > totalConfigSeconds && deviceWarning.WarningData.Any())
                                            {
                                                if (deviceWarning.Trend && deviceWarning.WarningData.Any())
                                                {
                                                    var warningLog = ClassExtension.ParentCopyToChild<WarningCurrent, WarningLog>(deviceWarning);
                                                    warningLog.IsWarning = false;
                                                    warningLog.WarningTime = endTime;
                                                    warningLogs.Add(warningLog);
                                                    deviceWarning.Trend = false;
                                                }
                                                deviceWarning.Current = deviceWarning.Current - 1 >= 0 ? deviceWarning.Current - 1 : 0;
                                                deviceWarning.WarningData.RemoveAt(0);
                                                deviceWarning.StartTime = deviceWarning.WarningData.Any() ? deviceWarning.WarningData.First().T : endTime;
                                                totalSeconds = (int)(endTime - deviceWarning.StartTime).TotalSeconds;
                                            }
                                            var actAddress = deviceWarning.PointerAddress - 1;
                                            var find = false;
                                            var npcValue = 0;
                                            switch (deviceWarning.VariableTypeId)
                                            {
                                                //变量
                                                case 1:
                                                    if (actAddress >= 0 && analysisData.vals.Count > actAddress)
                                                    {
                                                        find = true;
                                                        npcValue = analysisData.vals[actAddress];
                                                    }
                                                    break;
                                                //输入
                                                case 2:
                                                    if (actAddress >= 0 && analysisData.ins.Count > actAddress)
                                                    {
                                                        find = true;
                                                        npcValue = analysisData.ins[actAddress];
                                                    }
                                                    break;
                                                //输出
                                                case 3:
                                                    if (actAddress >= 0 && analysisData.outs.Count > actAddress)
                                                    {
                                                        find = true;
                                                        npcValue = analysisData.outs[actAddress];
                                                    }
                                                    break;
                                            }

                                            if (find)
                                            {
                                                var chu = Math.Pow(10, deviceWarning.Precision);
                                                var value = (decimal)(npcValue / chu);
                                                var conditionInfos = new List<WarningConditionInfo>
                                                {
                                                   new WarningConditionInfo( deviceWarning.Condition1, deviceWarning.Value1),
                                                   new WarningConditionInfo( deviceWarning.Condition2, deviceWarning.Value2),
                                                };
                                                if (MeetConditions(conditionInfos, deviceWarning.Logic, value))
                                                {
                                                    try
                                                    {
                                                        deviceWarning.Trend = true;
                                                        deviceWarning.Current++;
                                                        deviceWarning.WarningData.Add(new WarningData(endTime, value));
                                                        deviceWarning.StartTime = deviceWarning.WarningData.First().T;
                                                        deviceWarning.EndTime = endTime;
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        Console.WriteLine(e);
                                                        throw;
                                                    }

                                                    if (deviceWarning.Current >= deviceWarning.Count)
                                                    {
                                                        var warningLog = ClassExtension.ParentCopyToChild<WarningCurrent, WarningLog>(deviceWarning);
                                                        warningLog.IsWarning = true;
                                                        warningLog.WarningTime = endTime;
                                                        warningLogs.Add(warningLog);
                                                        deviceWarning.Reset();
                                                    }
                                                }
                                                //else
                                                //{
                                                //    if (deviceWarning.Trend && deviceWarning.WarningData.Any())
                                                //    {
                                                //        var warningLog = ClassExtension.ParentCopyToChild<WarningCurrent, WarningLog>(deviceWarning);
                                                //        warningLog.IsWarning = false;
                                                //        warningLog.WarningTime = endTime;
                                                //        warningLogs.Add(warningLog);

                                                //        deviceWarning.Trend = false;
                                                //        deviceWarning.Current = deviceWarning.Current - 1 >= 0 ? deviceWarning.Current - 1 : 0;
                                                //        deviceWarning.WarningData.RemoveAt(0);
                                                //        deviceWarning.StartTime = deviceWarning.WarningData.Any() ? deviceWarning.WarningData.First().T : endTime;
                                                //        deviceWarning.EndTime = endTime;
                                                //    }
                                                //}
                                            }
                                        }
                                    }
                                }
                            }
                            #endregion
                            RedisHelper.SetForever(idKey, endId);
                        }
                    }
                    if (CurrentData.Any())
                    {
                        ServerConfig.ApiDb.Execute(
                            "INSERT INTO `warning_current` (`CurrentTime`, `Type`, `ClassId`, `ItemId`, `DeviceId`, `DataType`, `SetId`, `ScriptId`, `EndTime`, `StartTime`, `Frequency`, `Interval`, `Count`, `Condition1`, `Value1`, `Condition2`, `Value2`, `DictionaryId`, `Current`, `Trend`, `Values`) " +
                            "VALUES (@CurrentTime, @Type, @ClassId, @ItemId, @DeviceId, @DataType, @SetId, @ScriptId, @EndTime, @StartTime, @Frequency, @Interval, @Count, @Condition1, @Value1, @Condition2, @Value2, @DictionaryId, @Current, @Trend, @Values) " +
                            "ON DUPLICATE KEY UPDATE `CurrentTime` = @CurrentTime, `ScriptId` = @ScriptId, `EndTime` = @EndTime, `StartTime` = @StartTime, `Frequency` = @Frequency, `Interval` = @Interval, `Count` = @Count, `Condition1` = @Condition1, `Value1` = @Value1, `Condition2` = @Condition2, `Value2` = @Value2, `DictionaryId` = @DictionaryId, `Current` = @Current, `Trend` = @Trend, `Values` = @Values;",
                            CurrentData.Where(x => x.Key.Item3 == WarningDataType.设备数据 && x.Key.Item4 == WarningType.设备 && x.Key.Item5 == 1).Select(y => y.Value));
                    }
                    if (warningLogs.Any())
                    {
                        ServerConfig.ApiDb.Execute(
                        "INSERT INTO `warning_log` (`CurrentTime`, `IsWarning`, `WarningTime`, `Type`, `ClassId`, `ItemId`, `DeviceId`, `DataType`, `SetId`, `ScriptId`, `EndTime`, `StartTime`, `Frequency`, `Interval`, `Count`, `Condition1`, `Value1`, `Condition2`, `Value2`, `DictionaryId`, `Current`, `Values`) " +
                        "VALUES (@CurrentTime, @IsWarning, @WarningTime, @Type, @ClassId, @ItemId, @DeviceId, @DataType, @SetId, @ScriptId, @EndTime, @StartTime, @Frequency, @Interval, @Count, @Condition1, @Value1, @Condition2, @Value2, @DictionaryId, @Current, @Values);",
                        warningLogs);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                RedisHelper.Remove(redisLock);
            }
        }

        /// <summary>
        /// 生产数据
        /// </summary>
        private static void 生产数据Warning()
        {
            //return;
#if !DEBUG
            if (RedisHelper.Get<int>("Debug") != 0)
            {
                return;
            }
#endif
            var redisPre = "Warning";
            var redisLock = $"{redisPre}:生产数据Lock";
            var redisTime = $"{redisPre}:生产数据Time";
            if (RedisHelper.SetIfNotExist(redisLock, DateTime.Now.ToStr()))
            {
                try
                {
                    RedisHelper.SetExpireAt(redisLock, DateTime.Now.AddMinutes(45));

                    var startTimeStr = RedisHelper.Get<string>(redisTime);
                    var startTime = _startTime;
                    if (startTimeStr.IsNullOrEmpty())
                    {
                        RedisHelper.SetForever(redisTime, startTime.ToStr());
                    }
                    else
                    {
                        DateTime.TryParse(startTimeStr, out startTime);
                    }

                    var allDeviceList = ServerConfig.ApiDb.Query<MonitoringProcess>(
                        "SELECT a.`Id` DeviceId, a.`Code`, b.DeviceCategoryId, b.CategoryName FROM `device_library` a JOIN (SELECT a.*, b.CategoryName FROM `device_model` a JOIN `device_category` b ON a.DeviceCategoryId = b.Id) b ON a.DeviceModelId = b.Id WHERE a.MarkedDelete = 0 ORDER BY a.Id;");

                    var warningLogs = new List<WarningLog>();

                    #region  预警检验

                    var warningKeys = CurrentData.Keys.Where(x => x.Item3 == WarningDataType.生产数据 && x.Item4 == WarningType.设备 && x.Item5 == 1).ToList();
                    var nowTime = DateTime.Now;
                    if (warningKeys.Any())
                    {
                        var paramDic = new Dictionary<string, string[]>
                        {
                            {"粗抛机", new []{ "CuPaoTime", "CuPaoFaChu", "CuPaoHeGe", "CuPaoLiePian", "CuPaoDeviceId"}},
                            {"精抛机", new []{ "JingPaoTime", "JingPaoFaChu", "JingPaoHeGe", "JingPaoLiePian", "JingPaoDeviceId"}},
                            {"研磨机", new []{ "YanMoTime", "YanMoFaChu", "YanMoHeGe", "YanMoLiePian", "YanMoDeviceId"}},
                        };

                        foreach (var param in paramDic)
                        {
                            var devices = allDeviceList.Where(x => x.CategoryName.Contains(param.Key));
                            if (devices.Any())
                            {
                                var category = param.Value;
                                var sql = string.Format(
                                    "SELECT {4} DeviceId, {0} Time, {1} FaChu, {2} HeGe, {3} LiePian " +
                                    "FROM `flowcard_library` WHERE {4} IN @DeviceId AND {0} >= @startTime AND {0} <= @endTime ORDER BY {0}",
                                    category[0], category[1], category[2], category[3], category[4]);
                                var monitoringProductionData = ServerConfig.ApiDb.Query<MonitoringProductionData>(sql, new
                                {
                                    DeviceId = devices.Select(x => x.DeviceId),
                                    startTime = startTime,
                                    endTime = nowTime,
                                }, 60);

                                foreach (var data in monitoringProductionData)
                                {
                                    var endTime = data.Time;
                                    warningKeys = CurrentData.Keys.Where(x => x.Item2 == data.DeviceId
                                                                              && x.Item3 == WarningDataType.生产数据
                                                                              && x.Item4 == WarningType.设备
                                                                              && x.Item5 == 1).ToList();
                                    foreach (var warningKey in warningKeys)
                                    {
                                        #region 汇总
                                        if (CurrentData.ContainsKey(warningKey))
                                        {
                                            var deviceWarning = CurrentData[warningKey];
                                            if (deviceWarning.IsSum)
                                            {
                                                continue;
                                            }
                                            deviceWarning.CurrentTime = endTime;
                                            if (deviceWarning.StartTime == default(DateTime))
                                            {
                                                deviceWarning.StartTime = endTime;
                                            }

                                            if (deviceWarning.EndTime == default(DateTime))
                                            {
                                                deviceWarning.EndTime = endTime;
                                            }
                                            if (deviceWarning.Item.IsNullOrEmpty())
                                            {
                                                continue;
                                            }

                                            if (!生产数据字段Dic.ContainsKey(deviceWarning.Item))
                                            {
                                                continue;
                                            }
                                            //"加工数", "合格数", "裂片数", "合格率(%)", "次品率(%)"
                                            var totalConfigSeconds = 0;
                                            var nowTotalSeconds = 0;
                                            var totalSeconds = 0;
                                            var paramList = new decimal[3];
                                            switch (生产数据字段Dic[deviceWarning.Item])
                                            {
                                                //"加工数"
                                                case 0:
                                                    totalConfigSeconds = TotalSeconds(deviceWarning.Interval, deviceWarning.Frequency);
                                                    nowTotalSeconds = (int)(data.Time - deviceWarning.StartTime).TotalSeconds;
                                                    totalSeconds = (int)(deviceWarning.EndTime - deviceWarning.StartTime).TotalSeconds;
                                                    paramList[0] = data.FaChu;
                                                    break;
                                                //"合格数"
                                                case 1:
                                                    totalConfigSeconds = TotalSeconds(deviceWarning.Interval, deviceWarning.Frequency);
                                                    nowTotalSeconds = (int)(data.Time - deviceWarning.StartTime).TotalSeconds;
                                                    totalSeconds = (int)(deviceWarning.EndTime - deviceWarning.StartTime).TotalSeconds;
                                                    paramList[0] = data.HeGe;
                                                    break;
                                                //"裂片数"
                                                case 2:
                                                    totalConfigSeconds = TotalSeconds(deviceWarning.Interval, deviceWarning.Frequency);
                                                    nowTotalSeconds = (int)(data.Time - deviceWarning.StartTime).TotalSeconds;
                                                    totalSeconds = (int)(deviceWarning.EndTime - deviceWarning.StartTime).TotalSeconds;
                                                    paramList[0] = data.LiePian;
                                                    break;
                                                //"合格率(%)"
                                                case 3:
                                                    totalConfigSeconds = TotalSeconds(deviceWarning.Interval, deviceWarning.Frequency);
                                                    nowTotalSeconds = (int)(data.Time - deviceWarning.StartTime).TotalSeconds;
                                                    totalSeconds = (int)(deviceWarning.EndTime - deviceWarning.StartTime).TotalSeconds;
                                                    paramList[0] = data.FaChu;
                                                    paramList[1] = data.HeGe;
                                                    break;
                                                //"次品率(%)"
                                                case 4:
                                                    totalConfigSeconds = TotalSeconds(deviceWarning.Interval, deviceWarning.Frequency);
                                                    nowTotalSeconds = (int)(data.Time - deviceWarning.StartTime).TotalSeconds;
                                                    totalSeconds = (int)(deviceWarning.EndTime - deviceWarning.StartTime).TotalSeconds;
                                                    paramList[0] = data.FaChu;
                                                    paramList[1] = data.FaChu - data.HeGe;
                                                    break;
                                            }

                                            while (totalSeconds > totalConfigSeconds && deviceWarning.WarningData.Any())
                                            {
                                                if (deviceWarning.Trend && deviceWarning.WarningData.Any())
                                                {
                                                    var warningLog = ClassExtension.ParentCopyToChild<WarningCurrent, WarningLog>(deviceWarning);
                                                    warningLog.IsWarning = false;
                                                    warningLog.WarningTime = endTime;
                                                    warningLogs.Add(warningLog);
                                                    deviceWarning.Trend = false;
                                                }
                                                deviceWarning.Current = deviceWarning.Current - 1 >= 0 ? deviceWarning.Current - 1 : 0;
                                                deviceWarning.WarningData.RemoveAt(0);
                                                deviceWarning.StartTime = deviceWarning.WarningData.Any() ? deviceWarning.WarningData.First().T : endTime;
                                                totalSeconds = (int)(endTime - deviceWarning.StartTime).TotalSeconds;
                                            }
                                            if (!paramList.Any())
                                            {
                                                continue;
                                            }

                                            var conditionInfos = new List<WarningConditionInfo>
                                            {
                                                new WarningConditionInfo( deviceWarning.Condition1, deviceWarning.Value1),
                                                new WarningConditionInfo( deviceWarning.Condition2, deviceWarning.Value2),
                                            };
                                            switch (生产数据字段参数数量Dic[deviceWarning.Item])
                                            {
                                                case 1:
                                                    if (nowTotalSeconds <= totalConfigSeconds)
                                                    {
                                                        deviceWarning.Value += paramList[0];
                                                        //deviceWarning.Trend = true;
                                                        //deviceWarning.WarningData.Add(new WarningData(data.Time, deviceWarning.Value));
                                                    }
                                                    break;
                                                case 2:
                                                    if (nowTotalSeconds <= totalConfigSeconds)
                                                    {
                                                        var index = 0;
                                                        deviceWarning.AddParam(index, paramList[index]);
                                                        index = 1;
                                                        deviceWarning.AddParam(index, paramList[index]);
                                                        var faChu = deviceWarning.GetParam(0);
                                                        var p = deviceWarning.GetParam(0);
                                                        deviceWarning.Value = faChu == 0 ? 0 : (p / faChu).ToRound();
                                                        //deviceWarning.Trend = true;
                                                        //deviceWarning.WarningData.Add(new WarningData(data.Time, deviceWarning.Value));
                                                    }
                                                    break;
                                                default: break;
                                            }

                                            if (MeetConditions(conditionInfos, deviceWarning.Logic, deviceWarning.Value))
                                            {
                                                try
                                                {
                                                    deviceWarning.Trend = true;
                                                    deviceWarning.Current++;
                                                    deviceWarning.WarningData.Add(new WarningData(endTime, deviceWarning.Value));
                                                    deviceWarning.StartTime = deviceWarning.WarningData.First().T;
                                                    deviceWarning.EndTime = endTime;
                                                }
                                                catch (Exception e)
                                                {
                                                    Console.WriteLine(e);
                                                    throw;
                                                }

                                                if (deviceWarning.Current >= deviceWarning.Count)
                                                {
                                                    var warningLog = ClassExtension.ParentCopyToChild<WarningCurrent, WarningLog>(deviceWarning);
                                                    warningLog.IsWarning = true;
                                                    warningLog.WarningTime = endTime;
                                                    warningLogs.Add(warningLog);
                                                    deviceWarning.Reset();
                                                }
                                            }
                                            //else
                                            //{
                                            //    if (deviceWarning.WarningData.Any())
                                            //    {
                                            //        var warningLog = ClassExtension.ParentCopyToChild<WarningCurrent, WarningLog>(deviceWarning);
                                            //        warningLog.IsWarning = true;
                                            //        warningLog.WarningTime = endTime;
                                            //        warningLogs.Add(warningLog);

                                            //        deviceWarning.Trend = false;
                                            //        deviceWarning.Current = deviceWarning.Current - 1 >= 0 ? deviceWarning.Current - 1 : 0;
                                            //        deviceWarning.WarningData.RemoveAt(0);
                                            //        deviceWarning.StartTime = deviceWarning.WarningData.Any() ? deviceWarning.WarningData.First().T : endTime;
                                            //        deviceWarning.EndTime = endTime;
                                            //    }
                                            //}
                                        }
                                        #endregion
                                    }


                                    var sumWarningKeys = CurrentData.Where(x => x.Key.Item2 == 0
                                                                                     && x.Key.Item3 == WarningDataType.生产数据
                                                                                     && x.Key.Item4 == WarningType.设备
                                                                                     && x.Key.Item5 == 1
                                                                                     && x.Value.IsSum
                                                                                     && x.Value.DeviceList.Contains(data.DeviceId)).Select(y => y.Key).ToList();


                                    foreach (var sumWarningKey in sumWarningKeys)
                                    {
                                        #region 汇总
                                        if (CurrentData.ContainsKey(sumWarningKey))
                                        {
                                            var sumDeviceWarning = CurrentData[sumWarningKey];
                                            if (!sumDeviceWarning.IsSum)
                                            {
                                                continue;
                                            }

                                            sumDeviceWarning.CurrentTime = endTime;
                                            if (sumDeviceWarning.StartTime == default(DateTime))
                                            {
                                                sumDeviceWarning.StartTime = endTime;
                                            }

                                            if (sumDeviceWarning.EndTime == default(DateTime))
                                            {
                                                sumDeviceWarning.EndTime = endTime;
                                            }
                                            if (sumDeviceWarning.Item.IsNullOrEmpty())
                                            {
                                                continue;
                                            }

                                            if (!生产数据汇总字段Dic.ContainsKey(sumDeviceWarning.Item))
                                            {
                                                continue;
                                            }
                                            //"总加工数", "总合格数", "总裂片数", "总合格率(%)", "总次品率(%)"
                                            var totalConfigSeconds = 0;
                                            var nowTotalSeconds = 0;
                                            var totalSeconds = 0;
                                            var paramList = new decimal[3];
                                            switch (生产数据汇总字段Dic[sumDeviceWarning.Item])
                                            {
                                                //"总加工数"
                                                case 0:
                                                    totalConfigSeconds = TotalSeconds(sumDeviceWarning.Interval, sumDeviceWarning.Frequency);
                                                    nowTotalSeconds = (int)(data.Time - sumDeviceWarning.StartTime).TotalSeconds;
                                                    totalSeconds = (int)(sumDeviceWarning.EndTime - sumDeviceWarning.StartTime).TotalSeconds;
                                                    paramList[0] = data.FaChu;
                                                    break;
                                                //"总合格数"
                                                case 1:
                                                    totalConfigSeconds = TotalSeconds(sumDeviceWarning.Interval, sumDeviceWarning.Frequency);
                                                    nowTotalSeconds = (int)(data.Time - sumDeviceWarning.StartTime).TotalSeconds;
                                                    totalSeconds = (int)(sumDeviceWarning.EndTime - sumDeviceWarning.StartTime).TotalSeconds;
                                                    paramList[0] = data.HeGe;
                                                    break;
                                                //"总裂片数"
                                                case 2:
                                                    totalConfigSeconds = TotalSeconds(sumDeviceWarning.Interval, sumDeviceWarning.Frequency);
                                                    nowTotalSeconds = (int)(data.Time - sumDeviceWarning.StartTime).TotalSeconds;
                                                    totalSeconds = (int)(sumDeviceWarning.EndTime - sumDeviceWarning.StartTime).TotalSeconds;
                                                    paramList[0] = data.LiePian;
                                                    break;
                                                //"总合格率(%)"
                                                case 3:
                                                    totalConfigSeconds = TotalSeconds(sumDeviceWarning.Interval, sumDeviceWarning.Frequency);
                                                    nowTotalSeconds = (int)(data.Time - sumDeviceWarning.StartTime).TotalSeconds;
                                                    totalSeconds = (int)(sumDeviceWarning.EndTime - sumDeviceWarning.StartTime).TotalSeconds;
                                                    paramList[0] = data.FaChu;
                                                    paramList[1] = data.HeGe;
                                                    break;
                                                //"总次品率(%)"
                                                case 4:
                                                    totalConfigSeconds = TotalSeconds(sumDeviceWarning.Interval, sumDeviceWarning.Frequency);
                                                    nowTotalSeconds = (int)(data.Time - sumDeviceWarning.StartTime).TotalSeconds;
                                                    totalSeconds = (int)(sumDeviceWarning.EndTime - sumDeviceWarning.StartTime).TotalSeconds;
                                                    paramList[0] = data.FaChu;
                                                    paramList[1] = data.FaChu - data.HeGe;
                                                    break;
                                            }

                                            while (totalSeconds > totalConfigSeconds && sumDeviceWarning.WarningData.Any())
                                            {
                                                if (sumDeviceWarning.Trend && sumDeviceWarning.WarningData.Any())
                                                {
                                                    var warningLog = ClassExtension.ParentCopyToChild<WarningCurrent, WarningLog>(sumDeviceWarning);
                                                    warningLog.IsWarning = false;
                                                    warningLog.WarningTime = endTime;
                                                    warningLogs.Add(warningLog);
                                                    sumDeviceWarning.Trend = false;
                                                }
                                                sumDeviceWarning.Current = sumDeviceWarning.Current - 1 >= 0 ? sumDeviceWarning.Current - 1 : 0;
                                                sumDeviceWarning.WarningData.RemoveAt(0);
                                                sumDeviceWarning.StartTime = sumDeviceWarning.WarningData.Any() ? sumDeviceWarning.WarningData.First().T : endTime;
                                                totalSeconds = (int)(endTime - sumDeviceWarning.StartTime).TotalSeconds;
                                            }
                                            if (!paramList.Any())
                                            {
                                                continue;
                                            }

                                            var sumConditionInfos = new List<WarningConditionInfo>
                                                    {
                                                        new WarningConditionInfo( sumDeviceWarning.Condition1, sumDeviceWarning.Value1),
                                                        new WarningConditionInfo( sumDeviceWarning.Condition2, sumDeviceWarning.Value2),
                                                    };
                                            switch (生产数据汇总字段参数数量Dic[sumDeviceWarning.Item])
                                            {
                                                case 1:
                                                    if (nowTotalSeconds <= totalConfigSeconds)
                                                    {
                                                        sumDeviceWarning.Value += paramList[0];
                                                        //sumDeviceWarning.Trend = true;
                                                        //sumDeviceWarning.WarningData.Add(new WarningData(data.Time, sumDeviceWarning.Value));
                                                    }
                                                    break;
                                                case 2:
                                                    if (nowTotalSeconds <= totalConfigSeconds)
                                                    {
                                                        var index = 0;
                                                        sumDeviceWarning.AddParam(index, paramList[index]);
                                                        index = 1;
                                                        sumDeviceWarning.AddParam(index, paramList[index]);
                                                        var faChu = sumDeviceWarning.GetParam(0);
                                                        var p = sumDeviceWarning.GetParam(0);
                                                        sumDeviceWarning.Value = faChu == 0 ? 0 : (p / faChu).ToRound();
                                                        //sumDeviceWarning.Trend = true;
                                                        //sumDeviceWarning.WarningData.Add(new WarningData(data.Time, sumDeviceWarning.Value));
                                                    }
                                                    break;
                                                default: break;
                                            }

                                            if (MeetConditions(sumConditionInfos, sumDeviceWarning.Logic, sumDeviceWarning.Value))
                                            {
                                                try
                                                {
                                                    sumDeviceWarning.Trend = true;
                                                    sumDeviceWarning.Current++;
                                                    sumDeviceWarning.WarningData.Add(new WarningData(endTime, sumDeviceWarning.Value));
                                                    sumDeviceWarning.StartTime = sumDeviceWarning.WarningData.First().T;
                                                    sumDeviceWarning.EndTime = endTime;
                                                }
                                                catch (Exception e)
                                                {
                                                    Console.WriteLine(e);
                                                    throw;
                                                }

                                                if (sumDeviceWarning.Current >= sumDeviceWarning.Count)
                                                {
                                                    var warningLog = ClassExtension.ParentCopyToChild<WarningCurrent, WarningLog>(sumDeviceWarning);
                                                    warningLog.IsWarning = true;
                                                    warningLog.WarningTime = endTime;
                                                    warningLogs.Add(warningLog);
                                                    sumDeviceWarning.Reset();
                                                }
                                            }
                                            //else
                                            //{
                                            //    if (sumDeviceWarning.WarningData.Any())
                                            //    {
                                            //        var warningLog = ClassExtension.ParentCopyToChild<WarningCurrent, WarningLog>(sumDeviceWarning);
                                            //        warningLog.IsWarning = true;
                                            //        warningLog.WarningTime = endTime;
                                            //        warningLogs.Add(warningLog);

                                            //        sumDeviceWarning.Trend = false;
                                            //        sumDeviceWarning.Current = sumDeviceWarning.Current - 1 >= 0 ? sumDeviceWarning.Current - 1 : 0;
                                            //        sumDeviceWarning.WarningData.RemoveAt(0);
                                            //        sumDeviceWarning.StartTime = sumDeviceWarning.WarningData.Any() ? sumDeviceWarning.WarningData.First().T : endTime;
                                            //        sumDeviceWarning.EndTime = endTime;
                                            //    }
                                            //}
                                        }
                                        #endregion
                                    }
                                }
                            }
                        }
                    }
                    #endregion

                    RedisHelper.SetForever(redisTime, nowTime.ToStr());
                    if (CurrentData.Any())
                    {
                        ServerConfig.ApiDb.Execute(
                            "INSERT INTO  `warning_current` (`CurrentTime`, `Type`, `ClassId`, `ItemId`, `DeviceId`, `DataType`, `SetId`, `ScriptId`, `CategoryId`, `IsSum`, `StartTime`, `EndTime`, `Frequency`, `Interval`, `Count`, `Condition1`, `Value1`, `Logic`, `Condition2`, `Value2`, `DictionaryId`, `Current`, `Trend`, `Values`) " +
                            "VALUES (@CurrentTime, @Type, @ClassId, @ItemId, @DeviceId, @DataType, @SetId, @ScriptId, @CategoryId, @IsSum, @StartTime, @EndTime, @Frequency, @Interval, @Count, @Condition1, @Value1, @Logic, @Condition2, @Value2, @DictionaryId, @Current, @Trend, @Values) " +
                            "ON DUPLICATE KEY UPDATE `CurrentTime` = @CurrentTime, `ScriptId` = @ScriptId, `CategoryId` = @CategoryId, `IsSum` = @IsSum, `StartTime` = @StartTime, `EndTime` = @EndTime, `Frequency` = @Frequency, `Interval` = @Interval, `Count` = @Count, `Condition1` = @Condition1, `Value1` = @Value1, `Logic` = @Logic, `Condition2` = @Condition2, `Value2` = @Value2, `DictionaryId` = @DictionaryId, `Current` = @Current, `Trend` = @Trend, `Values` = @Values;",
                            CurrentData.Where(x => x.Key.Item3 == WarningDataType.生产数据 && x.Key.Item4 == WarningType.设备 && x.Key.Item5 == 1).Select(y => y.Value));
                    }
                    if (warningLogs.Any())
                    {
                        ServerConfig.ApiDb.Execute(
                        "INSERT INTO `warning_log` (`CurrentTime`, `IsWarning`, `WarningTime`, `Type`, `ClassId`, `ItemId`, `DeviceId`, `DataType`, `SetId`, `ScriptId`, `CategoryId`, `IsSum`, `StartTime`, `EndTime`, `Frequency`, `Interval`, `Count`, `Condition1`, `Value1`, `Condition2`, `Value2`, `DictionaryId`, `Current`, `Values`) " +
                        "VALUES (@CurrentTime, @IsWarning, @WarningTime, @Type, @ClassId, @ItemId, @DeviceId, @DataType, @SetId, @ScriptId, @CategoryId, @IsSum, @StartTime, @EndTime, @Frequency, @Interval, @Count, @Condition1, @Value1, @Condition2, @Value2, @DictionaryId, @Current, @Values);",
                        warningLogs);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                RedisHelper.Remove(redisLock);
            }
        }

        /// <summary>
        /// 总计多少秒
        /// </summary>
        /// <param name="interval"></param>
        /// <param name="frequency"></param>
        /// <returns></returns>
        private static int TotalSeconds(WarningInterval interval, int frequency)
        {
            var total = 0;
            switch (interval)
            {
                case WarningInterval.秒:
                    total += frequency;
                    break;
                case WarningInterval.分:
                    total += 60 * frequency;
                    break;
                case WarningInterval.小时:
                    total += 60 * 60 * frequency;
                    break;
                case WarningInterval.天:
                    total += 24 * 60 * 60 * frequency;
                    break;
                case WarningInterval.周:
                    total += 7 * 24 * 60 * 60 * frequency;
                    break;
                case WarningInterval.月:
                    total += 30 * 24 * 60 * 60 * frequency;
                    break;
                case WarningInterval.年:
                    total += 365 * 24 * 60 * 60 * frequency;
                    break;
            }
            return total;
        }

        private class WarningConditionInfo
        {
            public WarningConditionInfo(WarningCondition condition, decimal value)
            {
                Condition = condition;
                Value = value;
            }
            public WarningCondition Condition { get; set; }
            public decimal Value { get; set; }
        }

        /// <summary>
        /// 是否满足条件
        /// </summary>
        /// <param name="conditionInfos"></param>
        /// <param name="logic"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool MeetConditions(IEnumerable<WarningConditionInfo> conditionInfos, WarningLogic logic, decimal value)
        {
            var meet = false;
            var res = new List<bool>();
            foreach (var conditionInfo in conditionInfos)
            {
                var r = false;
                switch (conditionInfo.Condition)
                {
                    case WarningCondition.大于:
                        r = value > conditionInfo.Value;
                        break;
                    case WarningCondition.大于等于:
                        r = value >= conditionInfo.Value;
                        break;
                    case WarningCondition.小于:
                        r = value < conditionInfo.Value;
                        break;
                    case WarningCondition.小于等于:
                        r = value <= conditionInfo.Value;
                        break;
                }
                res.Add(r);
            }

            switch (logic)
            {
                case WarningLogic.并且: meet = res.All(x => x); break;
                case WarningLogic.或者: meet = res.Any(x => x); break;
                case WarningLogic.不设置: meet = res.Any() && res.First(); break;
            }
            return meet;
        }
    }
}
