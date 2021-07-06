using ApiManagement.Base.Server;
using ApiManagement.Models.AccountManagementModel;
using ApiManagement.Models.DeviceManagementModel;
using ApiManagement.Models.FlowCardManagementModel;
using ApiManagement.Models.OtherModel;
using ApiManagement.Models.RepairManagementModel;
using ApiManagement.Models.StatisticManagementModel;
using ApiManagement.Models.Warning;
using Microsoft.EntityFrameworkCore.Internal;
using ModelBase.Base.Logger;
using ModelBase.Base.Utils;
using ModelBase.Models.Control;
using ModelBase.Models.Device;
using Newtonsoft.Json;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        #region Analysis
        private static readonly string aRedisPre = "Analysis";
        private static readonly string aLockKey = $"{aRedisPre}:Lock";
        private static readonly string aIdKey = $"{aRedisPre}:Id";
        private static readonly string aDeviceKey = $"{aRedisPre}:Device";
        private static readonly string aIdleSecondKey = $"{aRedisPre}:IdleSecond";
        private static readonly string aExceptDeviceKey = $"{aRedisPre}:ExceptDevice";
        private static readonly string aTimeKey = $"{aRedisPre}:Time";
        private static readonly string aProcessLogIdKey = $"{aRedisPre}:ProcessLogId";
        #endregion

        #region kanBan
        private static readonly string kbRedisPre = "kanBan";
        private static readonly string kbLockKey = $"{kbRedisPre}:Lock";
        private static readonly string kbTimeKey = $"{aRedisPre}:Time";
        private static readonly string kbIdKey = $"{kbRedisPre}:Id";
        private static readonly string kbInfoKey = $"{kbRedisPre}:Info";
        private static readonly string kbDeviceKey = $"{kbRedisPre}:Device";
        #endregion

        #region ProcessSum
        private static readonly string psRedisPre = "ProcessSum";
        private static readonly string psLockKey = $"{psRedisPre}:Lock";
        private static readonly string psIdKey = $"{psRedisPre}:Id";
        private static readonly string psDeviceKey = $"{psRedisPre}:Device";
        #endregion

        #region ProcessTime
        private static readonly string ptRedisPre = "ProcessTime";
        private static readonly string ptLockKey = $"{ptRedisPre}:Lock";
        private static readonly string ptIdKey = $"{ptRedisPre}:Id";
        private static readonly string ptDeviceKey = $"{ptRedisPre}:Device";
        #endregion

        #region AnalysisOther
        private static readonly string aoRedisPre = "AnalysisOther";
        private static readonly string aoLockKey = $"{aoRedisPre}:Lock";
        private static readonly string aoTimeKey = $"{aoRedisPre}:Time";
        #endregion

        #region Fault
        private static readonly string fRedisPre = "Fault";
        private static readonly string fLockKey = $"{fRedisPre}:Lock";
        #endregion

        #region Script
        private static readonly string sRedisPre = "Script";
        private static readonly string sLockKey = $"{sRedisPre}:Lock";
        #endregion

        #region UpdateProcessLog
        private static readonly string uplRedisPre = "UpdateProcessLog";
        private static readonly string uplLockKey = $"{uplRedisPre}:Lock";
        private static readonly string uplDeviceKey = $"{uplRedisPre}:Device";
        #endregion

        #region FlowCardReport
        private static readonly string fcrRedisPre = "FlowCardReport";
        private static readonly string fcrLockKey = $"{fcrRedisPre}:Lock";
        private static readonly string fcrDeviceKey = $"{fcrRedisPre}:Device";

        /// <summary>
        /// 研磨机 粗抛机  精抛机
        /// </summary>
        private static readonly Dictionary<string, string[]> paramDic = new Dictionary<string, string[]>
        {
            {"研磨机", new []{ "YanMoTime", "YanMoFaChu", "YanMoHeGe", "YanMoLiePian", "YanMoDeviceId", "YanMoJiaGongRen"}},
            {"粗抛机", new []{ "CuPaoTime", "CuPaoFaChu", "CuPaoHeGe", "CuPaoLiePian", "CuPaoDeviceId", "CuPaoJiaGongRen"}},
            {"精抛机", new []{ "JingPaoTime", "JingPaoFaChu", "JingPaoHeGe", "JingPaoLiePian", "JingPaoDeviceId", "JingPaoJiaGongRen"}},
        };
        /// <summary>
        /// 研磨1 粗抛 2  精抛 3 外观检验 4
        /// </summary>
        public static readonly Dictionary<int, string[]> ParamIntDic = new Dictionary<int, string[]>
        {
            {1, new []{ "YanMoTime", "YanMoFaChu", "YanMoHeGe", "YanMoLiePian", "YanMoDeviceId", "YanMoJiaGongRen"}},
            {2, new []{ "CuPaoTime", "CuPaoFaChu", "CuPaoHeGe", "CuPaoLiePian", "CuPaoDeviceId", "CuPaoJiaGongRen"}},
            {3, new []{ "JingPaoTime", "JingPaoFaChu", "JingPaoHeGe", "JingPaoLiePian", "JingPaoDeviceId", "JingPaoJiaGongRen"}},
            {4, new []{ "WaiGuanTime", "WaiGuanFaChu", "WaiGuanHeGe", "WaiGuanLiePian", "WaiGuanDeviceId", "WaiGuanJiaGongRen"}},
        };
        /// <summary>
        /// 研磨机 粗抛机  精抛机
        /// </summary>
        public static readonly Dictionary<string, string[]> ParamAbbrevDic = new Dictionary<string, string[]>
        {
            //{"研磨机", new []{ "YanMoTime", "YanMoFaChu", "YanMoHeGe", "YanMoLiePian", "YanMoDeviceId", "YanMoJiaGongRen"}},
            {"cpjg", new []{ "CuPaoTime", "CuPaoFaChu", "CuPaoHeGe", "CuPaoLiePian", "CuPaoDeviceId", "CuPaoJiaGongRen"}},
            {"jpjg", new []{ "JingPaoTime", "JingPaoFaChu", "JingPaoHeGe", "JingPaoLiePian", "JingPaoDeviceId", "JingPaoJiaGongRen"}},
            {"wgjy2", new []{ "WaiGuanTime", "WaiGuanFaChu", "WaiGuanHeGe", "WaiGuanLiePian", "WaiGuanDeviceId", "WaiGuanJiaGongRen"}},
        };
        #endregion

        #region FlowCardReportAnalysis
        private static readonly string fcraRedisPre = "FlowCardReportAnalysis";
        private static readonly string fcraLockKey = $"{fcraRedisPre}:Lock";
        private static readonly string fcraDeviceKey = $"{fcraRedisPre}:Device";
        #endregion
        public static int IdleSecond = 600;
        #region 变量
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
        public static int flowCardDId = 6;
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
        public static int actProcessDId = 65;
        /// <summary>
        /// 工艺 单行数据个数
        /// </summary>
        public static int actProcessDIdCount = 6;
        /// <summary>
        /// 工艺 8行6列
        /// </summary>
        public static int processCnt = 48;
        /// <summary>
        /// 洗盘按钮
        /// </summary>
        public static int washFlagDId = 116;
        /// <summary>
        /// 修盘按钮
        /// </summary>
        public static int repairFlagDId = 117;
        /// <summary>
        /// 加工工艺按钮
        /// </summary>
        public static int processFlagDId = 118;
        /// <summary>
        /// 设备常用脚本变量
        /// </summary>
        public static List<int> VariableNameIdList = new List<int>
        {
            stateDId,
            processCountDId,
            totalProcessTimeDId,
            flowCardDId,
            runTimeDId,
            processedTimeDId,
            leftProcessTimeDId,
            nextFlowCardDId ,
            currentProductDId,
            washFlagDId,
            repairFlagDId ,
            processFlagDId,
        };
        #endregion

        private static Timer _timer2S;
        private static Timer _timer5S;
        private static Timer _timer10S;
        private static Timer _timer60S;
#if DEBUG
        private static int _dealLength = 100;
#else        
        private static int _dealLength = 2000;
#endif

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
                    VariableNameIdList.Add(actProcessDId + i);
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

                if (!RedisHelper.Exists(aIdleSecondKey))
                {
                    RedisHelper.SetForever(aIdleSecondKey, 10 * 60);
                }

                RedisHelper.Remove(aLockKey);
                MonitoringKanBanDic.AddRange(ServerConfig.ApiDb.Query<MonitoringKanBan>("SELECT * FROM (SELECT * FROM `kanban_log` ORDER BY Date DESC) a GROUP BY a.Id;", null, 60).ToDictionary(x => x.Id));
                MonitoringKanBanDeviceDic.AddRange(ServerConfig.ApiDb.Query<MonitoringKanBanDevice>("SELECT * FROM (SELECT * FROM `kanban_device_state` ORDER BY Date DESC) a GROUP BY a.DeviceId;").ToDictionary(x => x.DeviceId));
                RedisHelper.Remove(kbLockKey);

                _timer2S = new Timer(DoSth_2s, null, 10000, 2000);
                _timer5S = new Timer(DoSth_5s, null, 10000, 5000);
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
#if !DEBUG
            if (RedisHelper.Get<int>(Debug) != 0)
            {
                return;
            }
#endif
            Analysis();
            AnalysisOther();
        }

        private static void DoSth_5s(object state)
        {
#if !DEBUG
            if (RedisHelper.Get<int>(Debug) != 0)
            {
                return;
            }
#endif
            UpdateKanBan(DateTime.Now);
        }
        private static void DoSth_10s(object state)
        {
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
            if (RedisHelper.Get<int>(Debug) != 0)
            {
                return;
            }
#endif

            if (RedisHelper.SetIfNotExist(psLockKey, DateTime.Now.ToStr()))
            {
                try
                {
                    RedisHelper.SetExpireAt(psLockKey, DateTime.Now.AddHours(1));
                    var startId = RedisHelper.Get<int>(psIdKey);
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
                            if (RedisHelper.Exists(psDeviceKey))
                            {
                                deviceList = RedisHelper.Get<Dictionary<int, MonitoringProcessSum>>(psDeviceKey);
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

                            RedisHelper.SetForever(psDeviceKey, deviceList);

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

                            RedisHelper.SetForever(psIdKey, endId);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                RedisHelper.Remove(psLockKey);
            }

        }

        /// <summary>
        /// 加工次数  分析
        /// </summary>
        private static void ProcessTime()
        {
#if !DEBUG
            if (RedisHelper.Get<int>(Debug) != 0)
            {
                return;
            }
#endif
            var offset = 60;
            if (RedisHelper.SetIfNotExist(ptLockKey, DateTime.Now.ToStr()))
            {
                try
                {
                    RedisHelper.SetExpireAt(ptLockKey, DateTime.Now.AddHours(1));
                    var startId = RedisHelper.Get<int>(ptIdKey);
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
                            if (RedisHelper.Exists(ptDeviceKey))
                            {
                                deviceList = RedisHelper.Get<Dictionary<int, MonitoringProcessTime>>(ptDeviceKey);
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

                            RedisHelper.SetForever(ptDeviceKey, deviceList);

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

                            RedisHelper.SetForever(ptIdKey, endId);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                RedisHelper.Remove(ptLockKey);
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
            //        ServerConfig.DataReadDb.Execute(
            //        $"DELETE FROM {ServerConfig.DataReadDb.Table} WHERE SendTime < @SendTime LIMIT 1000;", new
            //        {
            //            SendTime = DateTime.Today.AddDays(-3)
            //        }, 60);
            //        //ServerConfig.DataReadDb.Execute(
            //        //    $"DELETE FROM {ServerConfig.DataReadDb.Table} WHERE SendTime < @SendTime LIMIT 1000;OPTIMIZE TABLE {ServerConfig.DataReadDb.Table};", new
            //        //    {
            //        //        SendTime = DateTime.Today.AddDays(-3)
            //        //    }, 60);
            //        //ServerConfig.DataReadDb.Execute(
            //        //    $"DELETE FROM {ServerConfig.DataReadDb.Table} WHERE SendTime < @SendTime LIMIT 1000;OPTIMIZE TABLE {ServerConfig.DataReadDb.Table};", new
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
            if (RedisHelper.SetIfNotExist(aLockKey, DateTime.Now.ToStr()))
            {
                try
                {
                    RedisHelper.SetExpireAt(aLockKey, DateTime.Now.AddMinutes(30));
                    var now = DateTime.Now;
                    var workshop = WorkshopHelper.Instance.Get<Workshop>(1);

                    var processLogId = ServerConfig.ApiDb.Query<int>("SELECT MAX(Id) FROM `npc_monitoring_process_log`;").FirstOrDefault();
                    RedisHelper.SetForever(aProcessLogIdKey, processLogId);
                    var exceptDevicesStr = RedisHelper.Get<string>(aExceptDeviceKey);
                    //不需要读取真实的流程卡的设备列表
                    var exceptDevices = new List<int>();
                    if (!exceptDevicesStr.IsNullOrEmpty())
                    {
                        exceptDevices.AddRange(exceptDevicesStr.Split(",").Select(int.Parse));
                    }

                    //var monitoringKanBanList = new List<MonitoringKanBan>();
                    //var monitoringKanBanDeviceList = new List<MonitoringKanBanDevice>();
                    var monitoringProcesses = new List<MonitoringProcess>();
                    //加工日志
                    var oldProcessLogs = new List<MonitoringProcessLogFlag>();
                    var newProcessLogs = new List<MonitoringProcessLog>();
                    var flowCardProcessStep = new List<FlowCardProcessStepDetail>();

                    var allDeviceList = new Dictionary<int, MonitoringProcess>();
                    if (RedisHelper.Exists(aDeviceKey))
                    {
                        allDeviceList.AddRange(GetMonitoringProcesses().ToDictionary(x => x.DeviceId));

                        #region old

                        //allDeviceList.AddRange(ServerConfig.ApiDb.Query<MonitoringProcess>(
                        //    "SELECT a.Id DeviceId, c.DeviceCategoryId, c.CategoryName, a.`Code`, a.`ScriptId` FROM `device_library` a " +
                        //    "JOIN (SELECT a.*, b.CategoryName FROM `device_model` a " +
                        //    "JOIN `device_category` b ON a.DeviceCategoryId = b.Id) c ON a.DeviceModelId = c.Id WHERE a.MarkedDelete = 0;").ToDictionary(x => x.DeviceId));

                        //var redisDeviceList = RedisHelper.Get<string>(aDeviceKey).ToClass<IEnumerable<MonitoringProcess>>();
                        //if (redisDeviceList != null)
                        //{
                        //    foreach (var redisDevice in redisDeviceList)
                        //    {
                        //        var deviceId = redisDevice.DeviceId;
                        //        if (allDeviceList.ContainsKey(deviceId))
                        //        {
                        //            var device = allDeviceList[deviceId];
                        //            redisDevice.DeviceCategoryId = device.DeviceCategoryId;
                        //            redisDevice.CategoryName = device.CategoryName;
                        //            redisDevice.Code = device.Code;
                        //            redisDevice.ScriptId = device.ScriptId;
                        //            allDeviceList[deviceId] = redisDevice;
                        //            if (allDeviceList[deviceId].State == 1 && allDeviceList[deviceId].ProcessType == ProcessType.Idle)
                        //            {
                        //                allDeviceList[deviceId].ProcessType = ProcessType.Process;
                        //            }
                        //        }
                        //    }
                        //}
                        #endregion

                        var logIds = allDeviceList.Values.Select(x => x.LogId).Where(x => x != 0);
                        oldProcessLogs.AddRange(MonitoringProcessLogHelper.GetProcessLogFlags(logIds));
                    }
                    else
                    {
                        allDeviceList.AddRange(ServerConfig.ApiDb.Query<MonitoringProcess>(
                            "SELECT b.*, c.DeviceCategoryId, c.CategoryName, a.`Code`, a.`ScriptId` FROM `device_library` a " +
                            "JOIN `npc_proxy_link` b ON a.Id = b.DeviceId " +
                            "JOIN (SELECT a.*, b.CategoryName FROM `device_model` a " +
                            "JOIN `device_category` b ON a.DeviceCategoryId = b.Id) c ON a.DeviceModelId = c.Id WHERE a.MarkedDelete = 0;").ToDictionary(x => x.DeviceId));
                        var flag = true;
                        var currentProcessLog = MonitoringProcessLogHelper.GetDistinctProcessLogs(flag).ToDictionary(x => x.DeviceId);
                        foreach (var deviceId in allDeviceList.Keys)
                        {
                            if (currentProcessLog.ContainsKey(deviceId))
                            {
                                if (flag || currentProcessLog[deviceId].EndTime == default(DateTime))
                                {
                                    allDeviceList[deviceId].LogId = currentProcessLog[deviceId].Id;
                                    oldProcessLogs.Add(currentProcessLog[deviceId]);
                                }
                                allDeviceList[deviceId].LogTime =
                                    currentProcessLog[deviceId].EndTime != default(DateTime)
                                        ? currentProcessLog[deviceId].EndTime
                                        : currentProcessLog[deviceId].StartTime;
                                allDeviceList[deviceId].State =
                                    currentProcessLog[deviceId].ProcessType == ProcessType.Idle ? 0 : 1;
                                allDeviceList[deviceId].ProcessType =
                                    currentProcessLog[deviceId].ProcessType;
                            }
                            if (allDeviceList[deviceId].State == 1 && allDeviceList[deviceId].ProcessType == ProcessType.Idle)
                            {
                                allDeviceList[deviceId].ProcessType = ProcessType.Process;
                            }
                        }
                    }

                    var deviceIds = allDeviceList.Select(x => x.Key);
                    foreach (var deviceId in deviceIds)
                    {
                        allDeviceList[deviceId].Check();
                    }

                    var startId = RedisHelper.Get<int>(aIdKey);
                    var endId = startId;
                    var lastTime = RedisHelper.Get<DateTime>(aTimeKey);
                    var kanBanTime = lastTime;
                    if (workshop.Shifts == 0 || !workshop.ShiftTimeList.Any() || workshop.Shifts != workshop.ShiftTimeList.Count)
                    {
                        workshop.Shifts = 2;
                        workshop.ShiftTimes = "8:00:00,20:00:00";
                    }

                    //var currentWorkTime = DateTimeExtend.GetCurrentWorkTimeRanges(workshop.Shifts, workshop.ShiftTimeList, kanBanTime);
                    //var todayWorkDay = DateTimeExtend.GetDayWorkDayRange(workshop.Shifts, workshop.ShiftTimeList, now);
                    var kanBanWorkTime = DateTimeExtend.GetDayWorkDay(workshop.ShiftTimeList, kanBanTime);
                    var kanBanWorkDayEndTime = kanBanWorkTime.Item1.AddDays(1).AddSeconds(-1);
                    //var kanBanNextWorkStartTime = kanBanWorkTime.Item1.AddDays(1);

                    //var nextTime = kanBanTime.Date.AddDays(1);
                    //if (!kanBanTime.InLastTime() && !kanBanTime.InSameDay(now))
                    //{
                    //    kanBanTime = nextTime;
                    //}
                    if (kanBanTime.InLastTime(kanBanWorkDayEndTime))
                    {
                        Log.Debug("New Day ------------------------------------------------------------------");
                        kanBanTime = kanBanTime.AddSeconds(1);
                        foreach (var device in allDeviceList.Values)
                        {
                            device.Time = kanBanTime;
                            device.DayRest();
                        }
                    }

                    var mData = ServerConfig.DataReadDb.Query<MonitoringData>(
                        $"SELECT * FROM `{ServerConfig.DataReadDb.Table}` WHERE Id > @Id AND UserSend = 0 ORDER BY Id LIMIT @limit;",
                        new
                        {
                            Id = startId,
                            limit = _dealLength
                        });
                    var minTime = lastTime;
                    var maxTime = kanBanTime;
                    IEnumerable<MonitoringData> tData = null;
                    IEnumerable<DataNameDictionaryDetail> dataNameDictionaries = null;
                    if (mData.Any())
                    {
                        minTime = mData.Min(x => x.SendTime);
                        maxTime = mData.Max(x => x.SendTime);
                        var minSameDay = minTime.InSameWorkDay(kanBanWorkTime);
                        var maxSameDay = maxTime.InSameWorkDay(kanBanWorkTime);
                        //tData = !sameDay ? mData.Where(x => x.SendTime.Date < minTime.Date.AddDays(1))
                        if (minSameDay && maxSameDay)
                        {
                            tData = mData.OrderBy(x => x.SendTime).ToList();
                        }
                        else if (minSameDay && !maxSameDay)
                        {
                            tData = mData.Where(x => x.SendTime < kanBanWorkTime.Item2).OrderBy(x => x.SendTime).ToList();
                        }
                        else if (!minSameDay && maxSameDay)
                        {
                            tData = mData.Where(x => x.SendTime < kanBanWorkTime.Item1).OrderBy(x => x.SendTime).ToList();
                        }
                        else
                        {
                            tData = mData.OrderBy(x => x.SendTime).ToList();
                        }

                        if (tData.Any())
                        {
                            minTime = tData.Min(x => x.SendTime);
                            kanBanTime = maxTime = tData.Max(x => x.SendTime);
                            endId = tData.Last().Id;
                            if (endId <= startId)
                            {
                                tData = new List<MonitoringData>();
                            }

                            var scriptIds = tData.GroupBy(x => x.ScriptId).Select(x => x.Key).ToList();
                            dataNameDictionaries = scriptIds.Any() ? DataNameDictionaryHelper.GetDataNameDictionaryDetails(scriptIds, VariableNameIdList) : new List<DataNameDictionaryDetail>();
                        }
                        else
                        {
                            kanBanTime = minTime;
                            if (!minSameDay)
                            {
                                foreach (var device in allDeviceList.Values)
                                {
                                    device.Time = kanBanTime;
                                    device.DayRest();
                                }
                            }
                        }
                    }
                    else
                    {
                        kanBanTime = now;
                    }

                    foreach (var device in allDeviceList.Values)
                    {
                        if (device.Time == default(DateTime))
                        {
                            device.Time = kanBanTime;
                        }
                    }

                    var faultDevices = ServerConfig.ApiDb.Query<int>(
                        //"SELECT DeviceId FROM `fault_device_repair` WHERE DeviceId != 0 AND MarkedDelete != 1 AND State < @State AND FaultTime <= @FaultTime2 GROUP BY DeviceId;", new
                        "SELECT DeviceId FROM `fault_device_repair` WHERE DeviceId != 0 AND MarkedDelete != 1 AND State < @State AND FaultTime < @FaultTime2 GROUP BY DeviceId;", new
                        {
                            State = RepairStateEnum.Complete,
                            //FaultTime1 = kanBanTime.DayBeginTime(),
                            //FaultTime2 = kanBanTime.DayEndTime(),
                            FaultTime2 = kanBanWorkTime.Item2,
                        });
                    foreach (var deviceId in faultDevices)
                    {
                        if (allDeviceList.ContainsKey(deviceId))
                        {
                            allDeviceList[deviceId].FaultDevice = 1;
                        }
                    }

                    var productionData = new Dictionary<int, List<MonitoringProductionData>>();
                    if (deviceIds.Any())
                    {
                        productionData.AddRange(deviceIds.ToDictionary(x => x, x => new List<MonitoringProductionData>()));
                        foreach (var param in paramDic)
                        {
                            //var devices = allDeviceList.Values.Where(x => x.CategoryName.Contains(param.Key));
                            var category = param.Value;

                            var sql = string.Format(
                                "SELECT {4} DeviceId, {0} Time, {1} DayTotal, {2} DayQualified, {3} DayUnqualified " +
                                //"FROM `flowcard_library` WHERE {4} IN @DeviceId AND {0} >= @startTime AND {0} <= @endTime;",
                                "FROM `flowcard_library` WHERE {4} IN @DeviceId AND {0} >= @startTime AND {0} < @endTime;",
                                category[0], category[1], category[2], category[3], category[4]);
                            var pData = ServerConfig.ApiDb.Query<MonitoringProductionData>(sql, new
                            {
                                DeviceId = deviceIds,
                                startTime = kanBanWorkTime.Item1,
                                endTime = kanBanWorkTime.Item2,
                            }, 60);
                            if (!pData.Any())
                            {
                                continue;
                            }
                            foreach (var deviceId in deviceIds)
                            {
                                var pds = pData.Where(x => x.DeviceId == deviceId);
                                productionData[deviceId].AddRange(pds);
                            }
                        }
                        foreach (var deviceId in deviceIds)
                        {
                            if (!allDeviceList.ContainsKey(deviceId))
                            {
                                continue;
                            }

                            if (tData != null && tData.Any() && tData.Any(x => x.DeviceId == deviceId))
                            {
                                var dData = tData.Where(x => x.DeviceId == deviceId).OrderBy(x => x.SendTime);
                                #region  加工记录
                                if (dataNameDictionaries != null && dataNameDictionaries.Any() &&
                                    VariableNameIdList.All(x => dataNameDictionaries.Any(y => y.VariableNameId == x)))
                                {
                                    foreach (var data in dData)
                                    {
                                        var time = data.SendTime.NoMillisecond();
                                        if (allDeviceList[deviceId].LogTime != default(DateTime)
                                            && time <= allDeviceList[deviceId].LogTime)
                                        {
                                            continue;
                                        }

                                        allDeviceList[deviceId].Time = time;
                                        allDeviceList[deviceId].NormalDevice = 1;

                                        if (data.AnalysisData != null)
                                        {
                                            allDeviceList[deviceId].AnalysisData = data.AnalysisData; ;
                                            var lastProcessType = allDeviceList[deviceId].ProcessType;
                                            var thisProcessType = ProcessType.Idle;
                                            //洗盘按钮
                                            if (GetValue(data.AnalysisData, dataNameDictionaries, data.ScriptId, washFlagDId,
                                                out var v))
                                            {
                                                if (v > 0)
                                                {
                                                    thisProcessType = ProcessType.Wash;
                                                }
                                            }

                                            //修盘按钮
                                            if (GetValue(data.AnalysisData, dataNameDictionaries, data.ScriptId,
                                                repairFlagDId, out v))
                                            {
                                                if (v > 0)
                                                {
                                                    thisProcessType = ProcessType.Repair;
                                                }
                                            }

                                            //加工工艺按钮
                                            if (GetValue(data.AnalysisData, dataNameDictionaries, data.ScriptId,
                                                processFlagDId, out v))
                                            {
                                                if (v > 0)
                                                {
                                                    thisProcessType = ProcessType.Process;
                                                }
                                            }

                                            var currentFlowCardId = 0;
                                            //流程卡
                                            if (exceptDevices.Contains(deviceId))
                                            {
                                                currentFlowCardId = 0;
                                            }
                                            else
                                            {
                                                if (GetValue(data.AnalysisData, dataNameDictionaries, data.ScriptId,
                                                    flowCardDId, out v))
                                                {
                                                    currentFlowCardId = (int)v;
                                                }
                                            }

                                            FlowCardProcessStepDetail flowCardProcessStepDetail = null;

                                            #region 流程卡
                                            if (currentFlowCardId != 0)
                                            {
                                                allDeviceList[deviceId].FlowCardId = currentFlowCardId;
                                                var flowCard = FlowCardHelper.GetMenu(currentFlowCardId).FirstOrDefault();
                                                if (flowCard != null)
                                                {
                                                    var flowCardProcessStepDetails =
                                                        ServerConfig.ApiDb.Query<FlowCardProcessStepDetail>(
                                                            "SELECT a.* FROM `flowcard_process_step` a JOIN `device_process_step` b ON a.ProcessStepId = b.Id WHERE b.IsSurvey = 0 AND a.FlowCardId = @FlowCardId AND a.DeviceId = @DeviceId;",
                                                            new
                                                            {
                                                                FlowCardId = flowCard.Id,
                                                                DeviceId = deviceId
                                                            });
                                                    flowCardProcessStepDetail =
                                                        flowCardProcessStepDetails.FirstOrDefault();
                                                }
                                                allDeviceList[deviceId].FlowCard = flowCard?.FlowCardName ?? "";
                                            }

                                            //当前工艺数据
                                            var processData = new Dictionary<int, decimal[]>();
                                            for (var i = 0; i < processCnt; i++)
                                            {
                                                var key = i / actProcessDIdCount + 1;
                                                if (!processData.ContainsKey(key))
                                                {
                                                    processData.Add(key, new decimal[actProcessDIdCount]);
                                                }

                                                GetValue(data.AnalysisData, dataNameDictionaries, data.ScriptId,
                                                    actProcessDId + i, out v);
                                                var index = i % 6;
                                                processData[key][index] = v;
                                            }

                                            #endregion

                                            //var r = RandomSeed.Next(EnumHelper.EnumToList<ProcessType>().Count());
                                            //EnumHelper.TryParseInt(r, out thisProcessType);
                                            //状态
                                            if (GetValue(data.AnalysisData, dataNameDictionaries, data.ScriptId, stateDId,
                                                out var thisState))
                                            {
                                                //stateValue = RandomSeed.Next(3);
                                                if (thisState > 0 && thisProcessType == ProcessType.Idle)
                                                {
                                                    thisProcessType = ProcessType.Process;
                                                }
                                            }

                                            //thisProcessType = thisState > 0 ? thisProcessType : ProcessType.Idle;
                                            thisProcessType = thisState > 0 ? (thisProcessType == ProcessType.Idle ? ProcessType.Process : thisProcessType) : ProcessType.Idle;
                                            lastProcessType = allDeviceList[deviceId].State > 0 ? (lastProcessType == ProcessType.Idle ? ProcessType.Process : lastProcessType) : ProcessType.Idle;
                                            //开始加工时
                                            var bStart = allDeviceList[deviceId].State == 0 && thisState > 0;
                                            //停止加工时
                                            var bEnd = allDeviceList[deviceId].State == 1 && thisState == 0;
                                            //持续使用时
                                            var bUsing = allDeviceList[deviceId].State == 1 && thisState > 0;
                                            if (bUsing)
                                            {
                                                if (lastProcessType != thisProcessType)
                                                {
                                                    #region 设备可能在加工状态时，加工类型发生变化（最好没有这种情况）

                                                    if (!allDeviceList[deviceId].NewLog)
                                                    {
                                                        var oldProcessLog = oldProcessLogs.FirstOrDefault(x =>
                                                            x.Id == allDeviceList[deviceId].LogId);
                                                        if (oldProcessLog != null)
                                                        {
                                                            //if ((time - oldProcessLog.StartTime).TotalHours < 2)
                                                            {
                                                                oldProcessLog.EndTime = time;
                                                                oldProcessLog.Change = true;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            var newProcessLog = newProcessLogs.FirstOrDefault(x =>
                                                                x.Id == allDeviceList[deviceId].LogId);
                                                            if (newProcessLog != null)
                                                            {
                                                                newProcessLog.EndTime = time;
                                                                //if ((time - newProcessLog.StartTime).TotalHours < 2)
                                                                //{
                                                                //    newProcessLog.EndTime = time;
                                                                //}
                                                            }
                                                        }
                                                    }

                                                    var log = new MonitoringProcessLog
                                                    {
                                                        Id = ++processLogId,
                                                        ProcessType = thisProcessType,
                                                        OpName = thisProcessType.GetAttribute<DescriptionAttribute>()
                                                                     ?.Description ?? "",
                                                        DeviceId = deviceId,
                                                        StartTime = time,
                                                        ProcessData = processData.ToJson()
                                                    };

                                                    if (lastProcessType != ProcessType.Process &&
                                                        thisProcessType == ProcessType.Process)
                                                    {
                                                        if (flowCardProcessStepDetail != null &&
                                                            flowCardProcessStepDetail.ProcessTime == default(DateTime))
                                                        {
                                                            flowCardProcessStepDetail.ProcessTime = data.SendTime;
                                                            flowCardProcessStep.Add(flowCardProcessStepDetail);
                                                        }

                                                        log.FlowCardId = allDeviceList[deviceId].FlowCardId;
                                                        log.FlowCard = allDeviceList[deviceId].FlowCard;
                                                        log.ProcessorId = flowCardProcessStepDetail?.ProcessorId ?? 0;
                                                        log.ProcessData = processData.ToJson();
                                                        log.RequirementMid =
                                                            flowCardProcessStepDetail?.ProcessStepRequirementMid ?? 0;
                                                    }

                                                    if (lastProcessType == ProcessType.Process &&
                                                        thisProcessType != ProcessType.Process)
                                                    {
                                                        if (flowCardProcessStepDetail != null &&
                                                            flowCardProcessStepDetail.ProcessTime == default(DateTime))
                                                        {
                                                            flowCardProcessStepDetail.ProcessEndTime = data.SendTime;
                                                            flowCardProcessStep.Add(flowCardProcessStepDetail);
                                                        }
                                                    }

                                                    newProcessLogs.Add(log);
                                                    allDeviceList[deviceId].EndTime = time;
                                                    allDeviceList[deviceId].UpdateExtraData();

                                                    allDeviceList[deviceId].StartTime = time;
                                                    allDeviceList[deviceId].EndTime = default(DateTime);
                                                    allDeviceList[deviceId].LogId = log.Id;
                                                    allDeviceList[deviceId].LogTime = time;

                                                    #endregion
                                                }
                                            }
                                            //开始加工时
                                            else if (bStart)
                                            {
                                                if (!allDeviceList[deviceId].NewLog)
                                                {
                                                    var oldProcessLog = oldProcessLogs.FirstOrDefault(x =>
                                                        x.Id == allDeviceList[deviceId].LogId);
                                                    if (oldProcessLog != null)
                                                    {
                                                        //if ((time - oldProcessLog.StartTime).TotalHours < 2)
                                                        {
                                                            oldProcessLog.EndTime = time;
                                                            oldProcessLog.Change = true;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var newProcessLog = newProcessLogs.FirstOrDefault(x =>
                                                            x.Id == allDeviceList[deviceId].LogId);
                                                        if (newProcessLog != null)
                                                        {
                                                            //if ((time - newProcessLog.StartTime).TotalHours < 2)
                                                            {
                                                                newProcessLog.EndTime = time;
                                                            }
                                                        }
                                                    }
                                                }

                                                var log = new MonitoringProcessLog
                                                {
                                                    Id = ++processLogId,
                                                    ProcessType = thisProcessType,
                                                    OpName = thisProcessType.GetAttribute<DescriptionAttribute>()
                                                                 ?.Description ?? "",
                                                    DeviceId = deviceId,
                                                    StartTime = time,
                                                    ProcessData = processData.ToJson()
                                                };


                                                if (thisProcessType == ProcessType.Process)
                                                {
                                                    if (flowCardProcessStepDetail != null &&
                                                        flowCardProcessStepDetail.ProcessTime == default(DateTime))
                                                    {
                                                        flowCardProcessStepDetail.ProcessTime = data.SendTime;
                                                        flowCardProcessStep.Add(flowCardProcessStepDetail);
                                                    }

                                                    log.FlowCardId = allDeviceList[deviceId].FlowCardId;
                                                    log.FlowCard = allDeviceList[deviceId].FlowCard;
                                                    log.ProcessorId = flowCardProcessStepDetail?.ProcessorId ?? 0;
                                                    log.ProcessData = processData.ToJson();
                                                    log.RequirementMid =
                                                        flowCardProcessStepDetail?.ProcessStepRequirementMid ?? 0;
                                                }

                                                newProcessLogs.Add(log);

                                                allDeviceList[deviceId].StartTime = time;
                                                allDeviceList[deviceId].EndTime = default(DateTime);
                                                allDeviceList[deviceId].LogId = log.Id;
                                                allDeviceList[deviceId].LogTime = time;
                                            }
                                            //停止加工时
                                            else if (bEnd)
                                            {
                                                thisProcessType = ProcessType.Idle;
                                                if (!allDeviceList[deviceId].NewLog)
                                                {
                                                    var oldProcessLog = oldProcessLogs.FirstOrDefault(x =>
                                                        x.Id == allDeviceList[deviceId].LogId);
                                                    if (oldProcessLog != null)
                                                    {
                                                        //if ((time - oldProcessLog.StartTime).TotalHours < 2)
                                                        {
                                                            oldProcessLog.EndTime = time;
                                                            oldProcessLog.Change = true;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var newProcessLog = newProcessLogs.FirstOrDefault(x =>
                                                            x.Id == allDeviceList[deviceId].LogId);
                                                        if (newProcessLog != null)
                                                        {
                                                            //if ((time - newProcessLog.StartTime).TotalHours < 2)
                                                            {
                                                                newProcessLog.EndTime = time;
                                                            }
                                                        }
                                                    }
                                                }

                                                var log = new MonitoringProcessLog
                                                {
                                                    Id = ++processLogId,
                                                    ProcessType = thisProcessType,
                                                    OpName = thisProcessType.GetAttribute<DescriptionAttribute>()
                                                                 ?.Description ?? "",
                                                    DeviceId = deviceId,
                                                    StartTime = time,
                                                    ProcessData = processData.ToJson()
                                                };

                                                if (lastProcessType == ProcessType.Process)
                                                {
                                                    if (flowCardProcessStepDetail != null &&
                                                        flowCardProcessStepDetail.ProcessTime == default(DateTime))
                                                    {
                                                        flowCardProcessStepDetail.ProcessEndTime = data.SendTime;
                                                        flowCardProcessStep.Add(flowCardProcessStepDetail);
                                                    }
                                                }

                                                newProcessLogs.Add(log);
                                                allDeviceList[deviceId].EndTime = time;
                                                allDeviceList[deviceId].UpdateExtraData();

                                                allDeviceList[deviceId].StartTime = time;
                                                allDeviceList[deviceId].EndTime = default(DateTime);
                                                allDeviceList[deviceId].LogId = log.Id;
                                                allDeviceList[deviceId].LogTime = time;
                                            }

                                            //值大于0为使用中
                                            if (thisState > 0)
                                            {
                                                if (!allDeviceList[deviceId].UseList.Contains(deviceId))
                                                {
                                                    allDeviceList[deviceId].UseList.Add(deviceId);
                                                }
                                                if (!allDeviceList[deviceId].UseCodeList.Contains(allDeviceList[deviceId].Code))
                                                {
                                                    allDeviceList[deviceId].UseCodeList.Add(allDeviceList[deviceId].Code);
                                                }
                                                if (!allDeviceList[deviceId].MaxUseList.Contains(deviceId))
                                                {
                                                    allDeviceList[deviceId].MaxUseList.Add(deviceId);
                                                }
                                            }
                                            else
                                            {
                                                allDeviceList[deviceId].UseList.RemoveAll(x => x == deviceId);
                                                allDeviceList[deviceId].UseCodeList.RemoveAll(x => x == allDeviceList[deviceId].Code);
                                            }

                                            allDeviceList[deviceId].ProcessDevice = thisState > 0 ? 1 : 0;
                                            allDeviceList[deviceId].State = thisState > 0 ? 1 : 0;
                                            allDeviceList[deviceId].ProcessType = thisState > 0 ? thisProcessType : ProcessType.Idle;

                                            //总加工次数
                                            if (GetValue(data.AnalysisData, dataNameDictionaries, data.ScriptId,
                                                processCountDId, out var totalProcessCount))
                                            {
                                                if (totalProcessCount < 0)
                                                {
                                                    totalProcessCount = 0;
                                                }
                                                if (allDeviceList[deviceId].TotalProcessCount < totalProcessCount)
                                                {
                                                    allDeviceList[deviceId].ProcessCount +=
                                                        (int)totalProcessCount -
                                                        allDeviceList[deviceId].TotalProcessCount;
                                                }
                                            }
                                            allDeviceList[deviceId].TotalProcessCount = (int)totalProcessCount;

                                            //总加工时间
                                            if (GetValue(data.AnalysisData, dataNameDictionaries, data.ScriptId,
                                                totalProcessTimeDId, out var totalProcessTime))
                                            {
                                                if (totalProcessTime < 0)
                                                {
                                                    totalProcessTime = 0;
                                                }
                                                if (allDeviceList[deviceId].TotalProcessTime < totalProcessTime)
                                                {
                                                    allDeviceList[deviceId].ProcessTime +=
                                                        (int)totalProcessTime - allDeviceList[deviceId].TotalProcessTime;
                                                }
                                            }
                                            allDeviceList[deviceId].TotalProcessTime = (int)totalProcessTime;

                                            //总运行时间
                                            if (GetValue(data.AnalysisData, dataNameDictionaries, data.ScriptId, runTimeDId,
                                                out var totalRunTime))
                                            {
                                                if (totalRunTime < 0)
                                                {
                                                    totalRunTime = 0;
                                                }
                                                if (allDeviceList[deviceId].TotalRunTime < totalRunTime)
                                                {
                                                    allDeviceList[deviceId].RunTime +=
                                                        (int)totalRunTime - allDeviceList[deviceId].TotalRunTime;
                                                }
                                            }
                                            allDeviceList[deviceId].TotalRunTime = (int)totalRunTime;

                                            var rate = allDeviceList[deviceId].SingleProcessRate
                                                .FirstOrDefault(x => x.Id == deviceId);
                                            if (allDeviceList.ContainsKey(deviceId))
                                            {
                                                var device = allDeviceList[deviceId];
                                                var processTime = device.ProcessTime;
                                                allDeviceList[deviceId].ProcessTime = processTime;
                                                if (rate == null)
                                                {
                                                    allDeviceList[deviceId].SingleProcessRate.Add(
                                                        new ProcessUseRate
                                                        {
                                                            Id = device.DeviceId,
                                                            Code = device.Code,
                                                            Rate = (processTime * 1m / (24 * 3600)).ToRound(4)
                                                        });
                                                }
                                                else
                                                {
                                                    rate.Rate = (processTime * 1m / (24 * 3600)).ToRound(4);
                                                }
                                            }

                                            var pData = productionData[deviceId];
                                            var d = GetProductionDataByEndTime(pData, time);
                                            allDeviceList[deviceId].DayTotal = d.DayTotal;
                                            allDeviceList[deviceId].DayQualified = d.DayQualified;
                                            allDeviceList[deviceId].DayUnqualified = d.DayUnqualified;
                                            var monitoringProcess = new MonitoringProcess
                                            {
                                                DeviceId = deviceId,
                                                Time = time,
                                                State = allDeviceList[deviceId].State,
                                                ProcessCount = allDeviceList[deviceId].ProcessCount,
                                                TotalProcessCount = allDeviceList[deviceId].TotalProcessCount,
                                                ProcessTime = allDeviceList[deviceId].ProcessTime,
                                                TotalProcessTime = allDeviceList[deviceId].TotalProcessTime,
                                                RunTime = allDeviceList[deviceId].RunTime,
                                                TotalRunTime = allDeviceList[deviceId].TotalRunTime,
                                                Use = allDeviceList[deviceId].ProcessDevice,
                                                DayTotal = d.DayTotal,
                                                DayQualified = d.DayQualified,
                                                DayUnqualified = d.DayUnqualified,
                                            };

                                            monitoringProcess.Rate = (decimal)monitoringProcess.Use * 100 / monitoringProcess.Total;
                                            monitoringProcesses.Add(monitoringProcess);
                                        }
                                        else
                                        {
                                            //allDeviceList[deviceId].AnalysisData = new DeviceData();
                                        }
                                    }
                                }
                                else
                                {
                                    Log.Error(
                                        $"{deviceId},{allDeviceList[deviceId].ScriptId}, 缺少流程脚本配置:{VariableNameIdList.Where(x => dataNameDictionaries.All(y => y.VariableNameId != x)).ToJSON()}");
                                }
                                #endregion
                            }
                            else
                            {
                                var totalSeconds = (int)(maxTime - minTime).TotalSeconds;
                                if (totalSeconds == 0)
                                {
                                    continue;
                                }

                                allDeviceList[deviceId].DayRest(maxTime < kanBanWorkTime.Item1);
                                for (var i = 0; i < totalSeconds; i++)
                                {
                                    var endTime = minTime.AddSeconds(i);
                                    var pData = productionData[deviceId];
                                    var d = GetProductionDataByEndTime(pData, endTime);
                                    allDeviceList[deviceId].DayTotal = d.DayTotal;
                                    allDeviceList[deviceId].DayQualified = d.DayQualified;
                                    allDeviceList[deviceId].DayUnqualified = d.DayUnqualified;

                                    var monitoringProcess = new MonitoringProcess
                                    {
                                        DeviceId = deviceId,
                                        Time = endTime,
                                        State = allDeviceList[deviceId].State,
                                        ProcessCount = allDeviceList[deviceId].ProcessCount,
                                        TotalProcessCount = allDeviceList[deviceId].TotalProcessCount,
                                        ProcessTime = allDeviceList[deviceId].ProcessTime,
                                        TotalProcessTime = allDeviceList[deviceId].TotalProcessTime,
                                        RunTime = allDeviceList[deviceId].RunTime,
                                        TotalRunTime = allDeviceList[deviceId].TotalRunTime,
                                        Use = allDeviceList[deviceId].ProcessDevice,
                                        DayTotal = d.DayTotal,
                                        DayQualified = d.DayQualified,
                                        DayUnqualified = d.DayUnqualified,
                                    };

                                    monitoringProcess.Rate = (decimal)monitoringProcess.Use * 100 / monitoringProcess.Total;
                                    monitoringProcesses.Add(monitoringProcess);
                                }
                            }
                            allDeviceList[deviceId].UpdateAnalysis();
                        }
                    }

                    //RedisHelper.SetForever(aDeviceKey, deviceList.Values.Select(x => new
                    RedisHelper.SetForever(aDeviceKey, allDeviceList.Values
                        .Select(ClassExtension.CopyTo<MonitoringProcess, MonitoringProcessAnalysis>).OrderBy(x => x.DeviceId).ToJSON());

                    //UpdateKanBanLogic(kanBanTime, false);
                    //UpdateKanBan(allDeviceList, kanBanTime);
                    //monitoringKanBanList.AddRange(MonitoringKanBanDic.Values);
                    //monitoringKanBanDeviceList.AddRange(MonitoringKanBanDeviceDic.Values);


                    //RedisHelper.SetForever(aKanBanKey, monitoringKanBanList.ToJSON());

                    //RedisHelper.SetForever(aKanBanDeviceKey, monitoringKanBanDeviceList.ToJSON());

                    var changeLogs = oldProcessLogs.Where(x => x.Change);
                    if (changeLogs.Any())
                    {
                        MonitoringProcessLogHelper.Instance.Update<MonitoringProcessLog>(changeLogs);
                    }
                    if (newProcessLogs.Any())
                    {
                        MonitoringProcessLogHelper.Instance.Add<MonitoringProcessLog>(newProcessLogs);
                        ////RedisHelper.SetForever(processLogIdKey, processLogId);
                    }
                    if (flowCardProcessStep.Any())
                    {
                        ServerConfig.ApiDb.Execute("UPDATE flowcard_process_step SET " +
                                                   "`ProcessTime` = IF(@ProcessTime = '0001-01-01 00:00:00', `ProcessTime`, @ProcessTime)`" +
                                                   "`ProcessEndTime` = IF(@ProcessEndTime = '0001-01-01 00:00:00', `ProcessEndTime`, @ProcessEndTime)`" +
                                                   " WHERE `Id` = @Id;", flowCardProcessStep);
                    }

                    if (allDeviceList.Any())
                    {
                        Task.Run(() =>
                        {
                            ServerConfig.ApiDb.Execute(
                                "UPDATE npc_proxy_link SET `Time` = @Time, `State` = @State, `ProcessType` = @ProcessType, `LogId` = @LogId, " +
                                "`ProcessTime` = @ProcessTime, `TotalProcessTime` = @TotalProcessTime, `RunTime` = @RunTime, `TotalRunTime` = @TotalRunTime, " +
                                "`ProcessCount` = @ProcessCount, `TotalProcessCount` = @TotalProcessCount, " +
                                "`StartTime` = IF(@StartTime = '0001-01-01 00:00:00', NULL, @StartTime), " +
                                "`EndTime` = IF(@EndTime = '0001-01-01 00:00:00', NULL, @EndTime), " +
                                "`Data` = @Data, " +
                                "`DayTotal` = @DayTotal, " +
                                "`DayQualified` = @DayQualified, " +
                                "`DayUnqualified` = @DayUnqualified, " +
                                "`DayQualifiedRate` = @DayQualifiedRate, " +
                                "`DayUnqualifiedRate` = @DayUnqualifiedRate " +
                                "WHERE `DeviceId` = @DeviceId;",
                                allDeviceList.Values);
                        });
                    }

                    InsertAnalysis(monitoringProcesses);

                    RedisHelper.SetForever(aTimeKey, kanBanTime.ToStr());
                    RedisHelper.SetForever(aIdKey, endId);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                RedisHelper.Remove(aLockKey);
            }
        }

        public static bool GetValue(DeviceData deviceData, IEnumerable<DataNameDictionaryDetail> dataNameDictionaries, int scriptId, int dId, out decimal v)
        {
            var dn = dataNameDictionaries.FirstOrDefault(d => d.ScriptId == scriptId && d.VariableNameId == dId);
            if (deviceData != null && dn != null)
            {
                List<int> bl = null;
                switch (dn.VariableTypeId)
                {
                    case 1: bl = deviceData.vals; break;
                    case 2: bl = deviceData.ins; break;
                    case 3: bl = deviceData.outs; break;
                }

                if (bl != null && dn.PointerAddress > 0 && bl.Count > dn.PointerAddress - 1)
                {
                    var chu = Math.Pow(10, dn.Precision);
                    v = (decimal)(bl.ElementAt(dn.PointerAddress - 1) / chu);
                    return true;
                }
            }

            v = 0;
            return false;
        }

        private static MonitoringProductionData GetProductionDataByEndTime(IEnumerable<MonitoringProductionData> monitoringProcesses, DateTime endTime)
        {
            var data = monitoringProcesses.Where(x => x.Time <= endTime);
            return new MonitoringProductionData
            {
                DayTotal = data.Sum(x => x.DayTotal),
                DayQualified = data.Sum(x => x.DayQualified),
                DayUnqualified = data.Sum(x => x.DayUnqualified),
            };
        }

        private static void InsertAnalysis(IEnumerable<MonitoringProcess> monitoringProcesses,
            IEnumerable<MonitoringKanBan> monitoringKanBanList = null, IEnumerable<MonitoringKanBanDevice> monitoringKanBanDeviceList = null)
        {
            if (monitoringKanBanList != null && monitoringKanBanList.Any())
            {
                Task.Run(() =>
            {
                ServerConfig.ApiDb.Execute(
                    "INSERT INTO kanban_log (`Date`, `Time`, `Id`, `AllDevice`, `NormalDevice`, `ProcessDevice`, `IdleDevice`, `FaultDevice`, `ConnectErrorDevice`, `MaxUse`, `UseListStr`, " +
                    "`MaxUseListStr`, `MaxUseRate`, `MinUse`, `MinUseRate`, `MaxSimultaneousUseRate`, `MinSimultaneousUseRate`, `SingleProcessRateStr`, `AllProcessRate`, `RunTime`, `ProcessTime`, `IdleTime`, " +
                    "`DayTotal`, `DayQualified`, `DayUnqualified`, `DayQualifiedRate`, `DayUnqualifiedRate`, " +
                    "`ProductionData`) " +
                    "VALUES (@Date, @Time, @Id, @AllDevice, @NormalDevice, @ProcessDevice, @IdleDevice, @FaultDevice, @ConnectErrorDevice, @MaxUse, @UseListStr, " +
                    "@MaxUseListStr ,@MaxUseRate, @MinUse, @MinUseRate, @MaxSimultaneousUseRate, @MinSimultaneousUseRate, @SingleProcessRateStr, @AllProcessRate, @RunTime, @ProcessTime, @IdleTime, " +
                    "@DayTotal, @DayQualified, @DayUnqualified, @DayQualifiedRate, @DayUnqualifiedRate, " +
                    "@ProductionData) " +
                    "ON DUPLICATE KEY UPDATE `Time` = @Time, `AllDevice` = @AllDevice, `NormalDevice` = @NormalDevice, `ProcessDevice` = @ProcessDevice, `IdleDevice` = @IdleDevice, " +
                    "`FaultDevice` = @FaultDevice, `ConnectErrorDevice` = @ConnectErrorDevice, `MaxUse` = @MaxUse, `MaxUseListStr` = @MaxUseListStr, `UseListStr` = @UseListStr, " +
                    "`MaxUseRate` = @MaxUseRate, `MinUse` = @MinUse, `MinUseRate` = @MinUseRate, `MaxSimultaneousUseRate` = @MaxSimultaneousUseRate, `MinSimultaneousUseRate` = @MinSimultaneousUseRate, " +
                    "`SingleProcessRateStr` = @SingleProcessRateStr, `AllProcessRate` = @AllProcessRate, `RunTime` = @RunTime, `ProcessTime` = @ProcessTime, `IdleTime` = @IdleTime, " +
                    "`DayTotal` = @DayTotal, `DayQualified` = @DayQualified, `DayUnqualified` = @DayUnqualified, `DayQualifiedRate` = @DayQualifiedRate, `DayUnqualifiedRate` = @DayUnqualifiedRate, " +
                    "`ProductionData` = @ProductionData, `VariableData` = @VariableData;"
                    , monitoringKanBanList);

                if (monitoringKanBanDeviceList != null && monitoringKanBanDeviceList.Any())
                {
                    ServerConfig.ApiDb.Execute(
                    "INSERT INTO kanban_device_state (`Date`, `Time`, `DeviceId`, `AllDevice`, `NormalDevice`, `ProcessDevice`, `IdleDevice`, `FaultDevice`, `ConnectErrorDevice`, `MaxUse`, `UseListStr`, " +
                    "`MaxUseListStr`, `MaxUseRate`, `MinUse`, `MinUseRate`, `MaxSimultaneousUseRate`, `MinSimultaneousUseRate`, `SingleProcessRateStr`, `AllProcessRate`, `RunTime`, `ProcessTime`, `IdleTime`, " +
                    "`DayTotal`, `DayQualified`, `DayUnqualified`, `DayQualifiedRate`, `DayUnqualifiedRate`" +
                    ") VALUES (@Date, @Time, @DeviceId, @AllDevice, @NormalDevice, @ProcessDevice, @IdleDevice, @FaultDevice, @ConnectErrorDevice, @MaxUse, @UseListStr, " +
                    "@MaxUseListStr ,@MaxUseRate, @MinUse, @MinUseRate, @MaxSimultaneousUseRate, @MinSimultaneousUseRate, @SingleProcessRateStr, @AllProcessRate, @RunTime, @ProcessTime, @IdleTime, " +
                    "@DayTotal, @DayQualified, @DayUnqualified, @DayQualifiedRate, @DayUnqualifiedRate" +
                    ") " +
                    "ON DUPLICATE KEY UPDATE `Time` = @Time, `AllDevice` = @AllDevice, `NormalDevice` = @NormalDevice, `ProcessDevice` = @ProcessDevice, `IdleDevice` = @IdleDevice, " +
                    "`FaultDevice` = @FaultDevice, `ConnectErrorDevice` = @ConnectErrorDevice, `MaxUse` = @MaxUse, `MaxUseListStr` = @MaxUseListStr, `UseListStr` = @UseListStr, " +
                    "`MaxUseRate` = @MaxUseRate, `MinUse` = @MinUse, `MinUseRate` = @MinUseRate, `MaxSimultaneousUseRate` = @MaxSimultaneousUseRate, `MinSimultaneousUseRate` = @MinSimultaneousUseRate, " +
                    "`SingleProcessRateStr` = @SingleProcessRateStr, `AllProcessRate` = @AllProcessRate, `RunTime` = @RunTime, `ProcessTime` = @ProcessTime, `IdleTime` = @IdleTime, " +
                    "`DayTotal` = @DayTotal, `DayQualified` = @DayQualified, `DayUnqualified` = @DayUnqualified, `DayQualifiedRate` = @DayQualifiedRate, `DayUnqualifiedRate` = @DayUnqualifiedRate;"
                    , monitoringKanBanDeviceList);
                }

                if (monitoringProcesses != null && monitoringProcesses.Any())
                {
                    ServerConfig.ApiDb.ExecuteTrans(
                   "INSERT INTO `npc_monitoring_process` (`Time`, `DeviceId`, `State`, `ProcessCount`, `TotalProcessCount`, `ProcessTime`, `TotalProcessTime`, `RunTime`, `TotalRunTime`, `Use`, " +
                   "`Total`, `Rate`, `DayTotal`, `DayQualified`, `DayUnqualified`, `DayQualifiedRate`, `DayUnqualifiedRate`) " +
                   "VALUES (@Time, @DeviceId, @State, @ProcessCount, @TotalProcessCount, @ProcessTime, @TotalProcessTime, @RunTime, @TotalRunTime, @Use, @Total, @Rate, @DayTotal, @DayQualified, " +
                   "@DayUnqualified, @DayQualifiedRate, @DayUnqualifiedRate) " +
                   "ON DUPLICATE KEY UPDATE `Time` = @Time, `DeviceId` = @DeviceId, `State` = @State, `ProcessCount` = @ProcessCount, `TotalProcessCount` = @TotalProcessCount, " +
                   "`ProcessTime` = @ProcessTime, `TotalProcessTime` = @TotalProcessTime, `RunTime` = @RunTime, `TotalRunTime` = @TotalRunTime, `Use` = @Use, `Total` = @Total, `Rate` = @Rate, " +
                   "`DayTotal` = @DayTotal, `DayQualified` = @DayQualified, `DayUnqualified` = @DayUnqualified, `DayQualifiedRate` = @DayQualifiedRate, `DayUnqualifiedRate` = @DayUnqualifiedRate;",
                   monitoringProcesses);
                }
            });
            }
        }

        private static bool kbRun = false;
        private static void UpdateKanBan(DateTime time)
        {
            if (RedisHelper.SetIfNotExist(kbLockKey, DateTime.Now.ToStr()))
            {
                RedisHelper.SetExpireAt(kbLockKey, DateTime.Now.AddMinutes(10));
                kbRun = true;
                UpdateKanBanLogic(time);
                kbRun = false;
                RedisHelper.Remove(kbLockKey);
            }
            else
            {
                if (kbRun)
                {
                    return;
                }

                if (RedisHelper.Exists(kbInfoKey))
                {
                    var t = RedisHelper.Get<string>(kbInfoKey).ToClass<IEnumerable<MonitoringKanBan>>();
                    MonitoringKanBanDic = t.ToDictionary(x => x.Id);
                }
                if (RedisHelper.Exists(kbDeviceKey))
                {
                    var t = RedisHelper.Get<string>(kbDeviceKey).ToClass<IEnumerable<MonitoringKanBanDevice>>();
                    MonitoringKanBanDeviceDic = t.ToDictionary(x => x.DeviceId);
                }
            }
        }

        private static void UpdateKanBanLogic(DateTime time, bool real = true)
        {
            try
            {
                var monitoringKanBanList = new List<MonitoringKanBan>();
                var monitoringKanBanDeviceList = new List<MonitoringKanBanDevice>();
                var allDeviceList = GetMonitoringProcesses().ToDictionary(x => x.DeviceId);
                var sets = MonitoringKanBanSetHelper.Instance.GetAll<MonitoringKanBanSet>().ToList();
                sets.Add(new MonitoringKanBanSet
                {
                    Id = 0,
                    Type = KanBanEnum.设备详情看板,
                    DeviceIds = allDeviceList.Keys.Join()
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

                var workshopIds = sets.Select(x => x.WorkshopId).Where(x => x != 0).Distinct();
                var workshops = WorkshopHelper.Instance.GetAllByIds<Workshop>(workshopIds).ToDictionary(x => x.Id);
                var scriptIds = sets.SelectMany(x => x.VariableList.Select(y => y.ScriptId)).Where(x => x != 0).Distinct();
                var dataNameDictionaries = scriptIds.Any() ? DataNameDictionaryHelper.GetDataNameDictionaryDetails(scriptIds) : new List<DataNameDictionaryDetail>();
                var removeTypes = MonitoringKanBanDic.Keys.Where(type => sets.All(x => x.Id != type)).ToList();
                foreach (var removeType in removeTypes)
                {
                    MonitoringKanBanDic.Remove(removeType);
                }

                var keys = MonitoringKanBanDeviceDic.Keys.ToList();
                foreach (var deviceId in keys)
                {
                    if (allDeviceList.ContainsKey(deviceId))
                    {
                        //var t = (MonitoringProcess)allDeviceList[deviceId].Clone();
                        var x = ClassExtension.CopyTo<MonitoringProcess, MonitoringKanBanDevice>(allDeviceList[deviceId]);
                        MonitoringKanBanDeviceDic[deviceId] = x;
                    }
                    else
                    {
                        MonitoringKanBanDeviceDic.Remove(deviceId);
                    }
                }

                #region MonitoringKanBanDic
                foreach (var id in MonitoringKanBanDic.Keys)
                {
                    //Console.WriteLine(id);
                    var set = sets.FirstOrDefault(x => x.Id == id);
                    if (set == null)
                    {
                        continue;
                    }

#if DEBUG
                    if (set.Type != KanBanEnum.生产相关看板)
                    {
                        continue;
                    }
#endif
                    //var validDevice = allDevice.Where(x => x.NormalDevice == 1);
                    MonitoringKanBanDic[id].Time = time;
                    if (set.Type == KanBanEnum.设备详情看板)
                    {
                        if (!set.DeviceIdList.Any())
                        {
                            continue;
                        }
                        var setDevices = MonitoringKanBanDeviceDic.Values.Where(x => set.DeviceIdList.Contains(x.DeviceId));
                        #region 设备详情看板
                        MonitoringKanBanDic[id].AllDevice = setDevices.Count();
                        MonitoringKanBanDic[id].NormalDevice = setDevices.Count(x => x.NormalDevice == 1);
                        MonitoringKanBanDic[id].ProcessDevice = setDevices.Count(x => x.ProcessDevice == 1);
                        MonitoringKanBanDic[id].FaultDevice = setDevices.Count(x => x.FaultDevice == 1);
                        MonitoringKanBanDic[id].Use = setDevices.Count(x => x.Use == 1);
                        MonitoringKanBanDic[id].UseList = setDevices.SelectMany(x => x.UseList).Distinct().ToList();
                        MonitoringKanBanDic[id].MaxUseList = setDevices.SelectMany(x => x.MaxUseList).Distinct().ToList();
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
                        MonitoringKanBanDic[id].SingleProcessRate = setDevices.SelectMany(x => x.SingleProcessRate).ToList();
                        MonitoringKanBanDic[id].RunTime = setDevices.Sum(x => x.RunTime);
                        MonitoringKanBanDic[id].ProcessTime = setDevices.Sum(x => x.ProcessTime);
                        MonitoringKanBanDic[id].AllProcessRate = setDevices.Any()
                            ? (MonitoringKanBanDic[id].ProcessTime * 1m / (setDevices.Count() * 24 * 3600)).ToRound(4) : 0;
                        MonitoringKanBanDic[id].UseCodeList = setDevices.SelectMany(x => x.UseCodeList).Distinct().ToList();

                        MonitoringKanBanDic[id].DayTotal = setDevices.Sum(x => x.DayTotal);
                        MonitoringKanBanDic[id].DayQualified = setDevices.Sum(x => x.DayQualified);
                        MonitoringKanBanDic[id].DayUnqualified = setDevices.Sum(x => x.DayUnqualified);
                        MonitoringKanBanDic[id].ProductionList = setDevices.Select(x => new MonitoringProductionData
                        {
                            DeviceId = x.DeviceId,
                            Code = x.Code,
                            Time = x.Time,
                            DayTotal = x.DayTotal,
                            DayQualified = x.DayQualified,
                            DayUnqualified = x.DayUnqualified
                        }).ToList();
                        #endregion
                    }
                    else if (set.Type == KanBanEnum.设备状态看板)
                    {
                        if (!set.DeviceIdList.Any())
                        {
                            continue;
                        }
                        #region 设备状态看板
                        var mSetData = new List<MonitoringSetData>();
                        foreach (var deviceId in set.DeviceIdList)
                        {
                            if (!allDeviceList.ContainsKey(deviceId))
                            {
                                continue;
                            }
                            var device = allDeviceList[deviceId];
                            var deviceData = device.AnalysisData;

                            var t = new MonitoringSetData { Id = deviceId, ScriptId = device.ScriptId };
                            var vs = set.VariableList.Where(v => v.ScriptId == device.ScriptId).OrderBy(x => x.Order);
                            foreach (var x in vs)
                            {
                                var dn = dataNameDictionaries.FirstOrDefault(d =>
                                    d.VariableTypeId == x.VariableTypeId && d.PointerAddress == x.PointerAddress);

                                if (dn == null)
                                {
                                    continue;
                                }
                                var r = new MonitoringSetSingleDataDetail
                                {
                                    Order = x.Order,
                                    SubOrder = x.SubOrder,
                                    Delimiter = x.Delimiter,
                                    Sid = x.ScriptId,
                                    Type = x.VariableTypeId,
                                    Add = x.PointerAddress,
                                    VName = x.VariableName.IsNullOrEmpty() ? dn.VariableName ?? "" : x.VariableName,
                                };

                                if (dn.VariableTypeId == 1 && dn.VariableNameId == stateDId)
                                {
                                    r.V = device.State.ToString();
                                }
                                else if (deviceData != null)
                                {
                                    List<int> bl = null;
                                    switch (x.VariableTypeId)
                                    {
                                        case 1: bl = deviceData.vals; break;
                                        case 2: bl = deviceData.ins; break;
                                        case 3: bl = deviceData.outs; break;
                                        default: break;
                                    }

                                    if (bl != null)
                                    {
                                        if (bl.Count > x.PointerAddress - 1)
                                        {
                                            var chu = Math.Pow(10, dn.Precision);
                                            var v = (decimal)(bl.ElementAt(x.PointerAddress - 1) / chu);
                                            if (dn.VariableTypeId == 1 && (dn.VariableNameId == AnalysisHelper.flowCardDId || dn.VariableNameId == AnalysisHelper.nextFlowCardDId))
                                            {
                                                var flowCard = FlowCardHelper.Instance.Get<FlowCard>((int)v);
                                                r.V = flowCard?.FlowCardName ?? "";
                                            }
                                            else if (dn.VariableTypeId == 1 && dn.VariableNameId == AnalysisHelper.currentProductDId)
                                            {
                                                var production = ProductionHelper.Instance.Get<Production>((int)v);
                                                r.V = production?.ProductionProcessName ?? "";
                                            }
                                            else
                                            {
                                                r.V = v.ToString();
                                            }
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
                    else if (set.Type == KanBanEnum.生产相关看板)
                    {
                        #region 生产相关看板

                        var workshop = workshops.ContainsKey(set.WorkshopId)
                            ? workshops[set.WorkshopId]
                            : new Workshop
                            {
                                Shifts = 1,
                                ShiftTimes = "[\"00:00:00\",\"24:00:00\"]"
                            };
                        var currentWorkTimes = DateTimeExtend.GetCurrentWorkTimeRanges(workshop.Shifts, workshop.StatisticTimeList, time);
                        var workTime = DateTimeExtend.GetDayWorkDay(workshop.StatisticTimeList, time);
                        foreach (var item in set.ItemList)
                        {
                            var type = item.Item;
                            if (type != KanBanItemEnum.流程卡追踪 && !set.DeviceIdList.Any())
                            {
                                continue;
                            }

                            var key = $"{(int)item.Item}_{item.Col}_{item.Order}";
                            if (!MonitoringKanBanDic[id].Times.ContainsKey(key))
                            {
                                MonitoringKanBanDic[id].Times.Add(key, default(DateTime));
                            }
                            if (MonitoringKanBanDic[id].Times[key] != default(DateTime)
                                && (time - MonitoringKanBanDic[id].Times[key]).TotalSeconds < 10)
                            {
                                continue;
                            }
                            MonitoringKanBanDic[id].Times[key] = time;
                            var startTime = time.DayBeginTime();
                            var endTime = time.DayEndTime();
                            switch (item.Shifts)
                            {
                                case KanBanShiftsEnum.当前班次:
                                    startTime = currentWorkTimes.ElementAt(1).Item1;
                                    endTime = currentWorkTimes.ElementAt(1).Item2.AddSeconds(-1);
                                    break;
                                case KanBanShiftsEnum.上个班:
                                    startTime = currentWorkTimes.ElementAt(0).Item1;
                                    endTime = currentWorkTimes.ElementAt(0).Item2.AddSeconds(-1);
                                    break;
                                case KanBanShiftsEnum.今日:
                                    startTime = workTime.Item1;
                                    endTime = workTime.Item2.AddSeconds(-1);
                                    break;
                                case KanBanShiftsEnum.昨日:
                                    startTime = workTime.Item1.AddDays(-1);
                                    endTime = workTime.Item2.AddDays(-1).AddSeconds(-1);
                                    break;
                            }

                            if (endTime > time)
                            {
                                endTime = time;
                            }
                            var cha = (endTime - startTime).TotalMinutes;
                            var duration = item.Hour * 60 + item.Min;
                            if (duration != 0 && duration < cha)
                            {
                                startTime = endTime.AddMinutes(-duration);
                            }

                            if (!MonitoringKanBanDic[id].ItemData.ContainsKey(key))
                            {
                                MonitoringKanBanDic[id].ItemData.Add(key, new List<dynamic>());
                            }

                            //if (!(type == KanBanItemEnum.计划号工序推移图 || type == KanBanItemEnum.设备工序推移图 ||
                            //    type == KanBanItemEnum.操作工工序推移图))
                            //{
                            //    continue;
                            //}
                            MonitoringKanBanDic[id].ItemData[key].Clear();
                            if (type == KanBanItemEnum.异常报警)
                            {
                                //MonitoringKanBanDic[id].WarningLogs =
                                //    WarningLogHelper.GetWarningLogs(startTime, endTime, 0, 0, WarningType.设备, WarningDataType.生产数据,
                                //        set.DeviceIdList, new List<WarningItemType> { WarningItemType.SingleQualifiedRate }, 1).ToList();
                                //if (item.ConfigList.Length == 0)
                                {
                                    var configs1 = item.ConfigList.Length > 0 ? item.ConfigList[0] : new int[0];
                                    var configs2 = item.ConfigList.Length > 1 ? item.ConfigList[1] : new int[0];
                                    var data = WarningLogHelper.GetWarningLogs(workshop.Id, startTime, endTime, 0, 0,
                                        //WarningType.设备, WarningDataType.生产数据, null, null, configs1, configs2, 1);
                                        WarningType.设备, WarningDataType.生产数据, null, new List<WarningItemType> { WarningItemType.SingleQualifiedRate }, configs1, configs2, 1);
                                    if (!data.Any())
                                    {
                                        continue;
                                    }
                                    var wgData = data.Where(x => x.StepId == 32);
                                    var oldFcIds = wgData.Where(x => x.Interval == WarningInterval.每次
                                                                     && (x.ItemType == WarningItemType.SingleQualifiedRate || x.ItemType == WarningItemType.SingleUnqualifiedRate)
                                                                     && x.ExtraIdList.Count > 2)
                                        .Select(y => y.ExtraIdList.ElementAt(2));
                                    var stepReport = FlowCardReportGetHelper.GetReport(workshop.Id, default(DateTime), default(DateTime), 18, null, 0,
                                        null, 0, null, 0, oldFcIds);

                                    foreach (var log in data)
                                    {
                                        if (log.ItemType == WarningItemType.SingleQualifiedRate || log.ItemType == WarningItemType.SingleUnqualifiedRate)
                                        {
                                            //外观检验
                                            if (log.StepId == 32 && log.ExtraIdList.Count > 2)
                                            {
                                                var oldFcId = log.ExtraIdList.ElementAt(2);
                                                var report = stepReport.FirstOrDefault(x => x.OldFlowCardId == oldFcId);
                                                log.Code = report?.Code ?? log.Code;
                                                if (log.WarningData.Any() && report != null)
                                                {
                                                    try
                                                    {
                                                        var tp = JsonConvert
                                                            .DeserializeObject<string[]>(
                                                                log.WarningData.First().Param);
                                                        tp[1] = report.Processor;
                                                        log.WarningData.First().Param = tp.ToJSON();
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        Log.Error(e);
                                                    }
                                                }
                                            }

                                            if (log.WarningData.Any())
                                            {
                                                var wd = log.WarningData.First();
                                                var t = new List<string>();
                                                if (!wd.Param.IsNullOrEmpty() && wd.Param != "[]")
                                                {
                                                    var tp = JsonConvert
                                                        .DeserializeObject<IEnumerable<string>>(
                                                            wd.Param);
                                                    t.AddRange(tp);
                                                }
                                                if (!wd.OtherParam.IsNullOrEmpty() && wd.OtherParam != "[]")
                                                {
                                                    var tp = JsonConvert
                                                        .DeserializeObject<IEnumerable<BadTypeCount>>(
                                                            wd.OtherParam);
                                                    t.AddRange(tp.Where(x => x.count > 0).Select(y => $"{y.comment}:{ y.count}"));
                                                }
                                                log.Info = t.Join();
                                            }
                                        }

                                    }
                                    MonitoringKanBanDic[id].ItemData[key].AddRange(data);
                                }
                            }
                            else if (type == KanBanItemEnum.异常统计)
                            {
                                //相关设备
                                //MonitoringKanBanDic[id].WarningStatistics =
                                //    WarningStatisticHelper.GetWarningStatistic(WarningStatisticTime.天, startTime, null, WarningDataType.生产数据,
                                //        set.DeviceIdList, new List<WarningItemType> { WarningItemType.SingleQualifiedRate }).ToList();

                                //无相关设备
                                //MonitoringKanBanDic[id].WarningStatistics =
                                //    WarningStatisticHelper.GetWarningStatistic(WarningStatisticTime.天, startTime, null, WarningDataType.生产数据,
                                //        null, new List<WarningItemType> { WarningItemType.SingleQualifiedRate }).ToList();
                                //if (item.ConfigList.Length > 0)
                                {
                                    var configs1 = item.ConfigList.Length > 0 ? item.ConfigList[0] : new int[0];
                                    var configs2 = item.ConfigList.Length > 1 ? item.ConfigList[1] : new int[0];
                                    var logs = WarningLogHelper.GetWarningLogs(workshop.Id, startTime, endTime, 0, 0, WarningType.设备, WarningDataType.生产数据,
                                        null, null, configs1, configs2, 1);
                                    MonitoringKanBanDic[id].ItemData[key].AddRange(logs.GroupBy(x => new { x.ItemId, x.SetId, x.SetName, x.Range, x.Item })
                                        .Select(x => new WarningStatistic
                                        {
                                            Time = startTime.Date,
                                            SetId = x.Key.SetId,
                                            SetName = x.Key.SetName,
                                            ItemId = x.Key.ItemId,
                                            Item = x.Key.Item,
                                            Range = x.Key.Range,
                                            Count = x.Count()
                                        }));
                                }
                            }
                            else if (type == KanBanItemEnum.设备状态反馈)
                            {
                                IdleSecond = RedisHelper.Get<int>(aIdleSecondKey);
                                var devices = MonitoringProcessHelper.GetMonitoringProcesses();
                                var idleDevices = devices.Where(x => x.State == 0 && x.TotalTime > IdleSecond);
                                //var idleDevices = devices.Where(x => x.State == 0);
                                MonitoringKanBanDic[id].ItemData[key].AddRange(idleDevices.Select(x => new DeviceStateInfo
                                {
                                    DeviceId = x.DeviceId,
                                    Code = x.Code,
                                    IdleSecond = x.TotalTime,
                                    //IdleSecond = RandomSeed.Next(100000),
                                }).OrderBy(x => x.IdleSecond));
                            }
                            else if (type == KanBanItemEnum.设备预警状态)
                            {
                                var deviceList = WarningHelper.GetMonitoringProcesses(set.DeviceIdList);
                                var warningList = deviceList.Where(device => device.DeviceWarning);
                                MonitoringKanBanDic[id].ItemData[key].AddRange(warningList.Select(device =>
                                {
                                    var warnings = new List<MonitoringProcessWarningLog>();
                                    //warnings.AddRange(device.DeviceWarningList.Values.Concat(device.ProductWarningList.Values));
                                    warnings.AddRange(device.DeviceWarningList.Values);
                                    var warning = warnings.OrderByDescending(x => x.WarningTime).First();
                                    return new WarningDeviceInfo
                                    {
                                        Time = warning.WarningTime,
                                        DeviceId = device.DeviceId,
                                        Code = device.Code,
                                        ItemId = warning.ItemId,
                                        Item = warning.Item,
                                        SetId = warning.SetId,
                                        SetName = warning.SetName,
                                        Range = warning.Range,
                                        Value = warning.Value,
                                    };
                                }).OrderByDescending(x => x.Time));
                            }
                            else if (type == KanBanItemEnum.计划号日进度表)
                            {
                                var reports = FlowCardReportGetHelper.GetReport(workshop.Id, startTime, endTime, 18, null, 0, set.DeviceIdList)
                                    .GroupBy(x => x.ProductionId)
                                    .ToDictionary(x => x.Key, x => x.Sum(y => y.HeGe));
                                var productionPlans = ProductionPlanHelper.GetDetails(startTime.Date, startTime.Date, "蓝玻璃发抛光").Where(x => x.ProductionId != 0);
                                var existProductionPlanIds = reports.Select(x => x.Key)
                                    .Where(y => y != 0 && productionPlans.All(z => z.ProductionId != y));
                                var existProductionPlans = ProductionPlanHelper.GetDetails(startTime.Date, startTime.Date, 0, 0, existProductionPlanIds).Where(x => x.ProductionId != 0);
                                var productions = ProductionHelper.GetMenus(reports.Keys);
                                var pp = reports.Where(x => productions.Any(y => y.Id == x.Key))
                                    .Select(x =>
                                    {
                                        var production = productions.First(p => p.Id == x.Key);
                                        var plan = productionPlans.FirstOrDefault(p => p.ProductionId == x.Key) ??
                                                   existProductionPlans.FirstOrDefault(p => p.ProductionId == x.Key);
                                        return new ProductionSchedule
                                        {
                                            ProductionId = x.Key,
                                            Production = production.ProductionProcessName,
                                            Plan = plan?.Plan ?? 0,
                                            Actual = x.Value,
                                        };
                                    }).ToList();
                                var not = productionPlans.Where(x => pp.All(y => y.ProductionId != x.ProductionId)).ToList();
                                pp.AddRange(not.Select(z => new ProductionSchedule
                                {
                                    ProductionId = z.ProductionId,
                                    Production = z.ProductionProcessName,
                                    Plan = z.Plan,
                                    Actual = 0
                                }));
                                MonitoringKanBanDic[id].ItemData[key].AddRange(pp.OrderByDescending(x => x.Plan).ThenByDescending(x => x.Actual));
                            }
                            else if (type == KanBanItemEnum.设备日进度表)
                            {
                                var reports = FlowCardReportGetHelper.GetReport(workshop.Id, startTime, endTime, 18, null, 0, set.DeviceIdList)
                                    .GroupBy(x => x.DeviceId)
                                    .ToDictionary(x => x.Key, x => x.Sum(y => y.HeGe));
                                var deviceLibraries = DeviceLibraryHelper.GetMenu(reports.Keys);
                                MonitoringKanBanDic[id].ItemData[key].AddRange(reports.Where(x => deviceLibraries.Any(y => y.Id == x.Key))
                                    .Select(x =>
                                    {
                                        var deviceLibrary = deviceLibraries.First(p => p.Id == x.Key);
                                        return new DeviceSchedule
                                        {
                                            DeviceId = x.Key,
                                            Code = deviceLibrary.Code,
                                            Plan = 0,
                                            Actual = x.Value,
                                        };
                                    }).OrderByDescending(x => x.Actual));
                            }
                            else if (type == KanBanItemEnum.操作工日进度表)
                            {
                                var reports = FlowCardReportGetHelper.GetReport(workshop.Id, startTime, endTime, 18, null, 0, set.DeviceIdList)
                                    .GroupBy(x => x.ProcessorId)
                                    .ToDictionary(x => x.Key, x => x.Sum(y => y.HeGe));
                                var processes = AccountInfoHelper.GetAccountInfoByAccountIds(reports.Keys);
                                MonitoringKanBanDic[id].ItemData[key].AddRange(reports.Where(x => processes.Any(y => y.Id == x.Key))
                                    .Select(x =>
                                    {
                                        var process = processes.First(p => p.Id == x.Key);
                                        return new ProcessorSchedule
                                        {
                                            ProcessorId = x.Key,
                                            Processor = process.Name,
                                            Plan = 0,
                                            Actual = x.Value,
                                        };
                                    }).OrderByDescending(x => x.Actual));
                            }
                            else if (type == KanBanItemEnum.故障状态反馈)
                            {
                                //var faults = RepairRecordHelper.GetKanBan(workshop.Id);
                                var faults = RepairRecordHelper.GetKanBan();
                                MonitoringKanBanDic[id].ItemData[key].AddRange(faults.OrderByDescending(x => x.FaultTime));
                            }
                            else if (type == KanBanItemEnum.计划号工序推移图 || type == KanBanItemEnum.设备工序推移图 || type == KanBanItemEnum.操作工工序推移图)
                            {
                                //班制
                                var shift = 0;
                                //时间类型
                                var timeType = StatisticProcessTimeEnum.小时;
                                //时间范围
                                var range = 10;
                                //是否是合计
                                var isSum = 0;
                                //是否是比较
                                var isCompare = 0;
                                //比较的时间类型
                                var cTimeType = StatisticProcessTimeEnum.小时;
                                //比较的时间范围类型
                                var cTimeRangeType = StatisticProcessTimeRangeEnum.前多少时间;
                                List<int> cTimeRange = null;
                                List<int> steps = null;
                                var deviceIds = set.DeviceIdList;
                                deviceIds.Add(0);
                                List<int> productionIds = null;
                                List<Production> productions = new List<Production>();
                                List<int> processorIds = null;
                                //item.ConfigList 工序推移图[0][0] 班制[0][1]数据类型[0][2]时间范围;[1][...]工序
                                var index = 0;
                                var cIndex = 0;
                                if (item.ConfigList.Length > index)
                                {
                                    //班制
                                    cIndex = 0;
                                    shift = item.ConfigList[index].Length > cIndex ? item.ConfigList[index][cIndex] : shift;
                                    //时间类型
                                    cIndex = 1;
                                    timeType = item.ConfigList[index].Length > cIndex ? (StatisticProcessTimeEnum)item.ConfigList[index][cIndex] : timeType;
                                    //时间范围
                                    cIndex = 2;
                                    range = item.ConfigList[index].Length > cIndex ? item.ConfigList[index][cIndex] : range;
                                    //是否是合计
                                    cIndex = 3;
                                    isSum = item.ConfigList[index].Length > cIndex ? item.ConfigList[index][cIndex] : isSum;
                                    //是否是比较
                                    cIndex = 4;
                                    isCompare = item.ConfigList[index].Length > cIndex ? item.ConfigList[index][cIndex] : isCompare;
                                    //比较的时间类型
                                    cIndex = 5;
                                    cTimeType = item.ConfigList[index].Length > cIndex ? (StatisticProcessTimeEnum)item.ConfigList[index][cIndex] : cTimeType;
                                    //比较的时间范围类型
                                    cIndex = 6;
                                    cTimeRangeType = item.ConfigList[index].Length > cIndex ? (StatisticProcessTimeRangeEnum)item.ConfigList[index][cIndex] : cTimeRangeType;
                                }
                                //工序 当前只有单工序
                                index = 1;
                                if (item.ConfigList.Length > index && item.ConfigList[index].Length > 0)
                                {
                                    steps = new List<int> { item.ConfigList[index][0] };
                                }
                                //计划号
                                index = 2;
                                if (item.ConfigList.Length > index && item.ConfigList[index].Length > 0)
                                {
                                    productionIds = item.ConfigList[index].ToList();
                                    if (productionIds.Any())
                                    {
                                        productions.AddRange(ProductionHelper.Instance.GetByIds<Production>(productionIds));
                                    }
                                }
                                //操作工
                                index = 3;
                                if (item.ConfigList.Length > index && item.ConfigList[index].Length > 0)
                                {
                                    processorIds = item.ConfigList[index].ToList();
                                }
                                //对比时间范围
                                index = 4;
                                if (item.ConfigList.Length > index && item.ConfigList[index].Length > 0)
                                {
                                    //前多少时间 取[0], 指定时间 取数组, 时间范围 取数组
                                    cTimeRange = item.ConfigList[index].ToList();
                                }

                                try
                                {
                                    var processes = StatisticProcessHelper.StatisticProcesses(type, time, workshop, shift, timeType,
                                        range, isSum == 1, isCompare == 1, cTimeType, cTimeRangeType, cTimeRange, steps, deviceIds, productionIds, processorIds).ToList();
                                    MonitoringKanBanDic[id].ItemData[key].AddRange(processes);
                                }
                                catch (Exception e)
                                {
                                    Log.Error(e);
                                }
                            }
                            else if (type == KanBanItemEnum.流程卡追踪)
                            {
                                var p = new List<string> { "主管1", "主管2", "主管3", "主管4" };
                                var limit = RandomSeed.Next(1, 10);
                                var steps = DeviceProcessStepHelper.GetDetails(workshop.Id);
                                var processors = AccountInfoHelper.GetProcessorDetails();
                                var fcs = FlowCardHelper.GetFlowCardDetailLimit(limit);
                                var fks = new List<FenKa>();
                                var now = DateTime.Now;
                                foreach (var fc in fcs)
                                {
                                    var stepLen = RandomSeed.Next(1, steps.Count());
                                    var edTime = now.AddSeconds(-10 * stepLen * 60 * 60);
                                    var stepList = RandomSeed.NextN(0, steps.Count(), stepLen).OrderBy(x => x);
                                    var allSteps = stepList.Select(index => steps.ElementAt(index));
                                    var step = allSteps.ElementAt(0);
                                    var qualified = 36;
                                    // 0 等待 1 加工 2 完成
                                    var nStates = new List<Tuple<int, int>>
                                    {
                                        new Tuple<int, int>(0, 10),
                                        new Tuple<int, int>(1, 30),
                                        new Tuple<int, int>(2, 60),
                                    };
                                    var nState = RandomSeed.GetRandom(nStates);
                                    nState = 2;
                                    var r = FenKaFunc(now, fc.FlowCardName, qualified, 0, 0, false, 0, nState, edTime, step, allSteps, processors);
                                    r.Time = DateTime.Now.AddSeconds(-RandomSeed.Next(100));
                                    r.Creator = p.ElementAt(RandomSeed.Next(p.Count));
                                    r.FlowCard = fc.FlowCardName;
                                    r.Production = fc.ProductionProcessName;
                                    fks.Add(r);
                                }

                                var fenKas = FenKaSimpleFunc(fks, true);
                                fenKas = fenKas.Select(x =>
                                {
                                    x.FenKas = x.FenKas.Select(y =>
                                    {
                                        y.FenKas.Clear();
                                        return y;
                                    }).ToList();
                                    x.FlowCards = x.FenKas.Where(y => y.IsPart).Select(y => y.FFlowCard).ToList();
                                    return x;
                                }).ToList();
                                var dr = fenKas.OrderByDescending(x => x.Creator);
                                MonitoringKanBanDic[id].ItemData[key].AddRange(dr);
                            }
                            else if (type == KanBanItemEnum.计划号评分排行)
                            {
                                var ds = ProductionHelper.GetDetails();
                                MonitoringKanBanDic[id].ItemData[key].AddRange(ds.Select(x => new { Production = x.ProductionProcessName, Score = RandomSeed.Next(100) }).OrderBy(x => x.Score));
                            }
                            else if (type == KanBanItemEnum.设备评分排行)
                            {
                                var ds = DeviceLibraryHelper.GetDetails(workshop.Id);
                                MonitoringKanBanDic[id].ItemData[key].AddRange(ds.Select(x => new { x.Code, Score = RandomSeed.Next(100) }).OrderBy(x => x.Score));
                            }
                            else if (type == KanBanItemEnum.操作工评分排行)
                            {
                                var ds = AccountInfoHelper.GetProcessorDetails();
                                MonitoringKanBanDic[id].ItemData[key].AddRange(ds.Select(x => new { x.Name, Score = RandomSeed.Next(100) }).OrderBy(x => x.Score));
                            }
                        }
                        MonitoringKanBanDic[id].Check(set.ItemList);
                        #endregion
                    }
                }
                #endregion

                monitoringKanBanList.AddRange(MonitoringKanBanDic.Values);
                monitoringKanBanDeviceList.AddRange(MonitoringKanBanDeviceDic.Values);

                InsertAnalysis(null, monitoringKanBanList, monitoringKanBanDeviceList);

                RedisHelper.SetForever(kbInfoKey, monitoringKanBanList.ToJSON());
                RedisHelper.SetForever(kbDeviceKey, monitoringKanBanDeviceList.ToJSON());
                RedisHelper.SetForever(kbTimeKey, time.ToStr());
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
            RedisHelper.Remove(kbLockKey);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="now">当前时间</param>
        /// <param name="ka">上道工序 流程卡</param>
        /// <param name="total">上道工序 合格品数量</param>
        /// <param name="order">工序顺序</param>
        /// <param name="deep">分卡次数（深度）</param>
        /// <param name="isFenKa">是否是分卡</param>
        /// <param name="xvHao">分卡序号</param>
        /// <param name="state">上道工序 状态 0 等待 1 加工 2 完成</param>
        /// <param name="startTime">上道工序 开始时间</param>
        /// <param name="endTime">上道工序 结束时间</param>
        /// <param name="step">当前工序</param>
        /// <param name="allSteps">所有工序</param>
        /// <param name="processors">所有操作工</param>
        /// <returns></returns>
        private static FenKa FenKaFunc(DateTime now, string ka, int total, int order, int deep, bool isFenKa, int xvHao, int state,
            DateTime endTime, DeviceProcessStepDetail step, IEnumerable<DeviceProcessStepDetail> allSteps, IEnumerable<AccountInfo> processors)
        {
            // 0 等待 1 加工 2 完成
            var states = new Dictionary<int, string>
            {
                { 0, "等待中"},
                { 1, "加工中"},
                { 2, "已完成"}
            };
            var index = allSteps.IndexOf(step);
            var nextStep = allSteps.Count() > index + 1 ? allSteps.ElementAt(index + 1) : null;
            var r = new FenKa();
            r.Order = order;
            r.IsPart = isFenKa;
            r.FlowCard = ka;
            r.FFlowCard = isFenKa ? $"{ka}-{xvHao}" : ka;
            var nState = 0;
            var defaultMin = 10;
            var second = RandomSeed.Next(20 * 60, 60 * 60);
            var stTime = endTime.AddMinutes(defaultMin);
            var edTime = stTime.AddSeconds(second);
            var rate = RandomSeed.Next(0, 200);
            if (index == 0 && order == 0)
            {
                rate = 100;
            }

            var qualified = (int)(Math.Floor(total * (rate > 100 ? 100 : rate) / 100m));
            var unqualified = total - qualified;
            var qualifiedRate = total == 0 ? 0 : (qualified * 100m / total).ToRound();
            var unqualifiedRate = (100 - qualifiedRate).ToRound();
            // 0 上道工序等待
            if (state == 0)
            {
                nState = 0;
                r.StartTime = default(DateTime);
                r.EndTime = default(DateTime);
                r.CostTime = 0;
            }
            // 1 上道工序加工
            else if (state == 1)
            {
                nState = 0;
                r.StartTime = default(DateTime);
                r.EndTime = default(DateTime);
                r.CostTime = 0;
            }
            // 2 上道工序完成
            else if (state == 2)
            {
                var nStates = new List<Tuple<int, int>>
                {
                    new Tuple<int, int>(0, 10),
                    new Tuple<int, int>(1, 30),
                    new Tuple<int, int>(2, 60),
                };
                nState = RandomSeed.GetRandom(nStates);
                // 0 本次工序等待
                if (nState == 0)
                {
                    r.StartTime = default(DateTime);
                    r.EndTime = default(DateTime);
                    r.CostTime = (int)(now - endTime).TotalSeconds;
                }
                // 1 本次工序加工
                else if (nState == 1)
                {
                    r.StartTime = stTime;
                    r.EndTime = default(DateTime);
                    r.CostTime = (int)(now - endTime).TotalSeconds;
                }
                // 2 本次工序完成
                else if (nState == 2)
                {
                    r.StartTime = stTime;
                    r.EndTime = edTime;
                    r.CostTime = (int)(edTime - endTime).TotalSeconds;
                }
            }

            r.State = nState;
            r.StateDesc = states[nState];
            r.StepName = step.StepName;
            r.Processor = processors.ElementAt(RandomSeed.Next(processors.Count()))?.Name ?? "";
            r.Total = total;
            r.Qualified = qualified;
            r.Unqualified = unqualified;
            r.QualifiedRate = qualifiedRate;
            r.UnqualifiedRate = unqualifiedRate;
            var nextIsFenKa = RandomSeed.Next(0, 2) == 0;
            if (order > 10)
            {
                nextIsFenKa = false;
            }
            if (nextIsFenKa && nState != 2)
            {
                nextIsFenKa = false;
            }
            if (qualified < 2)
            {
                nextIsFenKa = false;
            }

            if (nextStep != null && qualified > 0)
            {
                if (!nextIsFenKa)
                {
                    if (nState == 2)
                    {
                        r.FenKas.Add(FenKaFunc(now, ka, qualified, order + 1, deep, false, 0, nState, edTime, nextStep, allSteps, processors));
                    }
                }
                else
                {
                    var max = qualified > 4 ? 4 : qualified;
                    var fenKaNum = RandomSeed.Next(2, max);
                    var totals = RandomSeed.NextN(qualified, fenKaNum, false);
                    for (var i = 0; i < fenKaNum; i++)
                    {
                        r.FenKas.Add(FenKaFunc(now, r.FFlowCard, totals.ElementAt(i), order + 1, deep + 1, true, i + 1, nState, edTime, nextStep, allSteps, processors));
                    }
                }
            }

            return r;
        }

        private static List<FenKa> FenKaSimpleFunc(List<FenKa> fks, bool isFirst = false)
        {
            var fenKas = new List<FenKa>();
            var con = false;
            var len = fks.Count();
            for (int i = 0; i < len; i++)
            {
                var fk = fks.ElementAt(i);
                var lastFk = isFirst ? fk : fk.FenKas.LastOrDefault();
                if (lastFk != null && lastFk.FenKas.Any())
                {
                    var fkLen = lastFk.FenKas.Count;
                    for (var j = 0; j < fkLen; j++)
                    {
                        var ffk = lastFk.FenKas[j];
                        var f = ClassExtension.CopyTo<FenKa, FenKa>(fk);
                        f.FenKas = new List<FenKa>();
                        if (!isFirst)
                        {
                            f.FenKas.AddRange(fk.FenKas);
                        }

                        f.FenKas.Add(ffk);
                        //f.NextFenKa = ffk;
                        if (ffk.FenKas.Any())
                        {
                            con = true;
                        }
                        fenKas.Add(f);
                    }

                }
                else
                {
                    fenKas.Add(fk);
                }
            }
            if (con)
            {
                return FenKaSimpleFunc(fenKas); ;
            }

            return fenKas;
        }

        private class FenKa
        {
            public int Order { get; set; }
            public FenKa()
            {
                FenKas = new List<FenKa>();
            }
            //public FenKa NextFenKa { get; set; }
            /// <summary>
            /// 添加时间
            /// </summary>
            public DateTime Time { get; set; }
            /// <summary>
            /// 添加人
            /// </summary>
            public string Creator { get; set; }
            /// <summary>
            /// 原卡
            /// </summary>
            public string FlowCard { get; set; }
            /// <summary>
            /// 是否分卡
            /// </summary>
            public bool IsPart { get; set; }
            /// <summary>
            /// 分卡
            /// </summary>
            public string FFlowCard { get; set; }
            public List<string> FlowCards { get; set; }
            /// <summary>
            /// 计划号
            /// </summary>
            public string Production { get; set; }
            public int State { get; set; }
            public string StateDesc { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public int CostTime { get; set; }
            public string StepName { get; set; }
            public string Processor { get; set; }
            public int Total { get; set; }
            public int Qualified { get; set; }
            public int Unqualified { get; set; }
            public decimal QualifiedRate { get; set; }
            public decimal UnqualifiedRate { get; set; }
            public List<FenKa> FenKas { get; set; }
        }

        public static int ttt = 0;
        /// <summary>
        /// 获取设备当前加工数据
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<MonitoringProcess> GetMonitoringProcesses(IEnumerable<int> deviceIds = null)
        {
            var allDeviceList = MonitoringProcessHelper.GetMonitoringProcesses(deviceIds).ToDictionary(x => x.DeviceId);
            if (RedisHelper.Exists(aDeviceKey))
            {
                allDeviceList.AddRange(ServerConfig.ApiDb.Query<MonitoringProcess>(
                    "SELECT a.Id DeviceId, c.DeviceCategoryId, c.CategoryName, a.`Code`, a.`ScriptId` FROM `device_library` a " +
                    "JOIN (SELECT a.*, b.CategoryName FROM `device_model` a " +
                    "JOIN `device_category` b ON a.DeviceCategoryId = b.Id) c ON a.DeviceModelId = c.Id WHERE a.MarkedDelete = 0;").ToDictionary(x => x.DeviceId));

                var redisDeviceList = RedisHelper.Get<string>(aDeviceKey).ToClass<IEnumerable<MonitoringProcess>>();
                if (redisDeviceList != null)
                {
                    foreach (var redisDevice in redisDeviceList)
                    {
                        var deviceId = redisDevice.DeviceId;
                        if (allDeviceList.ContainsKey(deviceId))
                        {
                            var device = allDeviceList[deviceId];
                            redisDevice.DeviceCategoryId = device.DeviceCategoryId;
                            redisDevice.CategoryName = device.CategoryName;
                            redisDevice.Code = device.Code;
                            redisDevice.ScriptId = device.ScriptId;
                            allDeviceList[deviceId] = redisDevice;
                            if (allDeviceList[deviceId].State == 1 && allDeviceList[deviceId].ProcessType == ProcessType.Idle)
                            {
                                allDeviceList[deviceId].ProcessType = ProcessType.Process;
                            }
                        }
                    }
                }
                //var redisDeviceList = RedisHelper.Get<string>(aDeviceKey).ToClass<IEnumerable<MonitoringProcess>>();
                //if (redisDeviceList != null)
                //{
                //    foreach (var redisDevice in redisDeviceList)
                //    {
                //        var deviceId = redisDevice.DeviceId;
                //        if (allDeviceList.ContainsKey(deviceId))
                //        {
                //            var device = allDeviceList[deviceId];
                //            redisDevice.DeviceCategoryId = device.DeviceCategoryId;
                //            redisDevice.Code = device.Code;
                //            redisDevice.ScriptId = device.ScriptId;
                //            allDeviceList[deviceId] = redisDevice;
                //        }
                //    }
                //}
            }
            return allDeviceList.Values;
        }

        /// <summary>
        /// 
        /// </summary>
        private static void AnalysisOther()
        {
#if !DEBUG
            if (RedisHelper.Get<int>(Debug) != 0)
            {
                return;
            }
#endif
            if (RedisHelper.SetIfNotExist(aoLockKey, DateTime.Now.ToStr()))
            {
                try
                {
                    RedisHelper.SetExpireAt(aoLockKey, DateTime.Now.AddMinutes(5));
                    var startTime = RedisHelper.Get<DateTime>(aoTimeKey);
                    if (startTime == default(DateTime))
                    {
                        startTime = RedisHelper.Get<string>(aoTimeKey).ToClass<DateTime>();
                    }
                    if (startTime == default(DateTime))
                    {
                        startTime = ServerConfig.ApiDb.Query<DateTime>("SELECT Time FROM `npc_monitoring_process` ORDER BY Time LIMIT 1;").FirstOrDefault();
                    }

                    if (startTime == default(DateTime))
                    {
                        RedisHelper.Remove(aoLockKey);
                        return;
                    }

                    //var t = Stopwatch.StartNew();
                    var deviceCount = DeviceLibraryHelper.Instance.GetCountAll();
                    if (deviceCount <= 0)
                    {
                        RedisHelper.Remove(aoLockKey);
                        return;
                    }
                    //Console.WriteLine($"1 {t.ElapsedMilliseconds}");
                    var mData = ServerConfig.ApiDb.Query<MonitoringProcess>(
                        "SELECT * FROM `npc_monitoring_process` WHERE Time >= @Time ORDER BY Time LIMIT @Limit;", new
                        {
                            Time = startTime,
                            Limit = _dealLength
                        });
                    //Console.WriteLine($"2 {t.ElapsedMilliseconds}");
                    if (mData.Any())
                    {
                        startTime = mData.Last().Time;
                        var table = MonitoringProcessHelper.Tables;
                        var l = table.Count;
                        //npc_monitoring_process_   每个表每台设备最后存储记录的时间
                        var resLast = new Dictionary<int, MonitoringProcess>[l];
                        for (var i = 0; i < l; i++)
                        {
                            var time = startTime;
                            switch (i)
                            {
                                case 0: time = startTime.NoSecond(); break;
                                case 1: time = startTime.NoMinute(); break;
                                case 2: time = startTime.NoHour(); break;
                                case 3: time = startTime.StartOfMonth(); break;
                            }
                            resLast[i] = ServerConfig.ApiDb.Query<MonitoringProcess>(
                                $"SELECT * FROM (SELECT * FROM {table[i]} WHERE Time = @Time ORDER BY Time DESC LIMIT @Limit) a GROUP BY a.DeviceId;", new
                                {
                                    Time = time,
                                    Limit = deviceCount
                                }).ToDictionary(x => x.DeviceId);
                        }
                        //Console.WriteLine($"3 {t.ElapsedMilliseconds}");

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
                        //Console.WriteLine($"4 {t.ElapsedMilliseconds}");

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
                        //Console.WriteLine($"5 {t.ElapsedMilliseconds}");
                        for (var i = 0; i < l; i++)
                        {
                            var data = res[i].Select(x => x.Value).OrderBy(y => y.Time);
                            var tab = table[i];
                            Task.Run(() =>
                            {
                                ServerConfig.ApiDb.Execute(
                                    $"INSERT INTO {tab} (`Time`, `DeviceId`, `State`, `ProcessCount`, `TotalProcessCount`, `ProcessTime`, `TotalProcessTime`, `RunTime`, `TotalRunTime`, `Use`, `Total`, `Rate`, `DayTotal`, `DayQualified`, `DayUnqualified`, `DayQualifiedRate`, `DayUnqualifiedRate`) " +
                                    $"VALUES (@Time, @DeviceId, @State, @ProcessCount, @TotalProcessCount, @ProcessTime, @TotalProcessTime, @RunTime, @TotalRunTime, @Use, @Total, @Rate, @DayTotal, @DayQualified, @DayUnqualified, @DayQualifiedRate, @DayUnqualifiedRate) " +
                                    "ON DUPLICATE KEY UPDATE `State` = @State, `ProcessCount` = @ProcessCount, `TotalProcessCount` = @TotalProcessCount, `ProcessTime` = @ProcessTime, `TotalProcessTime` = @TotalProcessTime, `RunTime` = @RunTime, `TotalRunTime` = @TotalRunTime, `Use` = @Use, `Total` = @Total, `Rate` = @Rate, `DayTotal` = @DayTotal, `DayQualified` = @DayQualified, `DayUnqualified` = @DayUnqualified, `DayQualifiedRate` = @DayQualifiedRate, `DayUnqualifiedRate` = @DayUnqualifiedRate;"
                                    , data);
                            });
                            //ServerConfig.ApiDb.Execute($"INSERT INTO {table[i]} (`Time`, `DeviceId`, `State`, `ProcessCount`, `TotalProcessCount`, `ProcessTime`, `TotalProcessTime`, `RunTime`, `TotalRunTime`, `Use`, `Total`, `Rate`) " +
                            //                           $"VALUES (@Time, @DeviceId, @State, @ProcessCount, @TotalProcessCount, @ProcessTime, @TotalProcessTime, @RunTime, @TotalRunTime, @Use, @Total, @Rate) ", data);
                        }
                        //Console.WriteLine($"6 {t.ElapsedMilliseconds}");
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
                    RedisHelper.SetForever(aoTimeKey, startTime.ToStr());
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                RedisHelper.Remove(aoLockKey);
            }
        }

        /// <summary>
        /// 故障统计
        /// </summary>
        private static void Fault()
        {
#if !DEBUG
            if (RedisHelper.Get<int>(Debug) != 0)
            {
                return;
            }
#endif

            if (RedisHelper.SetIfNotExist(fLockKey, DateTime.Now.ToStr()))
            {
                try
                {
                    RedisHelper.SetExpireAt(fLockKey, DateTime.Now.AddMinutes(5));
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
                RedisHelper.Remove(fLockKey);
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
            if (RedisHelper.SetIfNotExist(sLockKey, DateTime.Now.ToStr()))
            {
                RedisHelper.SetExpireAt(sLockKey, DateTime.Now.AddMinutes(5));
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
                RedisHelper.Remove(sLockKey);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private static void UpdateProcessLog()
        {
#if !DEBUG
            if (RedisHelper.Get<int>(Debug) != 0)
            {
                return;
            }
#endif
            if (RedisHelper.SetIfNotExist(uplLockKey, DateTime.Now.ToStr()))
            {
                try
                {
                    RedisHelper.SetExpireAt(uplLockKey, DateTime.Now.AddHours(1));
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
                                        r.AddRange(ServerConfig.DataReadDb.Query<MonitoringAnalysis>(
                                            $"SELECT * FROM `{ServerConfig.DataReadDb.Table}` WHERE DeviceId = @DeviceId AND UserSend = 0 AND SendTime BETWEEN @SendTime1 AND @SendTime2;",
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
                RedisHelper.Remove(uplLockKey);
            }
        }

        /// <summary>
        ///流程卡上报初始化
        /// </summary>
        public static void FlowCardReport(bool isReport = false)
        {
#if !DEBUG
            if (RedisHelper.Get<int>(Debug) != 0)
            {
                return;
            }
#endif

            var change = false;
            if (RedisHelper.SetIfNotExist(fcrLockKey, DateTime.Now.ToStr()))
            {
                try
                {
                    RedisHelper.SetExpireAt(fcrLockKey, DateTime.Now.AddMinutes(5));
                    var deviceList = new List<FlowCardReport>();
                    var dl = RedisHelper.Get<IEnumerable<FlowCardReport>>(fcrDeviceKey);
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
                        RedisHelper.SetForever(fcrDeviceKey, deviceList);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }

                RedisHelper.Remove(fcrLockKey);
            }
        }

        /// <summary>
        ///流程卡上报记录设备加工状态
        /// </summary>
        public static void FlowCardReportAnalysis()
        {
#if !DEBUG
            if (RedisHelper.Get<int>(Debug) != 0)
            {
                return;
            }
#endif

            if (RedisHelper.SetIfNotExist(fcraLockKey, DateTime.Now.ToStr()))
            {
                try
                {
                    RedisHelper.SetExpireAt(fcraLockKey, DateTime.Now.AddMinutes(5));
                    var dl = RedisHelper.Get<string>(fcraDeviceKey).ToClass<IEnumerable<FlowCardReport>>().ToDictionary(x => x.DeviceId);
                    var deviceList = DeviceLibraryHelper.Instance.GetAll<DeviceLibrary>();
                    var change = false;
                    foreach (var device in deviceList)
                    {
                        var fc = FlowCardReportHelper.GetDetail(device.Id).FirstOrDefault();
                        if (fc != null)
                        {
                            if (dl.ContainsKey(fc.DeviceId))
                            {
                                if (ClassExtension.HaveChange(dl[fc.DeviceId], fc))
                                {
                                    change = true;
                                    dl[fc.DeviceId] = fc;
                                }
                            }
                            else
                            {
                                change = true;
                                dl.Add(fc.DeviceId, fc);
                            }
                        }
                    }

                    if (change)
                    {
                        RedisHelper.SetForever(fcraDeviceKey, dl.Values.ToJSON());
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }

                RedisHelper.Remove(fcraLockKey);
            }
        }

        /// <summary>
        ///获取上次加工流程卡
        /// </summary>
        public static Dictionary<int, FlowCardReport> GetFlowCardReportAnalysis()
        {
            return RedisHelper.Get<string>(fcraDeviceKey).ToClass<IEnumerable<FlowCardReport>>().ToDictionary(x => x.DeviceId);
        }
    }
}
