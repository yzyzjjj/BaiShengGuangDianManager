using ApiManagement.Base.Control;
using ApiManagement.Base.Server;
using ApiManagement.Models;
using ApiManagement.Models.Analysis;
using Microsoft.Extensions.Configuration;
using ModelBase.Base.Logger;
using ModelBase.Base.Utils;
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
    public class DataStorageHelper
    {
        private static bool _analysisFirst;
        private static Timer _analysis;
        private static int _dealLength = 1000;
        public static void Init(IConfiguration configuration)
        {
            var _pre = "Analysis";
            var lockKey = $"{_pre}:Lock";
            var redisKey = $"{_pre}:Id";
            if (!_analysisFirst)
            {
                _analysisFirst = true;
                var analysis = ServerConfig.ApiDb.Query<MonitoringAnalysis>(
                    "SELECT SendTime, DeviceId FROM `npc_monitoring_analysis` ORDER BY SendTime desc LIMIT 1;").FirstOrDefault();

                if (analysis != null)
                {
                    var data = ServerConfig.DataStorageDb.Query<MonitoringData>(
                        "SELECT Id FROM `npc_monitoring_data` WHERE SendTime = @SendTime AND DeviceId = @DeviceId;", new
                        {
                            analysis.SendTime,
                            analysis.DeviceId
                        }).FirstOrDefault();

                    ServerConfig.RedisHelper.SetForever(redisKey, data.Id);
                }

                ServerConfig.RedisHelper.Remove(lockKey);
            }
            _analysis = new Timer(Analysis, null, 10000, 2000);
        }
        private static void Analysis(object state)
        {
            var _pre = "Analysis";
            var lockKey = $"{_pre}:Lock";
            var redisKey = $"{_pre}:Id";

            if (ServerConfig.RedisHelper.SetIfNotExist(lockKey, "lock"))
            {
                try
                {
                    var startId = ServerConfig.RedisHelper.Get<int>(redisKey);
                    var mData = ServerConfig.DataStorageDb.Query<MonitoringData>(
                        "SELECT * FROM `npc_monitoring_data` WHERE Id > @Id AND UserSend = 0 ORDER BY Id LIMIT @limit;", new
                        {
                            Id = startId,
                            limit = _dealLength
                        });
                    if (mData.Any())
                    {
                        var endId = mData.Last().Id;
                        if (endId > startId)
                        {
                            foreach (var data in mData)
                            {
                                var infoMessagePacket = new DeviceInfoMessagePacket(data.ValNum, data.InNum, data.OutNum);
                                var analysisData = infoMessagePacket.Deserialize(data.Data);
                                data.AnalysisData = new DeviceData();
                                if (analysisData != null)
                                {
                                    data.AnalysisData.vals = analysisData.vals;
                                    data.AnalysisData.ins = analysisData.ins;
                                    data.AnalysisData.outs = analysisData.outs;
                                }
                                data.Data = data.AnalysisData.ToJSON();
                            }

                            #region  加工记录
                            var stateDId = 1;
                            var pCountDId = 63;

                            var deviceList =
                                ServerConfig.ApiDb.Query<MonitoringProcess>("SELECT b.DeviceId, b.LastState, b.TodayProcessCount, b.TotalProcessCount, b.Time FROM `device_library` a " +
                                                                            "JOIN `npc_proxy_link` b ON a.Id = b.DeviceId WHERE a.MarkedDelete = 0;")
                                                                            .Where(x => mData.Any(y => y.DeviceId == x.DeviceId)).ToDictionary(x => x.DeviceId);
                            var monitoringProcesses = new List<MonitoringProcess>();
                            if (deviceList.Any())
                            {
                                var scripts = mData.GroupBy(x => x.ScriptId).Select(x => x.Key).ToList();
                                scripts.Add(0);
                                IEnumerable<UsuallyDictionary> usuallyDictionaries = null;
                                if (scripts.Any())
                                {
                                    usuallyDictionaries = ServerConfig.ApiDb.Query<UsuallyDictionary>(
                                        "SELECT * FROM `usually_dictionary` WHERE ScriptId IN @ScriptId AND (VariableNameId = @VariableNameId1 OR VariableNameId = @VariableNameId2);",
                                        new
                                        {
                                            ScriptId = scripts,
                                            VariableNameId1 = stateDId,
                                            VariableNameId2 = pCountDId,
                                        });
                                }

                                var lastData = mData.OrderBy(x => x.SendTime.NoMillisecond());
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
                                                var address =
                                                    udd?.DictionaryId ?? usuallyDictionaries.First(x =>
                                                            x.ScriptId == 0 && x.VariableNameId == pCountDId)
                                                        .DictionaryId;
                                                var actAddress = address - 1;
                                                if (analysisData.vals.Count >= actAddress)
                                                {
                                                    deviceList[data.DeviceId].TotalProcessCount =
                                                        analysisData.vals[actAddress];
                                                }

                                                //今日加工次数
                                                udd = usuallyDictionaries.FirstOrDefault(x =>
                                                    x.ScriptId == data.ScriptId && x.VariableNameId == stateDId);
                                                address = udd?.DictionaryId ?? usuallyDictionaries
                                                              .First(x => x.ScriptId == 0 &&
                                                                          x.VariableNameId == stateDId)
                                                              .DictionaryId;
                                                actAddress = address - 1;
                                                if (analysisData.vals.Count >= actAddress)
                                                {
                                                    var v = analysisData.vals[actAddress];
                                                    deviceList[data.DeviceId].TodayProcessCount =
                                                        deviceList[data.DeviceId].Time.InSameDay(data.SendTime)
                                                            ? deviceList[data.DeviceId].TodayProcessCount
                                                            : 0;
                                                    if (deviceList[data.DeviceId].LastState == 0 && v > 0)
                                                    {
                                                        deviceList[data.DeviceId].TodayProcessCount++;
                                                    }
                                                    deviceList[data.DeviceId].LastState = v > 0 ? 1 : 0;
                                                }
                                            }

                                            deviceList[data.DeviceId].ProcessCount =
                                                deviceList[data.DeviceId].TodayProcessCount;
                                        }
                                        deviceList[data.DeviceId].Time = data.SendTime.NoMillisecond();

                                        monitoringProcesses.Add(new MonitoringProcess
                                        {
                                            DeviceId = deviceList[data.DeviceId].DeviceId,
                                            Time = deviceList[data.DeviceId].Time,
                                            ProcessCount = deviceList[data.DeviceId].ProcessCount,
                                        });
                                    }

                                }
                            }
                            #endregion

                            ServerConfig.ApiDb.Execute(
                                "UPDATE npc_proxy_link SET `LastState` = @LastState, `TodayProcessCount` = @TodayProcessCount, `TotalProcessCount` = @TotalProcessCount, `Time` = @Time WHERE `DeviceId` = @DeviceId;",
                                deviceList.Values);

                            Task.Run(() =>
                            {
                                ServerConfig.ApiDb.ExecuteTrans(
                                    "INSERT INTO npc_monitoring_analysis (`SendTime`, `DeviceId`, `ScriptId`, `Ip`, `Port`, `Data`) " +
                                    "VALUES (@SendTime, @DeviceId, @ScriptId, @Ip, @Port, @Data);",
                                    mData);
                            });
                            Task.Run(() =>
                            {
                                ServerConfig.ApiDb.ExecuteTrans(
                                "INSERT INTO npc_monitoring_process (`Time`, `DeviceId`, `ProcessCount`) VALUES (@Time, @DeviceId, @ProcessCount);",
                                monitoringProcesses);
                            });

                            ServerConfig.RedisHelper.SetForever(redisKey, endId);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                ServerConfig.RedisHelper.Remove(lockKey);
            }
        }
    }
}
