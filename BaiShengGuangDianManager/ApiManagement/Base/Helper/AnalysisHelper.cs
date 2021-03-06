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
using System.Diagnostics;
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
        /// <summary>
        /// 设备状态
        /// </summary>
        public static int stateDId = 1;
        /// <summary>
        /// 已加工时间
        /// </summary>
        public static int processedTimeDId = 3;
        /// <summary>
        /// 剩余加工时间
        /// </summary>
        public static int leftProcessTimeDId = 4;
        /// <summary>
        /// 总加工次数
        /// </summary>
        public static int processCountDId = 63;
        /// <summary>
        /// 总加工时间
        /// </summary>
        public static int totalProcessTimeDId = 64;
        /// <summary>
        /// 当前加工流程卡号
        /// </summary>
        public static int currentFlowCardDId = 6;
        /// <summary>
        /// 流程卡号缓存 下次加工
        /// </summary>
        public static int nextFlowCardDId = 113;
        /// <summary>
        /// 当前加工计划号
        /// </summary>
        public static int currentProductDId = 114;
        /// <summary>
        /// 累积运行总时间
        /// </summary>
        public static int runTimeDId = 5;
        /// <summary>
        /// 设定工艺 起始地址  变量名带a
        /// </summary>
        public static int actProcessDid = 65;
        /// <summary>
        /// 工艺 8行6列
        /// </summary>
        public static int processCnt = 48;
        /// <summary>
        /// 设备常用脚本变量
        /// </summary>
        public static List<int> VariableNameIdList = new List<int>
        {
            stateDId,
            processCountDId,
            totalProcessTimeDId,
            currentFlowCardDId,
            runTimeDId
        };
        private static int _dealLength = 1000;
        //public static MonitoringKanBan MonitoringKanBan;
        //private static MonitoringKanBan _monitoringKanBan;
        /// <summary>
        /// Set Id
        /// </summary>
        public static Dictionary<int, MonitoringKanBan> MonitoringKanBanDic = new Dictionary<int, MonitoringKanBan>();
        /// <summary>
        /// DeviceId
        /// </summary>
        public static Dictionary<int, MonitoringKanBanDevice> MonitoringKanBanDeviceDic = new Dictionary<int, MonitoringKanBanDevice>();

        public static void Init()
        {
            try
            {
                for (var i = 0; i < processCnt; i++)
                {
                    VariableNameIdList.Add(actProcessDid + i);
                }

#if DEBUG
                Console.WriteLine("AnalysisHelper 调试模式已开启");
#else
                Console.WriteLine("AnalysisHelper 发布模式已开启");
#endif
                if (!RedisHelper.Exists(Debug))
                {
                    RedisHelper.SetForever(Debug, 0);
                }

                var redisPre = "Analysis";
                var redisLock = $"{redisPre}:Lock";
                var idKey = $"{redisPre}:Id";
                var deviceKey = $"{redisPre}:Device";
                RedisHelper.Remove(redisLock);
                MonitoringKanBanDic.AddRange(ServerConfig.ApiDb.Query<MonitoringKanBan>("SELECT * FROM (SELECT * FROM `npc_monitoring_kanban` ORDER BY Date DESC) a GROUP BY a.Id;", null, 60).ToDictionary(x => x.Id));
                MonitoringKanBanDeviceDic.AddRange(ServerConfig.ApiDb.Query<MonitoringKanBanDevice>("SELECT * FROM (SELECT * FROM `npc_monitoring_kanban_device` ORDER BY Date DESC) a GROUP BY a.DeviceId;").ToDictionary(x => x.DeviceId));

                _timer2S = new Timer(DoSth_2s, null, 10000, 2000);
                _timer10S = new Timer(DoSth_10s, null, 10000, 1000 * 10);
                _timer60S = new Timer(DoSth_60s, null, 5000, 1000 * 60);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private static void DoSth_2s(object state)
        {
            Analysis();
        }

        private static void DoSth_10s(object state)
        {
            AnalysisOther();
            Delete();
            Fault();
            ProcessSum();
            ProcessTime();
            Script();
        }

        private static void DoSth_60s(object state)
        {
            //UpdateProcessLog();
            //FlowCardReport();
        }

        /// <summary>
        /// 加工统计  分析
        /// </summary>
        private static void ProcessSum()
        {
#if !DEBUG
            if (RedisHelper.Get<int>("Debug") != 0)
            {
                return;
            }
#endif

            var redisPre = "ProcessSum";
            var redisLock = $"{redisPre}:Lock";
            var idKey = $"{redisPre}:Id";
            var deviceKey = $"{redisPre}:Device";
            if (RedisHelper.SetIfNotExist(redisLock, DateTime.Now.ToStr()))
            {
                try
                {
                    RedisHelper.SetExpireAt(redisLock, DateTime.Now.AddHours(1));
                    var startId = RedisHelper.Get<int>(idKey);
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
                            if (RedisHelper.Exists(deviceKey))
                            {
                                deviceList = RedisHelper.Get<Dictionary<int, MonitoringProcessSum>>(deviceKey);
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

                            RedisHelper.SetForever(deviceKey, deviceList);

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

                            RedisHelper.SetForever(idKey, endId);
                        }
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
        /// 加工次数  分析
        /// </summary>
        private static void ProcessTime()
        {
#if !DEBUG
            if (RedisHelper.Get<int>("Debug") != 0)
            {
                return;
            }
#endif

            var redisPre = "ProcessTime";
            var lockKey = $"{redisPre}:Lock";
            var idKey = $"{redisPre}:Id";
            var deviceKey = $"{redisPre}:Device";
            var offset = 60;
            if (RedisHelper.SetIfNotExist(lockKey, DateTime.Now.ToStr()))
            {
                try
                {
                    RedisHelper.SetExpireAt(lockKey, DateTime.Now.AddHours(1));
                    var startId = RedisHelper.Get<int>(idKey);
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
                            if (RedisHelper.Exists(deviceKey))
                            {
                                deviceList = RedisHelper.Get<Dictionary<int, MonitoringProcessTime>>(deviceKey);
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

                            RedisHelper.SetForever(deviceKey, deviceList);

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

                            RedisHelper.SetForever(idKey, endId);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                RedisHelper.Remove(lockKey);
            }

        }

        private static void Delete()
        {
            return;
            //var redisPre = "Time";
            //var redisLock = $"{redisPre}:Lock";
            //if (RedisHelper.SetIfNotExist(redisLock, DateTime.Now.ToStr()))
            //{
            //    try
            //    {
            //        RedisHelper.SetExpireAt(redisLock, DateTime.Now.AddMinutes(5));
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
            //    RedisHelper.Remove(redisLock);
            //}
        }

        /// <summary>
        /// 
        /// </summary>
        private static void Analysis()
        {
#if !DEBUG
            if (RedisHelper.Get<int>("Debug") != 0)
            {
                return;
            }
#endif
            var redisPre = "Analysis";
            var redisLock = $"{redisPre}:Lock";
            var idKey = $"{redisPre}:Id";
            var deviceKey = $"{redisPre}:Device";
            var exceptDeviceKey = $"{redisPre}:ExceptDevice";
            if (RedisHelper.SetIfNotExist(redisLock, DateTime.Now.ToStr()))
            {
                try
                {
                    var now = DateTime.Now;
                    RedisHelper.SetExpireAt(redisLock, DateTime.Now.AddMinutes(5));
                    if (!RedisHelper.Exists(exceptDeviceKey))
                    {
                        RedisHelper.SetForever(exceptDeviceKey, "");
                    }
                    var exceptDevicesStr = RedisHelper.Get<string>(exceptDeviceKey);
                    var exceptDevices = new List<int>();
                    if (!exceptDevicesStr.IsNullOrEmpty())
                    {
                        exceptDevices.AddRange(exceptDevicesStr.Split(",").Select(int.Parse));
                    }

                    var monitoringKanBanList = new List<MonitoringKanBan>();
                    var monitoringKanBanDeviceList = new List<MonitoringKanBanDevice>();
                    var monitoringProcesses = new List<MonitoringProcess>();
                    var flowCardProcessStep = new List<FlowCardProcessStepDetail>();

                    var allDeviceList = ServerConfig.ApiDb.Query<MonitoringProcess>(
                        "SELECT b.*, c.DeviceCategoryId, c.CategoryName, a.`Code`, a.`ScriptId` FROM `device_library` a JOIN `npc_proxy_link` b ON a.Id = b.DeviceId JOIN (SELECT a.*, b.CategoryName FROM `device_model` a JOIN `device_category` b ON a.DeviceCategoryId = b.Id) c ON a.DeviceModelId = c.Id WHERE a.MarkedDelete = 0;").ToList();

                    foreach (var device in allDeviceList)
                    {
                        if (!MonitoringKanBanDeviceDic.ContainsKey(device.DeviceId))
                        {
                            MonitoringKanBanDeviceDic.Add(device.DeviceId, new MonitoringKanBanDevice
                            {
                                Time = now,
                                Code = device.Code,
                                DeviceId = device.DeviceId,
                                AllDevice = 1
                            });
                        }
                        else
                        {
                            MonitoringKanBanDeviceDic[device.DeviceId].Code = device.Code;
                        }
                    }

                    var removeDeviceId =
                        MonitoringKanBanDeviceDic.Keys.Where(deviceId =>
                            allDeviceList.All(x => x.DeviceId != deviceId)).ToList();
                    foreach (var deviceId in removeDeviceId)
                    {
                        MonitoringKanBanDeviceDic.Remove(deviceId);
                    }

                    foreach (var key in MonitoringKanBanDeviceDic.Keys)
                    {
                        MonitoringKanBanDeviceDic[key].FaultDevice = 0;
                    }

                    var startId = RedisHelper.Get<int>(idKey);
                    var mData = ServerConfig.ApiDb.Query<MonitoringData>(
                        "SELECT * FROM `npc_monitoring_analysis` WHERE Id > @Id AND UserSend = 0 ORDER BY Id LIMIT @limit;", new
                        {
                            Id = startId,
                            limit = _dealLength
                        });

                    var kanBanTime = DateTime.Now;
                    string sql;
                    if (mData.Any())
                    {
                        var minTime = mData.Min(x => x.SendTime);
                        var maxTime = mData.Max(x => x.SendTime);
                        var faultDevices = ServerConfig.ApiDb.Query<dynamic>(
                            "SELECT DeviceId FROM `fault_device_repair` WHERE DeviceId != 0 AND MarkedDelete != 1 AND State < @State AND FaultTime <= @FaultTime2 GROUP BY DeviceId;", new
                            //"SELECT DeviceId FROM `fault_device_repair` WHERE DeviceId != 0 AND MarkedDelete != 1 AND State < 3 AND FaultTime >= @FaultTime1 AND FaultTime <= @FaultTime2 GROUP BY DeviceId;", new
                            {
                                State = RepairStateEnum.Complete,
                                FaultTime1 = minTime.DayBeginTime(),
                                FaultTime2 = maxTime.DayEndTime(),
                            });
                        foreach (var faultDevice in faultDevices)
                        {
                            var deviceId = faultDevice.DeviceId;
                            if (MonitoringKanBanDeviceDic.ContainsKey(deviceId))
                            {
                                MonitoringKanBanDeviceDic[deviceId].FaultDevice = 1;
                            }
                        }

                        mData = mData.OrderBy(x => x.SendTime);
                        var endId = mData.Last().Id;
                        if (endId > startId)
                        {
                            #region  加工记录
                            var existDeviceId = mData.GroupBy(x => x.DeviceId).Select(y => y.Key);
                            var deviceList = allDeviceList.Where(x => existDeviceId.Any(y => y == x.DeviceId)).ToDictionary(x => x.DeviceId, x => (MonitoringProcess)x.Clone());
                            if (RedisHelper.Exists(deviceKey))
                            {
                                var redisDeviceList = RedisHelper.Get<IEnumerable<RedisMonitoringProcess>>(deviceKey);
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

                            if (deviceList.Any())
                            {
                                var scriptIds = mData.GroupBy(x => x.ScriptId).Select(x => x.Key).ToList();
                                scriptIds.Add(0);
                                IEnumerable<UsuallyDictionary> usuallyDictionaries = null;
                                if (scriptIds.Any())
                                {
                                    usuallyDictionaries = ServerConfig.ApiDb.Query<UsuallyDictionary>(
                                        "SELECT * FROM `usually_dictionary` WHERE ScriptId IN @ScriptId AND VariableNameId IN @VariableNameId;",
                                        new
                                        {
                                            ScriptId = scriptIds,
                                            VariableNameId = VariableNameIdList,
                                        });
                                }

                                if (usuallyDictionaries == null || !VariableNameIdList.All(x => usuallyDictionaries.Any(y => y.VariableNameId == x)))
                                {
                                    Log.ErrorFormat("缺少变量配置:{0}", VariableNameIdList.Where(x => usuallyDictionaries.All(y => y.VariableNameId != x)).ToJSON());
                                    RedisHelper.Remove(redisLock);
                                    return;
                                }
                                var uDies = new Dictionary<Tuple<int, int>, int>();
                                foreach (var variableNameId in VariableNameIdList)
                                {
                                    foreach (var script in scriptIds.Where(x => x != 0))
                                    {
                                        var udd = usuallyDictionaries.FirstOrDefault(x =>
                                            x.ScriptId == script && x.VariableNameId == variableNameId);
                                        var address =
                                            udd?.DictionaryId ?? usuallyDictionaries.First(x =>
                                               x.ScriptId == 0 && x.VariableNameId == variableNameId).DictionaryId;

                                        uDies.Add(new Tuple<int, int>(script, variableNameId), address);
                                    }
                                }
                                if (usuallyDictionaries != null && usuallyDictionaries.Any())
                                {
                                    var firstNotInSameDay = false;
                                    foreach (var data in mData)
                                    {
                                        if (!deviceList.ContainsKey(data.DeviceId))
                                        {
                                            continue;
                                        }

                                        var time = data.SendTime.NoMillisecond();
                                        var containsDevice = MonitoringKanBanDeviceDic.ContainsKey(data.DeviceId);
                                        var sameDayDevice = containsDevice && deviceList[data.DeviceId].Time.InSameDay(time);
                                        if (!deviceList[data.DeviceId].Time.InSameDay(time))
                                        {
                                            deviceList[data.DeviceId].Time = time;
                                            var monitoringProcess = new MonitoringProcess
                                            {
                                                Time = deviceList[data.DeviceId].Time,
                                                DeviceId = deviceList[data.DeviceId].DeviceId,
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

                                            if (!firstNotInSameDay)
                                            {
                                                firstNotInSameDay = true;
                                                UpdateKanBan(allDeviceList, deviceList[data.DeviceId].Time, true);
                                                monitoringKanBanDeviceList.AddRange(MonitoringKanBanDeviceDic.Values);
                                                monitoringKanBanList.AddRange(MonitoringKanBanDic.Values);
                                            }

                                            deviceList[data.DeviceId].ProcessCount = 0;
                                            deviceList[data.DeviceId].ProcessTime = 0;
                                            deviceList[data.DeviceId].RunTime = 0;

                                            MonitoringKanBanDeviceDic[data.DeviceId] = new MonitoringKanBanDevice
                                            {
                                                Time = time,
                                                DeviceId = data.DeviceId,
                                                AllDevice = 1
                                            };
                                            if (sameDayDevice)
                                            {
                                                var faultDevice = ServerConfig.ApiDb.Query<int>(
                                                    "SELECT COUNT(1) FROM `fault_device_repair` WHERE DeviceId = @DeviceId AND FaultTime >= @FaultTime1 AND FaultTime <= @FaultTime2;", new
                                                    {
                                                        FaultTime1 = MonitoringKanBanDeviceDic[data.DeviceId].Time.DayBeginTime(),
                                                        FaultTime2 = MonitoringKanBanDeviceDic[data.DeviceId].Time.DayEndTime(),
                                                        data.DeviceId
                                                    }).FirstOrDefault() > 0;
                                                MonitoringKanBanDeviceDic[data.DeviceId].FaultDevice = faultDevice ? 1 : 0;
                                            }
                                        }
                                        MonitoringKanBanDeviceDic[data.DeviceId].Time = time;
                                        deviceList[data.DeviceId].Time = time;
                                        if (sameDayDevice)
                                        {
                                            MonitoringKanBanDeviceDic[data.DeviceId].NormalDevice = 1;
                                        }
                                        var analysisData = data.AnalysisData;
                                        if (analysisData != null)
                                        {
                                            MonitoringKanBanDeviceDic[data.DeviceId].AnalysisData = analysisData;
                                            var uKey = new Tuple<int, int>(data.ScriptId, currentFlowCardDId);
                                            if (!uDies.ContainsKey(uKey))
                                            {
                                                continue;
                                            }

                                            var actAddress = uDies[uKey] - 1;
                                            var currentFlowCardId = 0;
                                            var currentFlowCard = "";
                                            FlowCardProcessStepDetail flowCardProcessStepDetail = null;
                                            if (actAddress >= 0 && analysisData.vals.Count > actAddress)
                                            {
                                                currentFlowCardId = analysisData.vals[actAddress];
                                                currentFlowCard = currentFlowCardId.ToString();
                                            }

                                            //状态
                                            actAddress = uDies[new Tuple<int, int>(data.ScriptId, stateDId)] - 1;
                                            if (actAddress >= 0 && analysisData.vals.Count > actAddress)
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
                                                            ServerConfig.ApiDb
                                                                .Query<FlowCard>(
                                                                    "SELECT Id, FlowCardName FROM `flowcard_library` WHERE Id = @currentFlowCardId;",
                                                                    new { currentFlowCardId }).FirstOrDefault();

                                                        if (flowCard != null)
                                                        {
                                                            var flowCardProcessStepDetails =
                                                                ServerConfig.ApiDb.Query<FlowCardProcessStepDetail>(
                                                                    "SELECT a.* FROM `flowcard_process_step` a JOIN `device_process_step` b ON a.ProcessStepId = b.Id WHERE b.IsSurvey = 0 AND a.FlowCardId = @FlowCardId AND a.DeviceId = @DeviceId;",
                                                                    new
                                                                    {
                                                                        FlowCardId = flowCard.Id,
                                                                        DeviceId = data.DeviceId
                                                                    });
                                                            currentFlowCard = flowCard.FlowCardName;
                                                            flowCardProcessStepDetail = flowCardProcessStepDetails.FirstOrDefault();
                                                            if (flowCardProcessStepDetail != null)
                                                            {
                                                                sql = string.Empty;
                                                                //开始加工
                                                                if (bStart)
                                                                {
                                                                    if (flowCardProcessStepDetail.ProcessTime == default(DateTime))
                                                                    {
                                                                        flowCardProcessStepDetail.ProcessTime = data.SendTime;
                                                                        flowCardProcessStep.Add(flowCardProcessStepDetail);
                                                                        //sql =
                                                                        //    "UPDATE flowcard_process_step SET `ProcessTime` = @ProcessTime WHERE `Id` = @Id;";
                                                                    }
                                                                }

                                                                //停止加工
                                                                if (bEnd)
                                                                {
                                                                    //if (flowCardProcessStepDetail.ProcessEndTime == default(DateTime))
                                                                    //{
                                                                    flowCardProcessStepDetail.ProcessEndTime = data.SendTime;
                                                                    flowCardProcessStep.Add(flowCardProcessStepDetail);
                                                                    //sql =
                                                                    //    "UPDATE flowcard_process_step SET `ProcessEndTime` = @ProcessEndTime WHERE `Id` = @Id;";
                                                                    //}
                                                                }

                                                                //if (sql != string.Empty)
                                                                //{
                                                                //    ServerConfig.ApiDb.Execute(sql,
                                                                //        flowCardProcessStepDetail);
                                                                //}
                                                            }
                                                        }
                                                    }

                                                    //else if (deviceList[data.DeviceId].State == v && currentFlowCardId != 0)
                                                    //{
                                                    //    deviceList[data.DeviceId].FlowCardId = currentFlowCardId;
                                                    //}
                                                    deviceList[data.DeviceId].FlowCardId = currentFlowCardId;
                                                    if (MonitoringKanBanDeviceDic.ContainsKey(data.DeviceId))
                                                    {
                                                        MonitoringKanBanDeviceDic[data.DeviceId].ProcessDevice =
                                                            v > 0 ? 1 : 0;
                                                    }
                                                }

                                                var processData = new Dictionary<int, int[]>();
                                                ;
                                                var pActAddress =
                                                    uDies[
                                                        new Tuple<int, int>(data.ScriptId,
                                                            actProcessDid + processCnt - 1)] - 1;
                                                if (analysisData.vals.Count >= pActAddress)
                                                {
                                                    for (var i = 0; i < processCnt; i++)
                                                    {
                                                        pActAddress =
                                                            uDies[
                                                                new Tuple<int, int>(data.ScriptId, actProcessDid + i)] -
                                                            1;
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

                                                if (!exceptDevices.Contains(data.DeviceId))
                                                {
                                                    currentFlowCardId = 0;
                                                }

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
                                                            RequirementMid =
                                                                flowCardProcessStepDetail?.ProcessStepRequirementMid ??
                                                                0,
                                                        });

                                                    if (sameDayDevice)
                                                    {
                                                        MonitoringKanBanDeviceDic[data.DeviceId].ProcessDevice = 1;
                                                    }
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
                                                    if (sameDayDevice)
                                                    {
                                                        MonitoringKanBanDeviceDic[data.DeviceId].ProcessDevice = 0;
                                                    }
                                                }

                                                if (v > 0)
                                                {
                                                    if (!MonitoringKanBanDeviceDic[data.DeviceId].UseList
                                                        .Contains(data.DeviceId))
                                                    {
                                                        MonitoringKanBanDeviceDic[data.DeviceId].UseList
                                                            .Add(data.DeviceId);
                                                    }

                                                    if (!MonitoringKanBanDeviceDic[data.DeviceId].MaxUseList
                                                        .Contains(data.DeviceId))
                                                    {
                                                        MonitoringKanBanDeviceDic[data.DeviceId].MaxUseList
                                                            .Add(data.DeviceId);
                                                    }

                                                    MonitoringKanBanDeviceDic[data.DeviceId].ProcessDevice = 1;
                                                }

                                                if (v == 0)
                                                {
                                                    if (MonitoringKanBanDeviceDic[data.DeviceId].UseList
                                                        .Contains(data.DeviceId))
                                                    {
                                                        MonitoringKanBanDeviceDic[data.DeviceId].UseList
                                                            .Remove(data.DeviceId);
                                                    }

                                                    MonitoringKanBanDeviceDic[data.DeviceId].ProcessDevice = 0;
                                                }

                                                deviceList[data.DeviceId].State = v > 0 ? 1 : 0;
                                            }

                                            //总加工次数
                                            actAddress = uDies[new Tuple<int, int>(data.ScriptId, processCountDId)] - 1;
                                            if (actAddress >= 0 && analysisData.vals.Count > actAddress)
                                            {
                                                var totalProcessCount = analysisData.vals[actAddress];
                                                if (deviceList[data.DeviceId].TotalProcessCount < totalProcessCount)
                                                {
                                                    deviceList[data.DeviceId].ProcessCount +=
                                                        totalProcessCount - deviceList[data.DeviceId].TotalProcessCount;
                                                }

                                                deviceList[data.DeviceId].TotalProcessCount = totalProcessCount;
                                            }

                                            //总加工时间
                                            actAddress = uDies[new Tuple<int, int>(data.ScriptId, totalProcessTimeDId)] - 1;
                                            if (actAddress >= 0 && analysisData.vals.Count > actAddress)
                                            {
                                                var totalProcessTime = analysisData.vals[actAddress];
                                                if (deviceList[data.DeviceId].TotalProcessTime < totalProcessTime)
                                                {
                                                    deviceList[data.DeviceId].ProcessTime +=
                                                        totalProcessTime - deviceList[data.DeviceId].TotalProcessTime;
                                                }

                                                deviceList[data.DeviceId].TotalProcessTime = totalProcessTime;
                                            }

                                            //总运行时间
                                            actAddress = uDies[new Tuple<int, int>(data.ScriptId, runTimeDId)] - 1;
                                            if (actAddress >= 0 && analysisData.vals.Count > actAddress)
                                            {
                                                var totalRunTime = analysisData.vals[actAddress];
                                                if (deviceList[data.DeviceId].TotalRunTime < totalRunTime)
                                                {
                                                    deviceList[data.DeviceId].RunTime +=
                                                        totalRunTime - deviceList[data.DeviceId].TotalRunTime;
                                                }

                                                deviceList[data.DeviceId].TotalRunTime = totalRunTime;
                                            }

                                            var rate = MonitoringKanBanDeviceDic[data.DeviceId].SingleProcessRate
                                                .FirstOrDefault(x => x.Id == data.DeviceId);
                                            var device = allDeviceList.FirstOrDefault(x => x.DeviceId == data.DeviceId);
                                            if (device != null)
                                            {
                                                var ProcessTime =
                                                    device.Time.InSameDay(MonitoringKanBanDeviceDic[data.DeviceId].Time)
                                                        ? device.ProcessTime
                                                        : 0;
                                                MonitoringKanBanDeviceDic[data.DeviceId].ProcessTime = ProcessTime;
                                                if (rate == null)
                                                {
                                                    MonitoringKanBanDeviceDic[data.DeviceId].SingleProcessRate.Add(
                                                        new ProcessUseRate
                                                        {
                                                            Id = device.DeviceId,
                                                            Code = device.Code,
                                                            Rate = (ProcessTime * 1m / (24 * 3600)).ToRound(4)
                                                        });
                                                }
                                                else
                                                {
                                                    rate.Rate = (ProcessTime * 1m / (24 * 3600)).ToRound(4);
                                                }
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
                                                Use = allDeviceList.Count(x =>
                                                    Math.Abs((x.Time - time).TotalMinutes) <= 3 && x.State == 1),
                                                Total = allDeviceList.Count(),
                                            };

                                            monitoringProcess.Rate =
                                                (decimal)monitoringProcess.Use * 100 / monitoringProcess.Total;
                                            monitoringProcesses.Add(monitoringProcess);
                                        }
                                    }
                                }
                            }
                            #endregion

                            RedisHelper.SetForever(deviceKey, deviceList.Values.Select(x => new
                            {
                                x.DeviceId,
                                x.State,
                                x.Time,
                                TimeStr = x.Time.ToStr()
                            }));

                            if (flowCardProcessStep.Any())
                            {
                                ServerConfig.ApiDb.Execute("UPDATE flowcard_process_step SET " +
                                                           "`ProcessTime` = IF(@ProcessTime = '0001-01-01 00:00:00', `ProcessTime`, @ProcessTime)`" +
                                                           "`ProcessEndTime` = IF(@ProcessEndTime = '0001-01-01 00:00:00', `ProcessEndTime`, @ProcessEndTime)`" +
                                                           " WHERE `Id` = @Id;", flowCardProcessStep);
                            }
                            var updateDeviceList = deviceList.Values.Where(x =>
                                allDeviceList.Any(y => y.DeviceId == x.DeviceId) &&
                                x.HaveChange(allDeviceList.First(y => y.DeviceId == x.DeviceId)));
                            if (updateDeviceList.Any())
                            {
                                ServerConfig.ApiDb.Execute(
                                "UPDATE npc_proxy_link SET `Time` = @Time, `State` = @State, `ProcessCount` = @ProcessCount, `TotalProcessCount` = @TotalProcessCount, " +
                                "`ProcessTime` = @ProcessTime, `TotalProcessTime` = @TotalProcessTime, `RunTime` = @RunTime, `TotalRunTime` = @TotalRunTime WHERE `DeviceId` = @DeviceId;",
                                    updateDeviceList);
                            }
                            RedisHelper.SetForever(idKey, endId);
                        }

                        kanBanTime = mData.Last().SendTime.NoMillisecond();
                    }

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
                            sql = string.Format(
                                "SELECT {4} DeviceId, DATE({0}) Time, SUM({1}) FaChu, SUM({2}) HeGe, SUM({3}) LiePian, IF(SUM({1}) = 0, 0, round(SUM({2})/SUM({1}), 2)) Rate " +
                                "FROM `flowcard_library` WHERE {4} IN @DeviceId AND {0} >= @startTime AND {0} <= @endTime GROUP BY {4}",
                                category[0], category[1], category[2], category[3], category[4]);
                            var monitoringProductionData = ServerConfig.ApiDb.Query<MonitoringProductionData>(sql, new
                            {
                                DeviceId = devices.Select(x => x.DeviceId),
                                startTime = kanBanTime.DayBeginTime(),
                                endTime = kanBanTime.DayEndTime(),
                            }, 60);

                            foreach (var data in monitoringProductionData)
                            {
                                if (MonitoringKanBanDeviceDic.ContainsKey(data.DeviceId) && MonitoringKanBanDeviceDic[data.DeviceId].Time.InSameDay(data.Time))
                                {
                                    MonitoringKanBanDeviceDic[data.DeviceId].FaChu = data.FaChu;
                                    MonitoringKanBanDeviceDic[data.DeviceId].HeGe = data.HeGe;
                                    MonitoringKanBanDeviceDic[data.DeviceId].LiePian = data.LiePian;
                                }
                            }
                        }
                    }

                    UpdateKanBan(allDeviceList, kanBanTime, mData.Any());
                    monitoringKanBanDeviceList.AddRange(MonitoringKanBanDeviceDic.Values);
                    monitoringKanBanList.AddRange(MonitoringKanBanDic.Values);

                    InsertAnalysis(monitoringProcesses, monitoringKanBanList, monitoringKanBanDeviceList);

                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                RedisHelper.Remove(redisLock);
            }
        }

        private static void InsertAnalysis(IEnumerable<MonitoringProcess> monitoringProcesses, IEnumerable<MonitoringKanBan> monitoringKanBanList, IEnumerable<MonitoringKanBanDevice> monitoringKanBanDeviceList)
        {
            Task.Run(() =>
            {
                ServerConfig.ApiDb.Execute(
                    "INSERT INTO npc_monitoring_kanban (`Date`, `Time`, `Id`, `AllDevice`, `NormalDevice`, `ProcessDevice`, `IdleDevice`, `FaultDevice`, `ConnectErrorDevice`, `MaxUse`, `UseListStr`, " +
                    "`MaxUseListStr`, `MaxUseRate`, `MinUse`, `MinUseRate`, `MaxSimultaneousUseRate`, `MinSimultaneousUseRate`, `SingleProcessRateStr`, `AllProcessRate`, `RunTime`, " +
                    "`ProcessTime`, `IdleTime`, `FaChu`, `HeGe`, `LiePian`, `ProductionData`) " +
                    "VALUES (@Date, @Time, @Id, @AllDevice, @NormalDevice, @ProcessDevice, @IdleDevice, @FaultDevice, @ConnectErrorDevice, @MaxUse, @UseListStr, " +
                    "@MaxUseListStr ,@MaxUseRate, @MinUse, @MinUseRate, @MaxSimultaneousUseRate, @MinSimultaneousUseRate, @SingleProcessRateStr, @AllProcessRate, @RunTime, @ProcessTime, " +
                    "@IdleTime, @FaChu, @HeGe, @LiePian, @ProductionData) " +
                    "ON DUPLICATE KEY UPDATE `Time` = @Time, `AllDevice` = @AllDevice, `NormalDevice` = @NormalDevice, `ProcessDevice` = @ProcessDevice, `IdleDevice` = @IdleDevice, " +
                    "`FaultDevice` = @FaultDevice, `ConnectErrorDevice` = @ConnectErrorDevice, `MaxUse` = @MaxUse, `MaxUseListStr` = @MaxUseListStr, `UseListStr` = @UseListStr, " +
                    "`MaxUseRate` = @MaxUseRate, `MinUse` = @MinUse, `MinUseRate` = @MinUseRate, `MaxSimultaneousUseRate` = @MaxSimultaneousUseRate, `MinSimultaneousUseRate` = @MinSimultaneousUseRate, " +
                    "`SingleProcessRateStr` = @SingleProcessRateStr, `AllProcessRate` = @AllProcessRate, `RunTime` = @RunTime, `ProcessTime` = @ProcessTime, `IdleTime` = @IdleTime, " +
                    "`FaChu` = @FaChu, `HeGe` = @HeGe, `LiePian` = @LiePian, `ProductionData` = @ProductionData, `VariableData` = @VariableData;"
                    , monitoringKanBanList);

                ServerConfig.ApiDb.Execute(
                    "INSERT INTO npc_monitoring_kanban_device (`Date`, `Time`, `DeviceId`, `AllDevice`, `NormalDevice`, `ProcessDevice`, `IdleDevice`, `FaultDevice`, `ConnectErrorDevice`, `MaxUse`, `UseListStr`, " +
                    "`MaxUseListStr`, `MaxUseRate`, `MinUse`, `MinUseRate`, `MaxSimultaneousUseRate`, `MinSimultaneousUseRate`, `SingleProcessRateStr`, `AllProcessRate`, `RunTime`, " +
                    "`ProcessTime`, `IdleTime`, `FaChu`, `HeGe`, `LiePian`) VALUES (@Date, @Time, @DeviceId, @AllDevice, @NormalDevice, @ProcessDevice, @IdleDevice, @FaultDevice, @ConnectErrorDevice, @MaxUse, @UseListStr, " +
                    "@MaxUseListStr ,@MaxUseRate, @MinUse, @MinUseRate, @MaxSimultaneousUseRate, @MinSimultaneousUseRate, @SingleProcessRateStr, @AllProcessRate, @RunTime, @ProcessTime, @IdleTime, @FaChu, @HeGe, @LiePian) " +
                    "ON DUPLICATE KEY UPDATE `Time` = @Time, `AllDevice` = @AllDevice, `NormalDevice` = @NormalDevice, `ProcessDevice` = @ProcessDevice, `IdleDevice` = @IdleDevice, " +
                    "`FaultDevice` = @FaultDevice, `ConnectErrorDevice` = @ConnectErrorDevice, `MaxUse` = @MaxUse, `MaxUseListStr` = @MaxUseListStr, `UseListStr` = @UseListStr, " +
                    "`MaxUseRate` = @MaxUseRate, `MinUse` = @MinUse, `MinUseRate` = @MinUseRate, `MaxSimultaneousUseRate` = @MaxSimultaneousUseRate, `MinSimultaneousUseRate` = @MinSimultaneousUseRate, " +
                    "`SingleProcessRateStr` = @SingleProcessRateStr, `AllProcessRate` = @AllProcessRate, `RunTime` = @RunTime, `ProcessTime` = @ProcessTime, `IdleTime` = @IdleTime, " +
                    "`FaChu` = @FaChu, `HeGe` = @HeGe, `LiePian` = @LiePian;"
                    , monitoringKanBanDeviceList);

                ServerConfig.ApiDb.ExecuteTrans(
                   "INSERT INTO npc_monitoring_process (`Time`, `DeviceId`, `ProcessCount`, `TotalProcessCount`, `ProcessTime`, `TotalProcessTime`, `RunTime`, `TotalRunTime`, `State`, `Rate`, `Use`, `Total`) VALUES (@Time, @DeviceId, @ProcessCount, @TotalProcessCount, @ProcessTime, @TotalProcessTime, @RunTime, @TotalRunTime, @State, @Rate, @Use, @Total) " +
                   "ON DUPLICATE KEY UPDATE `Time` = @Time, `DeviceId` = @DeviceId, `State` = @State, `ProcessCount` = @ProcessCount, `TotalProcessCount` = @TotalProcessCount, `ProcessTime` = @ProcessTime, `TotalProcessTime` = @TotalProcessTime, `RunTime` = @RunTime, `TotalRunTime` = @TotalRunTime, `Use` = @Use, `Total` = @Total, `Rate` = @Rate;",
                   monitoringProcesses);
            });
        }

        private static void UpdateKanBan(IEnumerable<MonitoringProcess> allDeviceList, DateTime time, bool haveData)
        {
            var sets = MonitoringKanBanSetHelper.Instance.GetAll<MonitoringKanBanSet>().ToList();
            sets.Add(new MonitoringKanBanSet
            {
                Id = 0,
                Type = MonitoringKanBanEnum.设备详情看板,
                DeviceIds = allDeviceList.Select(x => x.DeviceId).Join()
            });
            foreach (var set in sets)
            {
                if (!MonitoringKanBanDic.ContainsKey(set.Id))
                {
                    MonitoringKanBanDic.Add(set.Id, new MonitoringKanBan
                    {
                        Time = time,
                        Id = set.Id
                    });
                }
            }

            var scriptIds = sets.SelectMany(x => x.VariableList.Select(y => y.ScriptId));
            var dataNameDictionaries = scriptIds.Any() ? DataNameDictionaryHelper.GetDataNameDictionaryDetails(scriptIds) : new List<DataNameDictionaryDetail>();
            var removeTypes = MonitoringKanBanDic.Keys.Where(type => sets.All(x => x.Id != type)).ToList();
            foreach (var removeType in removeTypes)
            {
                MonitoringKanBanDic.Remove(removeType);
            }

            if (!haveData)
            {
                for (var i = 0; i < MonitoringKanBanDeviceDic.Keys.Count; i++)
                {
                    var deviceId = MonitoringKanBanDeviceDic.Keys.ElementAt(i);
                    if (!MonitoringKanBanDeviceDic[deviceId].Time.InSameDay(time))
                    {
                        MonitoringKanBanDeviceDic[deviceId] = new MonitoringKanBanDevice
                        {
                            Time = time,
                            AllDevice = 1,
                            DeviceId = deviceId
                        };
                    }
                }

                var faultDevices = ServerConfig.ApiDb.Query<dynamic>(
                    "SELECT DeviceId FROM `fault_device_repair` WHERE DeviceId != 0 AND MarkedDelete != 1 AND State < @State AND FaultTime <= @FaultTime2 GROUP BY DeviceId;", new
                    //"SELECT DeviceId FROM `fault_device_repair` WHERE DeviceId != 0 AND MarkedDelete != 1 AND State < 3 AND FaultTime >= @FaultTime1 AND FaultTime <= @FaultTime2 GROUP BY DeviceId;", new
                    {
                        State = RepairStateEnum.Complete,
                        FaultTime1 = time.DayBeginTime(),
                        FaultTime2 = time.DayEndTime(),
                    });

                foreach (var faultDevice in faultDevices)
                {
                    var deviceId = faultDevice.DeviceId;
                    if (MonitoringKanBanDeviceDic.ContainsKey(deviceId))
                    {
                        MonitoringKanBanDeviceDic[deviceId].FaultDevice = 1;
                    }
                }

                for (var i = 0; i < MonitoringKanBanDic.Keys.Count; i++)
                {
                    var id = MonitoringKanBanDic.Keys.ElementAt(i);
                    if (MonitoringKanBanDic[id].Time.InSameDay(time))
                    {
                        MonitoringKanBanDic[id] = new MonitoringKanBan
                        {
                            Time = time,
                            Id = id
                        };
                    }
                }
            }

            #region MonitoringKanBanDic
            var validDate = MonitoringKanBanDeviceDic.Values.GroupBy(x => x.Date).Select(y => y.Key);
            foreach (var date in validDate)
            {
                foreach (var id in MonitoringKanBanDic.Keys)
                {
                    var set = sets.FirstOrDefault(x => x.Id == id);
                    if (set != null)
                    {
                        var allDevice = MonitoringKanBanDeviceDic.Values.Where(x => set.DeviceIdList.Contains(x.DeviceId));
                        var validMonitoringKanBanDevice = allDevice.Where(x => Math.Abs((x.Time - time).TotalMinutes) <= 5
                                                                               && x.Time.InSameDay(date));
                        MonitoringKanBanDic[id].Time = time;
                        if (set.Type == MonitoringKanBanEnum.设备详情看板)
                        {
                            #region 设备详情看板
                            MonitoringKanBanDic[id].AllDevice = set.DeviceIdList.Count();
                            MonitoringKanBanDic[id].NormalDevice =
                                validMonitoringKanBanDevice.Sum(x => x.FaultDevice > 0 ? 0 : x.NormalDevice);
                            MonitoringKanBanDic[id].ProcessDevice =
                            validMonitoringKanBanDevice.Sum(x => x.FaultDevice > 0 ? 0 : x.ProcessDevice);
                            MonitoringKanBanDic[id].FaultDevice =
                                allDevice.Sum(x => x.FaultDevice);
                            MonitoringKanBanDic[id].UseList = validMonitoringKanBanDevice.Where(x => x.FaultDevice == 0)
                                .SelectMany(x => x.UseList).Distinct().ToList();
                            MonitoringKanBanDic[id].MaxUseList = validMonitoringKanBanDevice.Where(x => x.FaultDevice == 0)
                                .SelectMany(x => x.MaxUseList).Distinct().ToList();
                            MonitoringKanBanDic[id].MaxUse = MonitoringKanBanDic[id].MaxUseList.Count;
                            MonitoringKanBanDic[id].MinUse = MonitoringKanBanDic[id].MinUse == -1 ? MonitoringKanBanDic[id].MaxUse :
                                (MonitoringKanBanDic[id].MinUse < MonitoringKanBanDic[id].MaxUse ? MonitoringKanBanDic[id].MinUse : MonitoringKanBanDic[id].MaxUse);
                            MonitoringKanBanDic[id].MaxSimultaneousUseRate =
                                MonitoringKanBanDic[id].MaxSimultaneousUseRate <
                                MonitoringKanBanDic[id].ProcessDevice
                                    ? MonitoringKanBanDic[id].ProcessDevice
                                    : MonitoringKanBanDic[id].MaxSimultaneousUseRate;
                            MonitoringKanBanDic[id].MinSimultaneousUseRate = MonitoringKanBanDic[id].MinSimultaneousUseRate == -1 ? MonitoringKanBanDic[id].MaxSimultaneousUseRate :
                                (MonitoringKanBanDic[id].MinSimultaneousUseRate < MonitoringKanBanDic[id].MaxSimultaneousUseRate ? MonitoringKanBanDic[id].MinSimultaneousUseRate : MonitoringKanBanDic[id].MaxSimultaneousUseRate);
                            MonitoringKanBanDic[id].SingleProcessRate = validMonitoringKanBanDevice.Where(x => x.FaultDevice == 0)
                                .SelectMany(x => x.SingleProcessRate).ToList();
                            MonitoringKanBanDic[id].RunTime = validMonitoringKanBanDevice.Sum(x => x.RunTime);
                            MonitoringKanBanDic[id].ProcessTime = validMonitoringKanBanDevice.Sum(x => x.ProcessTime);
                            MonitoringKanBanDic[id].AllProcessRate = validMonitoringKanBanDevice.Any()
                                ? (MonitoringKanBanDic[id].ProcessTime * 1m / (set.DeviceIdList.Count() * 24 * 3600))
                                .ToRound(4)
                                : 0;
                            MonitoringKanBanDic[id].UseCodeList = allDeviceList.OrderBy(x => x.DeviceId)
                                .Where(x => MonitoringKanBanDic[id].UseList.Contains(x.DeviceId)).Select(x => x.Code)
                                .ToList();

                            MonitoringKanBanDic[id].FaChu = allDevice.Sum(x => x.FaChu);
                            MonitoringKanBanDic[id].HeGe = allDevice.Sum(x => x.HeGe);
                            MonitoringKanBanDic[id].LiePian = allDevice.Sum(x => x.LiePian);
                            MonitoringKanBanDic[id].ProductionList = allDevice.Select(x => new MonitoringProductionData
                            {
                                DeviceId = x.DeviceId,
                                Code = x.Code,
                                Time = x.Time,
                                FaChu = x.FaChu,
                                HeGe = x.HeGe,
                                LiePian = x.LiePian,
                                Rate = x.FaChu == 0 ? 0 : ((decimal)x.HeGe / x.FaChu).ToRound()
                            }).ToList();
                            #endregion
                        }
                        else if (set.Type == MonitoringKanBanEnum.设备状态看板)
                        {
                            #region 设备状态看板
                            var mSetData = new List<MonitoringSetData>();
                            foreach (var deviceId in set.DeviceIdList)
                            {
                                var device = allDeviceList.FirstOrDefault(x => x.DeviceId == deviceId);
                                if (device == null)
                                {
                                    continue;
                                }

                                var t = new MonitoringSetData { Id = deviceId, ScriptId = device.ScriptId };
                                var deviceData = allDevice.FirstOrDefault(y => y.DeviceId == deviceId);
                                var vs = set.VariableList.Where(v => v.ScriptId == device.ScriptId).OrderBy(x => x.Order);

                                foreach (var x in vs)
                                {
                                    var dn = dataNameDictionaries.FirstOrDefault(d =>
                                        d.VariableTypeId == x.VariableTypeId && d.PointerAddress == x.PointerAddress);

                                    if (dn == null || (dn.VariableTypeId == 1 && (dn.VariableNameId == stateDId || dn.VariableNameId == currentFlowCardDId)))
                                    {
                                        continue;
                                    }

                                    var r = new MonitoringSetSingleDataDetail
                                    {
                                        Sid = x.ScriptId,
                                        Type = x.VariableTypeId,
                                        Add = x.PointerAddress,
                                        VName = x.VariableName.IsNullOrEmpty() ? dn.VariableName ?? "" : x.VariableName,
                                    };

                                    if (deviceData != null)
                                    {
                                        List<int> bl = null;
                                        switch (x.VariableTypeId)
                                        {
                                            case 1:
                                                bl = deviceData.AnalysisData.vals;
                                                break;
                                            case 2:
                                                bl = deviceData.AnalysisData.ins;
                                                break;
                                            case 3:
                                                bl = deviceData.AnalysisData.outs;
                                                break;
                                        }
                                        if (bl != null)
                                        {
                                            if (bl.Count > x.PointerAddress - 1)
                                            {
                                                var chu = Math.Pow(10, dn.Precision);
                                                var v = (decimal)(bl.ElementAt(x.PointerAddress - 1) / chu);
                                                r.V = v.ToString();
                                            }
                                        }
                                    }
                                    t.Data.Add(r);
                                }
                                mSetData.Add(t);
                            }

                            MonitoringKanBanDic[id].MSetData = mSetData;
                            #endregion
                        }
                    }
                }
            }
            #endregion
        }

        /// <summary>
        /// 
        /// </summary>
        private static void AnalysisOther()
        {
#if !DEBUG
            if (RedisHelper.Get<int>("Debug") != 0)
            {
                return;
            }
#endif
            var redisPre = "AnalysisOther";
            var redisLock = $"{redisPre}:Lock";
            var timeKey = $"{redisPre}:Time";
            if (RedisHelper.SetIfNotExist(redisLock, DateTime.Now.ToStr()))
            {
                try
                {
                    RedisHelper.SetExpireAt(redisLock, DateTime.Now.AddMinutes(5));
                    var startTime = RedisHelper.Get<DateTime>(timeKey);
                    if (startTime == default(DateTime))
                    {
                        startTime = ServerConfig.ApiDb.Query<DateTime>(
                            "SELECT Time FROM `npc_monitoring_process` ORDER BY Time LIMIT 1;").FirstOrDefault();
                    }

                    if (startTime == default(DateTime))
                    {
                        RedisHelper.Remove(redisLock);
                        return;
                    }

                    var deviceCount = ServerConfig.ApiDb.Query<int>(
                        "SELECT COUNT(1) FROM `device_library` WHERE MarkedDelete = 0;").FirstOrDefault();
                    //"SELECT COUNT(1) FROM `device_library` a JOIN `npc_proxy_link` b ON a.Id = b.DeviceId WHERE a.MarkedDelete = 0 AND b.Monitoring = 1;").FirstOrDefault();
                    if (deviceCount <= 0)
                    {
                        RedisHelper.Remove(redisLock);
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
                                if (process?.Time != null)
                                {
                                    res[i].Add(new Tuple<DateTime, int>(process.Time, process.DeviceId), process);
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
                    RedisHelper.SetForever(timeKey, startTime);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                RedisHelper.Remove(redisLock);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        private static void Fault()
        {
#if !DEBUG
            if (RedisHelper.Get<int>("Debug") != 0)
            {
                return;
            }
#endif

            var redisPre = "Fault";
            var redisLock = $"{redisPre}:Lock";
            if (RedisHelper.SetIfNotExist(redisLock, DateTime.Now.ToStr()))
            {
                try
                {
                    RedisHelper.SetExpireAt(redisLock, DateTime.Now.AddMinutes(5));
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
                RedisHelper.Remove(redisLock);
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

                            foreach (var faultSolver in repairRecordDetail.FaultSolvers)
                            {
                                if (singleFaultType.Operators.All(x => x.Name != faultSolver))
                                {
                                    singleFaultType.Operators.Add(new Operator
                                    {
                                        Name = faultSolver,
                                    });
                                }

                                var @operator = singleFaultType.Operators.First(x => x.Name == faultSolver);
                                @operator.Count++;
                                @operator.Time += repairRecordDetail.SolveTime > repairRecordDetail.FaultTime ? (int)(repairRecordDetail.SolveTime - repairRecordDetail.FaultTime).TotalSeconds : 0;
                            }
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
            if (RedisHelper.SetIfNotExist(redisLock, DateTime.Now.ToStr()))
            {
                RedisHelper.SetExpireAt(redisLock, DateTime.Now.AddMinutes(5));
                try
                {
                    var all = ServerConfig.ApiDb.Query<DataNameDictionary>("SELECT * FROM `data_name_dictionary` WHERE MarkedDelete = 0;");
                    var scripts = new List<ScriptVersion>();
                    foreach (var grouping in all.GroupBy(x => x.ScriptId))
                    {
                        var scriptId = grouping.Key;
                        var us = all.Where(x => x.ScriptId == scriptId);

                        var script = new ScriptVersion();
                        script.Id = scriptId;
                        script.ValueNumber = us.Count(x => x.VariableTypeId == 1);
                        script.InputNumber = us.Count(x => x.VariableTypeId == 2);
                        script.OutputNumber = us.Count(x => x.VariableTypeId == 3);
                        script.MaxValuePointerAddress = us.Any(x => x.VariableTypeId == 1) ? us.Where(x => x.VariableTypeId == 1).Max(x => x.PointerAddress) < 300 ? 300 : us.Where(x => x.VariableTypeId == 1).Max(x => x.PointerAddress) : 300;
                        script.MaxInputPointerAddress = us.Any(x => x.VariableTypeId == 2) ? us.Where(x => x.VariableTypeId == 2).Max(x => x.PointerAddress) < 255 ? 255 : us.Where(x => x.VariableTypeId == 2).Max(x => x.PointerAddress) : 255;
                        script.MaxOutputPointerAddress = us.Any(x => x.VariableTypeId == 3) ? us.Where(x => x.VariableTypeId == 3).Max(x => x.PointerAddress) < 255 ? 255 : us.Where(x => x.VariableTypeId == 3).Max(x => x.PointerAddress) : 255;
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
                RedisHelper.Remove(redisLock);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        private static void UpdateProcessLog()
        {
#if !DEBUG
            if (RedisHelper.Get<int>("Debug") != 0)
            {
                return;
            }
#endif
            var redisPre = "UpdateProcessLog";
            var redisLock = $"{redisPre}:Lock";
            if (RedisHelper.SetIfNotExist(redisLock, DateTime.Now.ToStr()))
            {
                try
                {
                    RedisHelper.SetExpireAt(redisLock, DateTime.Now.AddHours(1));
                    //设备状态
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
                                var cha = 10;
                                var r = new List<MonitoringAnalysis>();
                                DateTime sendTime1 = a.StartTime;
                                var deal = false;
                                while (true)
                                {
                                    var sendTime2 = sendTime1.AddMinutes(cha);
                                    if (sendTime2 >= nextStartTime)
                                    {
                                        sendTime2 = nextStartTime;
                                        deal = true;
                                    }
                                    if (sendTime1 < sendTime2)
                                    {
                                        r.AddRange(ServerConfig.ApiDb.Query<MonitoringAnalysis>(
                                            "SELECT * FROM `npc_monitoring_analysis` WHERE DeviceId = @DeviceId AND UserSend = 0 AND SendTime BETWEEN @SendTime1 AND @SendTime2;",
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
                RedisHelper.Remove(redisLock);
            }
        }

        /// <summary>
        ///流程卡上报初始化
        /// </summary>
        public static void FlowCardReport(bool isReport = false)
        {
#if !DEBUG
            if (RedisHelper.Get<int>("Debug") != 0)
            {
                return;
            }
#endif

            var redisPre = "FlowCardReport";
            var redisLock = $"{redisPre}:Lock";
            var deviceKey = $"{redisPre}:Device";
            var change = false;
            if (RedisHelper.SetIfNotExist(redisLock, DateTime.Now.ToStr()))
            {
                try
                {
                    RedisHelper.SetExpireAt(redisLock, DateTime.Now.AddMinutes(5));
                    var deviceList = new List<FlowCardReport>();
                    var dl = RedisHelper.Get<IEnumerable<FlowCardReport>>(deviceKey);
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
                        RedisHelper.SetForever(deviceKey, deviceList);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }

                RedisHelper.Remove(redisLock);
            }
        }
    }
}
