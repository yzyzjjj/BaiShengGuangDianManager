using ApiManagement.Base.Server;
using ApiManagement.Models.OtherModel;
using ApiManagement.Models.StatisticManagementModel;
using ApiManagement.Models.Warning;
using Microsoft.EntityFrameworkCore.Internal;
using ModelBase.Base.Logger;
using ModelBase.Base.Utils;
using ModelBase.Models.Device;
using ServiceStack;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ApiManagement.Base.Helper
{
    /// <summary>
    /// 预警
    /// </summary>
    public class WarningHelper
    {
        public static readonly string RedisReloadKey = "WarningHelper";
        private static string redisPre = "Warning";
        private static readonly string Debug = $"{redisPre}:Debug";
        private static readonly string LogIdKey = $"{redisPre}:LogId";

        private static readonly string dLockKey = $"{redisPre}:设备数据Lock";
        private static readonly string dIdKey = $"{redisPre}:设备数据Id";
        private static readonly string dDeviceKey = $"{redisPre}:设备数据Device";
        private static readonly string dTimeKey = $"{redisPre}:设备数据Time";
        private static readonly string dClearKey = $"{redisPre}:设备数据Clear";

        private static readonly string pLockKey = $"{redisPre}:生产数据Lock";
        private static readonly string pIdKey = $"{redisPre}:生产数据Id";
        private static readonly string pDayIdKey = $"{redisPre}:生产数据DayId";
        private static readonly string pDeviceKey = $"{redisPre}:生产数据Device";
        private static readonly string pTimeKey = $"{redisPre}:生产数据Time";
        private static readonly string pDayTimeKey = $"{redisPre}:生产数据日Time";

        private static readonly string sLockKey = $"{redisPre}:数据统计Lock";
        private static readonly string sTimeKey = $"{redisPre}:数据统计Time";

        public static List<Tuple<string, WarningItemType>> 生产数据字段 = new List<Tuple<string, WarningItemType>>();
        public static List<Tuple<string, WarningItemType>> 生产数据单次字段 = new List<Tuple<string, WarningItemType>>();
        public static List<Tuple<string, WarningItemType>> 生产数据合计字段 = new List<Tuple<string, WarningItemType>>();
        /// <summary>
        /// 预警项Id ItemId, 设备Id DeviceId, 数据类型 DataType, 预警类型， 预警设备分类 1研磨/抛光设备
        /// </summary>
        public static Dictionary<Tuple<int, int, WarningDataType, WarningType, int>, WarningCurrent> CurrentData;
        private static Timer _timer5S;
        private static int _dealLength = 2000;
        private static DateTime _startTime = DateTime.Today;
        private static int LogId;
        public static void Init()
        {
            try
            {
#if DEBUG
                Console.WriteLine("WarningHelper 调试模式已开启");
#else
                Console.WriteLine("WarningHelper 发布模式已开启");
#endif
                if (!RedisHelper.Exists(Debug))
                {
                    RedisHelper.SetForever(Debug, 0);
                }
                LogId = RedisHelper.Exists(LogIdKey) ?
                    RedisHelper.Get<int>(LogIdKey) :
                    ServerConfig.ApiDb.Query<int>("SELECT IFNULL(MAX(Id), 0) FROM `warning_log`;").FirstOrDefault();
                RedisHelper.SetForever(LogIdKey, LogId);

                生产数据字段.AddRange(EnumHelper.EnumToList<WarningItemType>()
                    //.Where(x => x.EnumValue <= 5)
                    .Select(x => new Tuple<string, WarningItemType>(x.Description, (WarningItemType)x.EnumValue)));
                生产数据单次字段.AddRange(EnumHelper.EnumToList<WarningItemType>()
                    .Where(x => x.EnumValue <= 5)
                    .Select(x => new Tuple<string, WarningItemType>(x.Description, (WarningItemType)x.EnumValue)));
                生产数据合计字段.AddRange(EnumHelper.EnumToList<WarningItemType>()
                    .Where(x => x.EnumValue >= 11 && x.EnumValue <= 15)
                    .Select(x => new Tuple<string, WarningItemType>(x.Description, (WarningItemType)x.EnumValue)));
                CurrentData = ServerConfig.ApiDb.Query<WarningCurrent>("SELECT a.*, b.VariableName Item, b.VariableTypeId, b.PointerAddress, b.`Precision`, c.WarningType, c.ClassId, c.`Name` SetName, c.`DeviceIds` DeviceIds FROM `warning_current` a " +
                                                                       "JOIN `data_name_dictionary` b ON a.DictionaryId = b.Id " +
                                                                       "JOIN `warning_set` c ON a.SetId = c.Id;")
                    .ToDictionary(x => new Tuple<int, int, WarningDataType, WarningType, int>(x.ItemId, x.DeviceId, x.DataType, x.WarningType, x.ClassId));
                LoadConfig();

                _timer5S = new Timer(DoSth_5s, null, 5000, 1000 * 5);
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
                "SELECT a.*, b.DataType, b.DeviceIds, b.ScriptId, c.VariableName Item, c.VariableTypeId, c.PointerAddress, c.`Precision`, b.WarningType, b.ClassId, b.`Name` SetName FROM `warning_set_item` a " +
                "JOIN `warning_set` b ON a.SetId = b.Id " +
                "JOIN `data_name_dictionary` c ON a.DictionaryId = c.Id WHERE a.MarkedDelete = 0 AND b.MarkedDelete = 0 AND c.MarkedDelete = 0 AND b.`Enable` = 1 AND b.`DataType` = @DataType;";
                var warningSetItemConfigs = ServerConfig.ApiDb.Query<WarningSetItemDetail>(sql, new { DataType = dataType });
                if (warningSetItemConfigs != null)
                {
                    foreach (var config in warningSetItemConfigs)
                    {
                        foreach (var deviceId in config.DeviceList)
                        {
                            var key = new Tuple<int, int, WarningDataType, WarningType, int>(config.Id, deviceId, config.DataType, config.WarningType, config.ClassId);
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
                                if (CurrentData[key].DataType == value.DataType && ClassExtension.HaveChange(CurrentData[key], value))
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
                    "SELECT b.*, a.*, b.`Name` SetName  FROM `warning_set_item` a JOIN `warning_set` b ON a.SetId = b.Id WHERE b.`Enable` = 1 AND b.DataType = @DataType AND a.MarkedDelete = 0 AND b.MarkedDelete = 0;";
                warningSetItemConfigs = ServerConfig.ApiDb.Query<WarningSetItemDetail>(sql, new { DataType = dataType });
                if (warningSetItemConfigs != null)
                {
                    foreach (var config in warningSetItemConfigs)
                    {
                        var key = new Tuple<int, int, WarningDataType, WarningType, int>(config.Id, 0, config.DataType, config.WarningType, config.ClassId);
                        var value = ClassExtension.ParentCopyToChild<WarningSetItemDetail, WarningCurrent>(config);
                        value.DeviceId = 0;
                        value.DeviceIds = "";
                        if (生产数据合计字段.All(x => x.Item2 != config.ItemType))
                        {
                            foreach (var deviceId in config.DeviceList)
                            {
                                key = new Tuple<int, int, WarningDataType, WarningType, int>(config.Id, deviceId, config.DataType, config.WarningType, config.ClassId);
                                value = ClassExtension.ParentCopyToChild<WarningSetItemDetail, WarningCurrent>(config);
                                value.DeviceId = deviceId;
                                value.DeviceIds = "";
                                value.ItemId = config.Id;
                                if (!CurrentData.ContainsKey(key))
                                {
                                    CurrentData.Add(key, value);
                                }
                                else
                                {
                                    if (CurrentData[key].DataType == value.DataType && ClassExtension.HaveChange(CurrentData[key], value))
                                    {
                                        CurrentData[key].Update(value);
                                    }
                                }
                            }
                        }
                        else
                        {
                            value.DeviceIds = config.DeviceIds;
                            value.ItemId = config.Id;
                            if (!CurrentData.ContainsKey(key))
                            {
                                CurrentData.Add(key, value);
                            }
                            else
                            {
                                if (CurrentData[key].DataType == value.DataType && ClassExtension.HaveChange(CurrentData[key], value))
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
                }
                #endregion
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        public static void UpdateConfig()
        {
            RedisHelper.PublishToTable(RedisReloadKey);
        }

        private static void DoSth_5s(object state)
        {
#if !DEBUG
            if (RedisHelper.Get<int>(Debug) != 0)
            {
                return;
            }
#endif
            //if (RedisHelper.Get<int>(Debug) != 0)
            //{
            //    return;
            //}

            if (!RedisHelper.Exists(LogIdKey))
            {
                LogId = ServerConfig.ApiDb.Query<int>("SELECT IFNULL(MAX(Id), 0) FROM `warning_log`;").FirstOrDefault();
                RedisHelper.SetForever(LogIdKey, LogId);
                return;
            }
            设备数据Warning();
            生产数据Warning();

            Statistic();
        }

        /// <summary>
        /// 设备数据
        /// </summary>
        private static void 设备数据Warning()
        {
            if (RedisHelper.SetIfNotExist(dLockKey, DateTime.Now.ToStr()))
            {
                try
                {
                    RedisHelper.SetExpireAt(dLockKey, DateTime.Now.AddMinutes(5));

                    var now = DateTime.Now;
                    var allDeviceList = new Dictionary<int, MonitoringProcess>();
                    if (RedisHelper.Exists(dDeviceKey))
                    {
                        allDeviceList.AddRange(MonitoringProcessHelper.GetMonitoringProcesses().ToDictionary(x => x.DeviceId));
                        var redisDeviceList = RedisHelper.Get<string>(dDeviceKey).ToClass<IEnumerable<MonitoringProcess>>();
                        if (redisDeviceList != null)
                        {
                            foreach (var redisDevice in redisDeviceList)
                            {
                                var deviceId = redisDevice.DeviceId;
                                if (allDeviceList.ContainsKey(deviceId))
                                {
                                    allDeviceList[deviceId].DeviceWarnings = redisDevice.DeviceWarnings;
                                }
                            }
                        }
                    }
                    else
                    {
                        allDeviceList.AddRange(MonitoringProcessHelper.GetMonitoringProcesses().ToDictionary(x => x.DeviceId));
                    }

                    var notDeals = WarningClearHelper.GetWarningClears(0);
                    if (notDeals.Any())
                    {
                        var sets = WarningSetHelper.GetMenus(notDeals.Select(x => x.SetId));
                        foreach (var deal in notDeals)
                        {
                            var set = sets.FirstOrDefault(x => x.Id == deal.SetId);
                            if (set == null)
                            {
                                continue;
                            }

                            deal.MarkedDateTime = now;
                            deal.OpTime = now;
                            deal.IsDeal = true;
                            foreach (var deviceId in deal.DeviceIdList)
                            {
                                if (allDeviceList.ContainsKey(deviceId))
                                {
                                    allDeviceList[deviceId].RemoveWarningData(set.DataType, deal.SetId);
                                }
                            }
                        }
                    }

                    var warningLogs = new List<WarningLog>();
                    var rTime = RedisHelper.Get<DateTime>(dTimeKey);
                    var dId = RedisHelper.Get<int>(dIdKey);
                    if (rTime == default(DateTime))
                    {
                        rTime = now;
                        RedisHelper.SetForever(dTimeKey, rTime.ToStr());
                        dId = ServerConfig.DataReadDb.Query<int>("SELECT Id FROM `npc_monitoring_analysis` WHERE SendTime < @rTime ORDER BY Id DESC LIMIT 1;",
                            new { rTime }).FirstOrDefault();
                        if (dId == 0)
                        {
                            RedisHelper.Remove(dTimeKey);
                        }
                        RedisHelper.SetForever(dIdKey, dId);
                        RedisHelper.Remove(dLockKey);
                        return;
                    }

                    var mData = ServerConfig.DataReadDb.Query<MonitoringData>(
                        "SELECT * FROM `npc_monitoring_analysis` WHERE Id > @dId AND UserSend = 0 ORDER BY Id LIMIT @limit;", new
                        {
                            dId,
                            limit = _dealLength
                        });
                    var endId = dId;

                    var bCal = CurrentData.Keys.Where(x => x.Item3 == WarningDataType.设备数据
                                                           && x.Item4 == WarningType.设备
                                                           && x.Item5 == 1).ToList();
                    if (mData.Any())
                    {
                        endId = mData.Max(x => x.Id);
                        mData = mData.OrderBy(x => x.SendTime);
                        if (endId > dId)
                        {
                            #region 预警检验
                            if (bCal.Any())
                            {
                                foreach (var data in mData)
                                {
                                    var deviceId = data.DeviceId;
                                    var currentTime = data.SendTime;
                                    var analysisData = data.AnalysisData;
                                    if (analysisData != null)
                                    {
                                        var warningKeys = bCal.Where(x => x.Item2 == deviceId).ToList();
                                        foreach (var warningKey in warningKeys)
                                        {
                                            // warningKey  预警项Id ItemId, 设备Id DeviceId, 数据类型 DataType, 预警类型， 预警设备分类 1研磨/抛光设备
                                            if (CurrentData.ContainsKey(warningKey))
                                            {
                                                var deviceWarning = CurrentData[warningKey];
                                                deviceWarning.CurrentTime = currentTime;
                                                if (GetValue(analysisData, deviceWarning, data.ScriptId, out var value))
                                                {
                                                    deviceWarning.Value = value;
                                                    var conditionInfos = new List<WarningConditionInfo>
                                                    {
                                                       new WarningConditionInfo( deviceWarning.Condition1, deviceWarning.Value1),
                                                       new WarningConditionInfo( deviceWarning.Condition2, deviceWarning.Value2),
                                                    };
                                                    if (MeetConditions(conditionInfos, deviceWarning.Logic, value))
                                                    {
                                                        deviceWarning.Trend = true;
                                                        deviceWarning.WarningData.Add(new WarningData(currentTime, value));
                                                        deviceWarning.UpdateValues();
                                                    }
                                                    else
                                                    {
                                                        var warningLog = ClassExtension.ParentCopyToChild<WarningCurrent, WarningLog>(deviceWarning);
                                                        if (deviceWarning.Interval == WarningInterval.连续 && deviceWarning.Counting)
                                                        {
                                                            //不满足消除一次
                                                            warningLog.Id = Interlocked.Increment(ref LogId);
                                                            warningLog.IsWarning = false;
                                                            warningLog.WarningTime = currentTime;
                                                            warningLog.WarningData = deviceWarning.WarningData.ToList();
                                                            warningLog.UpdateValues();
                                                            warningLogs.Add(warningLog);

                                                            deviceWarning.Trend = false;
                                                            deviceWarning.WarningData.RemoveAt(0);
                                                            deviceWarning.UpdateValues();
                                                        }
                                                    }
                                                }

                                                if (deviceWarning.Interval != WarningInterval.每次 && deviceWarning.Interval != WarningInterval.连续)
                                                {
                                                    //距离第一次满足条件已经过去的时间
                                                    var totalSeconds = (int)(currentTime - deviceWarning.StartTime).TotalSeconds;
                                                    while (deviceWarning.Counting && totalSeconds > deviceWarning.TotalConfigSeconds)
                                                    {
                                                        if (deviceWarning.Trend)
                                                        {
                                                            var warningLog = ClassExtension.ParentCopyToChild<WarningCurrent, WarningLog>(deviceWarning);
                                                            warningLog.Id = Interlocked.Increment(ref LogId);
                                                            warningLog.IsWarning = false;
                                                            warningLog.WarningTime = currentTime;
                                                            warningLog.WarningData = deviceWarning.WarningData.ToList();
                                                            warningLog.UpdateValues();
                                                            warningLogs.Add(warningLog);
                                                            deviceWarning.Trend = false;
                                                        }
                                                        deviceWarning.WarningData.RemoveAt(0);
                                                        deviceWarning.UpdateValues();
                                                        totalSeconds = (int)(currentTime - deviceWarning.StartTime).TotalSeconds;
                                                    }
                                                }

                                                if (deviceWarning.Current >= deviceWarning.Count)
                                                {
                                                    var warningLog = ClassExtension.CopyTo<WarningCurrent, WarningLog>(deviceWarning);
                                                    warningLog.Id = Interlocked.Increment(ref LogId);
                                                    warningLog.IsWarning = true;
                                                    warningLog.WarningTime = currentTime;
                                                    if (warningLogs.Any(x => x.ItemId == warningLog.ItemId
                                                                             && x.WarningTime == warningLog.WarningTime))
                                                    {
                                                        var ss = warningLogs.Where(x => x.ItemId == warningLog.ItemId
                                                                                        && x.WarningTime == warningLog.WarningTime).ToList();
                                                    }
                                                    warningLogs.Add(warningLog);
                                                    allDeviceList[deviceId].UpdateWarningData(WarningDataType.设备数据, warningLog);
                                                    deviceWarning.Reset();
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                foreach (var (deviceId, _) in allDeviceList)
                                {
                                    allDeviceList[deviceId].DeviceWarningList.Clear();
                                    allDeviceList[deviceId].UpdateWarningData();
                                }
                            }
                            #endregion
                        }
                    }
                    else
                    {
                        if (!bCal.Any())
                        {
                            endId = ServerConfig.DataReadDb.Query<int>("SELECT Id FROM `npc_monitoring_analysis` WHERE SendTime < @now ORDER BY Id DESC LIMIT 1;",
                            new { now }).FirstOrDefault();
                        }
                    }
                    RedisHelper.SetForever(dIdKey, endId);

                    RedisHelper.SetForever(dDeviceKey, allDeviceList.Values
                        .Select(ClassExtension.CopyTo<MonitoringProcess, MonitoringProcessWarning>).OrderBy(x => x.DeviceId).ToJSON());

                    if (notDeals.Any())
                    {
                        WarningClearHelper.Instance.Update(notDeals);
                    }

                    var update = allDeviceList.Values.Where(x => x.WarningChange);
                    if (update.Any())
                    {
                        Task.Run(() =>
                        {
                            ServerConfig.ApiDb.Execute(
                                "UPDATE npc_proxy_link SET `DeviceWarnings` = @DeviceWarnings WHERE `DeviceId` = @DeviceId;",
                                update);
                        });
                    }
                    if (CurrentData.Any())
                    {
                        ServerConfig.ApiDb.Execute(
                            "INSERT INTO `warning_current` (`CurrentTime`, `WarningType`, `ClassId`, `ItemId`, `DeviceId`, `DataType`, `SetId`, `ScriptId`, `CategoryId`, `StartTime`, `EndTime`, " +
                            "`Frequency`, `Interval`, `Count`, `Condition1`, `Value1`, `Logic`, `Condition2`, `Value2`, `DictionaryId`, `Current`, `Trend`, `Values`, `DeviceIds`) " +
                            "VALUES (@CurrentTime, @WarningType, @ClassId, @ItemId, @DeviceId, @DataType, @SetId, @ScriptId, @CategoryId, @StartTime, " +
                            "@EndTime, @Frequency, @Interval, @Count, @Condition1, @Value1, @Logic, @Condition2, @Value2, @DictionaryId, @Current, @Trend, @Values, @DeviceIds) " +
                            "ON DUPLICATE KEY UPDATE `CurrentTime` = @CurrentTime, `ScriptId` = @ScriptId, `CategoryId` = @CategoryId, `StartTime` = @StartTime, `EndTime` = @EndTime, " +
                            "`Frequency` = @Frequency, `Interval` = @Interval, `Count` = @Count, `Condition1` = @Condition1, `Value1` = @Value1, `Logic` = @Logic, `Condition2` = @Condition2, " +
                            "`Value2` = @Value2, `DictionaryId` = @DictionaryId, `Current` = @Current, `Trend` = @Trend, `Values` = @Values, `DeviceIds` = @DeviceIds;",
                            CurrentData.Where(x => x.Key.Item3 == WarningDataType.设备数据 && x.Key.Item4 == WarningType.设备 && x.Key.Item5 == 1).Select(y => y.Value));
                    }
                    RedisHelper.SetForever(LogIdKey, LogId);
                    if (warningLogs.Any())
                    {
                        try
                        {
                            WarningLogHelper.Instance.Add(warningLogs.OrderBy(x => x.Id));
                        }
                        catch (Exception e)
                        {
                            Log.Error(e);
                        }
                    }
                    RedisHelper.SetForever(dTimeKey, now.ToStr());
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                RedisHelper.Remove(dLockKey);
            }
        }

        public static bool GetValue(DeviceData deviceData, WarningCurrent warning, int scriptId, out decimal v)
        {
            List<int> bl = null;
            switch (warning.VariableTypeId)
            {
                //变量
                case 1: bl = deviceData.vals; break;
                //输入
                case 2: bl = deviceData.ins; break;
                //输出
                case 3: bl = deviceData.outs; break;
            }
            if (bl != null && warning.PointerAddress > 0 && bl.Count > warning.PointerAddress - 1)
            {
                var chu = Math.Pow(10, warning.Precision);
                v = (decimal)(bl.ElementAt(warning.PointerAddress - 1) / chu);
                return true;
            }

            v = 0;
            return false;
        }

        /// <summary>
        /// 生产数据
        /// </summary>
        private static void 生产数据Warning()
        {
            if (RedisHelper.SetIfNotExist(pLockKey, DateTime.Now.ToStr()))
            {
                try
                {
                    RedisHelper.SetExpireAt(pLockKey, DateTime.Now.AddMinutes(10));

                    var warningLogs = new List<WarningLog>();
                    var allDeviceList = new Dictionary<int, MonitoringProcess>();
                    if (RedisHelper.Exists(pDeviceKey))
                    {
                        allDeviceList.AddRange(MonitoringProcessHelper.GetMonitoringProcesses().ToDictionary(x => x.DeviceId));
                        var redisDeviceList = RedisHelper.Get<string>(pDeviceKey).ToClass<IEnumerable<MonitoringProcess>>();
                        if (redisDeviceList != null)
                        {
                            foreach (var redisDevice in redisDeviceList)
                            {
                                var deviceId = redisDevice.DeviceId;
                                if (allDeviceList.ContainsKey(deviceId))
                                {
                                    allDeviceList[deviceId].ProductWarnings = redisDevice.ProductWarnings;
                                }
                            }
                        }
                    }
                    else
                    {
                        allDeviceList.AddRange(MonitoringProcessHelper.GetMonitoringProcesses().ToDictionary(x => x.DeviceId));
                    }

                    var now = DateTime.Now;
                    var rTime = RedisHelper.Get<DateTime>(pTimeKey);
                    var pId = RedisHelper.Get<int>(pIdKey);
                    //var pDayId = RedisHelper.Get<int>(pDayIdKey);
                    if (rTime == default(DateTime))
                    {
                        rTime = now;
                        RedisHelper.SetForever(pTimeKey, rTime.ToStr());
                        pId = ServerConfig.ApiDb.Query<int>("SELECT IFNULL(MAX(Id), 0) FROM `flowcard_report` WHERE Time < @rTime AND State != 0 ORDER BY Id DESC LIMIT 1;",
                            new { rTime }).FirstOrDefault();
                        if (pId == 0)
                        {
                            RedisHelper.Remove(dTimeKey);
                        }
                        RedisHelper.SetForever(pIdKey, pId);
                        RedisHelper.SetForever(pDayTimeKey, rTime.AddDays(-1));
                        RedisHelper.Remove(pLockKey);
                        return;
                    }
                    #region 预警检验
                    var keys = CurrentData.Keys.Where(x => x.Item3 == WarningDataType.生产数据
                                                           && x.Item4 == WarningType.设备
                                                           && x.Item5 == 1).ToList();

                    var singleData = CurrentData.Where(x => keys.Contains(x.Key))
                        .Where(x => 生产数据单次字段.Any(y => y.Item2 == x.Value.ItemType)).ToDictionary(x => x.Key, x => x.Value);
                    #region 预警检验  单次加工相关预警
                    var endId = pId;
                    var mData = ServerConfig.ApiDb.Query<FlowCardReport>("SELECT * FROM `flowcard_report` WHERE Id > @pId AND State != 0 ORDER BY Id LIMIT @limit;",
                        new
                        {
                            pId,
                            limit = _dealLength
                        });

                    if (mData.Any())
                    {
                        endId = mData.Max(x => x.Id);
                        mData = mData.OrderBy(x => x.Time);
                        if (singleData.Any())
                        {
                            if (endId > pId)
                            {
                                foreach (var (deviceId, _) in allDeviceList)
                                {
                                    var reports = mData.Where(x => x.DeviceId == deviceId).OrderBy(x => x.Time);
                                    if (reports.Any())
                                    {
                                        var t = default(DateTime);
                                        foreach (var report in reports)
                                        {
                                            var time = report.Time;
                                            if (t > time)
                                            {
                                                throw new Exception("1111");
                                            }
                                            t = time;
                                            var deviceSingleData = singleData.Where(x => x.Key.Item2 == deviceId);
                                            //Console.WriteLine($"{deviceId}, {deviceSingleData.Count()}");
                                            foreach (var (_, deviceWarning) in deviceSingleData)
                                            {
                                                deviceWarning.CurrentTime = time;
                                                var f = true;
                                                switch (deviceWarning.ItemType)
                                                {
                                                    //[Description("单次加工数")]
                                                    case WarningItemType.SingleTotal:
                                                        deviceWarning.Value = report.Total;
                                                        break;
                                                    //[Description("单次合格数")]
                                                    case WarningItemType.SingleQualified:
                                                        deviceWarning.Value = report.HeGe;
                                                        break;
                                                    //[Description("单次次品数")]
                                                    case WarningItemType.SingleUnqualified:
                                                        deviceWarning.Value = report.LiePian;
                                                        break;
                                                    //[Description("单次合格率(%)")]
                                                    case WarningItemType.SingleQualifiedRate:
                                                        deviceWarning.Value = report.QualifiedRate;
                                                        break;
                                                    //[Description("单次次品率(%)")]
                                                    case WarningItemType.SingleUnqualifiedRate:
                                                        deviceWarning.Value = report.UnqualifiedRate;
                                                        break;
                                                    default:
                                                        f = false;
                                                        break;
                                                }

                                                if (f)
                                                {
                                                    var conditionInfos = new List<WarningConditionInfo>
                                                    {
                                                        new WarningConditionInfo(deviceWarning.Condition1, deviceWarning.Value1),
                                                        new WarningConditionInfo(deviceWarning.Condition2, deviceWarning.Value2),
                                                    };
                                                    if (MeetConditions(conditionInfos, deviceWarning.Logic, deviceWarning.Value))
                                                    {
                                                        deviceWarning.Trend = true;
                                                        var wd = new WarningData(time, deviceWarning.Value);
                                                        wd.AddParam(report.FlowCard);
                                                        wd.AddParam(report.Processor);
                                                        deviceWarning.WarningData.Add(wd);
                                                        deviceWarning.UpdateValues();
                                                    }
                                                    else
                                                    {
                                                        var warningLog = ClassExtension.ParentCopyToChild<WarningCurrent, WarningLog>(deviceWarning);
                                                        var clear = true;
                                                        if (deviceWarning.Interval == WarningInterval.连续 && deviceWarning.Counting)
                                                        {
                                                            clear = false;
                                                            //不满足消除一次
                                                            warningLog.Id = Interlocked.Increment(ref LogId);
                                                            warningLog.IsWarning = false;
                                                            warningLog.WarningTime = time;
                                                            warningLog.WarningData = deviceWarning.WarningData.ToList();
                                                            warningLog.UpdateValues();
                                                            warningLogs.Add(warningLog);

                                                            deviceWarning.Trend = false;
                                                            deviceWarning.WarningData.RemoveAt(0);
                                                            deviceWarning.UpdateValues();
                                                            if (deviceWarning.Current == 0)
                                                            {
                                                                clear = true;
                                                            }
                                                        }
                                                        if (clear)
                                                        {
                                                            var warning = allDeviceList[deviceId].ProductWarningList.Values
                                                                .FirstOrDefault(x => x.ItemId == deviceWarning.ItemId);

                                                            if (warning != null)
                                                            {
                                                                warningLog.Id = warning.LogId;
                                                                warningLog.ItemId = warning.ItemId;
                                                                allDeviceList[deviceId].UpdateWarningData(WarningDataType.生产数据, warningLog, false);
                                                            }
                                                        }
                                                    }

                                                    if (deviceWarning.Interval != WarningInterval.每次 && deviceWarning.Interval != WarningInterval.连续)
                                                    {
                                                        //距离第一次满足条件已经过去的时间
                                                        var totalSeconds = (int)(time - deviceWarning.StartTime).TotalSeconds;
                                                        while (deviceWarning.Counting && totalSeconds > deviceWarning.TotalConfigSeconds)
                                                        {
                                                            if (deviceWarning.Trend)
                                                            {
                                                                var warningLog = ClassExtension.ParentCopyToChild<WarningCurrent, WarningLog>(deviceWarning);
                                                                warningLog.Id = Interlocked.Increment(ref LogId);
                                                                warningLog.IsWarning = false;
                                                                warningLog.WarningTime = time;
                                                                warningLog.WarningData = deviceWarning.WarningData.ToList();
                                                                warningLog.UpdateValues();
                                                                warningLogs.Add(warningLog);
                                                                deviceWarning.Trend = false;
                                                            }

                                                            deviceWarning.WarningData.RemoveAt(0);
                                                            deviceWarning.UpdateValues();
                                                            totalSeconds = (int)(time - deviceWarning.StartTime).TotalSeconds;
                                                        }
                                                    }

                                                    if (deviceWarning.Current >= deviceWarning.Count)
                                                    {
                                                        var warningLog = ClassExtension.ParentCopyToChild<WarningCurrent, WarningLog>(deviceWarning);
                                                        warningLog.Id = Interlocked.Increment(ref LogId);
                                                        warningLog.IsWarning = true;
                                                        warningLog.WarningTime = time;
                                                        warningLogs.Add(warningLog);
                                                        allDeviceList[deviceId].UpdateWarningData(WarningDataType.生产数据, warningLog);
                                                        deviceWarning.Reset();
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!singleData.Any())
                        {
                            endId = ServerConfig.ApiDb.Query<int>("SELECT IFNULL(MAX(Id), 0) FROM `flowcard_report` WHERE Time < @now AND State != 0 ORDER BY Id DESC LIMIT 1;",
                            new { now }).FirstOrDefault();
                        }
                    }
                    RedisHelper.SetForever(pIdKey, endId);
                    #endregion

                    #region 预警检验  单台日加工相关预警
                    var pDayTime = RedisHelper.Get<DateTime>(pDayTimeKey);
                    if (!pDayTime.InSameDay(now) && (now - pDayTime).TotalMinutes >= 1)
                    {
                        var dayData = CurrentData.Where(x => keys.Contains(x.Key))
                            .Where(x => 生产数据单次字段.All(y => y.Item2 != x.Value.ItemType)).ToDictionary(x => x.Key, x => x.Value);
                        if (dayData.Any())
                        {
                            var deviceIds = dayData.SelectMany(x => x.Value.DeviceList).ToList();
                            deviceIds.AddRange(dayData.Select(x => x.Value.DeviceId));
                            deviceIds = deviceIds.Where(x => x != 0).Distinct().ToList();
                            var processes = MonitoringProcessHelper.GetMonitoringProcesses(2, pDayTime, deviceIds).ToDictionary(x => x.DeviceId);
                            if (processes.Any())
                            {
                                foreach (var (_, deviceWarning) in dayData)
                                {
                                    var deviceId = deviceWarning.DeviceId;
                                    if (!allDeviceList.ContainsKey(deviceId))
                                    {
                                        continue;
                                    }

                                    deviceWarning.CurrentTime = pDayTime;
                                    var f = true;
                                    IEnumerable<MonitoringProcess> deviceProcesses = null;
                                    MonitoringProcess p;
                                    switch (deviceWarning.ItemType)
                                    {
                                        //[Description("单台日加工数")]
                                        case WarningItemType.DeviceTotal:
                                            deviceWarning.Value = processes.ContainsKey(deviceId) ? processes[deviceId].DayTotal : 0;
                                            break;
                                        //[Description("单台日合格数")]
                                        case WarningItemType.DeviceQualified:
                                            deviceWarning.Value = processes.ContainsKey(deviceId) ? processes[deviceId].DayQualified : 0;
                                            break;
                                        //[Description("单台日次品数")]
                                        case WarningItemType.DeviceUnqualified:
                                            deviceWarning.Value = processes.ContainsKey(deviceId) ? processes[deviceId].DayUnqualified : 0;
                                            break;
                                        //[Description("单台日合格率(%)")]
                                        case WarningItemType.DeviceQualifiedRate:
                                            deviceWarning.Value = processes.ContainsKey(deviceId) ? processes[deviceId].DayQualifiedRate : 0;
                                            break;
                                        //[Description("单台日次品率(%)")]
                                        case WarningItemType.DeviceUnqualifiedRate:
                                            deviceWarning.Value = processes.ContainsKey(deviceId) ? processes[deviceId].DayUnqualifiedRate : 0;
                                            break;

                                        //[Description("已选设备日加工数")]
                                        case WarningItemType.DayTotal:
                                            deviceProcesses = processes.Where(x =>
                                                deviceWarning.DeviceList.Contains(x.Key)).Select(x => x.Value);
                                            deviceWarning.Value = deviceProcesses.Sum(x => x.DayTotal);
                                            break;
                                        //[Description("已选设备日合格数")]
                                        case WarningItemType.DayQualified:
                                            deviceProcesses = processes.Where(x =>
                                                deviceWarning.DeviceList.Contains(x.Key)).Select(x => x.Value);
                                            deviceWarning.Value = deviceProcesses.Sum(x => x.DayQualified);
                                            break;
                                        //[Description("已选设备日次品数")]
                                        case WarningItemType.DayUnqualified:
                                            deviceProcesses = processes.Where(x =>
                                                deviceWarning.DeviceList.Contains(x.Key)).Select(x => x.Value);
                                            deviceWarning.Value = deviceProcesses.Sum(x => x.DayUnqualified);
                                            break;
                                        //[Description("已选设备日合格率(%)")]
                                        case WarningItemType.DayQualifiedRate:
                                            deviceProcesses = processes.Where(x =>
                                                deviceWarning.DeviceList.Contains(x.Key)).Select(x => x.Value);
                                            p = new MonitoringProcess
                                            {
                                                DayTotal = deviceProcesses.Sum(x => x.DayTotal),
                                                DayQualified = deviceProcesses.Sum(x => x.DayQualified),
                                                DayUnqualified = deviceProcesses.Sum(x => x.DayUnqualified),
                                            };
                                            deviceWarning.Value = p.DayQualifiedRate;
                                            break;
                                        //[Description("已选设备日次品率(%)")]
                                        case WarningItemType.DayUnqualifiedRate:
                                            deviceProcesses = processes.Where(x =>
                                                deviceWarning.DeviceList.Contains(x.Key)).Select(x => x.Value);
                                            p = new MonitoringProcess
                                            {
                                                DayTotal = deviceProcesses.Sum(x => x.DayTotal),
                                                DayQualified = deviceProcesses.Sum(x => x.DayQualified),
                                                DayUnqualified = deviceProcesses.Sum(x => x.DayUnqualified),
                                            };
                                            deviceWarning.Value = p.DayUnqualifiedRate;
                                            break;
                                        default:
                                            f = false;
                                            break;
                                    }

                                    if (f)
                                    {
                                        var conditionInfos = new List<WarningConditionInfo>
                                            {
                                                new WarningConditionInfo(deviceWarning.Condition1, deviceWarning.Value1),
                                                new WarningConditionInfo(deviceWarning.Condition2, deviceWarning.Value2),
                                            };
                                        if (MeetConditions(conditionInfos, deviceWarning.Logic, deviceWarning.Value))
                                        {
                                            deviceWarning.Trend = true;
                                            deviceWarning.WarningData.Add(new WarningData(pDayTime, deviceWarning.Value));
                                            deviceWarning.UpdateValues();
                                        }
                                        else
                                        {
                                            if (生产数据合计字段.All(x => x.Item2 != deviceWarning.ItemType))
                                            {
                                                var warningLog = ClassExtension.ParentCopyToChild<WarningCurrent, WarningLog>(deviceWarning);
                                                var clear = true;
                                                if (deviceWarning.Interval == WarningInterval.连续 && deviceWarning.Counting)
                                                {
                                                    clear = false;
                                                    //不满足消除一次
                                                    warningLog.Id = Interlocked.Increment(ref LogId);
                                                    warningLog.IsWarning = false;
                                                    warningLog.WarningTime = pDayTime;
                                                    warningLog.WarningData = deviceWarning.WarningData.ToList();
                                                    warningLog.UpdateValues();
                                                    warningLogs.Add(warningLog);

                                                    deviceWarning.Trend = false;
                                                    deviceWarning.WarningData.RemoveAt(0);
                                                    deviceWarning.UpdateValues();
                                                    if (deviceWarning.Current == 0)
                                                    {
                                                        clear = true;
                                                    }
                                                }
                                                if (clear)
                                                {
                                                    var warning = allDeviceList[deviceId].ProductWarningList.Values
                                                        .FirstOrDefault(x => x.ItemId == deviceWarning.ItemId);

                                                    if (warning != null)
                                                    {
                                                        warningLog.Id = warning.LogId;
                                                        warningLog.ItemId = warning.ItemId;
                                                        allDeviceList[deviceId].UpdateWarningData(WarningDataType.生产数据, warningLog, false);
                                                    }
                                                }
                                            }
                                        }

                                        if (deviceWarning.Interval != WarningInterval.每次 && deviceWarning.Interval != WarningInterval.连续)
                                        {
                                            //距离第一次满足条件已经过去的时间
                                            var totalSeconds = (int)(pDayTime - deviceWarning.StartTime).TotalSeconds;
                                            while (deviceWarning.Counting && totalSeconds > deviceWarning.TotalConfigSeconds)
                                            {
                                                if (deviceWarning.Trend)
                                                {
                                                    var warningLog = ClassExtension.ParentCopyToChild<WarningCurrent, WarningLog>(deviceWarning);
                                                    warningLog.Id = Interlocked.Increment(ref LogId);
                                                    warningLog.IsWarning = false;
                                                    warningLog.WarningTime = pDayTime;
                                                    warningLog.WarningData = deviceWarning.WarningData.ToList();
                                                    warningLog.UpdateValues();
                                                    warningLogs.Add(warningLog);
                                                    deviceWarning.Trend = false;
                                                }

                                                deviceWarning.WarningData.RemoveAt(0);
                                                deviceWarning.UpdateValues();
                                                totalSeconds = (int)(pDayTime - deviceWarning.StartTime).TotalSeconds;
                                            }
                                        }

                                        if (deviceWarning.Current >= deviceWarning.Count)
                                        {
                                            var warningLog = ClassExtension.ParentCopyToChild<WarningCurrent, WarningLog>(deviceWarning);
                                            warningLog.Id = Interlocked.Increment(ref LogId);
                                            warningLog.IsWarning = true;
                                            warningLog.WarningTime = pDayTime;
                                            warningLogs.Add(warningLog);
                                            if (生产数据合计字段.All(x => x.Item2 != deviceWarning.ItemType))
                                            {
                                                allDeviceList[deviceId].UpdateWarningData(WarningDataType.生产数据, warningLog);
                                            }
                                            deviceWarning.Reset();
                                        }
                                    }
                                }
                                RedisHelper.SetForever(pDayTimeKey, now.Date.ToStr());
                            }
                        }
                        else
                        {
                        }
                    }

                    #endregion

                    if (!keys.Any())
                    {
                        foreach (var (deviceId, _) in allDeviceList)
                        {
                            allDeviceList[deviceId].ProductWarningList.Clear();
                            allDeviceList[deviceId].UpdateWarningData(WarningDataType.生产数据);
                        }
                    }

                    #endregion

                    RedisHelper.SetForever(pDeviceKey, allDeviceList.Values
                        .Select(ClassExtension.CopyTo<MonitoringProcess, MonitoringProcessWarning>).OrderBy(x => x.DeviceId).ToJSON());

                    var update = allDeviceList.Values.Where(x => x.WarningChange);
                    if (update.Any())
                    {
                        Task.Run(() =>
                        {
                            ServerConfig.ApiDb.Execute(
                                "UPDATE npc_proxy_link SET `ProductWarnings` = @ProductWarnings WHERE `DeviceId` = @DeviceId;",
                                update);
                        });
                    }

                    if (CurrentData.Any())
                    {
                        ServerConfig.ApiDb.Execute(
                            "INSERT INTO `warning_current` (`CurrentTime`, `WarningType`, `ClassId`, `ItemId`, `DeviceId`, `DataType`, `SetId`, `ScriptId`, `CategoryId`, `StartTime`, `EndTime`, " +
                            "`Frequency`, `Interval`, `Count`, `Condition1`, `Value1`, `Logic`, `Condition2`, `Value2`, `DictionaryId`, `Current`, `Trend`, `Values`, `DeviceIds`) " +
                            "VALUES (@CurrentTime, @WarningType, @ClassId, @ItemId, @DeviceId, @DataType, @SetId, @ScriptId, @CategoryId, @StartTime, " +
                            "@EndTime, @Frequency, @Interval, @Count, @Condition1, @Value1, @Logic, @Condition2, @Value2, @DictionaryId, @Current, @Trend, @Values, @DeviceIds) " +
                            "ON DUPLICATE KEY UPDATE `CurrentTime` = @CurrentTime, `ScriptId` = @ScriptId, `CategoryId` = @CategoryId, `StartTime` = @StartTime, `EndTime` = @EndTime, " +
                            "`Frequency` = @Frequency, `Interval` = @Interval, `Count` = @Count, `Condition1` = @Condition1, `Value1` = @Value1, `Logic` = @Logic, `Condition2` = @Condition2, " +
                            "`Value2` = @Value2, `DictionaryId` = @DictionaryId, `Current` = @Current, `Trend` = @Trend, `Values` = @Values, `DeviceIds` = @DeviceIds;",
                            CurrentData.Where(x => x.Key.Item3 == WarningDataType.生产数据 && x.Key.Item4 == WarningType.设备 && x.Key.Item5 == 1).Select(y => y.Value));
                    }

                    RedisHelper.SetForever(LogIdKey, LogId);
                    if (warningLogs.Any())
                    {
                        WarningLogHelper.Instance.Add(warningLogs);
                    }
                    RedisHelper.SetForever(pTimeKey, now.ToStr());
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                RedisHelper.Remove(pLockKey);
            }
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
                var r = true;
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

        private static void Statistic()
        {
#if !DEBUG
            if (RedisHelper.Get<int>(Debug) != 0)
            {
                return;
            }
#endif
            if (RedisHelper.SetIfNotExist(sLockKey, DateTime.Now.ToStr()))
            {
                try
                {
                    RedisHelper.SetExpireAt(sLockKey, DateTime.Now.AddMinutes(10));
                    var items = WarningSetItemHelper.Instance.GetAll<WarningSetItem>();
                    if (items.Any())
                    {
                        var today = DateTime.Today;
                        var rTime = RedisHelper.Get<DateTime>(sTimeKey);
                        if (rTime == default(DateTime))
                        {
                            rTime = today;
                        }
                        var startTime = rTime.DayBeginTime();
                        var endTime = rTime.AddDays(1).DayBeginTime();
                        var args = new List<Tuple<string, string, dynamic>>
                        {
                            new Tuple<string, string, dynamic>("IsWarning", "=", true),
                            new Tuple<string, string, dynamic>("WarningTime", ">=", startTime),
                            new Tuple<string, string, dynamic>("WarningTime", "<=", endTime)
                        };
                        if (!rTime.InSameDay(today))
                        {
                            rTime = rTime.AddDays(1);
                        }
                        var logs = WarningLogHelper.Instance.CommonGet<WarningLog>(args).OrderBy(x => x.WarningTime);
                        if (logs.Any())
                        {
                            var es = EnumHelper.EnumToList<WarningStatisticTime>(true);
                            foreach (var e in es)
                            {
                                var timeEnum = (WarningStatisticTime)e.EnumValue;
                                //public WarningStatistic(DateTime time, int setId, string setName, int itemId, string item, string range, int count)
                                var statistics = logs.GroupBy(x => new { WarningTime = GetWarningTime(timeEnum, x.WarningTime), x.SetId, x.SetName, x.ItemId, x.Item, x.Range })
                                    .Select(x => new WarningStatistic(x.Key.WarningTime, x.Key.SetId, x.Key.SetName, x.Key.ItemId, x.Key.Item, x.Key.Range, x.Count()));
                                var itemIds = statistics.Select(x => x.ItemId).Distinct();
                                if (itemIds.Any())
                                {
                                    var oldStatistics = WarningStatisticHelper.GetWarningStatistic(timeEnum, startTime, endTime, itemIds);
                                    if (WarningStatisticHelper.Tables.ContainsKey(timeEnum) && statistics.Any())
                                    {
                                        var update = statistics.Where(x =>
                                            oldStatistics.Any(y => y.Time == x.Time && y.ItemId == x.ItemId)
                                            && ClassExtension.HaveChange(oldStatistics.First(y => y.Time == x.Time && y.ItemId == x.ItemId), x));

                                        if (update.Any())
                                        {
                                            WarningStatisticHelper.Update(timeEnum, update);
                                        }
                                        var add = statistics.Where(x => !oldStatistics.Any(y => y.Time == x.Time && y.ItemId == x.ItemId));
                                        if (add.Any())
                                        {
                                            WarningStatisticHelper.Add(timeEnum, add);
                                        }
                                    }
                                }
                            }
                        }
                        RedisHelper.SetForever(sTimeKey, rTime.ToDateStr());
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                RedisHelper.Remove(sLockKey);
            }
        }

        private static DateTime GetWarningTime(WarningStatisticTime timeEnum, DateTime time)
        {
            switch (timeEnum)
            {
                case WarningStatisticTime.分:
                    return time.NoSecond();
                case WarningStatisticTime.时:
                    return time.NoMinute();
                case WarningStatisticTime.天:
                    return time.NoHour();
            }

            return time;
        }

        /// <summary>
        /// 获取设备当前预警状态
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<MonitoringProcess> GetMonitoringProcesses(IEnumerable<int> deviceIds = null)
        {
            var allDeviceList = MonitoringProcessHelper.GetMonitoringProcesses(deviceIds).ToDictionary(x => x.DeviceId);
            if (RedisHelper.Exists(dDeviceKey))
            {
                var redisDeviceList = RedisHelper.Get<string>(dDeviceKey).ToClass<IEnumerable<MonitoringProcess>>();
                if (redisDeviceList != null)
                {
                    foreach (var redisDevice in redisDeviceList)
                    {
                        var deviceId = redisDevice.DeviceId;
                        if (allDeviceList.ContainsKey(deviceId))
                        {
                            allDeviceList[deviceId].DeviceWarnings = redisDevice.DeviceWarnings;
                        }
                    }
                }
            }

            if (RedisHelper.Exists(pDeviceKey))
            {
                allDeviceList.AddRange(MonitoringProcessHelper.GetMonitoringProcesses(deviceIds).ToDictionary(x => x.DeviceId));
                var redisDeviceList = RedisHelper.Get<string>(pDeviceKey).ToClass<IEnumerable<MonitoringProcess>>();
                if (redisDeviceList != null)
                {
                    foreach (var redisDevice in redisDeviceList)
                    {
                        var deviceId = redisDevice.DeviceId;
                        if (allDeviceList.ContainsKey(deviceId))
                        {
                            allDeviceList[deviceId].ProductWarnings = redisDevice.ProductWarnings;
                        }
                    }
                }
            }

            return allDeviceList.Values;
        }
    }
}
