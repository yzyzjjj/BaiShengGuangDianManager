using ApiManagement.Base.Helper;
using Microsoft.EntityFrameworkCore.Internal;
using ModelBase.Base.Logger;
using ModelBase.Base.Utils;
using Newtonsoft.Json;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartProcessDevice
    {
        public int Rate = 85;
        /// <summary>
        /// 设备id
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// 设备状态
        /// </summary>
        public SmartDeviceOperateState State { get; set; } = SmartDeviceOperateState.未加工;
        /// <summary>
        /// 设备类别
        /// </summary>
        public int CategoryId { get; set; }
        /// <summary>
        /// 流程卡流程id
        /// </summary>
        public int ProcessId { get; set; }
        /// <summary>
        /// 加工人id
        /// </summary>
        public int ProcessorId { get; set; }
        /// <summary>
        /// 加工开始时间
        /// </summary>
        public DateTime StartTime { get; set; }
        /// <summary>
        /// 预计加工结束时间
        /// </summary>
        public DateTime EndTime { get; set; }
        public bool LastDone { get; set; }
        /// <summary>
        /// 完成所有加工 预计结束时间
        /// </summary>
        public DateTime FinalEndTime
        {
            get
            {
                if (State == SmartDeviceOperateState.加工中)
                {
                    var thisProcess = NextProcesses.First();
                    return EndTime.AddSeconds((thisProcess.Item3 - ThisProcessCount) * thisProcess.Item4 + NextProcesses.Take(1).Sum(x => x.Item3 * x.Item4));
                }
                else
                {
                    return DateTime.Now.AddSeconds(NextProcesses.Take(1).Sum(x => x.Item3 * x.Item4));
                }
            }
        }

        /// <summary>
        /// 本次总加工次数（含正在加工）
        /// </summary>
        public int ThisProcessCount { get; set; }
        /// <summary>
        /// 总加工次数（含正在加工）
        /// </summary>
        public int TotalProcessCount { get; set; }
        /// <summary>
        /// 本次总加工时长
        /// </summary>
        public int ThisTotalSecond { get; set; }
        /// <summary>
        /// 已加工时间
        /// </summary>
        public int ThisDoingSecond => State == SmartDeviceOperateState.加工中 ? (int)(DateTime.Now - StartTime).TotalSeconds : 0;
        /// <summary>
        /// 剩余加工时间
        /// </summary>
        public int ThisLeftSecond => ThisTotalSecond - ThisDoingSecond;
        /// <summary>
        /// 总计加工时间
        /// </summary>
        public decimal TotalSecond { get; set; }
        /// <summary>
        /// 下次加工流程卡流程id 加工人id  单次加工数量  总加工次数 单次加工时间
        /// </summary>
        public List<Tuple<int, int, int, int, int>> NextProcesses { get; set; } = new List<Tuple<int, int, int, int, int>>();

        /// <summary>
        /// 设备初始化
        /// </summary>
        public void Init()
        {
            State = SmartDeviceOperateState.未加工;
            ProcessId = 0;
            ThisProcessCount = 0;
            ThisTotalSecond = 0;
            ProcessorId = 0;
            StartTime = default(DateTime);
            EndTime = default(DateTime);
            LastDone = false;
        }

        /// <summary>
        /// 开始下次加工
        /// </summary>
        public bool StartNextProcess()
        {
            var f = false;
            if (NextProcesses.Any())
            {
                LastDone = false;
                ProcessId = NextProcesses.First().Item1;
                ProcessorId = NextProcesses.First().Item2;
                var processNumber = NextProcesses.First().Item3;
                var processCount = NextProcesses.First().Item4;
                ThisTotalSecond = NextProcesses.First().Item5;
                var process = SmartFlowCardProcessHelper.Instance.Get<SmartFlowCardProcess>(ProcessId);
                if (process != null && process.State == SmartFlowCardProcessState.等待中)
                {
                    if (State == SmartDeviceOperateState.未加工)
                    {
                        ThisProcessCount++;
                        f = true;
                        var now = DateTime.Now;
                        process.State = SmartFlowCardProcessState.加工中;
                        StartTime = now;
                        if (process.Qualified + process.Unqualified + process.Doing == 0)
                        {
                            process.StartTime = StartTime;
                        }
                        EndTime = now.AddSeconds(ThisTotalSecond);
                        process.Count++;
                        process.Doing = process.Left < processNumber ? process.Left : processNumber;
                        SmartFlowCardProcessHelper.Instance.Update(process);
                        WorkFlowHelper.Instance.OnSmartFlowCardProcessChanged(new List<SmartFlowCardProcess> { process });
                        State = SmartDeviceOperateState.加工中;
                    }
                }
            }
            return f;
        }

        /// <summary>
        /// 完成本次加工
        /// </summary>
        public bool CompleteThisProcess(out int processorId)
        {
            processorId = 0;
            var now = DateTime.Now;
            var f = false;
            if (State == SmartDeviceOperateState.加工中)
            {
                if (EndTime <= now)
                {
                    f = true;
                    if (ProcessId != 0)
                    {
                        var process = SmartFlowCardProcessHelper.Instance.Get<SmartFlowCardProcess>(ProcessId);
                        if (process != null)
                        {
                            var 合格率 = (RateWeight)RandomSeed.GetWeightRandom(ScheduleHelper.合格率);
                            var qualified = process.Doing * 合格率.Rate / 100;
                            var unqualified = process.Doing - qualified;
                            process.Qualified += qualified;
                            process.Unqualified += unqualified;
                            process.Doing = 0;
                            process.EndTime = now;
                            var second = (int)(process.EndTime - process.StartTime).TotalSeconds;
                            ThisTotalSecond += second;
                            TotalSecond += second;
                            var last = process.Left == 0;
                            process.State = last ? SmartFlowCardProcessState.已完成 : SmartFlowCardProcessState.等待中;
                            var createUserId = SmartUserHelper.Instance.GetSmartUserAccountById(process.ProcessorId) ?? "";
                            if (last)
                            {
                                processorId = ProcessorId;
                                NextProcesses.Remove(NextProcesses.First());
                                SmartFlowCardProcessHelper.Instance.UpdateSmartFlowCardProcessNextBefore(process.FlowCardId, process.Id, process.Qualified);
                                if (process.Rate < Rate)
                                {
                                    process.Fault = true;
                                    SmartProcessFaultHelper.Instance.Add(new SmartProcessFault
                                    {
                                        CreateUserId = createUserId,
                                        MarkedDateTime = now,
                                        FaultTime = now,
                                        Type = ProcessFault.合格率低,
                                        Remark = $"合格率{process.Rate.ToRound()}%,低于{Rate}%",
                                        DeviceId = process.DeviceId,
                                        FlowCardId = process.FlowCardId,
                                        ProcessId = process.Id,
                                    });
                                }

                                Init();
                                LastDone = true;
                            }
                            SmartFlowCardProcessHelper.Instance.Update(process);

                            process.StartTime = StartTime;
                            var log = new SmartFlowCardProcessLog(createUserId, now, process, qualified, unqualified);
                            SmartFlowCardProcessLogHelper.Instance.Add(log);
                        }
                    }
                    State = SmartDeviceOperateState.准备中;
                    StartTime = now;
                    EndTime = now.AddSeconds(30);
                }
            }
            return f;
        }

        public void ReadyDone()
        {
            var now = DateTime.Now;
            if (State == SmartDeviceOperateState.准备中)
            {
                if (EndTime <= now)
                {
                    if (LastDone)
                    {
                        Init();
                    }
                    else
                    {

                        State = SmartDeviceOperateState.未加工;
                    }
                }
            }
        }

        /// <summary>
        /// 本次加工机器坏了， 全部不合格
        /// </summary>
        public bool BreakThisProcess(out int processorId)
        {
            processorId = 0;
            var now = DateTime.Now;
            var f = false;
            if (State == SmartDeviceOperateState.故障中)
            {
                f = true;
                if (ProcessId != 0)
                {
                    var process = SmartFlowCardProcessHelper.Instance.Get<SmartFlowCardProcess>(ProcessId);
                    if (process != null)
                    {
                        process.State = SmartFlowCardProcessState.暂停中;
                        process.Unqualified += process.Doing;
                        process.Doing = 0;
                        process.EndTime = now;
                        SmartFlowCardProcessHelper.Instance.Update(process);
                        var last = process.Left == 0;
                        if (last)
                        {
                            NextProcesses.Remove(NextProcesses.First());
                        }
                        processorId = ProcessorId;
                    }
                }

                ProcessId = 0;
                ProcessorId = 0;
                StartTime = default(DateTime);
                EndTime = default(DateTime);
            }
            return f;
        }
    }

    public class SmartProcessDeviceDetail : SmartProcessDevice
    {
        public string StateStr => State.ToString();
        /// <summary>
        /// 机台号
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// 设备类别
        /// </summary>
        public string Category { get; set; }
        /// <summary>
        /// 流程卡
        /// </summary>
        public string FlowCard => NextProcess.Any() ? NextProcess.First().Item1 : "";
        /// <summary>
        /// 加工前
        /// </summary>
        public int Before { get; set; }
        /// <summary>
        /// 合格数量
        /// </summary>
        public int Qualified { get; set; }
        /// <summary>
        /// 不合格数量
        /// </summary>
        public int Unqualified { get; set; }
        /// <summary>
        /// 进度
        /// </summary>
        public decimal Progress => Before != 0 ? ((decimal)(Qualified + Unqualified) / Before).ToRound(4) * 100 : 0;
        public DateTime DeliveryTime { get; set; }
        /// <summary>
        /// 下次加工流程卡和流程
        /// </summary>
        public List<Tuple<string, string>> NextProcess { get; set; } = new List<Tuple<string, string>>();
    }

    public class SmartProcessor
    {
        /// <summary>
        /// 加工人id
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// 加工次数
        /// </summary>
        public int Count { get; set; }
        /// <summary>
        /// 总加工次数
        /// </summary>
        public int TotalCount { get; set; }
    }

    public class SmartFlowCardProcessDevice : SmartFlowCardProcess
    {
        /// <summary>
        /// 加工设备类型
        /// </summary>
        public int DeviceCategoryId { get; set; }
        /// <summary>
        /// 加工数量
        /// </summary>
        public int ProcessNumber { get; set; }
        /// <summary>
        /// 工艺数据
        /// </summary>
        public string ProcessData { get; set; }
        public List<SmartProcessCraft> Crafts
        {
            get
            {
                try
                {
                    if (!ProcessData.IsNullOrEmpty())
                    {
                        return JsonConvert.DeserializeObject<List<SmartProcessCraft>>(ProcessData);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(ProcessData);
                    Log.Error(e);
                }
                return new List<SmartProcessCraft>();
            }
        }
        public decimal TotalSecond => Crafts.Sum(x => x.TotalSecond);
    }
}
