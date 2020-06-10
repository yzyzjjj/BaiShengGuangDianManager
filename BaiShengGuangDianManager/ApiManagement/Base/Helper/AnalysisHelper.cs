using ApiManagement.Base.Server;
using ApiManagement.Models.DeviceManagementModel;
using ApiManagement.Models.FlowCardManagementModel;
using ApiManagement.Models.OtherModel;
using ApiManagement.Models.RepairManagementModel;
using ApiManagement.Models.StatisticManagementModel;
using Microsoft.EntityFrameworkCore.Internal;
using ModelBase.Base.Logger;
using ModelBase.Base.Utils;
using ModelBase.Models.Control;
using ServiceStack;
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
    public class AnalysisHelper
    {
        private static readonly string Debug = "Debug";

        private static Timer _timer2S;
        private static Timer _timer10S;
        private static Timer _timer60S;
        private static Timer _script;
        private static int _dealLength = 500;

        public static MonitoringKanBan MonitoringKanBan;
        private static MonitoringKanBan _monitoringKanBan;
        public static void Init()
        {
            try
            {
                Script();
#if DEBUG
                Console.WriteLine("AnalysisHelper 调试模式已开启");
                //AnalysisOther();
                //_analysis = new Timer(Analysis, null, 10000, 2000);
                //_processLog = new Timer(UpdateProcessLog, null, 5000, 1000 * 60);
#else
                if (!ServerConfig.RedisHelper.Exists(Debug))
                {
                    ServerConfig.RedisHelper.SetForever(Debug, 0);
                }

                var analysisOtherPre = "AnalysisOther";
                var analysisOtherLock = $"{analysisOtherPre}:Lock";

                var analysisPre = "Analysis";
                var analysisLockKey = $"{analysisPre}:Lock";
                var analysisIdKey = $"{analysisPre}:Id";

                ServerConfig.RedisHelper.Remove(analysisOtherLock);
                MonitoringKanBan = ServerConfig.ApiDb.Query<MonitoringKanBan>("SELECT * FROM `npc_monitoring_kanban` ORDER BY `Date` DESC LIMIT 1;").FirstOrDefault();
                var startId1 = ServerConfig.RedisHelper.Get<int>(analysisIdKey);
                Thread.Sleep(2000);
                var startId2 = ServerConfig.RedisHelper.Get<int>(analysisIdKey);
                if (startId1 == 0 && startId2 == 0)
                {
                    var id = ServerConfig.ApiDb.Query<int>("SELECT Id FROM `npc_monitoring_analysis` ORDER BY Id LIMIT 1;").FirstOrDefault();
                    if (id != 0)
                    {
                        ServerConfig.RedisHelper.SetForever(analysisIdKey, id);
                    }
                }
                if (startId1 == startId2)
                {
                    ServerConfig.RedisHelper.Remove(analysisLockKey);
                }

                Console.WriteLine("AnalysisHelper 发布模式已开启");
                _timer2S = new Timer(DoSth_2s, null, 10000, 2000);
                _timer10S = new Timer(DoSth_10s, null, 10000, 1000 * 10);
                _timer60S = new Timer(DoSth_60s, null, 5000, 1000 * 60);
#endif
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private static void DoSth_2s(object state)
        {
            Analysis();
            AnalysisOther();
            Delete();
        }

        private static void DoSth_10s(object state)
        {
            Fault();
            ProcessSum();
            ProcessTime();
        }

        private static void DoSth_60s(object state)
        {
            UpdateProcessLog();
            FlowCardReport();
        }

        /// <summary>
        /// 加工统计  分析
        /// </summary>
        private static void ProcessSum()
        {

#if !DEBUG
            if (ServerConfig.RedisHelper.Get<int>("Debug") != 0)
            {
                return;
            }
#endif

            var redisPre = "ProcessSum";
            var redisLock = $"{redisPre}:Lock";
            var idKey = $"{redisPre}:Id";
            var deviceKey = $"{redisPre}:Device";
            if (ServerConfig.RedisHelper.SetIfNotExist(redisLock, DateTime.Now.ToStr()))
            {
                try
                {
                    ServerConfig.RedisHelper.SetExpireAt(redisLock, DateTime.Now.AddHours(1));
                    var startId = ServerConfig.RedisHelper.Get<int>(idKey);
                    var mData = ServerConfig.ApiDb.Query<MonitoringProcessLogDetail>(
                        "SELECT a.*, b.ProductionProcessName FROM `npc_monitoring_process_log` a JOIN (SELECT a.*, b.ProductionProcessName FROM `flowcard_library` a JOIN `production_library` b ON a.ProductionProcessId = b.Id) b ON a.FlowCardId = b.Id WHERE a.Id > @Id AND OpName = '加工' AND NOT ISNULL(EndTime) LIMIT @limit;", new
                        {
                            Id = startId,
                            limit = _dealLength
                        }, 1200);
                    if (mData.Any())
                    {
                        var endId = mData.Last().Id;
                        if (endId > startId)
                        {
                            #region  加工记录
                            var deviceList = new Dictionary<int, MonitoringProcessSum>();
                            if (ServerConfig.RedisHelper.Exists(deviceKey))
                            {
                                deviceList = ServerConfig.RedisHelper.Get<Dictionary<int, MonitoringProcessSum>>(deviceKey);
                            }

                            var monitoringProcessSums = new List<MonitoringProcessSum>();
                            var i = 0;
                            foreach (var data in mData)
                            {
                                i++;
                                if (!deviceList.ContainsKey(data.DeviceId))
                                {
                                    deviceList.Add(data.DeviceId, new MonitoringProcessSum
                                    {
                                        DeviceId = data.DeviceId,
                                        StartTime = data.StartTime,
                                        EndTime = data.EndTime,
                                        ProductionProcessName = data.ProductionProcessName,
                                        Count = 1,
                                        ProcessData = data.ProcessData,
                                    });
                                }
                                else
                                {
                                    if (deviceList[data.DeviceId].ProcessData == data.ProcessData)
                                    {
                                        deviceList[data.DeviceId].Count++;
                                        deviceList[data.DeviceId].EndTime = data.EndTime;
                                        if (!deviceList[data.DeviceId].ProductionProcessName.IsNullOrEmpty())
                                        {
                                            var productionProcessName = deviceList[data.DeviceId].ProductionProcessName.Split(",").ToList();
                                            if (productionProcessName.All(x => x != data.ProductionProcessName))
                                            {
                                                productionProcessName.Add(data.ProductionProcessName);
                                                deviceList[data.DeviceId].ProductionProcessName =
                                                    productionProcessName.Join();
                                            }
                                        }
                                        else
                                        {
                                            deviceList[data.DeviceId].ProductionProcessName =
                                                data.ProductionProcessName;
                                        }

                                        if (i == mData.Count())
                                        {
                                            monitoringProcessSums.Add(deviceList[data.DeviceId]);
                                        }
                                    }
                                    else
                                    {
                                        var d = (MonitoringProcessSum)deviceList[data.DeviceId].Clone();
                                        monitoringProcessSums.Add(d);
                                        deviceList[data.DeviceId] = new MonitoringProcessSum
                                        {
                                            DeviceId = data.DeviceId,
                                            StartTime = data.StartTime,
                                            EndTime = data.EndTime,
                                            ProductionProcessName = data.ProductionProcessName,
                                            Count = 1,
                                            ProcessData = data.ProcessData,
                                        };
                                    }
                                }
                            }

                            #endregion

                            ServerConfig.RedisHelper.SetForever(deviceKey, deviceList);

                            Task.Run(() =>
                            {
                                try
                                {
                                    ServerConfig.ApiDb.ExecuteTrans(
                                        "INSERT INTO npc_monitoring_process_sum (`DeviceId`, `Count`, `ProcessData`, `StartTime`, `EndTime`, `ProductionProcessName`) " +
                                        "VALUES (@DeviceId, @Count, @ProcessData, @StartTime, @EndTime, @ProductionProcessName);",
                                        monitoringProcessSums);
                                }
                                catch (Exception e)
                                {
                                    Log.Error(e);
                                }
                            });

                            ServerConfig.RedisHelper.SetForever(idKey, endId);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                ServerConfig.RedisHelper.Remove(redisLock);
            }

        }

        /// <summary>
        /// 加工次数  分析
        /// </summary>
        private static void ProcessTime()
        {
#if !DEBUG
            if (ServerConfig.RedisHelper.Get<int>("Debug") != 0)
            {
                return;
            }
#endif

            var redisPre = "ProcessTime";
            var lockKey = $"{redisPre}:Lock";
            var idKey = $"{redisPre}:Id";
            var deviceKey = $"{redisPre}:Device";
            var offset = 60;
            if (ServerConfig.RedisHelper.SetIfNotExist(lockKey, DateTime.Now.ToStr()))
            {
                try
                {
                    ServerConfig.RedisHelper.SetExpireAt(lockKey, DateTime.Now.AddHours(1));
                    var startId = ServerConfig.RedisHelper.Get<int>(idKey);
                    var mData = ServerConfig.ApiDb.Query<MonitoringProcessLogDetail>(
                        "SELECT a.*, b.ProductionProcessName FROM `npc_monitoring_process_log` a JOIN (SELECT a.*, b.ProductionProcessName FROM `flowcard_library` a JOIN `production_library` b ON a.ProductionProcessId = b.Id) b ON a.FlowCardId = b.Id WHERE a.Id > @Id AND OpName = '加工' AND NOT ISNULL(EndTime) LIMIT @limit;", new
                        {
                            Id = startId,
                            limit = _dealLength
                        }, 1200);
                    if (mData.Any())
                    {
                        var endId = mData.Last().Id;
                        if (endId > startId)
                        {
                            #region  加工记录
                            var deviceList = new Dictionary<int, MonitoringProcessTime>();
                            if (ServerConfig.RedisHelper.Exists(deviceKey))
                            {
                                deviceList = ServerConfig.RedisHelper.Get<Dictionary<int, MonitoringProcessTime>>(deviceKey);
                            }

                            var monitoringProcessTimes = new List<MonitoringProcessTime>();
                            var i = 0;
                            foreach (var data in mData)
                            {
                                i++;
                                if (!deviceList.ContainsKey(data.DeviceId))
                                {
                                    deviceList.Add(data.DeviceId, new MonitoringProcessTime
                                    {
                                        DeviceId = data.DeviceId,
                                        LastTime = data.TotalTime,
                                        MinTime = data.TotalTime,
                                        MaxTime = data.TotalTime,
                                        StartTime = data.StartTime,
                                        EndTime = data.EndTime,
                                        ProductionProcessName = data.ProductionProcessName,
                                        Count = 1,
                                        ProcessData = data.ProcessData,
                                    });
                                }
                                else
                                {
                                    if (deviceList[data.DeviceId].ProcessData == data.ProcessData && Math.Abs(deviceList[data.DeviceId].LastTime - data.TotalTime) < offset)
                                    {
                                        deviceList[data.DeviceId].Count++;
                                        deviceList[data.DeviceId].LastTime = data.TotalTime;
                                        deviceList[data.DeviceId].EndTime = data.EndTime;
                                        if (deviceList[data.DeviceId].MinTime > data.TotalTime)
                                        {
                                            deviceList[data.DeviceId].MinTime = data.TotalTime;
                                        }
                                        if (deviceList[data.DeviceId].MaxTime < data.TotalTime)
                                        {
                                            deviceList[data.DeviceId].MaxTime = data.TotalTime;
                                        }
                                        if (!deviceList[data.DeviceId].ProductionProcessName.IsNullOrEmpty())
                                        {
                                            var productionProcessName = deviceList[data.DeviceId].ProductionProcessName.Split(",").ToList();
                                            if (productionProcessName.All(x => x != data.ProductionProcessName))
                                            {
                                                productionProcessName.Add(data.ProductionProcessName);
                                                deviceList[data.DeviceId].ProductionProcessName =
                                                    productionProcessName.Join();
                                            }
                                        }
                                        else
                                        {
                                            deviceList[data.DeviceId].ProductionProcessName =
                                                data.ProductionProcessName;
                                        }
                                        if (i == mData.Count())
                                        {
                                            monitoringProcessTimes.Add(deviceList[data.DeviceId]);
                                        }
                                    }
                                    else
                                    {
                                        var d = (MonitoringProcessTime)deviceList[data.DeviceId].Clone();
                                        monitoringProcessTimes.Add(d);
                                        deviceList[data.DeviceId] = new MonitoringProcessTime
                                        {
                                            DeviceId = data.DeviceId,
                                            LastTime = data.TotalTime,
                                            MinTime = data.TotalTime,
                                            MaxTime = data.TotalTime,
                                            StartTime = data.StartTime,
                                            EndTime = data.EndTime,
                                            ProductionProcessName = data.ProductionProcessName,
                                            Count = 1,
                                            ProcessData = data.ProcessData,
                                        };
                                    }
                                }

                            }

                            #endregion

                            ServerConfig.RedisHelper.SetForever(deviceKey, deviceList);

                            Task.Run(() =>
                            {
                                try
                                {
                                    ServerConfig.ApiDb.ExecuteTrans(
                                        "INSERT INTO npc_monitoring_process_time (`DeviceId`, `Count`, `ProcessData`, `MinTime`, `MaxTime`, `AvgTime`, `StartTime`, `EndTime`, `ProductionProcessName`) " +
                                        "VALUES (@DeviceId, @Count, @ProcessData, @MinTime, @MaxTime, @AvgTime, @StartTime, @EndTime, @ProductionProcessName);",
                                        monitoringProcessTimes);
                                }
                                catch (Exception e)
                                {
                                    Log.Error(e);
                                }
                            });

                            ServerConfig.RedisHelper.SetForever(idKey, endId);
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

        private static void Delete()
        {
            return;
            //var redisPre = "Time";
            //var redisLock = $"{redisPre}:Lock";
            //if (ServerConfig.RedisHelper.SetIfNotExist(redisLock, DateTime.Now.ToStr()))
            //{
            //    try
            //    {
            //        ServerConfig.RedisHelper.SetExpireAt(redisLock, DateTime.Now.AddMinutes(5));
            //        ServerConfig.ApiDb.Execute(
            //        "DELETE FROM npc_monitoring_analysis WHERE SendTime < @SendTime LIMIT 1000;", new
            //        {
            //            SendTime = DateTime.Today.AddDays(-3)
            //        }, 60);
            //        //ServerConfig.DataStorageDb.Execute(
            //        //    "DELETE FROM npc_monitoring_analysis WHERE SendTime < @SendTime LIMIT 1000;OPTIMIZE TABLE npc_monitoring_analysis;", new
            //        //    {
            //        //        SendTime = DateTime.Today.AddDays(-3)
            //        //    }, 60);
            //        //ServerConfig.DataStorageDb.Execute(
            //        //    "DELETE FROM npc_monitoring_analysis WHERE SendTime < @SendTime LIMIT 1000;OPTIMIZE TABLE npc_monitoring_analysis;", new
            //        //    {
            //        //        SendTime = DateTime.Today.AddMonths(-3)
            //        //    }, 60);

            //    }
            //    catch (Exception e)
            //    {
            //        Log.Error(e);
            //    }
            //    ServerConfig.RedisHelper.Remove(redisLock);
            //}
        }

        /// <summary>
        /// 
        /// </summary>
        private static void Analysis()
        {
#if !DEBUG
            if (ServerConfig.RedisHelper.Get<int>("Debug") != 0)
            {
                return;
            }
#endif
            var redisPre = "Analysis";
            var redisLock = $"{redisPre}:Lock";
            var idKey = $"{redisPre}:Id";
            var deviceKey = $"{redisPre}:Device";
            if (ServerConfig.RedisHelper.SetIfNotExist(redisLock, DateTime.Now.ToStr()))
            {
                try
                {
                    ServerConfig.RedisHelper.SetExpireAt(redisLock, DateTime.Now.AddMinutes(5));
                    var startId = ServerConfig.RedisHelper.Get<int>(idKey);
                    var mData = ServerConfig.ApiDb.Query<MonitoringData>(
                        "SELECT * FROM `npc_monitoring_analysis` WHERE Id > @Id AND UserSend = 0 ORDER BY Id LIMIT @limit;", new
                        {
                            Id = startId,
                            limit = _dealLength
                        });
                    if (mData.Any())
                    {
                        mData = mData.OrderBy(x => x.SendTime);
                        var endId = mData.Last().Id;
                        if (endId > startId)
                        {
                            #region  加工记录
                            //设备状态
                            var stateDId = 1;
                            //总加工次数
                            var processCountDId = 63;
                            //总加工时间
                            var processTimeDId = 64;
                            //当前加工流程卡号
                            var currentFlowCardDId = 6;
                            //累积运行总时间
                            var runTimeDId = 5;
                            var variableNameIdList = new List<int>
                            {
                                stateDId,
                                processCountDId,
                                processTimeDId,
                                currentFlowCardDId,
                                runTimeDId
                            };
                            var actProcessDid = 65;
                            var processCnt = 48;
                            for (var i = 0; i < processCnt; i++)
                            {
                                variableNameIdList.Add(actProcessDid + i);
                            }

                            var allDeviceList = ServerConfig.ApiDb.Query<MonitoringProcess>(
                                 "SELECT b.*, c.DeviceCategoryId, a.`Code` FROM `device_library` a JOIN `npc_proxy_link` b ON a.Id = b.DeviceId JOIN `device_model` c ON a.DeviceModelId = c.Id WHERE a.MarkedDelete = 0;");

                            var existDeviceId = mData.GroupBy(x => x.DeviceId).Select(y => y.Key);
                            var deviceList = allDeviceList.Where(x => existDeviceId.Any(y => y == x.DeviceId)).ToDictionary(x => x.DeviceId);
                            if (ServerConfig.RedisHelper.Exists(deviceKey))
                            {
                                var redisDeviceList = ServerConfig.RedisHelper.Get<IEnumerable<RedisMonitoringProcess>>(deviceKey);
                                if (redisDeviceList != null)
                                {
                                    foreach (var device in redisDeviceList)
                                    {
                                        if (deviceList.ContainsKey(device.DeviceId))
                                        {
                                            deviceList[device.DeviceId].State = device.State;
                                            deviceList[device.DeviceId].Time = device.Time;
                                        }
                                    }
                                }
                            }

                            var monitoringProcesses = new List<MonitoringProcess>();
                            if (deviceList.Any())
                            {
                                var scripts = mData.GroupBy(x => x.ScriptId).Select(x => x.Key).ToList();
                                scripts.Add(0);
                                IEnumerable<UsuallyDictionary> usuallyDictionaries = null;
                                if (scripts.Any())
                                {
                                    usuallyDictionaries = ServerConfig.ApiDb.Query<UsuallyDictionary>(
                                        "SELECT * FROM `usually_dictionary` WHERE ScriptId IN @ScriptId AND VariableNameId IN @VariableNameId;",
                                        new
                                        {
                                            ScriptId = scripts,
                                            VariableNameId = variableNameIdList,
                                        });
                                }

                                if (!variableNameIdList.All(x => usuallyDictionaries.Any(y => y.VariableNameId == x)))
                                {
                                    Log.ErrorFormat("缺少变量配置:{0}", variableNameIdList.Where(x => usuallyDictionaries.All(y => y.VariableNameId != x)).ToJSON());
                                    ServerConfig.RedisHelper.Remove(redisLock);
                                    return;
                                }
                                var uDies = new Dictionary<Tuple<int, int>, int>();
                                foreach (var variableNameId in variableNameIdList)
                                {
                                    foreach (var script in scripts.Where(x => x != 0))
                                    {
                                        var udd = usuallyDictionaries.FirstOrDefault(x =>
                                            x.ScriptId == script && x.VariableNameId == variableNameId);
                                        var address =
                                            udd?.DictionaryId ?? usuallyDictionaries.First(x =>
                                               x.ScriptId == 0 && x.VariableNameId == variableNameId).DictionaryId;

                                        uDies.Add(new Tuple<int, int>(script, variableNameId), address);
                                    }
                                }

                                if (MonitoringKanBan == null)
                                {
                                    MonitoringKanBan = new MonitoringKanBan
                                    {
                                        Time = mData.FirstOrDefault().SendTime.NoMillisecond(),
                                    };
                                }
                                var faultDeviceCount = ServerConfig.ApiDb.Query<int>(
                                    "SELECT COUNT(1) FROM (SELECT * FROM `fault_device_repair` WHERE DeviceId != 0 AND FaultTime >= @FaultTime1 AND FaultTime <= @FaultTime2 GROUP BY DeviceCode) a;", new
                                    {
                                        FaultTime1 = MonitoringKanBan?.Time.DayBeginTime() ?? DateTime.Now.DayBeginTime(),
                                        FaultTime2 = MonitoringKanBan?.Time.DayEndTime() ?? DateTime.Now.DayEndTime()

                                    }).FirstOrDefault();

                                _monitoringKanBan = _monitoringKanBan ?? new MonitoringKanBan
                                {
                                    Time = MonitoringKanBan.Time.NoMillisecond(),
                                    FaultDevice = faultDeviceCount,
                                    AllDevice = allDeviceList.Count(),
                                };
                                if (usuallyDictionaries != null && usuallyDictionaries.Any())
                                {
                                    foreach (var data in mData)
                                    {
                                        var time = data.SendTime.NoMillisecond();
                                        if (!deviceList[data.DeviceId].Time.InSameDay(time))
                                        {
                                            deviceList[data.DeviceId].ProcessCount = 0;
                                            deviceList[data.DeviceId].ProcessTime = 0;
                                            deviceList[data.DeviceId].RunTime = 0;
                                        }
                                        var analysisData = data.AnalysisData;
                                        if (analysisData != null)
                                        {
                                            var actAddress = uDies[new Tuple<int, int>(data.ScriptId, currentFlowCardDId)] - 1;
                                            var currentFlowCardId = 0;
                                            var currentFlowCard = "";
                                            FlowCardProcessStepDetail flowCardProcessStepDetail = null;
                                            if (analysisData.vals.Count >= actAddress)
                                            {
                                                currentFlowCardId = analysisData.vals[actAddress];
                                                currentFlowCard = currentFlowCardId.ToString();
                                            }

                                            //状态
                                            actAddress = uDies[new Tuple<int, int>(data.ScriptId, stateDId)] - 1;
                                            if (analysisData.vals.Count >= actAddress)
                                            {
                                                var v = analysisData.vals[actAddress];
                                                //开始加工
                                                var bStart = deviceList[data.DeviceId].State == 0 && v > 0;
                                                //停止加工
                                                var bEnd = deviceList[data.DeviceId].State == 1 && v == 0;
                                                if (currentFlowCardId != 0)
                                                {
                                                    if (bStart || bEnd)
                                                    {
                                                        var flowCard =
                                                            ServerConfig.ApiDb.Query<FlowCardLibrary>("SELECT * FROM `flowcard_library` WHERE Id = @Id;", new { Id = currentFlowCardId }).FirstOrDefault();

                                                        if (flowCard != null)
                                                        {
                                                            var flowCardProcessStepDetails = ServerConfig.ApiDb.Query<FlowCardProcessStepDetail>(
                                                                "SELECT a.* FROM `flowcard_process_step` a JOIN `device_process_step` b ON a.ProcessStepId = b.Id WHERE b.IsSurvey = 0 AND a.FlowCardId = @FlowCardId AND a.DeviceId = @DeviceId;", new
                                                                {
                                                                    FlowCardId = flowCard.Id,
                                                                    DeviceId = data.DeviceId
                                                                });
                                                            currentFlowCard = flowCard.FlowCardName;
                                                            flowCardProcessStepDetail = flowCardProcessStepDetails.FirstOrDefault();
                                                            if (flowCardProcessStepDetail != null)
                                                            {
                                                                var sql = string.Empty;
                                                                //开始加工
                                                                if (bStart)
                                                                {
                                                                    if (flowCardProcessStepDetail.ProcessTime == default(DateTime))
                                                                    {
                                                                        flowCardProcessStepDetail.ProcessTime = data.SendTime;
                                                                        sql =
                                                                            "UPDATE flowcard_process_step SET `ProcessTime` = @ProcessTime WHERE `Id` = @Id;";
                                                                    }
                                                                }

                                                                //停止加工
                                                                if (bEnd)
                                                                {
                                                                    //if (flowCardProcessStepDetail.ProcessEndTime == default(DateTime))
                                                                    //{
                                                                    flowCardProcessStepDetail.ProcessEndTime = data.SendTime;
                                                                    sql =
                                                                        "UPDATE flowcard_process_step SET `ProcessEndTime` = @ProcessEndTime WHERE `Id` = @Id;";
                                                                    //}
                                                                }
                                                                if (sql != string.Empty)
                                                                {
                                                                    ServerConfig.ApiDb.Execute(sql, flowCardProcessStepDetail);
                                                                }
                                                            }
                                                        }
                                                    }
                                                    else if (deviceList[data.DeviceId].State == v && currentFlowCardId != 0)
                                                    {
                                                        deviceList[data.DeviceId].FlowCardId = currentFlowCardId;
                                                    }
                                                    deviceList[data.DeviceId].FlowCardId = currentFlowCardId;
                                                }

                                                var processData = new Dictionary<int, int[]>(); ;
                                                var pActAddress = uDies[new Tuple<int, int>(data.ScriptId, actProcessDid + processCnt - 1)] - 1;
                                                if (analysisData.vals.Count >= pActAddress)
                                                {
                                                    for (var i = 0; i < processCnt; i++)
                                                    {
                                                        pActAddress = uDies[new Tuple<int, int>(data.ScriptId, actProcessDid + i)] - 1;
                                                        var pv = analysisData.vals[pActAddress];

                                                        var key = i / 6 + 1;
                                                        if (!processData.ContainsKey(key))
                                                        {
                                                            processData.Add(key, new int[6]);
                                                        }


                                                        var index = i % 6;
                                                        processData[key][index] = pv;
                                                    }
                                                }

                                                //currentFlowCardId = 0;
                                                //开始加工
                                                if (bStart)
                                                {
                                                    ServerConfig.ApiDb.Execute(
                                                        "INSERT INTO `npc_monitoring_process_log` (`DeviceId`, `StartTime`, `FlowCardId`, `FlowCard`, `ProcessorId`, `ProcessData`, `RequirementMid`) " +
                                                        "VALUES(@DeviceId, @StartTime, @FlowCardId, @FlowCard, @ProcessorId, @ProcessData, @RequirementMid) ON DUPLICATE KEY UPDATE `DeviceId` = @DeviceId;",
                                                        new MonitoringProcessLog
                                                        {
                                                            DeviceId = data.DeviceId,
                                                            StartTime = time,
                                                            FlowCardId = currentFlowCardId,
                                                            FlowCard = currentFlowCard,
                                                            ProcessorId = flowCardProcessStepDetail?.ProcessorId ?? 0,
                                                            ProcessData = processData.ToJson(),
                                                            RequirementMid = flowCardProcessStepDetail?.ProcessStepRequirementMid ?? 0,
                                                        });
                                                }

                                                //停止加工
                                                if (bEnd)
                                                {
                                                    ServerConfig.ApiDb.Execute(
                                                        "UPDATE `npc_monitoring_process_log` SET EndTime = @EndTime WHERE Id = ((SELECT Id FROM ( SELECT MAX(Id) Id FROM `npc_monitoring_process_log` WHERE DeviceId = @DeviceId) a)) AND ISNULL(EndTime) AND @EndTime >= StartTime;",
                                                        new MonitoringProcessLog
                                                        {
                                                            DeviceId = data.DeviceId,
                                                            EndTime = time,
                                                        });
                                                }

                                                if (v > 0)
                                                {
                                                    if (!_monitoringKanBan.UseList.Contains(data.DeviceId))
                                                    {
                                                        _monitoringKanBan.UseList.Add(data.DeviceId);
                                                    }
                                                    if (!_monitoringKanBan.MaxUseList.Contains(data.DeviceId))
                                                    {
                                                        _monitoringKanBan.MaxUseList.Add(data.DeviceId);
                                                    }
                                                }

                                                if (v == 0)
                                                {
                                                    if (_monitoringKanBan.UseList.Contains(data.DeviceId))
                                                    {
                                                        _monitoringKanBan.UseList.Remove(data.DeviceId);
                                                    }
                                                }

                                                deviceList[data.DeviceId].State = v > 0 ? 1 : 0;
                                            }

                                            //总加工次数
                                            actAddress = uDies[new Tuple<int, int>(data.ScriptId, processCountDId)] - 1;
                                            if (analysisData.vals.Count >= actAddress)
                                            {
                                                var totalProcessCount = analysisData.vals[actAddress];
                                                if (deviceList[data.DeviceId].TotalProcessCount < totalProcessCount)
                                                {
                                                    deviceList[data.DeviceId].ProcessCount += totalProcessCount - deviceList[data.DeviceId].TotalProcessCount;
                                                }
                                                deviceList[data.DeviceId].TotalProcessCount = totalProcessCount;
                                            }

                                            //总加工时间
                                            actAddress = uDies[new Tuple<int, int>(data.ScriptId, processTimeDId)] - 1;
                                            if (analysisData.vals.Count >= actAddress)
                                            {
                                                var totalProcessTime = analysisData.vals[actAddress];
                                                if (deviceList[data.DeviceId].TotalProcessTime < totalProcessTime)
                                                {
                                                    deviceList[data.DeviceId].ProcessTime += totalProcessTime - deviceList[data.DeviceId].TotalProcessTime;
                                                }
                                                deviceList[data.DeviceId].TotalProcessTime = totalProcessTime;
                                            }

                                            //总运行时间
                                            actAddress = uDies[new Tuple<int, int>(data.ScriptId, runTimeDId)] - 1;
                                            if (analysisData.vals.Count >= actAddress)
                                            {
                                                var totalRunTime = analysisData.vals[actAddress];
                                                if (deviceList[data.DeviceId].TotalRunTime < totalRunTime)
                                                {
                                                    deviceList[data.DeviceId].RunTime += totalRunTime - deviceList[data.DeviceId].TotalRunTime;
                                                }
                                                deviceList[data.DeviceId].TotalRunTime = totalRunTime;
                                            }

                                            deviceList[data.DeviceId].Time = time;

                                            var monitoringProcess = new MonitoringProcess
                                            {
                                                DeviceId = deviceList[data.DeviceId].DeviceId,
                                                Time = deviceList[data.DeviceId].Time,
                                                State = deviceList[data.DeviceId].State,
                                                ProcessCount = deviceList[data.DeviceId].ProcessCount,
                                                TotalProcessCount = deviceList[data.DeviceId].TotalProcessCount,
                                                ProcessTime = deviceList[data.DeviceId].ProcessTime,
                                                TotalProcessTime = deviceList[data.DeviceId].TotalProcessTime,
                                                RunTime = deviceList[data.DeviceId].RunTime,
                                                TotalRunTime = deviceList[data.DeviceId].TotalRunTime,
                                                Use = allDeviceList.Count(x => Math.Abs((x.Time - time).TotalMinutes) <= 3 && x.State == 1),
                                                Total = allDeviceList.Count(),
                                            };

                                            monitoringProcess.Rate = (decimal)monitoringProcess.Use * 100 / monitoringProcess.Total;
                                            monitoringProcesses.Add(monitoringProcess);
                                        }

                                        //Update(allDeviceList, time);
                                        if (!_monitoringKanBan.Time.InSameDay(time))
                                        {
                                            _monitoringKanBan = new MonitoringKanBan
                                            {
                                                Time = time,
                                                FaultDevice = faultDeviceCount,
                                                AllDevice = allDeviceList.Count(),
                                            };
                                        }
                                        //else
                                        //{
                                        //    _monitoringKanban.UseList = MonitoringKanBan.UseList;
                                        //    _monitoringKanban.SingleProcessRate =
                                        //        MonitoringKanBan.SingleProcessRate;
                                        //}
                                    }

                                    UpdateKanBan(allDeviceList, mData.Last().SendTime.NoMillisecond());
                                }
                            }
                            #endregion

                            ServerConfig.RedisHelper.SetForever(deviceKey, deviceList.Values.Select(x => new
                            {
                                x.DeviceId,
                                x.State,
                                x.Time
                            }));

                            ServerConfig.ApiDb.Execute(
                                "UPDATE npc_proxy_link SET `Time` = @Time, `State` = @State, `ProcessCount` = @ProcessCount, `TotalProcessCount` = @TotalProcessCount, " +
                                "`ProcessTime` = @ProcessTime, `TotalProcessTime` = @TotalProcessTime, `RunTime` = @RunTime, `TotalRunTime` = @TotalRunTime WHERE `DeviceId` = @DeviceId;",
                                deviceList.Values);

                            ServerConfig.ApiDb.Execute(
                                "INSERT INTO npc_monitoring_kanban (`Date`, `AllDevice`, `NormalDevice`, `ProcessDevice`, `IdleDevice`, `FaultDevice`, `ConnectErrorDevice`, `MaxUse`, `UseListStr`, " +
                                "`MaxUseListStr`, `MaxUseRate`, `MinUse`, `MinUseRate`, `MaxSimultaneousUseRate`, `MinSimultaneousUseRate`, `SingleProcessRateStr`, `AllProcessRate`, `RunTime`, " +
                                "`ProcessTime`, `IdleTime`) VALUES (@Date, @AllDevice, @NormalDevice, @ProcessDevice, @IdleDevice, @FaultDevice, @ConnectErrorDevice, @MaxUse, @UseListStr, " +
                                "@MaxUseListStr ,@MaxUseRate, @MinUse, @MinUseRate, @MaxSimultaneousUseRate, @MinSimultaneousUseRate, @SingleProcessRateStr, @AllProcessRate, @RunTime, @ProcessTime, @IdleTime) " +
                                "ON DUPLICATE KEY UPDATE `AllDevice` = @AllDevice, `NormalDevice` = @NormalDevice, `ProcessDevice` = @ProcessDevice, `IdleDevice` = @IdleDevice, `FaultDevice` = @FaultDevice, `ConnectErrorDevice` = @ConnectErrorDevice, `MaxUse` = @MaxUse, `MaxUseListStr` = @MaxUseListStr, `UseListStr` = @UseListStr, `MaxUseRate` = @MaxUseRate, `MinUse` = @MinUse, `MinUseRate` = @MinUseRate, `MaxSimultaneousUseRate` = @MaxSimultaneousUseRate, `MinSimultaneousUseRate` = @MinSimultaneousUseRate, `SingleProcessRateStr` = @SingleProcessRateStr, `AllProcessRate` = @AllProcessRate, `RunTime` = @RunTime, `ProcessTime` = @ProcessTime, `IdleTime` = @IdleTime;"
                                , MonitoringKanBan);

                            try
                            {
                                InsertAnalysis(monitoringProcesses);
                            }
                            catch (Exception e)
                            {
                                Log.Error(e);
                            }

                            ServerConfig.RedisHelper.SetForever(idKey, endId);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                ServerConfig.RedisHelper.Remove(redisLock);
            }
        }

        private static void InsertAnalysis(IEnumerable<MonitoringProcess> monitoringProcesses)
        {
            Task.Run(() =>
            {
                ServerConfig.ApiDb.ExecuteTrans(
                   "INSERT INTO npc_monitoring_process (`Time`, `DeviceId`, `ProcessCount`, `TotalProcessCount`, `ProcessTime`, `TotalProcessTime`, `RunTime`, `TotalRunTime`, `State`, `Rate`, `Use`, `Total`) VALUES (@Time, @DeviceId, @ProcessCount, @TotalProcessCount, @ProcessTime, @TotalProcessTime, @RunTime, @TotalRunTime, @State, @Rate, @Use, @Total) " +
                   "ON DUPLICATE KEY UPDATE `Time` = @Time, `DeviceId` = @DeviceId, `State` = @State, `ProcessCount` = @ProcessCount, `TotalProcessCount` = @TotalProcessCount, `ProcessTime` = @ProcessTime, `TotalProcessTime` = @TotalProcessTime, `RunTime` = @RunTime, `TotalRunTime` = @TotalRunTime, `Use` = @Use, `Total` = @Total, `Rate` = @Rate;",
                   monitoringProcesses);
            });
        }

        private static void UpdateKanBan(IEnumerable<MonitoringProcess> allDeviceList, DateTime time)
        {
            _monitoringKanBan.Time = time;
            var validDevice = allDeviceList.Where(x => Math.Abs((x.Time - _monitoringKanBan.Time).TotalMinutes) <= 5);
            _monitoringKanBan.NormalDevice = validDevice.Count();
            _monitoringKanBan.ProcessDevice = validDevice.Count(x => x.State == 1);
            _monitoringKanBan.MaxUseList = _monitoringKanBan.MaxUseList.Distinct().ToList();
            _monitoringKanBan.MaxUse = _monitoringKanBan.MaxUseList.Count;
            _monitoringKanBan.MinUse = _monitoringKanBan.MaxUse;
            _monitoringKanBan.MaxSimultaneousUseRate = _monitoringKanBan.ProcessDevice;
            _monitoringKanBan.MinSimultaneousUseRate = _monitoringKanBan.MaxSimultaneousUseRate;
            foreach (var device in allDeviceList)
            {
                var rate = _monitoringKanBan.SingleProcessRate.FirstOrDefault(x =>
                    x.Id == device.DeviceId);
                if (rate == null)
                {
                    _monitoringKanBan.SingleProcessRate.Add(new ProcessUseRate
                    {
                        Id = device.DeviceId,
                        Code = device.Code,
                        Rate = (device.ProcessTime * 1m / (24 * 3600)).ToRound(4)
                    });
                }
                else
                {
                    rate.Rate = (device.ProcessTime * 1m / (24 * 3600)).ToRound(4);
                }
            }

            var todayDevice = allDeviceList.Where(x => x.Time.InSameDay(_monitoringKanBan.Time));
            _monitoringKanBan.RunTime = todayDevice.Sum(x => x.RunTime);
            _monitoringKanBan.ProcessTime = todayDevice.Sum(x => x.ProcessTime);
            _monitoringKanBan.AllProcessRate = todayDevice.Any() ? (_monitoringKanBan.ProcessTime * 1m / (todayDevice.Count() * 24 * 3600)).ToRound(4) : 0;

            _monitoringKanBan.UseCodeList = allDeviceList.OrderBy(x => x.DeviceId).Where(x => _monitoringKanBan.UseList.Contains(x.DeviceId)).Select(x => x.Code).ToList();
            MonitoringKanBan.Update(_monitoringKanBan);
        }

        /// <summary>
        /// 
        /// </summary>
        private static void AnalysisOther()
        {
#if !DEBUG
            if (ServerConfig.RedisHelper.Get<int>("Debug") != 0)
            {
                return;
            }
#endif
            var redisPre = "AnalysisOther";
            var redisLock = $"{redisPre}:Lock";
            var timeKey = $"{redisPre}:Time";
            if (ServerConfig.RedisHelper.SetIfNotExist(redisLock, DateTime.Now.ToStr()))
            {
                try
                {
                    ServerConfig.RedisHelper.SetExpireAt(redisLock, DateTime.Now.AddMinutes(5));
                    var startTime = ServerConfig.RedisHelper.Get<DateTime>(timeKey);
                    if (startTime == default(DateTime))
                    {
                        startTime = ServerConfig.ApiDb.Query<DateTime>(
                            "SELECT Time FROM `npc_monitoring_process` ORDER BY Time LIMIT 1;").FirstOrDefault();
                    }

                    if (startTime == default(DateTime))
                    {
                        ServerConfig.RedisHelper.Remove(redisLock);
                        return;
                    }

                    var deviceCount = ServerConfig.ApiDb.Query<int>(
                        "SELECT COUNT(1) FROM `device_library` a JOIN `npc_proxy_link` b ON a.Id = b.DeviceId WHERE a.MarkedDelete = 0;").FirstOrDefault();
                    //"SELECT COUNT(1) FROM `device_library` a JOIN `npc_proxy_link` b ON a.Id = b.DeviceId WHERE a.MarkedDelete = 0 AND b.Monitoring = 1;").FirstOrDefault();
                    if (deviceCount <= 0)
                    {
                        ServerConfig.RedisHelper.Remove(redisLock);
                        return;
                    }
                    var mData = ServerConfig.ApiDb.Query<MonitoringProcess>(
                        "SELECT * FROM `npc_monitoring_process` WHERE Time >= @Time ORDER BY Time LIMIT @Limit;", new
                        {
                            Time = startTime,
                            Limit = _dealLength
                        });
                    if (mData.Any())
                    {
                        startTime = mData.Last().Time;
                        var l = 4;

                        var table = new Dictionary<int, string>
                        {
                            {0, "npc_monitoring_process_min"},
                            {1, "npc_monitoring_process_hour"},
                            {2, "npc_monitoring_process_day"},
                            {3, "npc_monitoring_process_month"},
                        };
                        //npc_monitoring_process_   每个表每台设备最后存储记录的时间
                        var resLast = new Dictionary<int, MonitoringProcess>[l];
                        for (var i = 0; i < l; i++)
                        {
                            var time = startTime;
                            switch (i)
                            {
                                case 0:
                                    time = startTime.NoSecond(); break;
                                case 1:
                                    time = startTime.NoMinute(); break;
                                case 2:
                                    time = startTime.NoHour(); break;
                                case 3:
                                    time = startTime.StartOfMonth(); break;
                            }
                            resLast[i] = ServerConfig.ApiDb.Query<MonitoringProcess>(
                                $"SELECT * FROM (SELECT * FROM {table[i]} WHERE Time = @Time ORDER BY Time DESC LIMIT @Limit) a GROUP BY a.DeviceId;", new
                                {
                                    Time = time,
                                    Limit = deviceCount
                                }).ToDictionary(x => x.DeviceId);
                        }

                        #region new
                        //npc_monitoring_process_   分析后得到的每个表的数据
                        var res = new Dictionary<Tuple<DateTime, int>, MonitoringProcess>[l];
                        for (var i = 0; i < l; i++)
                        {
                            res[i] = new Dictionary<Tuple<DateTime, int>, MonitoringProcess>();
                            foreach (var process in resLast[i].Values)
                            {
                                if (process != null && process.Time != null)
                                {
                                    res[i].Add(new Tuple<DateTime, int>(process.Time, process.DeviceId), process);
                                }
                                else
                                {
                                    var a = 1;
                                }
                            }
                        }

                        foreach (var da in mData)
                        {
                            for (var i = 0; i < l; i++)
                            {
                                var data = (MonitoringProcess)da.Clone();
                                switch (i)
                                {
                                    case 0:
                                        data.Time = data.Time.NoSecond();
                                        break;
                                    case 1:
                                        data.Time = data.Time.NoMinute();
                                        break;
                                    case 2:
                                        data.Time = data.Time.NoHour();
                                        break;
                                    case 3:
                                        data.Time = data.Time.StartOfMonth();
                                        break;
                                }
                                var key = new Tuple<DateTime, int>(data.Time, data.DeviceId);
                                if (!res[i].ContainsKey(key))
                                {
                                    res[i].Add(key, data);
                                }
                                else
                                {
                                    res[i][key] = data;
                                }
                            }
                        }
                        for (var i = 0; i < l; i++)
                        {
                            var data = res[i].Select(x => x.Value).OrderBy(y => y.Time);
                            ServerConfig.ApiDb.Execute($"INSERT INTO {table[i]} (`Time`, `DeviceId`, `State`, `ProcessCount`, `TotalProcessCount`, `ProcessTime`, `TotalProcessTime`, `RunTime`, `TotalRunTime`, `Use`, `Total`, `Rate`) VALUES (@Time, @DeviceId, @State, @ProcessCount, @TotalProcessCount, @ProcessTime, @TotalProcessTime, @RunTime, @TotalRunTime, @Use, @Total, @Rate) " +
                                                        "ON DUPLICATE KEY UPDATE `State` = @State, `ProcessCount` = @ProcessCount, `TotalProcessCount` = @TotalProcessCount, `ProcessTime` = @ProcessTime, `TotalProcessTime` = @TotalProcessTime, `RunTime` = @RunTime, `TotalRunTime` = @TotalRunTime, `Use` = @Use, `Total` = @Total, `Rate` = @Rate;"
                                                       , data);

                            //ServerConfig.ApiDb.Execute($"INSERT INTO {table[i]} (`Time`, `DeviceId`, `State`, `ProcessCount`, `TotalProcessCount`, `ProcessTime`, `TotalProcessTime`, `RunTime`, `TotalRunTime`, `Use`, `Total`, `Rate`) " +
                            //                           $"VALUES (@Time, @DeviceId, @State, @ProcessCount, @TotalProcessCount, @ProcessTime, @TotalProcessTime, @RunTime, @TotalRunTime, @Use, @Total, @Rate) ", data);
                        }
                        #endregion
                        #region old
                        //npc_monitoring_process_   分析后得到的每个表的数据
                        //var res = new List<MonitoringProcess>[l];
                        //for (var i = 0; i < l; i++)
                        //{
                        //    res[i] = new List<MonitoringProcess>();
                        //}
                        //foreach (var da in mData)
                        //{
                        //    for (var i = 0; i < l; i++)
                        //    {
                        //        var data = (MonitoringProcess)da.Clone();
                        //        switch (i)
                        //        {
                        //            case 0:
                        //                data.Time = data.Time.NoSecond();
                        //                break;
                        //            case 1:
                        //                data.Time = data.Time.NoMinute();
                        //                break;
                        //            case 2:
                        //                data.Time = data.Time.NoHour();
                        //                break;
                        //            case 3:
                        //                data.Time = data.Time.StartOfMonth();
                        //                break;
                        //        }
                        //        var n = false;
                        //        if (!resLast[i].ContainsKey(data.DeviceId))
                        //        {
                        //            n = true;
                        //            resLast[i].Add(data.DeviceId, data);
                        //        }

                        //        var d = resLast[i][data.DeviceId];
                        //        var f = false;
                        //        switch (i)
                        //        {
                        //            case 0:
                        //                f = d.Time.InSameMinute(data.Time);
                        //                break;
                        //            case 1:
                        //                f = d.Time.InSameHour(data.Time);
                        //                break;
                        //            case 2:
                        //                f = d.Time.InSameDay(data.Time);
                        //                break;
                        //            case 3:
                        //                f = d.Time.InSameMonth(data.Time);
                        //                break;
                        //        }
                        //        if (n)
                        //        {
                        //            continue;
                        //        }

                        //        if (!f)
                        //        {
                        //            res[i].Add(resLast[i][data.DeviceId]);
                        //        }
                        //        resLast[i][data.DeviceId] = data;
                        //    }
                        //}
                        //for (var i = 0; i < l; i++)
                        //{
                        //    foreach (var process in resLast[i].Values)
                        //    {

                        //        switch (i)
                        //        {
                        //            case 0:
                        //                if (!res[i].Any(x => x.DeviceId == process.DeviceId && x.Time.InSameMinute(process.Time)))
                        //                {
                        //                    res[i].Add(process);
                        //                }
                        //                break;
                        //            case 1:
                        //                if (!res[i].Any(x => x.DeviceId == process.DeviceId && x.Time.InSameHour(process.Time)))
                        //                {
                        //                    res[i].Add(process);
                        //                }
                        //                break;
                        //            case 2:
                        //                if (!res[i].Any(x => x.DeviceId == process.DeviceId && x.Time.InSameDay(process.Time)))
                        //                {
                        //                    res[i].Add(process);
                        //                }
                        //                break;
                        //            case 3:
                        //                if (!res[i].Any(x => x.DeviceId == process.DeviceId && x.Time.InSameMonth(process.Time)))
                        //                {
                        //                    res[i].Add(process);
                        //                }
                        //                break;
                        //        }
                        //    }
                        //    ServerConfig.ApiDb.Execute($"INSERT INTO {table[i]} (`Time`, `DeviceId`, `State`, `ProcessCount`, `TotalProcessCount`, `ProcessTime`, `TotalProcessTime`, `RunTime`, `TotalRunTime`, `Use`, `Total`, `Rate`) VALUES (@Time, @DeviceId, @State, @ProcessCount, @TotalProcessCount, @ProcessTime, @TotalProcessTime, @RunTime, @TotalRunTime, @Use, @Total, @Rate) " +
                        //                                "ON DUPLICATE KEY UPDATE `State` = @State, `ProcessCount` = @ProcessCount, `TotalProcessCount` = @TotalProcessCount, `ProcessTime` = @ProcessTime, `TotalProcessTime` = @TotalProcessTime, `RunTime` = @RunTime, `TotalRunTime` = @TotalRunTime, `Use` = @Use, `Total` = @Total, `Rate` = @Rate;"
                        //                               , res[i].OrderBy(x => x.Time));
                        //}
                        #endregion
                    }
                    ServerConfig.RedisHelper.SetForever(timeKey, startTime);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                ServerConfig.RedisHelper.Remove(redisLock);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        private static void Fault()
        {
#if !DEBUG
            if (ServerConfig.RedisHelper.Get<int>("Debug") != 0)
            {
                return;
            }
#endif

            var redisPre = "Fault";
            var redisLock = $"{redisPre}:Lock";
            if (ServerConfig.RedisHelper.SetIfNotExist(redisLock, DateTime.Now.ToStr()))
            {
                try
                {
                    ServerConfig.RedisHelper.SetExpireAt(redisLock, DateTime.Now.AddMinutes(5));
                    var now = DateTime.Now;
                    var today = DateTime.Today;
                    if ((now - now.Date).TotalSeconds < 10)
                    {
                        today = today.AddDays(-1);
                    }

                    FaultCal(today);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                ServerConfig.RedisHelper.Remove(redisLock);
            }
        }

        public static void FaultCal(DateTime today, bool isStatistic = true)
        {
            today = today.Date;
            if (!isStatistic && DateTime.Now.InSameDay(today))
            {
                return;
            }
            try
            {
                var all = ServerConfig.ApiDb.Query<string>("SELECT b.SiteName FROM `device_library` a JOIN `site` b ON a.SiteId = b.Id WHERE b.SiteName IS NOT NULL AND a.MarkedDelete = 0;");

                var field = FaultDevice.GetField(new List<string> { "DeviceCode" }, "a.");
                var faultDevicesAll = ServerConfig.ApiDb.Query<FaultDeviceDetail>($"SELECT {field}, IFNULL(b.`Code`, a.DeviceCode) DeviceCode, b.SiteName, c.FaultTypeName FROM `fault_device_repair` a " +
                                                                                  $"JOIN (SELECT a.*, b.SiteName FROM `device_library` a JOIN `site` b ON a.SiteId = b.Id ) b ON a.DeviceId = b.`Id` " +
                                                                                  $"JOIN `fault_type` c ON a.FaultTypeId = c.Id " +
                                                                                  $"WHERE b.SiteName IS NOT NULL AND FaultTime >= @FaultTime1 AND FaultTime < @FaultTime2;", new
                                                                                  {
                                                                                      FaultTime1 = today,
                                                                                      FaultTime2 = today.AddDays(1)
                                                                                  });

                field = RepairRecord.GetField(new List<string> { "DeviceCode" }, "a.");
                var repairRecordsAll = ServerConfig.ApiDb.Query<RepairRecordDetail>($"SELECT {field}, IFNULL(b.`Code`, a.DeviceCode) DeviceCode, b.SiteName, c.FaultTypeName FROM `fault_device_repair` a " +
                                                                                    $"JOIN ( SELECT a.*, b.SiteName FROM `device_library` a JOIN `site` b ON a.SiteId = b.Id ) b ON a.DeviceId = b.`Id` " +
                                                                                    $"JOIN `fault_type` c ON a.FaultTypeId1 = c.Id " +
                                                                                    $"WHERE b.SiteName IS NOT NULL AND SolveTime >= @SolveTime1 AND SolveTime < @SolveTime2 AND State = @State;", new
                                                                                    {
                                                                                        SolveTime1 = today,
                                                                                        SolveTime2 = today.AddDays(1),
                                                                                        State = RepairStateEnum.Complete
                                                                                    });

                var workshops =
                    ServerConfig.ApiDb.Query<string>(
                        "SELECT SiteName FROM `site` WHERE MarkedDelete = 0 GROUP BY SiteName ORDER BY Id;");
                var monitoringFaults = new Dictionary<Tuple<DateTime, string>, MonitoringFault>();
                var h = 24;
                for (int i = 0; i < h; i++)
                {
                    var time1 = today.AddHours(i);
                    var time2 = time1.AddHours(1);
                    foreach (var workshop in workshops)
                    {
                        var key = new Tuple<DateTime, string>(time1, workshop);
                        if (!monitoringFaults.ContainsKey(key))
                        {
                            monitoringFaults.Add(key, new MonitoringFault
                            {
                                Date = time1,
                                Workshop = workshop
                            });
                        }
                        var monitoringFault = monitoringFaults[key];
                        #region 上报

                        var faultDevices = faultDevicesAll.Where(x => x.FaultTime >= time1 && x.FaultTime < time2);
                        var faultDeviceDetails = faultDevices.Where(x => x.SiteName == workshop && !x.Cancel);
                        monitoringFault.FaultDevice = faultDeviceDetails.GroupBy(x => x.DeviceId).Count();
                        monitoringFault.ReportFaultType = faultDeviceDetails.GroupBy(x => x.FaultTypeId).Count();
                        monitoringFault.ReportCount = faultDeviceDetails.Count();
                        monitoringFault.ReportCancel = faultDevices.Count(x => x.SiteName == workshop && x.MarkedDelete && x.Cancel);

                        foreach (var faultDeviceDetail in faultDeviceDetails)
                        {
                            var faultId = faultDeviceDetail.FaultTypeId;
                            var faultName = faultDeviceDetail.FaultTypeName;
                            if (monitoringFault.ReportSingleFaultType.All(x => x.FaultId != faultId))
                            {
                                monitoringFault.ReportSingleFaultType.Add(new SingleFaultType
                                {
                                    FaultId = faultId,
                                    FaultName = faultName
                                });
                            }

                            var singleFaultType = monitoringFault.ReportSingleFaultType.First(x => x.FaultId == faultId);
                            singleFaultType.Count++;

                            if (singleFaultType.DeviceFaultTypes.All(x => x.Code != faultDeviceDetail.DeviceCode))
                            {
                                singleFaultType.DeviceFaultTypes.Add(new DeviceFaultType
                                {
                                    Code = faultDeviceDetail.DeviceCode,
                                });
                            }

                            var deviceFaultType = singleFaultType.DeviceFaultTypes.First(x => x.Code == faultDeviceDetail.DeviceCode);
                            deviceFaultType.Count++;

                            if (singleFaultType.Operators.All(x => x.Name != faultDeviceDetail.Proposer))
                            {
                                singleFaultType.Operators.Add(new Operator
                                {
                                    Name = faultDeviceDetail.Proposer,
                                });
                            }

                            var @operator = singleFaultType.Operators.First(x => x.Name == faultDeviceDetail.Proposer);
                            @operator.Count++;
                            monitoringFault.ReportSingleFaultTypeStr = monitoringFault.ReportSingleFaultType.OrderBy(x => x.FaultId).ToJSON();
                        }

                        monitoringFault.Confirmed = ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `fault_device_repair` a JOIN ( SELECT a.*, b.SiteName FROM `device_library` a JOIN `site` b ON a.SiteId = b.Id ) b ON a.DeviceId = b.`Id` JOIN `fault_type` c ON a.FaultTypeId = c.Id WHERE b.SiteName IS NOT NULL AND a.MarkedDelete = @MarkedDelete AND a.State = @State AND b.SiteName= @SiteName;", new
                        {
                            MarkedDelete = 0,
                            State = RepairStateEnum.Confirm,
                            SiteName = workshop,
                        }).FirstOrDefault();
                        monitoringFault.Repairing = ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `fault_device_repair` a JOIN ( SELECT a.*, b.SiteName FROM `device_library` a JOIN `site` b ON a.SiteId = b.Id ) b ON a.DeviceId = b.`Id` JOIN `fault_type` c ON a.FaultTypeId = c.Id WHERE b.SiteName IS NOT NULL AND a.MarkedDelete = @MarkedDelete AND a.State = @State AND b.SiteName= @SiteName;", new
                        {
                            MarkedDelete = 0,
                            State = RepairStateEnum.Repair,
                            SiteName = workshop,
                        }).FirstOrDefault();

                        monitoringFault.AllDevice = all.Count(x => x == workshop);

                        #endregion

                        #region 维修
                        var repairRecords = repairRecordsAll.Where(x => x.SolveTime >= time1 && x.SolveTime < time2);
                        var repairRecordDetails = repairRecords.Where(x => x.SiteName == workshop && !x.Cancel);
                        monitoringFault.RepairCount = repairRecordDetails.Count();
                        monitoringFault.RepairFaultType = repairRecordDetails.GroupBy(x => x.FaultTypeId1).Count();
                        monitoringFault.RepairCancel = repairRecordDetails.Count(x => x.SiteName == workshop && x.MarkedDelete && x.Cancel);

                        foreach (var repairRecordDetail in repairRecordDetails)
                        {
                            var faultId = repairRecordDetail.FaultTypeId1;
                            var faultName = repairRecordDetail.FaultTypeName;

                            if (monitoringFault.RepairSingleFaultType.All(x => x.FaultId != faultId))
                            {
                                monitoringFault.RepairSingleFaultType.Add(new SingleFaultType
                                {
                                    FaultId = faultId,
                                    FaultName = faultName
                                });
                            }

                            var singleFaultType = monitoringFault.RepairSingleFaultType.First(x => x.FaultId == faultId);
                            singleFaultType.Count++;

                            if (singleFaultType.DeviceFaultTypes.All(x => x.Code != repairRecordDetail.DeviceCode))
                            {
                                singleFaultType.DeviceFaultTypes.Add(new DeviceFaultType
                                {
                                    Code = repairRecordDetail.DeviceCode,
                                });
                            }

                            var deviceFaultType = singleFaultType.DeviceFaultTypes.First(x => x.Code == repairRecordDetail.DeviceCode);
                            deviceFaultType.Count++;

                            if (singleFaultType.Operators.All(x => x.Name != repairRecordDetail.FaultSolver))
                            {
                                singleFaultType.Operators.Add(new Operator
                                {
                                    Name = repairRecordDetail.FaultSolver,
                                });
                            }

                            var @operator = singleFaultType.Operators.First(x => x.Name == repairRecordDetail.FaultSolver);
                            @operator.Count++;
                            @operator.Time += repairRecordDetail.SolveTime > repairRecordDetail.FaultTime ? (int)(repairRecordDetail.SolveTime - repairRecordDetail.FaultTime).TotalSeconds : 0;

                            monitoringFault.RepairSingleFaultTypeStr = monitoringFault.RepairSingleFaultType.OrderBy(x => x.FaultId).ToJSON();
                        }
                        #endregion
                    }
                }

                ServerConfig.ApiDb.ExecuteTrans(
                    "INSERT INTO npc_monitoring_fault_hour (`Date`, `Workshop`, `AllDevice`, `FaultDevice`, `ReportFaultType`, `ReportCount`, `ReportCancel`, `ReportSingleFaultTypeStr`, `ReportFaultRate`, `Confirmed`, `Repairing`, `ReportRepaired`, `ExtraRepaired`, `RepairFaultType`, `RepairCount`, `RepairSingleFaultTypeStr`, `RepairCancel`) VALUES (@Date, @Workshop, @AllDevice, @FaultDevice, @ReportFaultType, @ReportCount, @ReportCancel, @ReportSingleFaultTypeStr, @ReportFaultRate, @Confirmed, @Repairing, @ReportRepaired, @ExtraRepaired, @RepairFaultType, @RepairCount, @RepairSingleFaultTypeStr, @RepairCancel) " +
                    "ON DUPLICATE KEY UPDATE `AllDevice` = @AllDevice, `FaultDevice` = @FaultDevice, `ReportFaultType` = @ReportFaultType, `ReportCount` = @ReportCount, `ReportCancel` = @ReportCancel, `ReportSingleFaultTypeStr` = @ReportSingleFaultTypeStr, `ReportFaultRate` = @ReportFaultRate, `Confirmed` = @Confirmed, `Repairing` = @Repairing, `ReportRepaired` = @ReportRepaired, `ExtraRepaired` = @ExtraRepaired, `RepairFaultType` = @RepairFaultType, `RepairCount` = @RepairCount, `RepairSingleFaultTypeStr` = @RepairSingleFaultTypeStr, `RepairCancel` = @RepairCancel",
                    monitoringFaults.Values.OrderBy(x => x.Date));

                var npcMonitoringDay = new Dictionary<string, MonitoringFault>();
                foreach (var workshop in workshops)
                {
                    npcMonitoringDay.Add(workshop, new MonitoringFault
                    {
                        Date = today,
                        Workshop = workshop
                    });
                }

                foreach (var monitoringFault in monitoringFaults.Values)
                {
                    npcMonitoringDay[monitoringFault.Workshop].DayAdd(monitoringFault);
                }

                ServerConfig.ApiDb.ExecuteTrans(
                    "INSERT INTO npc_monitoring_fault (`Date`, `Workshop`, `AllDevice`, `FaultDevice`, `ReportFaultType`, `ReportCount`, `ReportCancel`, `ReportSingleFaultTypeStr`, `ReportFaultRate`, `Confirmed`, `Repairing`, `ReportRepaired`, `ExtraRepaired`, `RepairFaultType`, `RepairCount`, `RepairSingleFaultTypeStr`, `RepairCancel`) VALUES (@Date, @Workshop, @AllDevice, @FaultDevice, @ReportFaultType, @ReportCount, @ReportCancel, @ReportSingleFaultTypeStr, @ReportFaultRate, @Confirmed, @Repairing, @ReportRepaired, @ExtraRepaired, @RepairFaultType, @RepairCount, @RepairSingleFaultTypeStr, @RepairCancel) " +
                    "ON DUPLICATE KEY UPDATE `AllDevice` = @AllDevice, `FaultDevice` = @FaultDevice, `ReportFaultType` = @ReportFaultType, `ReportCount` = @ReportCount, `ReportCancel` = @ReportCancel, `ReportSingleFaultTypeStr` = @ReportSingleFaultTypeStr, `ReportFaultRate` = @ReportFaultRate, `Confirmed` = @Confirmed, `Repairing` = @Repairing, `ReportRepaired` = @ReportRepaired, `ExtraRepaired` = @ExtraRepaired, `RepairFaultType` = @RepairFaultType, `RepairCount` = @RepairCount, `RepairSingleFaultTypeStr` = @RepairSingleFaultTypeStr, `RepairCancel` = @RepairCancel",
                    npcMonitoringDay.Values);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private static void Script()
        {
            var redisPre = "Script";
            var redisLock = $"{redisPre}:Lock";
            if (ServerConfig.RedisHelper.SetIfNotExist(redisLock, DateTime.Now.ToStr()))
            {
                ServerConfig.RedisHelper.SetExpireAt(redisLock, DateTime.Now.AddMinutes(5));
                try
                {
                    var all = ServerConfig.ApiDb.Query<UsuallyDictionary>("SELECT * FROM `usually_dictionary` WHERE MarkedDelete = 0;");
                    var scripts = new List<ScriptVersion>();
                    foreach (var grouping in all.GroupBy(x => x.ScriptId))
                    {
                        var scriptId = grouping.Key;
                        var us = all.Where(x => x.ScriptId == scriptId).OrderBy(x => x.VariableNameId)
                            .ThenByDescending(y => y.VariableTypeId);

                        var script = new ScriptVersion();
                        script.Id = scriptId;
                        script.ValueNumber = us.Count(x => x.VariableTypeId == 1);
                        script.InputNumber = us.Count(x => x.VariableTypeId == 2);
                        script.OutputNumber = us.Count(x => x.VariableTypeId == 3);
                        script.MaxValuePointerAddress = us.Any(x => x.VariableTypeId == 1) ? us.Where(x => x.VariableTypeId == 1).Max(x => x.DictionaryId) < 300 ? 300 : us.Where(x => x.VariableTypeId == 1).Max(x => x.DictionaryId) : 300;
                        script.MaxInputPointerAddress = us.Any(x => x.VariableTypeId == 2) ? us.Where(x => x.VariableTypeId == 2).Max(x => x.DictionaryId) < 255 ? 255 : us.Where(x => x.VariableTypeId == 2).Max(x => x.DictionaryId) : 255;
                        script.MaxOutputPointerAddress = us.Any(x => x.VariableTypeId == 3) ? us.Where(x => x.VariableTypeId == 3).Max(x => x.DictionaryId) < 255 ? 255 : us.Where(x => x.VariableTypeId == 3).Max(x => x.DictionaryId) : 255;
                        var msg = new DeviceInfoMessagePacket(script.MaxValuePointerAddress, script.MaxInputPointerAddress, script.MaxOutputPointerAddress);
                        script.HeartPacket = msg.Serialize();
                        scripts.Add(script);
                    }

                    ServerConfig.ApiDb.Execute(
                        "UPDATE script_version SET `ValueNumber` = @ValueNumber, `InputNumber` = @InputNumber, `OutputNumber` = @OutputNumber, " +
                        "`MaxValuePointerAddress` = @MaxValuePointerAddress, `MaxInputPointerAddress` = @MaxInputPointerAddress, `MaxOutputPointerAddress` = @MaxOutputPointerAddress, " +
                        "`HeartPacket` = @HeartPacket WHERE `Id` = @Id;", scripts);

                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                ServerConfig.RedisHelper.Remove(redisLock);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        private static void UpdateProcessLog()
        {
#if !DEBUG
            if (ServerConfig.RedisHelper.Get<int>("Debug") != 0)
            {
                return;
            }
#endif
            var redisPre = "UpdateProcessLog";
            var redisLock = $"{redisPre}:Lock";
            if (ServerConfig.RedisHelper.SetIfNotExist(redisLock, DateTime.Now.ToStr()))
            {
                try
                {
                    ServerConfig.RedisHelper.SetExpireAt(redisLock, DateTime.Now.AddHours(1));
                    //设备状态
                    var stateDId = 1;
                    var all = ServerConfig.ApiDb.Query<dynamic>("SELECT a.Id, a.DeviceId, a.StartTime, ScriptId FROM (SELECT a.Id, DeviceId, StartTime, ScriptId FROM `npc_monitoring_process_log` a JOIN `device_library` b ON a.DeviceId = b.Id WHERE ISNULL(EndTime) GROUP BY DeviceId ) a JOIN ( SELECT MAX(Id) Id, DeviceId FROM `npc_monitoring_process_log` GROUP BY DeviceId ) b ON a.DeviceId = b.DeviceId WHERE a.Id != b.Id;");
                    var deviceList = all.ToDictionary(x => x.DeviceId);
                    if (deviceList.Any())
                    {
                        var scripts = all.GroupBy(x => x.ScriptId).Select(x => x.Key).ToList();
                        scripts.Add(0);
                        IEnumerable<UsuallyDictionary> usuallyDictionaries = null;
                        if (scripts.Any())
                        {
                            usuallyDictionaries = ServerConfig.ApiDb.Query<UsuallyDictionary>(
                                "SELECT * FROM `usually_dictionary` WHERE ScriptId IN @ScriptId AND VariableNameId = @VariableNameId;",
                                new
                                {
                                    ScriptId = scripts,
                                    VariableNameId = stateDId,
                                });
                        }

                        var uDies = new Dictionary<Tuple<int, int>, int>();
                        foreach (var script in scripts.Where(x => x != 0))
                        {
                            var udd = usuallyDictionaries.FirstOrDefault(x =>
                                x.ScriptId == script && x.VariableNameId == stateDId);
                            var address =
                                udd?.DictionaryId ?? usuallyDictionaries.First(x =>
                                        x.ScriptId == 0 && x.VariableNameId == stateDId)
                                    .DictionaryId;

                            uDies.Add(new Tuple<int, int>(script, stateDId), address);
                        }

                        foreach (var a in all)
                        {
                            var nextStartTime = ServerConfig.ApiDb.Query<DateTime>("SELECT StartTime FROM `npc_monitoring_process_log` WHERE DeviceId = @DeviceId AND OpName = '加工' AND Id > @Id LIMIT 1;", new
                            {
                                DeviceId = a.DeviceId,
                                Id = a.Id
                            }).FirstOrDefault();
                            if (nextStartTime != default(DateTime))
                            {
                                var r = new List<MonitoringAnalysis>();
                                DateTime sendTime1 = a.StartTime;
                                var deal = false;
                                while (true)
                                {
                                    var sendTime2 = sendTime1.AddMinutes(30);
                                    if (sendTime2 >= nextStartTime)
                                    {
                                        sendTime2 = nextStartTime;
                                        deal = true;
                                    }
                                    if (sendTime1 < sendTime2)
                                    {
                                        r.AddRange(ServerConfig.ApiDb.Query<MonitoringAnalysis>(
                                            "SELECT * FROM `npc_monitoring_analysis` WHERE DeviceId = @DeviceId AND SendTime BETWEEN @SendTime1 AND @SendTime2 AND UserSend = 0;",
                                                new
                                                {
                                                    DeviceId = a.DeviceId,
                                                    SendTime1 = sendTime1,
                                                    SendTime2 = sendTime2,
                                                }, 120).OrderBy(x => x.SendTime));
                                    }

                                    if (deal)
                                    {
                                        break;
                                    }

                                    sendTime1 = sendTime2;
                                }
                                if (r.Any())
                                {
                                    r = r.OrderBy(x => x.SendTime).ToList();
                                    var actAddress = uDies[new Tuple<int, int>(a.ScriptId, stateDId)] - 1;
                                    var analysis = r.FirstOrDefault(x => x.AnalysisData.vals.Count > actAddress && x.AnalysisData.vals[actAddress] == 0);
                                    if (analysis != null)
                                    {
                                        ServerConfig.ApiDb.Execute(
                                            "UPDATE`npc_monitoring_process_log` SET EndTime = @EndTime, Later = 1 WHERE Id = @Id;",
                                            new MonitoringProcessLog
                                            {
                                                EndTime = analysis.SendTime.NoMillisecond(),
                                                Id = a.Id
                                            });
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                ServerConfig.RedisHelper.Remove(redisLock);
            }
        }

        /// <summary>
        ///流程卡上报初始化
        /// </summary>
        public static void FlowCardReport(bool isReport = false)
        {
#if !DEBUG
            if (ServerConfig.RedisHelper.Get<int>("Debug") != 0)
            {
                return;
            }
#endif

            var redisPre = "FlowCardReport";
            var redisLock = $"{redisPre}:Lock";
            var deviceKey = $"{redisPre}:Device";
            var change = false;
            if (ServerConfig.RedisHelper.SetIfNotExist(redisLock, DateTime.Now.ToStr()))
            {
                try
                {
                    ServerConfig.RedisHelper.SetExpireAt(redisLock, DateTime.Now.AddMinutes(5));
                    var deviceList = new List<FlowCardReport>();
                    var dl = ServerConfig.RedisHelper.Get<IEnumerable<FlowCardReport>>(deviceKey);
                    var deviceListDb = ServerConfig.ApiDb.Query<FlowCardReport>(
                        "SELECT IFNULL(b.Id, 0) Id, IFNULL(b.DeviceId, a.Id) DeviceId FROM `device_library` a LEFT JOIN (SELECT MAX(id) Id, DeviceId FROM `npc_monitoring_process_log` WHERE OpName = '加工' AND NOT ISNULL(EndTime) GROUP BY DeviceId) b ON a.Id = b.DeviceId");
                    if (dl != null)
                    {
                        deviceList.AddRange(dl);
                    }

                    foreach (var device in deviceListDb)
                    {
                        if (deviceList.All(x => x.DeviceId != device.DeviceId))
                        {
                            change = true;
                            deviceList.Add(device);
                        }
                    }

                    if (change)
                    {
                        ServerConfig.RedisHelper.SetForever(deviceKey, deviceList);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }

                ServerConfig.RedisHelper.Remove(redisLock);
            }
        }
    }
}
