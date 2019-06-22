using ApiManagement.Base.Control;
using ApiManagement.Base.Server;
using ApiManagement.Models;
using ApiManagement.Models.Analysis;
using Microsoft.Extensions.Configuration;
using ModelBase.Base.Logger;
using ModelBase.Base.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ApiManagement.Base.Helper
{
    /// <summary>
    /// 数据解析
    /// </summary>
    public partial class DataStorageHelper
    {
        private static Timer _analysis;
        private static Timer _statisticProcess;
        private static int _dealLength = 1000;
        private static int _due = 3;
        private static bool _isSupplementaryData;
        public static void Init(IConfiguration configuration)
        {
            _analysis = new Timer(Analysis, null, 5000, 2000);
            _statisticProcess = new Timer(Process, null, 10000, 1000);
        }
        private static void Analysis(object state)
        {
            var _pre = "Analysis";
            var lockKey = $"{_pre}:Lock";
            var redisKey = $"{_pre}:Id";
            if (ServerConfig.RedisHelper.SetIfNotExist(lockKey, "lock"))
            {
                var startId = ServerConfig.RedisHelper.Get<int>(redisKey);
                var mData = ServerConfig.DataStorageDb.Query<dynamic>(
                    "SELECT * FROM `npc_monitoring_data` WHERE Id > @Id ORDER BY Id LIMIT @limit;", new
                    {
                        Id = startId,
                        limit = _dealLength
                    });
                if (mData.Any())
                {
                    var endId = mData.Last().Id;
                    if (endId > startId)
                    {
                        ServerConfig.RedisHelper.SetForever(redisKey, endId);
                        var nullData = new DeviceData().ToJSON();
                        foreach (var data in mData)
                        {
                            var infoMessagePacket = new DeviceInfoMessagePacket(data.ValNum, data.InNum, data.OutNum);
                            var analysisData = infoMessagePacket.Deserialize(data.Data);
                            data.Data = analysisData == null ? nullData : JsonConvert.SerializeObject(analysisData);
                        }

                        Task.Run(() =>
                        {
                            ServerConfig.ApiDb.ExecuteAsync(
                                "INSERT INTO npc_monitoring_analysis (`SendTime`, `DeviceId`, `ScriptId`, `Ip`, `Port`, `Data`) " +
                                "VALUES (@SendTime, @DeviceId, @ScriptId, @Ip, @Port, @Data);",
                                mData);
                        });
                    }
                }
                ServerConfig.RedisHelper.Remove(lockKey);
            }
        }

        private static void Process(object state)
        {
            var _pre = "Process";
            var lockKey = $"{_pre}:Lock";
            var redisKey = $"{_pre}:Time";
            var stateDId = 1;
            var pCountDId = 63;
            if (_isSupplementaryData)
            {
                return;
            }

            if (ServerConfig.RedisHelper.SetIfNotExist(lockKey, "lock"))
            {
                var now = DateTime.Now.NoMillisecond().AddSeconds(-_due);
                var lastTime = ServerConfig.RedisHelper.Get<DateTime>(redisKey);
                var startTime = lastTime == default(DateTime) ? now.AddSeconds(-1) : lastTime.NoMillisecond();
                if (startTime >= now)
                {
                    ServerConfig.RedisHelper.Remove(lockKey);
                    return;
                }

                if ((int)(now - startTime).TotalSeconds != 1)
                {
                    _isSupplementaryData = true;
                    SupplementaryData(startTime.AddSeconds(1), now);
                    ServerConfig.RedisHelper.Remove(lockKey);
                    _isSupplementaryData = false;
                    return;
                }

                startTime = now;
                var endTime = startTime.AddSeconds(1);
                ServerConfig.RedisHelper.SetForever(redisKey, startTime.ToStr());

                var deviceList =
                    ServerConfig.ApiDb.Query<MonitoringProcess>("SELECT b.DeviceId, b.LastState, b.TodayProcessCount, b.TotalProcessCount FROM `device_library` a " +
                                                                "JOIN `npc_proxy_link` b ON a.Id = b.DeviceId WHERE a.MarkedDelete = 0 AND b.Monitoring = 1;").ToDictionary(x => x.DeviceId);
                if (!deviceList.Any())
                {
                    ServerConfig.RedisHelper.Remove(lockKey);
                    return;
                }

                foreach (var device in deviceList)
                {
                    device.Value.Time = now;
                }

                var mData = ServerConfig.ApiDb.Query<MonitoringAnalysis>(
                    "SELECT * FROM `npc_monitoring_analysis` WHERE SendTime BETWEEN @startTime AND @endTime ORDER BY SendTime;", new
                    {
                        startTime = startTime.ToStr(),
                        endTime = endTime.ToStr()
                    });
                if (mData.Any())
                {
                    var scripts = mData.GroupBy(x => x.ScriptId).Select(x => x.Key).ToList();
                    scripts.Add(0);
                    IEnumerable<UsuallyDictionary> usuallyDictionaries = null;
                    if (scripts.Any())
                    {
                        usuallyDictionaries = ServerConfig.ApiDb.Query<UsuallyDictionary>(
                            "SELECT * FROM `usually_dictionary` WHERE ScriptId IN @ScriptId AND (VariableNameId = @VariableNameId1 OR VariableNameId = @VariableNameId2);", new
                            {
                                ScriptId = scripts,
                                VariableNameId1 = stateDId,
                                VariableNameId2 = pCountDId,
                            });
                    }

                    foreach (var data in mData)
                    {
                        var analysisData = data.AnalysisData;
                        if (usuallyDictionaries != null && usuallyDictionaries.Any())
                        {
                            //总加工次数
                            var udd = usuallyDictionaries.FirstOrDefault(x =>
                                x.ScriptId == data.ScriptId && x.VariableNameId == pCountDId);
                            var address = udd?.DictionaryId ?? usuallyDictionaries.First(x => x.ScriptId == 0 && x.VariableNameId == pCountDId).DictionaryId;
                            var actAddress = address - 1;
                            if (analysisData.vals.Count >= actAddress)
                            {
                                deviceList[data.DeviceId].TotalProcessCount = analysisData.vals[actAddress];
                            }

                            //今日加工次数
                            udd = usuallyDictionaries.FirstOrDefault(x =>
                               x.ScriptId == data.ScriptId && x.VariableNameId == stateDId);
                            address = udd?.DictionaryId ?? usuallyDictionaries.First(x => x.ScriptId == 0 && x.VariableNameId == stateDId).DictionaryId;
                            actAddress = address - 1;
                            if (analysisData.vals.Count >= actAddress)
                            {
                                var v = analysisData.vals[actAddress];
                                deviceList[data.DeviceId].TodayProcessCount = lastTime.InSameDay(now) ? deviceList[data.DeviceId].TodayProcessCount : 0;
                                if (deviceList[data.DeviceId].LastState == 0 && v > 0)
                                {
                                    deviceList[data.DeviceId].LastState = 1;
                                    deviceList[data.DeviceId].TodayProcessCount++;
                                }
                                else if (deviceList[data.DeviceId].LastState == 0)
                                {
                                    deviceList[data.DeviceId].LastState = 0;
                                }
                            }
                            deviceList[data.DeviceId].ProcessCount = deviceList[data.DeviceId].TodayProcessCount;
                        }
                    }

                    ServerConfig.ApiDb.Execute(
                        "UPDATE npc_proxy_link SET `LastState` = @LastState, `TotalProcessCount` = @TotalProcessCount WHERE `DeviceId` = @DeviceId;",
                        deviceList.Values);

                    Task.Run(() =>
                    {
                        ServerConfig.ApiDb.ExecuteAsync(
                            "INSERT INTO npc_monitoring_process (`Time`, `DeviceId`, `ProcessCount`) VALUES (@Time, @DeviceId, @ProcessCount);",
                            deviceList.Values);
                    });
                }

                ServerConfig.RedisHelper.Remove(lockKey);
            }
        }

        private static void SupplementaryData(DateTime startTime, DateTime now)
        {
            try
            {
                var _pre = "Process";
                var lockKey = $"{_pre}:Lock";
                var redisKey = $"{_pre}:Time";
                var totalSeconds = (int)(now - startTime).TotalSeconds;
                var stateDId = 1;
                var pCountDId = 63;

                var deviceList =
                    ServerConfig.ApiDb.Query<MonitoringProcess>("SELECT b.DeviceId, b.LastState, b.TodayProcessCount, b.TotalProcessCount FROM `device_library` a " +
                                                                "JOIN `npc_proxy_link` b ON a.Id = b.DeviceId WHERE a.MarkedDelete = 0 AND b.Monitoring = 1;").ToDictionary(x => x.DeviceId);
                if (!deviceList.Any())
                {
                    return;
                }

                var endTime = now;
                var mData = ServerConfig.ApiDb.Query<MonitoringAnalysis>(
                    "SELECT * FROM `npc_monitoring_analysis` WHERE SendTime BETWEEN @startTime AND @endTime ORDER BY SendTime;", new
                    {
                        startTime = startTime.ToStr(),
                        endTime = endTime.ToStr()
                    });
                if (mData.Any())
                {
                    var maxTime = mData.Last().SendTime.NoMillisecond();
                    if (maxTime < now)
                    {
                        return;
                    }
                    var scripts = mData.GroupBy(x => x.ScriptId).Select(x => x.Key).ToList();
                    scripts.Add(0);
                    IEnumerable<UsuallyDictionary> usuallyDictionaries = null;
                    if (scripts.Any())
                    {
                        usuallyDictionaries = ServerConfig.ApiDb.QueryWithTime<UsuallyDictionary>(
                            "SELECT * FROM `usually_dictionary` WHERE ScriptId IN @ScriptId AND (VariableNameId = @VariableNameId1 OR VariableNameId = @VariableNameId2);", new
                            {
                                ScriptId = scripts,
                                VariableNameId1 = stateDId,
                                VariableNameId2 = pCountDId,
                            }, 90);
                    }

                    for (int i = 0; i < totalSeconds; i++)
                    {
                        var lastTime = startTime.AddSeconds(i);
                        foreach (var device in deviceList)
                        {
                            device.Value.Time = lastTime;
                        }
                        var lastData = mData.Where(x => x.SendTime.NoMillisecond() == lastTime);
                        if (lastData.Any())
                        {
                            foreach (var data in lastData)
                            {
                                if (usuallyDictionaries != null && usuallyDictionaries.Any())
                                {
                                    var analysisData = data.AnalysisData;
                                    if (analysisData != null)
                                    {
                                        //总加工次数
                                        var udd = usuallyDictionaries.FirstOrDefault(x =>
                                            x.ScriptId == data.ScriptId && x.VariableNameId == pCountDId);
                                        var address = udd?.DictionaryId ?? usuallyDictionaries.First(x => x.ScriptId == 0 && x.VariableNameId == pCountDId).DictionaryId;
                                        var actAddress = address - 1;
                                        if (analysisData.vals.Count >= actAddress)
                                        {
                                            deviceList[data.DeviceId].TotalProcessCount = analysisData.vals[actAddress];
                                        }

                                        //今日加工次数
                                        udd = usuallyDictionaries.FirstOrDefault(x =>
                                            x.ScriptId == data.ScriptId && x.VariableNameId == stateDId);
                                        address = udd?.DictionaryId ?? usuallyDictionaries.First(x => x.ScriptId == 0 && x.VariableNameId == stateDId).DictionaryId;
                                        actAddress = address - 1;
                                        if (analysisData.vals.Count >= actAddress)
                                        {
                                            var v = analysisData.vals[actAddress];
                                            deviceList[data.DeviceId].TodayProcessCount = lastTime.InSameDay(now) ? deviceList[data.DeviceId].TodayProcessCount : 0;
                                            if (deviceList[data.DeviceId].LastState == 0 && v > 0)
                                            {
                                                deviceList[data.DeviceId].LastState = 1;
                                                deviceList[data.DeviceId].TodayProcessCount++;
                                            }
                                            else if (deviceList[data.DeviceId].LastState == 0)
                                            {
                                                deviceList[data.DeviceId].LastState = 0;
                                            }
                                        }
                                    }
                                    deviceList[data.DeviceId].ProcessCount = deviceList[data.DeviceId].TodayProcessCount;
                                }
                            }
                            ServerConfig.ApiDb.Execute(
                                "INSERT INTO npc_monitoring_process (`Time`, `DeviceId`, `ProcessCount`) VALUES (@Time, @DeviceId, @ProcessCount);",
                                deviceList.Values);
                        }
                    }

                    ServerConfig.ApiDb.Execute(
                        "UPDATE npc_proxy_link SET `LastState` = @LastState, `TodayProcessCount` = @TodayProcessCount, `TotalProcessCount` = @TotalProcessCount WHERE `DeviceId` = @DeviceId;",
                        deviceList.Values);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
    }
}
