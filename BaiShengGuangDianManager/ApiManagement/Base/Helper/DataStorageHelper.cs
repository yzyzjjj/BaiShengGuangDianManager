using ApiManagement.Base.Control;
using ApiManagement.Base.Server;
using ApiManagement.Models;
using ApiManagement.Models.Analysis;
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
    public class DataStorageHelper
    {
        private static bool _analysisFirst;
        private static Timer _analysis;
        private static Timer _delete;
        private static int _dealLength = 1000;
        private static MonitoringKanban _monitoringKanban;
        public static void Init(IConfiguration configuration)
        {
            var monitoringKanban = ServerConfig.ApiDb.Query<MonitoringKanban>("SELECT * FROM `npc_monitoring_kanban` WHERE `Date` = @Date;", new
            {
                Date = DateTime.Today
            }).FirstOrDefault();
            ServerConfig.MonitoringKanban = monitoringKanban;
            var _pre = "Analysis";
            var lockKey = $"{_pre}:Lock";
            var redisKey = $"{_pre}:Id";
            if (!_analysisFirst)
            {
                _analysisFirst = true;
                var startId1 = ServerConfig.RedisHelper.Get<int>(redisKey);
                Thread.Sleep(2000);
                var startId2 = ServerConfig.RedisHelper.Get<int>(redisKey);
                if (startId1 == startId2)
                {
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
            }
            _analysis = new Timer(Analysis, null, 10000, 2000);
            _delete = new Timer(Delete, null, 10000, 1000);
        }

        private static void Delete(object state)
        {
            ServerConfig.DataStorageDb.Execute(
                "DELETE FROM npc_monitoring_data WHERE SendTime < ADDDATE(DATE(NOW()), -3) LIMIT 1000");
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
                            var allDeviceList = ServerConfig.ApiDb.Query<MonitoringProcess>(
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
                                    Log.ErrorFormat("缺少变量配置:{0}", variableNameIdList.Where(x => !usuallyDictionaries.Any(y => y.VariableNameId == x)).ToJSON());
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
                                var lastData = mData.OrderBy(x => x.SendTime.NoMillisecond());
                                if (lastData.Any())
                                {
                                    var time = DateTime.Now.NoMillisecond();
                                    foreach (var data in lastData)
                                    {
                                        time = data.SendTime.NoMillisecond();

                                        if (_monitoringKanban.Time != data.SendTime.NoMillisecond())
                                        {
                                            if (_monitoringKanban.InitCount > 0 || _monitoringKanban.Init)
                                            {
                                                if (Update(allDeviceList, time))
                                                {
                                                    ServerConfig.ApiDb.Execute(
                                                        "INSERT INTO npc_monitoring_kanban (`Date`, `AllDevice`, `NormalDevice`, `ProcessDevice`, `IdleDevice`, `FaultDevice`, `ConnectErrorDevice`, `MaxUse`, `UseListStr`, `MaxUseRate`, `MinUse`, `MinUseRate`, `MaxSimultaneousUseRate`, `MinSimultaneousUseRate`, `SingleProcessRateStr`, `AllProcessRate`, `RunTime`, `ProcessTime`, `IdleTime`) VALUES (@Date, @AllDevice, @NormalDevice, @ProcessDevice, @IdleDevice, @FaultDevice, @ConnectErrorDevice, @MaxUse, @UseListStr, @MaxUseRate, @MinUse, @MinUseRate, @MaxSimultaneousUseRate, @MinSimultaneousUseRate, @SingleProcessRateStr, @AllProcessRate, @RunTime, @ProcessTime, @IdleTime) " +
                                                        "ON DUPLICATE KEY UPDATE `AllDevice` = @AllDevice, `NormalDevice` = @NormalDevice, `ProcessDevice` = @ProcessDevice, `IdleDevice` = @IdleDevice, `FaultDevice` = @FaultDevice, `ConnectErrorDevice` = @ConnectErrorDevice, `MaxUse` = @MaxUse, `UseListStr` = @UseListStr, `MaxUseRate` = @MaxUseRate, `MinUse` = @MinUse, `MinUseRate` = @MinUseRate, `MaxSimultaneousUseRate` = @MaxSimultaneousUseRate, `MinSimultaneousUseRate` = @MinSimultaneousUseRate, `SingleProcessRateStr` = @SingleProcessRateStr, `AllProcessRate` = @AllProcessRate, `RunTime` = @RunTime, `ProcessTime` = @ProcessTime, `IdleTime` = @IdleTime;"
                                                        , ServerConfig.MonitoringKanban);

                                                    _monitoringKanban = new MonitoringKanban
                                                    {
                                                        Time = data.SendTime.NoMillisecond(),
                                                        FaultDevice = faultDeviceCount,
                                                        AllDevice = allDeviceList.Count(),
                                                    };
                                                    if (_monitoringKanban.Time.InSameDay(DateTime.Now))
                                                    {
                                                        _monitoringKanban.UseList = ServerConfig.MonitoringKanban.UseList;
                                                    }
                                                }

                                            }
                                        }

                                        if (usuallyDictionaries != null && usuallyDictionaries.Any())
                                        {
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
                                                if (analysisData.vals.Count >= actAddress)
                                                {
                                                    currentFlowCardId = analysisData.vals[actAddress];
                                                }

                                                actAddress = uDies[new Tuple<int, int>(data.ScriptId, stateDId)] - 1;
                                                if (analysisData.vals.Count >= actAddress)
                                                {
                                                    var v = analysisData.vals[actAddress];
                                                    if (currentFlowCardId != 0)
                                                    {
                                                        //开始加工
                                                        var bStart = deviceList[data.DeviceId].State == 0 && v == 1;
                                                        //停止加工
                                                        var bEnd = deviceList[data.DeviceId].State == 1 && v == 0;
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
                                                                var flowCardProcessStepDetail = flowCardProcessStepDetails.FirstOrDefault();
                                                                if (flowCardProcessStepDetail != null)
                                                                {
                                                                    var sql = string.Empty;
                                                                    //开始加工
                                                                    if (bStart)
                                                                    {
                                                                        if (flowCardProcessStepDetail.ProcessTime == default(DateTime))
                                                                        {
                                                                            flowCardProcessStepDetail.ProcessTime = data.ReceiveTime;
                                                                            sql =
                                                                                "UPDATE flowcard_process_step SET `ProcessTime` = @ProcessTime WHERE `Id` = @Id;";
                                                                        }
                                                                    }

                                                                    //停止加工
                                                                    if (bEnd)
                                                                    {
                                                                        //if (flowCardProcessStepDetail.ProcessEndTime == default(DateTime))
                                                                        //{
                                                                        flowCardProcessStepDetail.ProcessEndTime = data.ReceiveTime;
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
                                            }
                                        }
                                        deviceList[data.DeviceId].Time = data.SendTime.NoMillisecond();

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
                                    Update(allDeviceList, time);
                                }
                            }
                            #endregion

                            ServerConfig.ApiDb.Execute(
                                "UPDATE npc_proxy_link SET `Time` = @Time, `State` = @State, `ProcessCount` = @ProcessCount, `TotalProcessCount` = @TotalProcessCount, " +
                                "`ProcessTime` = @ProcessTime, `TotalProcessTime` = @TotalProcessTime, `RunTime` = @RunTime, `TotalRunTime` = @TotalRunTime WHERE `DeviceId` = @DeviceId;",
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
                                "INSERT INTO npc_monitoring_process (`Time`, `DeviceId`, `ProcessCount`, `TotalProcessCount`, `ProcessTime`, `TotalProcessTime`, `RunTime`, `TotalRunTime`, `State`, `Rate`, `Use`, `Total`) " +
                                "VALUES (@Time, @DeviceId, @ProcessCount, @TotalProcessCount, @ProcessTime, @TotalProcessTime, @RunTime, @TotalRunTime, @State, @Rate, @Use, @Total);",
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
            _monitoringKanban.AllProcessRate = (todayDevice.Sum(x => x.ProcessTime) * 1m / (todayDevice.Count() * 24 * 3600)).ToRound(4);
            _monitoringKanban.RunTime = todayDevice.Sum(x => x.RunTime);
            _monitoringKanban.ProcessTime = todayDevice.Sum(x => x.ProcessTime);

            _monitoringKanban.UseCodeList = allDeviceList.OrderBy(x => x.DeviceId).Where(x => _monitoringKanban.UseList.Contains(x.DeviceId)).Select(x => x.Code).ToList();
            if (ServerConfig.MonitoringKanban == null)
            {
                ServerConfig.MonitoringKanban = new MonitoringKanban();
            }

            return ServerConfig.MonitoringKanban.Update(_monitoringKanban);
        }
    }
}
