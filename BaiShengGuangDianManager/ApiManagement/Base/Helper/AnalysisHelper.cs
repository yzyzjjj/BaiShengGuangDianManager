using ApiManagement.Base.Control;
using ApiManagement.Base.Server;
using ApiManagement.Models;
using ApiManagement.Models.Analysis;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using ModelBase.Base.Logger;
using ModelBase.Base.Utils;
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
        private static readonly string AnalysisPre = "Analysis";
        private static readonly string AnalysisLock = $"{AnalysisPre}:Lock";
        private static readonly string AnalysisKey = $"{AnalysisPre}:Id";

        private static readonly string AnalysisOtherPre = "AnalysisOther";
        private static readonly string AnalysisOtherLock = $"{AnalysisOtherPre}:Lock";
        private static readonly string AnalysisOtherKey = $"{AnalysisOtherPre}:Time";

        private static Timer _analysis;
        private static Timer _analysisOther;
        private static Timer _delete;
        private static Timer _fault;
        private static Timer _script;
        private static Timer _processLog;
        private static bool _isDelete;
        private static bool _isFault;
        private static bool _isScript;
        private static bool _isProcessLog;
        private static int _dealLength = 500;
        private static MonitoringKanban _monitoringKanban;
        public static void Init(IConfiguration configuration)
        {
            try
            {
                _script = new Timer(Script, null, 5000, Timeout.Infinite);
#if DEBUG
                Console.WriteLine("AnalysisHelper 调试模式已开启");
#else
                if (!ServerConfig.RedisHelper.Exists(Debug))
                {
                    ServerConfig.RedisHelper.SetForever(Debug, 0);
                }

                ServerConfig.RedisHelper.Remove(AnalysisOtherLock);
                var monitoringKanban = ServerConfig.ApiDb.Query<MonitoringKanban>("SELECT * FROM `npc_monitoring_kanban` WHERE `Date` = @Date;", new
                {
                    Date = DateTime.Today
                }).FirstOrDefault();
                ServerConfig.MonitoringKanban = monitoringKanban;
                var startId1 = ServerConfig.RedisHelper.Get<int>(AnalysisKey);
                Thread.Sleep(2000);
                var startId2 = ServerConfig.RedisHelper.Get<int>(AnalysisKey);
                if (startId1 == 0 && startId2 == 0)
                {
                    var data = ServerConfig.DataStorageDb.Query<MonitoringData>("SELECT Id FROM `npc_monitoring_data` ORDER BY SendTime LIMIT 1;").FirstOrDefault();
                    if (data != null)
                    {
                        ServerConfig.RedisHelper.SetForever(AnalysisKey, data.Id);
                    }
                }
                if (startId1 == startId2)
                {
                    ServerConfig.RedisHelper.Remove(AnalysisLock);
                }

                Console.WriteLine("发布模式已开启");
                _analysis = new Timer(Analysis, null, 10000, 2000);
                _analysisOther = new Timer(AnalysisOther, null, 12000, 2000);
                _delete = new Timer(Delete, null, 10000, 1000);
                _fault = new Timer(Fault, null, 10000, 1000 * 10);
                _processLog = new Timer(UpdateProcessLog, null, 5000, 1000 * 60);
#endif
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private static void Delete(object state)
        {
            try
            {
                if (_isDelete)
                {
                    return;
                }

                _isDelete = true;
                ServerConfig.DataStorageDb.Execute(
                    "DELETE FROM npc_monitoring_data WHERE SendTime < ADDDATE(DATE(NOW()), -3) LIMIT 1000", 60);
                _isDelete = false;
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        private static void Analysis(object state)
        {
#if !DEBUG
            if (ServerConfig.RedisHelper.Get<int>("Debug") != 0)
            {
                return;
            }
#endif

            var _pre = "Analysis";
            var deviceKey = $"{_pre}:Device";
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
                                else
                                {
                                    data.AnalysisData = null;
                                }
                                data.Data = data.AnalysisData.ToJSON();
                            }

                            #region  加工记录
                            //设备状态
                            var stateDId = 1;
                            //总加工次数
                            var processCountDId = 63;
                            //总加工时间
                            var processTimeDId = 64;
                            //当前加工流程卡号
                            var currentFlowCardDId = 6;
                            //连续运行时间
                            var runTimeDId = 2;
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

                            IEnumerable<MonitoringProcess> allDeviceList = null;
                            //if (ServerConfig.RedisHelper.Exists(deviceKey))
                            //{
                            //    allDeviceList = ServerConfig.RedisHelper.Get<IEnumerable<MonitoringProcess>>(deviceKey);
                            //}
                            //else
                            //{
                            //    allDeviceList = ServerConfig.ApiDb.Query<MonitoringProcess>(
                            //        "SELECT b.*, c.DeviceCategoryId, a.`Code` FROM `device_library` a JOIN `npc_proxy_link` b ON a.Id = b.DeviceId JOIN `device_model` c ON a.DeviceModelId = c.Id WHERE a.MarkedDelete = 0;");
                            //}

                            allDeviceList = ServerConfig.ApiDb.Query<MonitoringProcess>(
                                "SELECT b.*, c.DeviceCategoryId, a.`Code` FROM `device_library` a JOIN `npc_proxy_link` b ON a.Id = b.DeviceId JOIN `device_model` c ON a.DeviceModelId = c.Id WHERE a.MarkedDelete = 0;");

                            var deviceList = allDeviceList.Where(x => mData.Any(y => y.DeviceId == x.DeviceId)).ToDictionary(x => x.DeviceId);
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
                                    ServerConfig.RedisHelper.Remove(lockKey);
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
                                                    x.ScriptId == 0 && x.VariableNameId == variableNameId)
                                                .DictionaryId;

                                        uDies.Add(new Tuple<int, int>(script, variableNameId), address);
                                    }
                                }

                                var faultDeviceCount = ServerConfig.ApiDb.Query<int>(
                                    "SELECT COUNT(1) FROM ( SELECT * FROM `fault_device` WHERE MarkedDelete = 0 ORDER BY DeviceCode, State DESC ) a GROUP BY DeviceCode;").FirstOrDefault();

                                _monitoringKanban = _monitoringKanban ?? new MonitoringKanban
                                {
                                    Time = DateTime.Now.NoMillisecond(),
                                    FaultDevice = faultDeviceCount,
                                    AllDevice = allDeviceList.Count(),
                                };
                                var lastData = mData.OrderBy(x => x.SendTime);
                                if (lastData.Any())
                                {
                                    var time = DateTime.Now.NoMillisecond();
                                    foreach (var data in lastData)
                                    {
                                        time = data.SendTime.NoMillisecond();
                                        if (_monitoringKanban.Time != time)
                                        {
                                            if (_monitoringKanban.InitCount > 0 || _monitoringKanban.Init)
                                            {
                                                if (Update(allDeviceList, time))
                                                {
                                                    _monitoringKanban = new MonitoringKanban
                                                    {
                                                        Time = time,
                                                        FaultDevice = faultDeviceCount,
                                                        AllDevice = allDeviceList.Count(),
                                                    };
                                                    if (_monitoringKanban.Time.InSameDay(DateTime.Now))
                                                    {
                                                        _monitoringKanban.UseList = ServerConfig.MonitoringKanban.UseList;
                                                        _monitoringKanban.SingleProcessRate = ServerConfig.MonitoringKanban.SingleProcessRate;
                                                    }
                                                }
                                            }
                                        }

                                        if (usuallyDictionaries == null || !usuallyDictionaries.Any())
                                        {
                                            continue;
                                        }

                                        var analysisData = data.AnalysisData;
                                        if (analysisData != null)
                                        {
                                            if (time == _monitoringKanban.Time.NoMillisecond())
                                            {
                                                _monitoringKanban.Init = true;
                                                _monitoringKanban.InitCount++;
                                            }

                                            var actAddress = uDies[new Tuple<int, int>(data.ScriptId, currentFlowCardDId)] - 1;
                                            var currentFlowCardId = 0;
                                            FlowCardProcessStepDetail flowCardProcessStepDetail = null;
                                            if (analysisData.vals.Count >= actAddress)
                                            {
                                                currentFlowCardId = analysisData.vals[actAddress];
                                            }

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

                                                //开始加工
                                                if (bStart)
                                                {
                                                    ServerConfig.ApiDb.Execute(
                                                        "INSERT INTO `npc_monitoring_process_log` (`DeviceId`, `StartTime`, `FlowCardId`, `ProcessorId`, `ProcessData`, `RequirementMid`) " +
                                                        "VALUES(@DeviceId, @StartTime, @FlowCardId, @ProcessorId, @ProcessData, @RequirementMid);",
                                                        new MonitoringProcessLog
                                                        {
                                                            DeviceId = data.DeviceId,
                                                            StartTime = time,
                                                            FlowCardId = currentFlowCardId,
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
                                                if (v > 0 && !_monitoringKanban.UseList.Contains(data.DeviceId))
                                                {
                                                    _monitoringKanban.UseList.Add(data.DeviceId);
                                                }
                                                if (v == 0 && _monitoringKanban.UseList.Contains(data.DeviceId))
                                                {
                                                    _monitoringKanban.UseList.Remove(data.DeviceId);
                                                }
                                                deviceList[data.DeviceId].State = v > 0 ? 1 : 0;
                                            }

                                            //总加工次数
                                            actAddress = uDies[new Tuple<int, int>(data.ScriptId, processCountDId)] - 1;
                                            if (analysisData.vals.Count >= actAddress)
                                            {
                                                var totalProcessCount = analysisData.vals[actAddress];
                                                if (deviceList[data.DeviceId].TotalProcessTime != 0)
                                                {
                                                    deviceList[data.DeviceId].ProcessCount =
                                                        deviceList[data.DeviceId].Time.InSameDay(data.SendTime)
                                                            ? deviceList[data.DeviceId].ProcessCount : 0;

                                                    deviceList[data.DeviceId].ProcessCount +=
                                                        totalProcessCount - deviceList[data.DeviceId].TotalProcessCount;
                                                }
                                                deviceList[data.DeviceId].TotalProcessCount = totalProcessCount;
                                            }

                                            //总加工时间
                                            actAddress = uDies[new Tuple<int, int>(data.ScriptId, processTimeDId)] - 1;
                                            if (analysisData.vals.Count >= actAddress)
                                            {
                                                var totalProcessTime = analysisData.vals[actAddress];
                                                if (deviceList[data.DeviceId].TotalProcessTime != 0)
                                                {
                                                    deviceList[data.DeviceId].ProcessTime =
                                                        deviceList[data.DeviceId].Time.InSameDay(data.SendTime)
                                                            ? deviceList[data.DeviceId].ProcessTime : 0;

                                                    deviceList[data.DeviceId].ProcessTime +=
                                                        totalProcessTime - deviceList[data.DeviceId].TotalProcessTime;
                                                }
                                                deviceList[data.DeviceId].TotalProcessTime = totalProcessTime;
                                            }

                                            //总运行时间
                                            actAddress = uDies[new Tuple<int, int>(data.ScriptId, runTimeDId)] - 1;
                                            if (analysisData.vals.Count >= actAddress)
                                            {
                                                var totalRunTime = analysisData.vals[actAddress];
                                                if (deviceList[data.DeviceId].TotalRunTime != 0)
                                                {
                                                    deviceList[data.DeviceId].RunTime =
                                                        deviceList[data.DeviceId].Time.InSameDay(data.SendTime)
                                                            ? deviceList[data.DeviceId].RunTime : 0;

                                                    deviceList[data.DeviceId].RunTime +=
                                                        totalRunTime - deviceList[data.DeviceId].TotalRunTime;
                                                }
                                                deviceList[data.DeviceId].TotalRunTime = totalRunTime;
                                            }

                                            deviceList[data.DeviceId].Time = time;

                                            monitoringProcesses.Add(new MonitoringProcess
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
                                                Use = deviceList.Values.Count(x => x.State == 1),
                                                Total = allDeviceList.Count(),
                                                Rate = (decimal)deviceList.Values.Count(x => x.State == 1) * 100 / allDeviceList.Count(),
                                            });
                                        }
                                    }
                                    Update(allDeviceList, time);
                                }
                            }
                            #endregion

                            //ServerConfig.RedisHelper.SetForever(deviceKey, allDeviceList);

                            ServerConfig.ApiDb.Execute(
                                "UPDATE npc_proxy_link SET `Time` = @Time, `State` = @State, `ProcessCount` = @ProcessCount, `TotalProcessCount` = @TotalProcessCount, " +
                                "`ProcessTime` = @ProcessTime, `TotalProcessTime` = @TotalProcessTime, `RunTime` = @RunTime, `TotalRunTime` = @TotalRunTime WHERE `DeviceId` = @DeviceId;",
                                deviceList.Values);

                            ServerConfig.ApiDb.Execute(
                                "INSERT INTO npc_monitoring_kanban (`Date`, `AllDevice`, `NormalDevice`, `ProcessDevice`, `IdleDevice`, `FaultDevice`, `ConnectErrorDevice`, `MaxUse`, `UseListStr`, `MaxUseRate`, `MinUse`, `MinUseRate`, `MaxSimultaneousUseRate`, `MinSimultaneousUseRate`, `SingleProcessRateStr`, `AllProcessRate`, `RunTime`, `ProcessTime`, `IdleTime`) VALUES (@Date, @AllDevice, @NormalDevice, @ProcessDevice, @IdleDevice, @FaultDevice, @ConnectErrorDevice, @MaxUse, @UseListStr, @MaxUseRate, @MinUse, @MinUseRate, @MaxSimultaneousUseRate, @MinSimultaneousUseRate, @SingleProcessRateStr, @AllProcessRate, @RunTime, @ProcessTime, @IdleTime) " +
                                "ON DUPLICATE KEY UPDATE `AllDevice` = @AllDevice, `NormalDevice` = @NormalDevice, `ProcessDevice` = @ProcessDevice, `IdleDevice` = @IdleDevice, `FaultDevice` = @FaultDevice, `ConnectErrorDevice` = @ConnectErrorDevice, `MaxUse` = @MaxUse, `UseListStr` = @UseListStr, `MaxUseRate` = @MaxUseRate, `MinUse` = @MinUse, `MinUseRate` = @MinUseRate, `MaxSimultaneousUseRate` = @MaxSimultaneousUseRate, `MinSimultaneousUseRate` = @MinSimultaneousUseRate, `SingleProcessRateStr` = @SingleProcessRateStr, `AllProcessRate` = @AllProcessRate, `RunTime` = @RunTime, `ProcessTime` = @ProcessTime, `IdleTime` = @IdleTime;"
                                , ServerConfig.MonitoringKanban);

                            Task.Run(() =>
                            {
                                try
                                {
                                    ServerConfig.ApiDb.ExecuteTrans(
                                        "INSERT INTO npc_monitoring_analysis (`SendTime`, `DeviceId`, `ScriptId`, `Ip`, `Port`, `Data`) VALUES (@SendTime, @DeviceId, @ScriptId, @Ip, @Port, @Data) " +
                                            "ON DUPLICATE KEY UPDATE `SendTime` = @SendTime, `DeviceId` = @DeviceId, `ScriptId` = @ScriptId, `Ip` = @Ip, `Port` = @Port, `Data` = @Data;",
                                        mData);
                                }
                                catch (Exception e)
                                {
                                    Log.Error(e);
                                }
                            });
                            Task.Run(() =>
                            {
                                try
                                {
                                    ServerConfig.ApiDb.ExecuteTrans(
                                        "INSERT INTO npc_monitoring_process (`Time`, `DeviceId`, `ProcessCount`, `TotalProcessCount`, `ProcessTime`, `TotalProcessTime`, `RunTime`, `TotalRunTime`, `State`, `Rate`, `Use`, `Total`) VALUES (@Time, @DeviceId, @ProcessCount, @TotalProcessCount, @ProcessTime, @TotalProcessTime, @RunTime, @TotalRunTime, @State, @Rate, @Use, @Total) " +
                                        "ON DUPLICATE KEY UPDATE `Time` = @Time, `DeviceId` = @DeviceId, `State` = @State, `ProcessCount` = @ProcessCount, `TotalProcessCount` = @TotalProcessCount, `ProcessTime` = @ProcessTime, `TotalProcessTime` = @TotalProcessTime, `RunTime` = @RunTime, `TotalRunTime` = @TotalRunTime, `Use` = @Use, `Total` = @Total, `Rate` = @Rate;",
                                        monitoringProcesses);
                                }
                                catch (Exception e)
                                {
                                    Log.Error(e);
                                }
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

        private static bool Update(IEnumerable<MonitoringProcess> allDeviceList, DateTime time)
        {
            _monitoringKanban.InitCount = 0;
            _monitoringKanban.Time = time;
            var validDevice = allDeviceList.Where(x => Math.Abs((x.Time - _monitoringKanban.Time).TotalSeconds) < 5);
            _monitoringKanban.NormalDevice = validDevice.Count();
            _monitoringKanban.ProcessDevice = validDevice.Count(x => x.State == 1);
            _monitoringKanban.MaxUse = _monitoringKanban.UseList.Count;
            _monitoringKanban.MinUse = _monitoringKanban.MaxUse;
            _monitoringKanban.MaxSimultaneousUseRate = _monitoringKanban.ProcessDevice;
            _monitoringKanban.MinSimultaneousUseRate = _monitoringKanban.MaxSimultaneousUseRate;
            foreach (var device in allDeviceList)
            {
                var rate = _monitoringKanban.SingleProcessRate.FirstOrDefault(x =>
                    x.Id == device.DeviceId);
                if (rate == null)
                {
                    _monitoringKanban.SingleProcessRate.Add(new ProcessUseRate
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

            var todayDevice = allDeviceList.Where(x => x.Time.InSameDay(_monitoringKanban.Time));
            //_monitoringKanban.AllProcessRate = (todayDevice.Sum(x => x.ProcessTime) * 1m / (allDeviceList.Count() * 24 * 3600)).ToRound(4);
            _monitoringKanban.AllProcessRate = todayDevice.Any() ? (todayDevice.Sum(x => x.ProcessTime) * 1m / (todayDevice.Count() * 24 * 3600)).ToRound(4) : 0;
            _monitoringKanban.RunTime = todayDevice.Sum(x => x.RunTime);
            _monitoringKanban.ProcessTime = todayDevice.Sum(x => x.ProcessTime);

            _monitoringKanban.UseCodeList = allDeviceList.OrderBy(x => x.DeviceId).Where(x => _monitoringKanban.UseList.Contains(x.DeviceId)).Select(x => x.Code).ToList();
            if (ServerConfig.MonitoringKanban == null)
            {
                ServerConfig.MonitoringKanban = new MonitoringKanban();
            }

            return ServerConfig.MonitoringKanban.Update(_monitoringKanban);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        private static void AnalysisOther(object state)
        {
#if !DEBUG
            if (ServerConfig.RedisHelper.Get<int>("Debug") != 0)
            {
                return;
            }
#endif

            if (ServerConfig.RedisHelper.SetIfNotExist(AnalysisOtherLock, "lock"))
            {
                try
                {
                    var startTime = ServerConfig.RedisHelper.Get<DateTime>(AnalysisOtherKey);
                    if (startTime == default(DateTime))
                    {
                        startTime = ServerConfig.ApiDb.Query<DateTime>(
                            "SELECT Time FROM `npc_monitoring_process` ORDER BY Time LIMIT 1;").FirstOrDefault();
                    }

                    if (startTime == default(DateTime))
                    {
                        ServerConfig.RedisHelper.Remove(AnalysisOtherLock);
                        return;
                    }

                    var deviceCount = ServerConfig.ApiDb.Query<int>(
                        "SELECT COUNT(1) FROM `device_library` a JOIN `npc_proxy_link` b ON a.Id = b.DeviceId WHERE a.MarkedDelete = 0 AND b.Monitoring = 1;").FirstOrDefault();
                    if (deviceCount <= 0)
                    {
                        ServerConfig.RedisHelper.Remove(AnalysisOtherLock);
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
                                $"SELECT * FROM ( SELECT DeviceId, Time FROM {table[i]} WHERE Time = @Time ORDER BY Time DESC LIMIT @Limit ) a GROUP BY a.DeviceId;", new
                                {
                                    Time = time,
                                    Limit = deviceCount
                                }).ToDictionary(x => x.DeviceId);
                        }

                        var res = new List<MonitoringProcess>[l];
                        for (var i = 0; i < l; i++)
                        {
                            res[i] = new List<MonitoringProcess>();
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
                                var n = false;
                                if (!resLast[i].ContainsKey(data.DeviceId))
                                {
                                    n = true;
                                    resLast[i].Add(data.DeviceId, data);
                                }

                                var d = resLast[i][data.DeviceId];
                                var f = false;
                                switch (i)
                                {
                                    case 0:
                                        f = d.Time.InSameMinute(data.Time);
                                        break;
                                    case 1:
                                        f = d.Time.InSameHour(data.Time);
                                        break;
                                    case 2:
                                        f = d.Time.InSameDay(data.Time);
                                        break;
                                    case 3:
                                        f = d.Time.InSameMonth(data.Time);
                                        break;
                                }
                                if (n)
                                {
                                    continue;
                                }

                                if (!f)
                                {
                                    res[i].Add(resLast[i][data.DeviceId]);
                                }
                                resLast[i][data.DeviceId] = data;
                            }
                        }
                        for (var i = 0; i < l; i++)
                        {
                            foreach (var process in resLast[i].Values)
                            {

                                switch (i)
                                {
                                    case 0:
                                        if (!res[i].Any(x => x.DeviceId == process.DeviceId && x.Time.InSameMinute(process.Time)))
                                        {
                                            res[i].Add(process);
                                        }
                                        break;
                                    case 1:
                                        if (!res[i].Any(x => x.DeviceId == process.DeviceId && x.Time.InSameHour(process.Time)))
                                        {
                                            res[i].Add(process);
                                        }
                                        break;
                                    case 2:
                                        if (!res[i].Any(x => x.DeviceId == process.DeviceId && x.Time.InSameDay(process.Time)))
                                        {
                                            res[i].Add(process);
                                        }
                                        break;
                                    case 3:
                                        if (!res[i].Any(x => x.DeviceId == process.DeviceId && x.Time.InSameMonth(process.Time)))
                                        {
                                            res[i].Add(process);
                                        }
                                        break;
                                }
                            }
                            ServerConfig.ApiDb.Execute($"INSERT INTO {table[i]} (`Time`, `DeviceId`, `State`, `ProcessCount`, `TotalProcessCount`, `ProcessTime`, `TotalProcessTime`, `RunTime`, `TotalRunTime`, `Use`, `Total`, `Rate`) VALUES (@Time, @DeviceId, @State, @ProcessCount, @TotalProcessCount, @ProcessTime, @TotalProcessTime, @RunTime, @TotalRunTime, @Use, @Total, @Rate) " +
                                                        "ON DUPLICATE KEY UPDATE `State` = @State, `ProcessCount` = @ProcessCount, `TotalProcessCount` = @TotalProcessCount, `ProcessTime` = @ProcessTime, `TotalProcessTime` = @TotalProcessTime, `RunTime` = @RunTime, `TotalRunTime` = @TotalRunTime, `Use` = IF(`Use` < @Use, @Use, `Use`), `Total` = @Total, `Rate` = IF(`Rate` < @Rate, @Rate, `Rate`);"
                                                       , res[i].OrderBy(x => x.Time));
                        }
                    }
                    ServerConfig.RedisHelper.SetForever(AnalysisOtherKey, startTime);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                ServerConfig.RedisHelper.Remove(AnalysisOtherLock);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        private static void Fault(object state)
        {
#if !DEBUG
            if (ServerConfig.RedisHelper.Get<int>("Debug") != 0)
            {
                return;
            }
#endif

            if (_isFault)
            {
                return;
            }

            _isFault = true;
            var now = DateTime.Now;
            var today = DateTime.Today;
            if ((now - now.Date).TotalSeconds < 10)
            {
                today = today.AddDays(-1);
            }

            FaultCal(today);
            _isFault = false;
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

                var faultDevicesAll = ServerConfig.ApiDb.Query<FaultDeviceDetail>("SELECT a.*, b.SiteName, c.FaultTypeName FROM `fault_device` a LEFT JOIN ( SELECT a.*, b.SiteName FROM `device_library` a JOIN `site` b ON a.SiteId = b.Id ) b ON a.DeviceCode = b.`Code` JOIN `fault_type` c ON a.FaultTypeId = c.Id WHERE b.SiteName IS NOT NULL AND FaultTime >= @FaultTime1 AND FaultTime < @FaultTime2;", new
                {
                    FaultTime1 = today,
                    FaultTime2 = today.AddDays(1),
                });

                var repairRecordsAll = ServerConfig.ApiDb.Query<RepairRecordDetail>("SELECT a.*, b.SiteName, c.FaultTypeName FROM `repair_record` a LEFT JOIN ( SELECT a.*, b.SiteName FROM `device_library` a JOIN `site` b ON a.SiteId = b.Id ) b ON a.DeviceCode = b.`Code` JOIN `fault_type` c ON a.FaultTypeId1 = c.Id WHERE b.SiteName IS NOT NULL AND SolveTime >= @SolveTime1 AND SolveTime < @SolveTime2;", new
                {
                    SolveTime1 = today,
                    SolveTime2 = today.AddDays(1),
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
                        monitoringFault.FaultDevice = faultDeviceDetails.GroupBy(x => x.DeviceCode).Count();
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

                        monitoringFault.Confirmed = ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `fault_device` a LEFT JOIN ( SELECT a.*, b.SiteName FROM `device_library` a JOIN `site` b ON a.SiteId = b.Id ) b ON a.DeviceCode = b.`Code` JOIN `fault_type` c ON a.FaultTypeId = c.Id WHERE b.SiteName IS NOT NULL AND a.MarkedDelete = @MarkedDelete AND a.State = @State AND b.SiteName= @SiteName;", new
                        {
                            MarkedDelete = 0,
                            State = 1,
                            SiteName = workshop,
                        }).FirstOrDefault();
                        monitoringFault.Repairing = ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `fault_device` a LEFT JOIN ( SELECT a.*, b.SiteName FROM `device_library` a JOIN `site` b ON a.SiteId = b.Id ) b ON a.DeviceCode = b.`Code` JOIN `fault_type` c ON a.FaultTypeId = c.Id WHERE b.SiteName IS NOT NULL AND a.MarkedDelete = @MarkedDelete AND a.State = @State AND b.SiteName= @SiteName;", new
                        {
                            MarkedDelete = 0,
                            State = 2,
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

        private static void Script(object state)
        {
            if (_isScript)
            {
                return;
            }

            _isScript = true;

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

            _isScript = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        private static void UpdateProcessLog(object state)
        {
#if !DEBUG
            if (ServerConfig.RedisHelper.Get<int>("Debug") != 0)
            {
                return;
            }
#endif

            if (_isProcessLog)
            {
                return;
            }

            _isProcessLog = true;

            try
            {
                var all = ServerConfig.ApiDb.Query<dynamic>("SELECT a.Id, a.DeviceId, a.StartTime, ScriptId FROM (SELECT a.Id, DeviceId, StartTime, ScriptId FROM `npc_monitoring_process_log` a JOIN `device_library` b ON a.DeviceId = b.Id WHERE ISNULL(EndTime) GROUP BY DeviceId ) a JOIN ( SELECT MAX(Id) Id, DeviceId FROM `npc_monitoring_process_log` GROUP BY DeviceId ) b ON a.DeviceId = b.DeviceId WHERE a.Id != b.Id;");

                //设备状态
                var stateDId = 1;

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
                            var d = ServerConfig.ApiDb.Query<MonitoringAnalysis>(
                                "SELECT * FROM `npc_monitoring_analysis` WHERE DeviceId = @DeviceId AND SendTime BETWEEN @SendTime1 AND @SendTime2;",
                                new
                                {
                                    DeviceId = a.DeviceId,
                                    SendTime1 = a.StartTime,
                                    SendTime2 = nextStartTime,
                                }).OrderBy(x => x.SendTime);

                            var actAddress = uDies[new Tuple<int, int>(a.ScriptId, stateDId)] - 1;
                            var analysis = d.FirstOrDefault(x => x.AnalysisData.vals[actAddress] == 0);
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
            catch (Exception e)
            {
                Log.Error(e);
            }

            _isProcessLog = false;
        }
    }
}
