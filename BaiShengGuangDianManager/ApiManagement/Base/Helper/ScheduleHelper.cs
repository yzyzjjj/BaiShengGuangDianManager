﻿using ApiManagement.Base.Server;
using ApiManagement.Models.SmartFactoryModel;
using ModelBase.Base.Utils;
using Newtonsoft.Json;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static ApiManagement.Models.SmartFactoryModel.ScheduleState;

namespace ApiManagement.Base.Helper
{
    /// <summary>
    /// 生产排程
    /// </summary>
    public class ScheduleHelper
    {
        public static List<DeviceStateWeight> 故障_暂停概率 = new List<DeviceStateWeight>
        {
            new DeviceStateWeight( SmartDeviceOperateState.未加工, 500),
            new DeviceStateWeight( SmartDeviceOperateState.故障中, 1),
            new DeviceStateWeight( SmartDeviceOperateState.暂停中, 1),
        };
        public static List<RateWeight> 合格率 = new List<RateWeight>
        {
            new RateWeight( 100, 10000),
            new RateWeight( 99, 500),
            new RateWeight( 98, 500),
            new RateWeight( 95, 500),
            new RateWeight( 94, 500),
            new RateWeight( 90, 500),
            new RateWeight( 88, 400),
            new RateWeight( 85, 400),
            new RateWeight( 80, 400),
            new RateWeight( 75, 400),
            new RateWeight( 70, 300),
            new RateWeight( 75, 300),
            new RateWeight( 72, 300),
            new RateWeight( 65, 200),
            new RateWeight( 0, 200),
        };

        private const string RedisPre = "Schedule";
        /// <summary>
        /// 最近一次排程时间
        /// </summary>
        private static string DateKey = $"{RedisPre}:Date";
        private static string DateLock = $"{RedisPre}:DateLock";
        private static string StateKey = $"{RedisPre}:State";
        private static string ProcessorKey = $"{RedisPre}:Processor";
        private static string ProcessorLockKey = $"{RedisPre}:ProcessorLock";
        //private static List<SmartProcessDevice> _devices;
        private static List<SmartProcessor> _processors;
        public static readonly ScheduleHelper Instance = new ScheduleHelper();
        private static Timer _scheduleTimer;

        public ScheduleHelper()
        {
            _processors = new List<SmartProcessor>();
        }

        public void Init()
        {
            //_scheduleTimer = new Timer(NeedArrange, null, 5000, 1000 * 60 * 1);

            //            if (!ServerConfig.RedisHelper.Exists(StateKey))
            //            {
            //                ServerConfig.RedisHelper.SetForever(StateKey, 1);
            //            }

            //            var categories = SmartDeviceCategoryHelper.Instance.GetAll<SmartDeviceCategory>();
            //            foreach (var category in categories)
            //            {
            //                UnLockDevice(category.Id);
            //            }
            //            UnLockProcessor();
            //            InitDevice();
            //            InitProcessor();
            //#if DEBUG
            //            return;
            //#endif
            //            var doProcess = new Thread(Do);
            //            doProcess.Start();
        }

        private void NeedArrange(object obj)
        {
            if (ServerConfig.RedisHelper.SetIfNotExist(DateLock, DateTime.Now.ToStrx()))
            {
                ServerConfig.RedisHelper.SetExpireAt(DateLock, DateTime.Now.AddMinutes(5));
                var last = ServerConfig.RedisHelper.Get<DateTime>(DateKey);
                var now = DateTime.Now;
                if (last == default(DateTime) || last < now.Date)
                {
                    var tasks = new List<SmartTaskOrderConfirm>();
                    var schedule = new List<SmartTaskOrderScheduleDetail>();
                    ArrangeSchedule(tasks, ref schedule, true);
                    ServerConfig.RedisHelper.SetForever(DateKey, now);
                }
            }
        }

        public void InitDevice()
        {
            var devices = new List<SmartProcessDevice>();
            var deviceList = SmartDeviceHelper.Instance.GetAll<SmartDevice>();
            var categoryIds = deviceList.GroupBy(x => x.CategoryId).Select(y => y.Key);
            foreach (var categoryId in categoryIds)
            {
                devices.Clear();
                var deviceKey = GetDeviceKey(categoryId);
                var processDevices = ServerConfig.RedisHelper.Get<List<SmartProcessDevice>>(deviceKey);
                var categoryDevices = deviceList.Where(x => x.CategoryId == categoryId);
                if (processDevices == null)
                {
                    devices.AddRange(categoryDevices.Select(x => new SmartProcessDevice
                    {
                        Id = x.Id,
                        CategoryId = categoryId,
                    }));
                    UpdateDevices(categoryId, devices);
                }
                else
                {
                    if (categoryDevices.Count() != processDevices.Count
                        || categoryDevices.Any(x => processDevices.All(y => y.Id != x.Id))
                        || processDevices.Any(x => categoryDevices.All(y => y.Id != x.Id)))
                    {
                        //删除
                        var delete = processDevices.Where(x => categoryDevices.All(y => y.Id != x.Id)).ToList();
                        //添加除删除外的
                        devices.AddRange(processDevices.Where(x => delete.Any(y => y.Id == x.Id)));
                        //添加
                        var add = categoryDevices.Where(x => processDevices.All(y => y.Id != x.Id)).ToList();
                        devices.AddRange(add.Select(x => new SmartProcessDevice
                        {
                            Id = x.Id,
                            CategoryId = categoryId,
                        }));
                        UpdateDevices(categoryId, devices);
                    }
                }
            }
        }

        public void InitProcessor()
        {
            var users = SmartUserHelper.Instance.GetAll<SmartUser>();
            var processorList = ServerConfig.RedisHelper.Get<List<SmartProcessor>>(ProcessorKey);
            if (processorList == null)
            {
                _processors.AddRange(users.Select(x => new SmartProcessor
                {
                    Id = x.Id
                }));
            }
            else
            {
                var delete = _processors.Where(x => users.All(y => y.Id != x.Id));
                _processors.AddRange(processorList.Where(x => delete.All(y => y.Id != x.Id)).Select(z => new SmartProcessor
                {
                    Id = z.Id
                }));
            }

            var add = users.Where(x => _processors.All(y => y.Id != x.Id)).Select(z => new SmartProcessor
            {
                Id = z.Id
            });
            _processors.AddRange(add);
            UpdateProcessors(_processors);
        }

        private void Do()
        {
            while (true)
            {
#if !DEBUG
                if (ServerConfig.RedisHelper.Get<int>("StateKey") != 1)
                {
                    return;
                }
#endif
                Simulate();

                var categories = SmartDeviceCategoryHelper.Instance.GetAll<SmartDeviceCategory>();
                var releaseProcessors = new List<int>();
                foreach (var category in categories)
                {
                    while (true)
                    {
                        if (LockDevice(category.Id, "加工"))
                        {
                            var deviceKey = GetDeviceKey(category.Id);
                            var processDevices = ServerConfig.RedisHelper.Get<List<SmartProcessDevice>>(deviceKey);
                            foreach (var device in processDevices)
                            {
                                device.StartNextProcess();
                                if (device.CompleteThisProcess(out var processorId) && processorId != 0)
                                {
                                    releaseProcessors.Add(processorId);
                                }
                                device.ReadyDone();
                            }
                            UpdateDevices(category.Id, processDevices);
                            UnLockDevice(category.Id);
                            break;
                        }
                    }
                }

                if (releaseProcessors.Any())
                {
                    ReleaseProcess(releaseProcessors);
                }

                Thread.Sleep(5000);
            }
        }

        private string GetDeviceKey(int categoryId)
        {
            return $"{RedisPre}:Device{categoryId}";
        }

        private string GetDeviceLockKey(int categoryId)
        {
            return $"{RedisPre}:DeviceLock{categoryId}";
        }

        private bool LockDevice(int categoryId, string op)
        {
            var deviceKey = GetDeviceLockKey(categoryId);
            return ServerConfig.RedisHelper.SetIfNotExist(deviceKey, $"{DateTime.Now.ToStrx()}:{op}");
        }
        private void UnLockDevice(int categoryId)
        {
            var deviceKey = GetDeviceLockKey(categoryId);
            ServerConfig.RedisHelper.Remove(deviceKey);
        }
        private bool LockProcessor(string op)
        {
            return ServerConfig.RedisHelper.SetIfNotExist(ProcessorLockKey, $"{DateTime.Now.ToStrx()}:{op}");
        }
        private void UnLockProcessor()
        {
            ServerConfig.RedisHelper.Remove(ProcessorLockKey);
        }

        public IEnumerable<SmartProcessDevice> Devices()
        {
            var devices = new List<SmartProcessDevice>();
            var categories = SmartDeviceCategoryHelper.Instance.GetAll<SmartDeviceCategory>();
            var categoryIds = categories.GroupBy(x => x.Id).Select(y => y.Key);
            foreach (var categoryId in categoryIds)
            {
                var deviceKey = GetDeviceKey(categoryId);
                var processDevices = ServerConfig.RedisHelper.Get<List<SmartProcessDevice>>(deviceKey);
                devices.AddRange(processDevices);
            }
            return devices;
        }

        /// <summary>
        /// 更新设备
        /// </summary>
        /// <returns></returns>
        private void UpdateDevices(Dictionary<int, SmartProcessDevice> devices)
        {
            foreach (var device in devices)
            {
                var deviceKey = GetDeviceKey(device.Key);
                ServerConfig.RedisHelper.SetForever(deviceKey, device.Value);
            }
        }

        /// <summary>
        /// 更新设备
        /// </summary>
        /// <returns></returns>
        private void UpdateDevices(int categoryId, IEnumerable<SmartProcessDevice> devices)
        {
            var deviceKey = GetDeviceKey(categoryId);
            ServerConfig.RedisHelper.SetForever(deviceKey, devices);
        }

        /// <summary>
        /// 更新加工人
        /// </summary>
        /// <returns></returns>
        private void UpdateProcessors(IEnumerable<SmartProcessor> processors)
        {
            ServerConfig.RedisHelper.SetForever(ProcessorKey, processors);
        }
        /// <summary>
        ///  获取设备状况
        /// </summary>
        /// <param name="categoryId">设备类型id</param>
        /// <param name="deviceId">设备id</param>
        /// <returns></returns>
        public SmartDeviceOperateState GetDeviceState(int categoryId, int deviceId)
        {
            var deviceKey = GetDeviceKey(categoryId);
            var devices = ServerConfig.RedisHelper.Get<List<SmartProcessDevice>>(deviceKey);
            return devices.FirstOrDefault(x => x.Id == deviceId)?.State ?? SmartDeviceOperateState.缺失;
        }

        /// <summary>
        ///  获取加工人状况
        /// </summary>
        /// <param name="processorId">加工人id</param>
        /// <returns></returns>
        public static SmartProcessor GetProcessorState(int processorId)
        {
            return _processors.FirstOrDefault(x => x.Id == processorId);
        }
        ///// <summary>
        /////  获取加工人状况
        ///// </summary>
        ///// <param name="processorId">加工人id</param>
        ///// <returns></returns>
        //public static SmartProcessor GetProcessorState(int processorId)
        //{
        //    var processors = ServerConfig.RedisHelper.Get<List<SmartProcessor>>(ProcessorKey);
        //    return processors.FirstOrDefault(x => x.Id == processorId);
        //}

        /// <summary>
        /// 安排加工人
        /// </summary>
        /// <returns></returns>
        private ScheduleState ArrangeProcess(string op, out int processorId)
        {
            ScheduleState state;
            processorId = 0;
            if (!_processors.Any())
            {
                UnLockProcessor();
                state = 缺少工人;
            }
            else
            {

                var i = RandomSeed.Next(0, _processors.Count - 1);
                var 闲置加工人 = _processors.ElementAt(i);
                var processor = 闲置加工人;
                processorId = processor.Id;
                processor.Count++;
                processor.TotalCount++;
                UpdateProcessors(_processors);
                state = 成功;
            }

            return state;
        }
        ///// <summary>
        ///// 安排加工人
        ///// </summary>
        ///// <returns></returns>
        //public ScheduleState ArrangeProcess(string op, out int processorId)
        //{
        //    ScheduleState state;
        //    processorId = 0;
        //    while (true)
        //    {
        //        if (!LockProcessor($"安排加工人2-{op}"))
        //        {
        //            continue;
        //        }

        //        var processors = ServerConfig.RedisHelper.Get<List<SmartProcessor>>(ProcessorKey);
        //        if (!processors.Any())
        //        {
        //            UnLockProcessor();
        //            state = 缺少工人;
        //            break;
        //        }

        //        var users = SmartUserHelper.Instance.GetAll<SmartUser>();
        //        var 闲置加工人 = processors.OrderBy(z => z.TotalCount).Where(x => x.Count < (users.FirstOrDefault(y => y.Id == x.Id)?.ProcessCount ?? 0));
        //        if (!闲置加工人.Any())
        //        {
        //            UnLockProcessor();
        //            state = 工人繁忙;
        //            break;
        //        }

        //        var processor = 闲置加工人.First();
        //        processorId = processor.Id;
        //        processor.Count++;
        //        processor.TotalCount++;
        //        UpdateProcessors(processors);
        //        UnLockProcessor();
        //        state = 成功;
        //        break;
        //    }
        //    return state;
        //}
        /// <summary>
        /// 释放加工人
        /// </summary>
        /// <returns></returns>
        private void ReleaseProcess(int processorId)
        {
            var processor = _processors.FirstOrDefault(x => x.Id == processorId);
            if (processor != null)
            {
                processor.Count--;
                UpdateProcessors(_processors);
            }
        }
        ///// <summary>
        ///// 释放加工人
        ///// </summary>
        ///// <returns></returns>
        //public void ReleaseProcess(int processorId)
        //{
        //    while (true)
        //    {
        //        if (!LockProcessor($"释放加工人-{processorId}"))
        //        {
        //            continue;
        //        }

        //        var processors = ServerConfig.RedisHelper.Get<List<SmartProcessor>>(ProcessorKey);
        //        var processor = processors.FirstOrDefault(x => x.Id == processorId);
        //        if (processor != null)
        //        {
        //            processor.Count--;
        //            UpdateProcessors(processors);
        //        }
        //        UnLockProcessor();
        //        break;
        //    }
        //}
        /// <summary>
        /// 释放加工人
        /// </summary>
        /// <returns></returns>
        private void ReleaseProcess(IEnumerable<int> processorIds)
        {
            foreach (var processorId in processorIds)
            {
                var processor = _processors.FirstOrDefault(x => x.Id == processorId);
                if (processor != null)
                {
                    processor.Count--;
                }
            }
            UpdateProcessors(_processors);
        }
        ///// <summary>
        ///// 释放加工人
        ///// </summary>
        ///// <returns></returns>
        //public void ReleaseProcess(IEnumerable<int> processorIds)
        //{
        //    while (true)
        //    {
        //        if (!LockProcessor($"释放加工人-{processorIds.Join()}"))
        //        {
        //            continue;
        //        }

        //        var processors = ServerConfig.RedisHelper.Get<List<SmartProcessor>>(ProcessorKey);
        //        foreach (var processorId in processorIds)
        //        {
        //            var processor = processors.FirstOrDefault(x => x.Id == processorId);
        //            if (processor != null)
        //            {
        //                processor.Count--;
        //            }
        //        }
        //        UpdateProcessors(processors);
        //        UnLockProcessor();
        //        break;
        //    }
        //}

        /// <summary>
        /// 安排设备和加工人
        /// </summary>
        /// <param name="deviceCategoryId">设备类型</param>
        /// <param name="flowCardProcessId">流程卡流程id</param>
        /// <param name="processNumber">单次加工数量</param>
        /// <param name="processCount">总加工次数</param>
        /// <param name="totalSecond">总时间</param>
        /// <param name="deviceId"></param>
        /// <param name="processorId"></param>
        /// <returns></returns>
        private ScheduleState Arrange(int deviceCategoryId, int flowCardProcessId, int processNumber, int processCount, int totalSecond, out int deviceId, ref int processorId)
        {
            var state = 成功;
            deviceId = 0;
            if (deviceCategoryId == 0)
            {
                var i = RandomSeed.Next(0, _processors.Count);
                var 闲置加工人 = _processors.ElementAt(i);
                processorId = 闲置加工人.Id;
                return state;
            }

            while (true)
            {
                if (!_processors.Any())
                {
                    state = 缺少工人;
                    break;
                }
                SmartProcessor processor;
                if (processorId == 0)
                {
                    var i = RandomSeed.Next(0, _processors.Count);
                    var 闲置加工人 = _processors.ElementAt(i);
                    processor = 闲置加工人;
                }
                else
                {
                    var pid = processorId;
                    processor = _processors.FirstOrDefault(x => x.Id == pid);
                    if (processor == null)
                    {
                        state = 缺少工人;
                        break;
                    }
                }

                while (true)
                {
                    if (!LockDevice(deviceCategoryId, $"安排设备1-{flowCardProcessId}"))
                    {
                        continue;
                    }

                    var deviceKey = GetDeviceKey(deviceCategoryId);
                    var devices = ServerConfig.RedisHelper.Get<List<SmartProcessDevice>>(deviceKey);
                    if (!devices.Any())
                    {
                        state = 缺少设备;
                        UnLockDevice(deviceCategoryId);
                        break;
                    }
                    var 闲置设备 = devices.Where(x => x.CategoryId == deviceCategoryId && x.State == SmartDeviceOperateState.未加工)
                                        .OrderBy(a => a.FinalEndTime)
                                        .ThenBy(b => b.NextProcesses.Count)
                                        .ThenBy(c => c.Id);
                    if (闲置设备.Any())
                    {
                        var device = 闲置设备.First();
                        deviceId = device.Id;
                        if (!device.NextProcesses.Any(x => x.Item1 == flowCardProcessId && x.Item2 == processor.Id))
                        {
                            device.NextProcesses.Add(new Tuple<int, int, int, int, int>(flowCardProcessId, processorId, processNumber, processCount, totalSecond));
                            processor.Count++;
                            processor.TotalCount++;
                        }
                    }

                    if (deviceId == 0)
                    {
                        var 准备中设备 = devices.Where(x => x.CategoryId == deviceCategoryId && x.State == SmartDeviceOperateState.准备中);
                        if (准备中设备.Any())
                        {
                            var device = 准备中设备.First();
                            deviceId = device.Id;
                            if (!device.NextProcesses.Any(x => x.Item1 == flowCardProcessId && x.Item2 == processor.Id))
                            {
                                device.NextProcesses.Add(new Tuple<int, int, int, int, int>(flowCardProcessId, processorId, processNumber, processCount, totalSecond));
                                processor.Count++;
                                processor.TotalCount++;
                            }
                        }
                    }

                    if (deviceId == 0)
                    {
                        state = 设备繁忙;
                    }
                    else
                    {
                        UpdateDevices(deviceCategoryId, devices);
                    }
                    break;
                }
                UnLockDevice(deviceCategoryId);
                break;
            }
            return state;
        }

        ///// <summary>
        ///// 安排设备和加工人
        ///// </summary>
        ///// <param name="deviceCategoryId">设备类型</param>
        ///// <param name="flowCardProcessId">流程卡流程id</param>
        ///// <param name="processNumber">单次加工数量</param>
        ///// <param name="processCount">总加工次数</param>
        ///// <param name="totalSecond">总时间</param>
        ///// <param name="deviceId"></param>
        ///// <param name="processorId"></param>
        ///// <returns></returns>
        //public ScheduleState Arrange(int deviceCategoryId, int flowCardProcessId, int processNumber, int processCount, int totalSecond, out int deviceId, ref int processorId)
        //{
        //    var state = 成功;
        //    deviceId = 0;
        //    if (deviceCategoryId == 0)
        //    {
        //        processorId = 0;
        //        return state;
        //    }

        //    while (true)
        //    {
        //        if (!LockProcessor($"安排加工人1-{flowCardProcessId}"))
        //        {
        //            continue;
        //        }

        //        var processors = ServerConfig.RedisHelper.Get<List<SmartProcessor>>(ProcessorKey);
        //        if (!processors.Any())
        //        {
        //            state = 缺少工人;
        //            UnLockProcessor();
        //            break;
        //        }
        //        var users = SmartUserHelper.Instance.GetAll<SmartUser>();
        //        SmartProcessor processor;
        //        if (processorId == 0)
        //        {
        //            var 闲置加工人 = processors.OrderBy(a => a.TotalCount).ThenBy(b => b.Id).Where(x => x.Count < (users.FirstOrDefault(y => y.Id == x.Id)?.ProcessCount ?? 0));
        //            if (闲置加工人.Any())
        //            {
        //                processor = 闲置加工人.First();
        //                processorId = processor.Id;
        //            }
        //            else
        //            {
        //                state = 工人繁忙;
        //                UnLockProcessor();
        //                break;
        //            }

        //        }
        //        else
        //        {
        //            var pid = processorId;
        //            processor = processors.FirstOrDefault(x => x.Id == pid);
        //            if (processor == null)
        //            {
        //                state = 缺少工人;
        //                UnLockProcessor();
        //                break;
        //            }
        //        }

        //        while (true)
        //        {
        //            if (!LockDevice(deviceCategoryId, $"安排设备1-{flowCardProcessId}"))
        //            {
        //                continue;
        //            }

        //            var deviceKey = GetDeviceKey(deviceCategoryId);
        //            var devices = ServerConfig.RedisHelper.Get<List<SmartProcessDevice>>(deviceKey);
        //            if (!devices.Any())
        //            {
        //                state = 缺少设备;
        //                UnLockDevice(deviceCategoryId);
        //                UnLockProcessor();
        //                break;
        //            }
        //            var 闲置设备 = devices.Where(x => x.CategoryId == deviceCategoryId && x.State == SmartDeviceState.未加工)
        //                                .OrderBy(a => a.FinalEndTime)
        //                                .ThenBy(b => b.NextProcesses.Count)
        //                                .ThenBy(c => c.Id);
        //            if (闲置设备.Any())
        //            {
        //                var device = 闲置设备.First();
        //                deviceId = device.Id;
        //                if (!device.NextProcesses.Any(x => x.Item1 == flowCardProcessId && x.Item2 == processor.Id))
        //                {
        //                    device.NextProcesses.Add(new Tuple<int, int, int, int, int>(flowCardProcessId, processorId, processNumber, processCount, totalSecond));
        //                    processor.Count++;
        //                    processor.TotalCount++;
        //                }
        //            }

        //            if (deviceId == 0)
        //            {
        //                var 准备中设备 = devices.Where(x => x.CategoryId == deviceCategoryId && x.State == SmartDeviceState.准备中);
        //                if (准备中设备.Any())
        //                {
        //                    var device = 准备中设备.First();
        //                    deviceId = device.Id;
        //                    if (!device.NextProcesses.Any(x => x.Item1 == flowCardProcessId && x.Item2 == processor.Id))
        //                    {
        //                        device.NextProcesses.Add(new Tuple<int, int, int, int, int>(flowCardProcessId, processorId, processNumber, processCount, totalSecond));
        //                        processor.Count++;
        //                        processor.TotalCount++;
        //                    }
        //                }
        //            }

        //            if (deviceId == 0)
        //            {
        //                state = 设备繁忙;
        //            }
        //            else
        //            {
        //                UpdateDevices(deviceCategoryId, devices);
        //                UpdateProcessors(processors);
        //            }
        //            break;
        //        }
        //        UnLockDevice(deviceCategoryId);
        //        UnLockProcessor();
        //        break;
        //    }
        //    return state;
        //}

        /// <summary>
        /// 更新设备状态
        /// </summary>
        /// <param name="deviceId">设备id</param>
        /// <param name="deviceCategoryId">设备类型id</param>
        /// <param name="state"></param>
        /// <returns></returns>
        private void UpdateDeviceState(int deviceId, int deviceCategoryId, SmartDeviceOperateState state)
        {
            while (LockDevice(deviceCategoryId, $"更新设备状态-{state}"))
            {
                var deviceKey = GetDeviceKey(deviceCategoryId);
                var devices = ServerConfig.RedisHelper.Get<List<SmartProcessDevice>>(deviceKey);
                var device = devices.FirstOrDefault(x => x.Id == deviceId && x.State != state);
                if (device != null)
                {
                    device.State = state;
                    if (state == SmartDeviceOperateState.故障中)
                    {
                        if (device.BreakThisProcess(out var processorId))
                        {
                            UpdateDevices(deviceCategoryId, devices);
                            ReleaseProcess(processorId);
                        }
                    }
                }
                UnLockDevice(deviceCategoryId);
            }
        }

        ///// <summary>
        /////  获取某类型的设备
        ///// </summary>
        ///// <param name="deviceCategoryId">设备类型</param>
        ///// <returns></returns>
        //public static string GenDevice(int deviceCategoryId)
        //{
        //    _devices = ServerConfig.RedisHelper.Get<List<SmartDeviceProcess>>(DeviceKey);
        //    var 闲置设备 = _devices.Where(x=>x.State == SmartDeviceState.未加工)
        //    return results.FirstOrDefault() ?? "";
        //}

        private void Simulate()
        {
            var createUserId = "System";
            var markedDateTime = DateTime.Now;
            var faults = new List<SmartProcessFault>();
            var processDevices = ServerConfig.ApiDb.Query<SmartFlowCardProcessDevice>("SELECT a.*, b.DeviceCategoryId, b.ProcessNumber, b.ProcessData FROM " +
                                                                        "(SELECT * FROM `t_flow_card_process` WHERE MarkedDelete = 0 AND `State` != @state GROUP BY FlowCardId) a " +
                                                                        "JOIN (SELECT a.Id, b.DeviceCategoryId, b.Process, a.ProcessNumber, a.ProcessData FROM `t_product_process` a " +
                                                                        "JOIN (SELECT a.Id, b.DeviceCategoryId, b.Process FROM `t_process_code_category_process` a " +
                                                                        "JOIN `t_process` b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id WHERE a.MarkedDelete = 0 ORDER BY a.Id;", new { state = SmartFlowCardProcessState.已完成 });
            var categroy0 = new List<int>();
            var categroy_0 = new List<int>();
            var dict = new Dictionary<int, bool>();
            var processTime = 10;
            var checkTime = 10;
            var rate = 85;
            foreach (var processDevice in processDevices)
            {
                if (processDevice.Fault)
                {
                    continue;
                }
                if (processDevice.Before <= 0)
                {
                    faults.Add(new SmartProcessFault
                    {
                        CreateUserId = createUserId,
                        MarkedDateTime = markedDateTime,
                        FaultTime = markedDateTime,
                        Type = ProcessFault.缺少原材料,
                        DeviceId = processDevice.DeviceId,
                        FlowCardId = processDevice.FlowCardId,
                        ProcessId = processDevice.Id,
                    });
                    processDevice.State = SmartFlowCardProcessState.暂停中;
                    processDevice.Fault = true;
                    categroy0.Add(processDevice.Id);
                    continue;
                }

                if (processDevice.DeviceCategoryId == 0)
                {
                    if (!dict.ContainsKey(processDevice.DeviceCategoryId))
                    {
                        dict.Add(processDevice.DeviceCategoryId, true);
                    }
                    if (processDevice.State == SmartFlowCardProcessState.未加工 || processDevice.State == SmartFlowCardProcessState.等待中)
                    {
                        if (dict[processDevice.DeviceCategoryId])
                        {
                            if (ArrangeProcess("检验", out var processorId) == 成功)
                            {
                                processDevice.State = SmartFlowCardProcessState.加工中;
                                processDevice.ProcessorId = processorId;
                                processDevice.StartTime = markedDateTime;
                                processDevice.Count++;
                                var productProcess = SmartProductProcessHelper.Instance.Get<SmartProductProcess>(processDevice.ProcessId);
                                processDevice.Doing = processDevice.Left < productProcess.ProcessNumber ? processDevice.Left : productProcess.ProcessNumber;
                                categroy0.Add(processDevice.Id);
                            }
                        }
                        else
                        {
                            dict[processDevice.DeviceCategoryId] = false;
                        }
                    }
                    else if (processDevice.State == SmartFlowCardProcessState.加工中)
                    {
                        var productProcess = SmartProductProcessHelper.Instance.Get<SmartProductProcess>(processDevice.ProcessId);
                        var endTime = processDevice.StartTime.AddSeconds((double)(productProcess.TotalSecond < checkTime ? checkTime : productProcess.TotalSecond));
                        if (endTime <= markedDateTime)
                        {
                            processDevice.EndTime = markedDateTime;
                            processDevice.State = SmartFlowCardProcessState.等待中;
                            var 合格率 = (RateWeight)RandomSeed.GetWeightRandom(ScheduleHelper.合格率);
                            var qualified = processDevice.Doing * 合格率.Rate / 100;
                            var unqualified = processDevice.Doing - qualified;
                            processDevice.Qualified += qualified;
                            processDevice.Unqualified += unqualified;
                            processDevice.Doing = 0;
                            var last = processDevice.Left == 0;
                            if (last)
                            {
                                processDevice.State = SmartFlowCardProcessState.已完成;
                                ReleaseProcess(processDevice.ProcessorId);
                                SmartFlowCardProcessHelper.Instance.UpdateSmartFlowCardProcessNextBefore(processDevice.FlowCardId, processDevice.Id, processDevice.Qualified);
                                if (processDevice.Rate < rate)
                                {
                                    processDevice.Fault = true;
                                    faults.Add(new SmartProcessFault
                                    {
                                        CreateUserId = createUserId,
                                        MarkedDateTime = markedDateTime,
                                        FaultTime = markedDateTime,
                                        Type = ProcessFault.合格率低,
                                        Remark = $"合格率{processDevice.Rate.ToRound()}%,低于{rate}%",
                                        DeviceId = processDevice.DeviceId,
                                        FlowCardId = processDevice.FlowCardId,
                                        ProcessId = processDevice.Id,
                                    });
                                }
                            }
                            var processor = SmartUserHelper.Instance.GetSmartUserAccountById(processDevice.ProcessorId) ?? "";
                            var log = new SmartFlowCardProcessLog(processor, markedDateTime, processDevice, qualified, unqualified);
                            SmartFlowCardProcessLogHelper.Instance.Add(log);
                            categroy0.Add(processDevice.Id);
                        }
                    }
                }
                else
                {
                    if (processDevice.State == SmartFlowCardProcessState.加工中)
                    {
                        var weight = (DeviceStateWeight)RandomSeed.GetWeightRandom(故障_暂停概率);
                        if (weight != null)
                        {
                            switch (weight.State)
                            {
                                case SmartDeviceOperateState.故障中:
                                    faults.Add(new SmartProcessFault
                                    {
                                        CreateUserId = createUserId,
                                        MarkedDateTime = markedDateTime,
                                        FaultTime = markedDateTime,
                                        Type = ProcessFault.设备故障,
                                        DeviceId = processDevice.DeviceId,
                                        FlowCardId = processDevice.FlowCardId,
                                        ProcessId = processDevice.Id,
                                    });
                                    processDevice.State = SmartFlowCardProcessState.暂停中;
                                    processDevice.Fault = true;
                                    UpdateDeviceState(processDevice.DeviceId, processDevice.DeviceCategoryId, weight.State);
                                    categroy_0.Add(processDevice.Id);
                                    break;
                                case SmartDeviceOperateState.暂停中:
                                    processDevice.State = SmartFlowCardProcessState.暂停中;
                                    UpdateDeviceState(processDevice.DeviceId, processDevice.DeviceCategoryId, weight.State);
                                    categroy_0.Add(processDevice.Id);
                                    break;
                            }
                        }
                    }
                    else if (processDevice.State == SmartFlowCardProcessState.未加工)
                    {
                        if (!dict.ContainsKey(processDevice.DeviceCategoryId))
                        {
                            dict.Add(processDevice.DeviceCategoryId, true);
                        }

                        if (dict[processDevice.DeviceCategoryId])
                        {
                            var totalSecond = processDevice.TotalSecond < processTime ? processTime : processDevice.TotalSecond;
                            var processorId = processDevice.ProcessorId;
                            var processCount = (int)Math.Ceiling((decimal)processDevice.Before / processDevice.ProcessNumber);
                            if (Arrange(processDevice.DeviceCategoryId, processDevice.Id, processDevice.ProcessNumber, processCount, (int)totalSecond,
                                out var deviceId, ref processorId) == ScheduleState.成功)
                            {
                                processDevice.State = SmartFlowCardProcessState.等待中;
                                processDevice.DeviceId = deviceId;
                                processDevice.ProcessorId = processorId;
                                categroy_0.Add(processDevice.Id);
                            }
                            else
                            {
                                dict[processDevice.DeviceCategoryId] = false;
                            }
                        }
                    }
                }
            }
            SmartFlowCardProcessHelper.Instance.UpdateSmartFlowCardProcessArrange(processDevices.Where(x => categroy_0.Contains(x.Id)));
            SmartFlowCardProcessHelper.Instance.Update(processDevices.Where(x => categroy0.Contains(x.Id)));
            SmartProcessFaultHelper.Instance.Add<SmartProcessFault>(faults);
            WorkFlowHelper.Instance.OnSmartFlowCardProcessChanged(processDevices.Where(x => categroy0.Contains(x.Id)));
        }
        private const int Day = 100;

        /// <summary>
        /// 安排入口
        /// </summary>
        /// <param name="tasks">所有待排程任务</param>
        /// <param name="isArrange">是否安排 入库</param>
        /// <param name="arrangeId">是否安排过</param>
        /// <param name="createUserId">安排人</param>
        /// <param name="markedDateTime">安排时间</param>
        /// <param name="type">排程方法 （1）最短工期（2）最早交货期（3）按照工期和交货期之间的距离（4）CR值</param>
        /// https://blog.csdn.net/console11/article/details/96288314
        public static List<SmartTaskOrderScheduleCostDays> ArrangeSchedule(
            IEnumerable<SmartTaskOrderConfirm> tasks,
            ref List<SmartTaskOrderScheduleDetail> schedule,
            bool isArrange = false,
            string createUserId = "",
            DateTime markedDateTime = default(DateTime),
            string arrangeId = "")
        {
            //if (arrangeId == "")
            //{

            //}
            var costDays = new List<SmartTaskOrderScheduleCostDays>();
            if (arrangeId == "")
            {
                var now = DateTime.Now;
                var waitTasks = new List<SmartTaskOrderConfirm>();
                var allTasks = SmartTaskOrderHelper.Instance.GetNotDoneSmartTaskOrders();
                var arrangeTasks = allTasks.Where(x => tasks.All(y => y.Id != x.Id))
                    .Select(ClassExtension.ParentCopyToChild<SmartTaskOrder, SmartTaskOrderConfirm>);
                if (arrangeTasks.Any())
                {
                    var needs =
                        SmartTaskOrderNeedHelper.Instance.GetSmartTaskOrderNeedsByTaskOrderIds(
                            arrangeTasks.Select(x => x.Id));
                    foreach (var task in arrangeTasks)
                    {
                        var tNeeds = needs.Where(x => x.TaskOrderId == task.Id)
                            .Select(ClassExtension.ParentCopyToChild<SmartTaskOrderNeed, SmartTaskOrderSchedule>).Select(y => (SmartTaskOrderSchedule)y.Clone());
                        task.Needs.AddRange(tNeeds);
                    }

                    waitTasks.AddRange(arrangeTasks);
                }
                var taskIds = tasks.Select(x => x.Id);
                if (taskIds.Any())
                {
                    var theseTasks = SmartTaskOrderHelper.Instance.GetByIds<SmartTaskOrder>(taskIds);
                    foreach (var task in tasks)
                    {
                        var tTask = theseTasks.FirstOrDefault(x => x.Id == task.Id);
                        if (tTask != null)
                        {
                            task.Arranged = tTask.Arranged;
                            task.ProductId = tTask.ProductId;
                            task.Target = tTask.Target;
                            task.StartTime = tTask.StartTime;
                            task.EndTime = tTask.EndTime;
                            task.LevelId = tTask.LevelId;
                            task.TaskOrder = tTask.TaskOrder;
                        }
                    }
                    waitTasks.AddRange(tasks);
                }

                //所有工序
                var processes = SmartProcessHelper.Instance.GetAll<SmartProcess>();
                var productIds = waitTasks.Select(x => x.ProductId);
                // 任务单计划号
                var products = SmartProductHelper.Instance.GetByIds<SmartProduct>(productIds);
                // 计划号产能
                var productCapacities = SmartProductCapacityHelper.Instance.GetSmartProductCapacities(productIds);
                var capacityIds = products.Select(x => x.CapacityId);
                // 产能设置
                var smartCapacityLists = SmartCapacityListHelper.Instance.GetSmartCapacityListsWithOrder(capacityIds);
                //设备型号数量
                var deviceList = SmartDeviceHelper.Instance.GetAll<SmartDevice>();
                var modelCount = deviceList.GroupBy(x => x.ModelId).Select(y => new SmartDeviceModelCount
                {
                    ModelId = y.Key,
                    Count = y.Count()
                });
                //人员等级数量
                var operatorList = SmartOperatorHelper.Instance.GetAll<SmartOperator>();
                var operatorCount = operatorList.GroupBy(x => new { x.ProcessId, x.LevelId }).Select(y => new SmartOperatorCount
                {
                    ProcessId = y.Key.ProcessId,
                    LevelId = y.Key.LevelId,
                    Count = y.Count()
                });
                var batch = SmartTaskOrderScheduleHelper.Instance.GetSmartTaskOrderScheduleBatch();
                //工序已安排数量
                var schedules = SmartTaskOrderScheduleHelper.Instance.GetSmartTaskOrderScheduleByBatch(batch);

                //设备 0  人员 1
                var way = 2;
                var total = 4;
                var cnScore = new int[way][];
                var cnWaitTasks = new List<SmartTaskOrderConfirm>[way][];
                var cnSchedules = new Dictionary<DateTime, SmartTaskOrderScheduleDay>[way][];
                var cnCostList = new List<SmartTaskOrderScheduleCostDays>[way][];
                for (var i = 0; i < way; i++)
                {
                    cnScore[i] = new int[total];
                    cnWaitTasks[i] = new List<SmartTaskOrderConfirm>[total];
                    cnSchedules[i] = new Dictionary<DateTime, SmartTaskOrderScheduleDay>[total];
                    cnCostList[i] = new List<SmartTaskOrderScheduleCostDays>[total];
                    for (var j = 0; j < total; j++)
                    {
                        cnScore[i][j] = 0;
                        cnWaitTasks[i][j] = new List<SmartTaskOrderConfirm>();
                        cnSchedules[i][j] = new Dictionary<DateTime, SmartTaskOrderScheduleDay>();
                        cnCostList[i][j] = new List<SmartTaskOrderScheduleCostDays>();
                    }
                }

                foreach (var task in waitTasks)
                {
                    task.MarkedDateTime = markedDateTime;
                    task.Arranged = true;
                    var product = products.FirstOrDefault(x => x.Id == task.ProductId);
                    task.Product = product.Product;
                    task.CapacityId = product.CapacityId;
                    task.CapacityCostDay = product.Number != 0 ? (int)Math.Ceiling((decimal)task.Target / product.Number) : 0;

                    var preProcessId = 0;
                    var i = 0;
                    foreach (var need in task.Needs)
                    {
                        if (i++ == 0)
                        {
                            //首次加工原料数量
                            need.Have = need.Put;
                        }
                        var processId = need.ProcessId;
                        need.PreProcessId = preProcessId;
                        need.Process = processes.FirstOrDefault(x => x.Id == need.PId)?.Process ?? "";
                        var pre = task.Needs.FirstOrDefault(x => x.ProcessId == preProcessId);
                        if (pre != null)
                        {
                            pre.NextProcessId = processId;
                        }
                        preProcessId = processId;
                    }

                    var s = task.ToJSON();


                    for (var j = 0; j < total; j++)
                    {
                        for (var k = 0; k < way; k++)
                        {
                            cnWaitTasks[k][j].Add(JsonConvert.DeserializeObject<SmartTaskOrderConfirm>(s));
                        }
                    }
                }

                for (var i = 0; i < total; i++)
                {
                    var f = true;
                    for (var j = 0; j < way; j++)
                    {
                        var sc = new Dictionary<DateTime, SmartTaskOrderScheduleDay>();
                        switch (i)
                        {
                            //最短工期
                            case 0:
                                sc = MinimumWorkingPeriod(tasks, ref cnWaitTasks[j][i], schedules, productCapacities,
                                    smartCapacityLists, deviceList, modelCount, operatorList, operatorCount, now, j == 0);
                                break;
                            //最早交货期
                            case 1:
                                sc = EarliestDeliveryDate(tasks, ref cnWaitTasks[j][i], schedules, productCapacities,
                                    smartCapacityLists, deviceList, modelCount, operatorList, operatorCount, now, j == 0);
                                break;
                            //按照工期和交货期之间的距离
                            case 2:
                                sc = WorkingPeriodAndDeliveryDate(tasks, ref cnWaitTasks[j][i], schedules, productCapacities,
                                    smartCapacityLists, deviceList, modelCount, operatorList, operatorCount, now, j == 0);
                                break;
                            //CR值
                            case 3:
                                sc = CR(tasks, ref cnWaitTasks[j][i], schedules, productCapacities,
                                    smartCapacityLists, deviceList, modelCount, operatorList, operatorCount, now, j == 0);
                                break;
                            default:
                                f = false;
                                break;
                        }

                        cnSchedules[j][i] = sc;
                    }

                    if (!f)
                    {
                        continue;
                    }
                    //回复耗时
                    //var d = waitTasks1_d.Where(x => tasks.Any(y => y.Id == x.Id));
                    //var o = waitTasks1_o.Where(x => tasks.Any(y => y.Id == x.Id));
                    var d = cnWaitTasks[0][i];
                    var o = cnWaitTasks[1][i];

                    foreach (var task in waitTasks)
                    {
                        var dCost = d.FirstOrDefault(x => x.Id == task.Id);
                        var oCost = o.FirstOrDefault(x => x.Id == task.Id);
                        var dEstimatedStartTime = DateTime.Today;
                        var dEstimatedCompleteTime = DateTime.Today;
                        var oEstimatedStartTime = DateTime.Today;
                        var oEstimatedCompleteTime = DateTime.Today;
                        if (dCost == null || oCost == null)
                        {
                            continue;
                        }

                        var dC = new SmartTaskOrderScheduleCostDays
                        {
                            Id = task.Id,
                            TaskOrder = task.TaskOrder,
                            ProductId = task.ProductId,
                            Product = task.Product,
                            MustCompleteTime = task.EndTime,
                        };
                        var oC = new SmartTaskOrderScheduleCostDays
                        {
                            Id = task.Id,
                            TaskOrder = task.TaskOrder,
                            ProductId = task.ProductId,
                            Product = task.Product,
                            MustCompleteTime = task.EndTime,
                        };
                        foreach (var need in task.Needs)
                        {
                            var day = new SmartTaskOrderScheduleCostDay
                            {
                                ProcessId = need.ProcessId,
                                PId = need.PId,
                                Process = need.Process,
                                Order = need.Order,
                                DeviceDay = dCost.Needs.FirstOrDefault(x => x.ProcessId == need.ProcessId)?.CostDay ?? 0,
                                OperatorDay = oCost.Needs.FirstOrDefault(x => x.ProcessId == need.ProcessId)?.CostDay ?? 0
                            };
                            //设备开始时间
                            var estimatedStartTime =
                                dCost.Needs.FirstOrDefault(x => x.ProcessId == need.ProcessId)?.EstimatedStartTime ??
                                DateTime.Today;
                            if (estimatedStartTime != default(DateTime))
                            {
                                dEstimatedStartTime = (estimatedStartTime < dEstimatedStartTime ? estimatedStartTime : dEstimatedStartTime);
                            }

                            //设备结束时间
                            var estimatedCompleteTime =
                                dCost.Needs.FirstOrDefault(x => x.ProcessId == need.ProcessId)?.EstimatedCompleteTime ??
                                DateTime.Today;

                            if (estimatedCompleteTime != default(DateTime))
                            {
                                dEstimatedCompleteTime = (estimatedCompleteTime > dEstimatedCompleteTime ? estimatedCompleteTime : dEstimatedCompleteTime);
                            }

                            //人员开始时间
                            estimatedStartTime =
                                oCost.Needs.FirstOrDefault(x => x.ProcessId == need.ProcessId)?.EstimatedStartTime ??
                                DateTime.Today;
                            if (estimatedStartTime != default(DateTime))
                            {
                                oEstimatedStartTime = (estimatedStartTime < oEstimatedStartTime ? estimatedStartTime : oEstimatedStartTime);
                            }

                            //人员结束时间
                            estimatedCompleteTime =
                                oCost.Needs.FirstOrDefault(x => x.ProcessId == need.ProcessId)?.EstimatedCompleteTime ??
                               DateTime.Today;

                            if (estimatedCompleteTime != default(DateTime))
                            {
                                oEstimatedCompleteTime = (estimatedCompleteTime > oEstimatedCompleteTime ? estimatedCompleteTime : oEstimatedCompleteTime);
                            }

                            dC.CostDays.Add(day);
                            oC.CostDays.Add(day);
                        }

                        dC.EstimatedStartTime = dEstimatedStartTime;
                        dC.EstimatedCompleteTime = dEstimatedCompleteTime;
                        cnCostList[0][i].Add(dC);

                        oC.EstimatedStartTime = oEstimatedStartTime;
                        oC.EstimatedCompleteTime = oEstimatedCompleteTime;
                        cnCostList[1][i].Add(oC);
                        var dCostDay = (int)(dEstimatedCompleteTime - dEstimatedStartTime).TotalDays + 1;
                        var oCostDay = (int)(oEstimatedCompleteTime - oEstimatedStartTime).TotalDays + 1;
                        if (dCostDay < oCostDay)
                        {
                            cnScore[0][i]++;
                        }
                        else
                        {
                            cnScore[1][i]++;
                        }
                    }
                }

                BestArrange(way, total, cnScore, waitTasks, cnCostList, cnSchedules, out costDays, out var best);
                //schedules = best.Values.OrderBy(x => x.ProcessTime).SelectMany(y => y.Needs).SelectMany(z => z.Value).Where(t => tasks.Any(task => task.Id == t.TaskOrderId));
                schedules = best.Values.OrderBy(x => x.ProcessTime).SelectMany(y => y.Needs).SelectMany(z => z.Value);
                schedule.AddRange(schedules);
                if (isArrange)
                {
                    taskIds = waitTasks.Select(x => x.Id);
                    var oldNeeds = SmartTaskOrderNeedHelper.Instance.GetSmartTaskOrderNeedsByTaskOrderIds(taskIds);
                    var newNeeds = tasks.SelectMany(x => x.Needs);
                    foreach (var need in newNeeds)
                    {
                        var old = oldNeeds.FirstOrDefault(x =>
                            x.Batch == batch && x.TaskOrderId == need.TaskOrderId && x.PId == need.PId);
                        if (old != null)
                        {
                            need.Stock = old.Stock;
                            need.DoneTarget = old.DoneTarget;
                            need.HavePut = old.HavePut;
                            need.Done = old.Done;
                            need.DoingCount = old.DoingCount;
                            need.Doing = old.Doing;
                            need.IssueCount = old.IssueCount;
                            need.Issue = old.Issue;
                        }

                        need.Batch = batch + 1;
                        need.CreateUserId = createUserId;
                        need.MarkedDateTime = markedDateTime;
                    }

                    SmartTaskOrderNeedHelper.Instance.Add(newNeeds);
                    SmartTaskOrderScheduleHelper.Instance.Add(schedules.Select(x =>
                    {
                        x.Batch = batch + 1;
                        x.CreateUserId = createUserId;
                        x.MarkedDateTime = markedDateTime;
                        return x;
                    }));
                    var indexes = best.Values.OrderBy(x => x.ProcessTime).SelectMany(y => y.CapacityIndex.Select(z =>
                        new SmartTaskOrderScheduleIndex
                        {
                            ProcessTime = y.ProcessTime,
                            PId = z.Key,
                            Index = z.Value
                        }));
                    SmartTaskOrderScheduleIndexHelper.Instance.Add(indexes.Select(x =>
                    {
                        x.Batch = batch + 1;
                        x.CreateUserId = createUserId;
                        x.MarkedDateTime = markedDateTime;
                        return x;
                    }));
                    SmartTaskOrderHelper.Instance.Arrange(waitTasks);
                    //排程是否改变
                    var haveChange = false;
                    var arrange = new List<int>();


                    if (haveChange)
                    {
                        WorkFlowHelper.Instance.OnTaskOrderArrangeChanged(arrange);
                    }
                }
            }
            return costDays;
        }

        /// <summary>
        /// 最优计算
        /// </summary>
        /// <param name="way"></param>
        /// <param name="total"></param>
        /// <param name="cnScore"></param>
        /// <param name="waitTasks"></param>
        /// <param name="cnCostList"></param>
        /// <param name="cnSchedules"></param>
        /// <param name="costDays"></param>
        /// <param name="schedules"></param>
        private static void BestArrange(int way, int total,
            int[][] cnScore,
            List<SmartTaskOrderConfirm> waitTasks,
            List<SmartTaskOrderScheduleCostDays>[][] cnCostList,
            Dictionary<DateTime, SmartTaskOrderScheduleDay>[][] cnSchedules,
            out List<SmartTaskOrderScheduleCostDays> costDays,
            out Dictionary<DateTime, SmartTaskOrderScheduleDay> schedules)
        {
            costDays = new List<SmartTaskOrderScheduleCostDays>();
            schedules = new Dictionary<DateTime, SmartTaskOrderScheduleDay>();
            if (way != cnScore.Length)
            {
                return;
            }

            List<SmartTaskOrderScheduleCostDays> l = null;
            Dictionary<DateTime, SmartTaskOrderScheduleDay> s = null;
            var b = 0;
            List<SmartTaskOrderScheduleCostDays> l1 = null;
            Dictionary<DateTime, SmartTaskOrderScheduleDay> s1 = null;
            var j1 = -1;
            var b1 = 0;
            for (var i = 0; i < total; i++)
            {
                var dScore = cnScore[0];
                var oScore = cnScore[1];
                b = dScore[i] <= oScore[i] ? 0 : 1;
                l = cnCostList[b][i];
                s = cnSchedules[b][i];

                if (!l.Any())
                {
                    if (i == 1)
                    {
                        costDays = cnCostList[b1][j1];
                        schedules = cnSchedules[b1][j1];
                    }
                    else
                    {

                    }
                    break;
                }
                if (i != 0)
                {
                    if (l.Sum(x => x.OverdueDay) < l1.Sum(x => x.OverdueDay))
                    {
                        costDays = cnCostList[b][i];
                        schedules = cnSchedules[b][i];
                    }
                    else
                    {
                        costDays = cnCostList[b1][j1];
                        schedules = cnSchedules[b1][j1];
                    }

                }

                j1++;
                b1 = dScore[i] <= oScore[i] ? 0 : 1;
                l1 = cnCostList[b1][j1];
                s1 = cnSchedules[b1][j1];
            }

            costDays = costDays.OrderBy(x => x.EstimatedStartTime).ThenBy(y => y.EstimatedCompleteTime).ToList();
        }


        /// <summary>
        /// 最短工期
        /// </summary>
        /// <param name="arrangeTasks">本次安排任务</param>
        /// <param name="allTasks">本次安排 + 已安排未完成任务</param>
        /// <param name="schedules">已排生产</param>
        /// <param name="productCapacities"></param>
        /// <param name="smartCapacityLists"></param>
        /// <param name="deviceList"></param>
        /// <param name="modelCounts"></param>
        /// <param name="operatorList"></param>
        /// <param name="operatorCounts"></param>
        /// <param name="time"></param>
        /// <param name="arrangeDevice">按设备产能计算</param>
        /// <returns></returns>
        private static Dictionary<DateTime, SmartTaskOrderScheduleDay> MinimumWorkingPeriod(
            IEnumerable<SmartTaskOrderConfirm> arrangeTasks,
            ref List<SmartTaskOrderConfirm> allTasks,
            IEnumerable<SmartTaskOrderScheduleDetail> schedules,
            IEnumerable<SmartProductCapacityDetail> productCapacities,
            IEnumerable<SmartCapacityListDetail> smartCapacityLists,
            IEnumerable<SmartDevice> deviceList,
            IEnumerable<SmartDeviceModelCount> modelCounts,
            IEnumerable<SmartOperator> operatorList,
            IEnumerable<SmartOperatorCount> operatorCounts,
            DateTime time,
            bool arrangeDevice = false)
        {
            //S级任务  排产不可修改
            //var superTasks = tasks.Where(x => x.LevelId == 0).OrderBy(x => x.EndTime).ThenBy(x => x.Id);
            var superTasks = allTasks.Where(x => x.LevelId == 1).OrderBy(x => x.EndTime).ThenBy(x => x.Id);
            //非S级任务  排产可修改
            var normalTasks = allTasks.Where(x => x.LevelId != 1);
            //有时间要求的任务 先按生产天数从小到大排，再按截止时间从小到大排
            var timeLimitTasks = normalTasks.Where(x => x.EndTime != default(DateTime)).OrderBy(y => y.CapacityCostDay).ThenBy(y => y.EndTime).ToList();
            //没有时间要求的任务 先按生产天数从小到大排，再按目标量从小到大排
            var notTimeLimitTasks = normalTasks.Where(x => x.EndTime == default(DateTime)).OrderBy(y => y.CapacityCostDay).ThenBy(y => y.Target).ToList();
            var newSchedules = new Dictionary<DateTime, SmartTaskOrderScheduleDay>();
            var minTime = time.Date;
            var setStartTime = allTasks.Where(x => x.StartTime != default(DateTime));
            if (setStartTime.Any() && setStartTime.Min(y => y.StartTime) < minTime)
            {
                minTime = setStartTime.Min(y => y.StartTime);
            }
            var setEndTime = allTasks.Where(x => x.EndTime != default(DateTime));
            //最大时间
            var maxTime = !setEndTime.Any() ? minTime.AddDays(Day) : setEndTime.Max(x => x.EndTime);
            if (maxTime < minTime)
            {
                maxTime = minTime.AddDays(Day);
            }
            newSchedules.AddRange(AddDay(newSchedules, maxTime).ToDictionary(x => x.Key, x => x.Value));
            foreach (var schedule in schedules)
            {
                //S级任务且已排产
                var task = superTasks.FirstOrDefault(x => x.Id == schedule.TaskOrderId);
                if (task != null && task.Arranged)
                {
                    if (!newSchedules.ContainsKey(schedule.ProcessTime))
                    {
                        newSchedules.Add(schedule.ProcessTime, new SmartTaskOrderScheduleDay(schedule.ProcessTime));
                    }

                    newSchedules[schedule.ProcessTime].AddTaskOrderSchedule(schedule);
                }
            }

            MinimumWorkingPeriodCal(ref timeLimitTasks, ref newSchedules, productCapacities, smartCapacityLists,
                deviceList, modelCounts, operatorList, operatorCounts, time, arrangeDevice);
            MinimumWorkingPeriodCal(ref notTimeLimitTasks, ref newSchedules, productCapacities, smartCapacityLists,
                deviceList, modelCounts, operatorList, operatorCounts, time, arrangeDevice);

            return newSchedules;
        }

        /// <summary>
        /// 最短工期计算
        /// </summary>
        /// <param name="tasks"></param>
        /// <param name="newSchedules"></param>
        /// <param name="productCapacities"></param>
        /// <param name="smartCapacityLists"></param>
        /// <param name="deviceList"></param>
        /// <param name="modelCounts"></param>
        /// <param name="operatorList"></param>
        /// <param name="operatorCounts"></param>
        /// <param name="time"></param>
        /// <param name="arrangeDevice"></param>
        private static void MinimumWorkingPeriodCal(
            ref List<SmartTaskOrderConfirm> tasks,
            ref Dictionary<DateTime, SmartTaskOrderScheduleDay> newSchedules,
            IEnumerable<SmartProductCapacityDetail> productCapacities,
            IEnumerable<SmartCapacityListDetail> smartCapacityLists,
            IEnumerable<SmartDevice> deviceList,
            IEnumerable<SmartDeviceModelCount> modelCounts,
            IEnumerable<SmartOperator> operatorList,
            IEnumerable<SmartOperatorCount> operatorCounts,
            DateTime time,
            bool arrangeDevice = false)
        {
            var count = tasks.Count();
            var t = time.Date;
            var doneCount = 0;
            while (doneCount < count)
            {
                foreach (var task in tasks)
                {
                    if (task.Left == 0)
                    {
                        continue;
                    }
                    var productId = task.ProductId;
                    var capacityId = task.CapacityId;
                    //产能配置
                    var capacityList = smartCapacityLists.Where(x => x.CapacityId == capacityId);
                    //计划号单日工序实际产能列表
                    var pCapacities = productCapacities.Where(x => x.ProductId == productId);
                    if (pCapacities.Any(x => x.DeviceNumber == 0 || x.OperatorNumber == 0))
                    {
                        continue;
                    }

                    for (var i = 0; i < task.Needs.Count; i++)
                    {
                        var need = task.Needs.ElementAt(i);
                        if (need.Have == 0 && need.LeftPut == 0)
                        {
                            continue;
                        }

                        if (need.LeftPut < 0)
                        {
                            continue;
                        }
                        var processId = need.ProcessId;
                        //工序单日产能配置
                        var cList = capacityList.FirstOrDefault(x => x.ProcessId == processId);
                        need.PId = cList.PId;
                        need.Process = cList.Process;
                        if (cList.CategoryId != 0)
                        {
                            var devices = cList.DeviceList;
                            foreach (var device in devices)
                            {
                                device.Count = modelCounts.FirstOrDefault(x => (int)x.ModelId == device.ModelId) != null
                                    ? (int)modelCounts.FirstOrDefault(x => (int)x.ModelId == device.ModelId).Count : 0;
                            }
                            cList.DNumber = devices.Sum(x => x.Total);
                        }
                        var operators = cList.OperatorList;
                        foreach (var op in operators)
                        {
                            op.Count = operatorCounts.FirstOrDefault(x => (int)x.ProcessId == need.PId && (int)x.LevelId == op.LevelId) != null
                                ? (int)operatorCounts.FirstOrDefault(x => (int)x.ProcessId == need.PId && (int)x.LevelId == op.LevelId).Count : 0;
                        }
                        cList.ONumber = operators.Sum(x => x.Total);
                        if (newSchedules[t].HaveProcessLeftCapacity(need.PId))
                        {
                            if (need.DoneTarget == 0 && need.EstimatedStartTime == default(DateTime))
                            {
                                need.EstimatedStartTime = t;
                                if (task.StartTime == default(DateTime))
                                {
                                    task.StartTime = t;
                                }
                            }
                            //计划号工序单日产能
                            var pCapacity = pCapacities.FirstOrDefault(x => x.ProcessId == processId);
                            //剩余产能指数
                            var left = newSchedules[t].ProcessLeftCapacityIndex(need.PId);
                            //根据安排设备还是产能计算
                            var capacity = arrangeDevice ? (cList.CategoryId == 0 ? cList.ONumber : cList.DNumber) : cList.ONumber;
                            //剩余产能
                            var number = (int)(capacity * left).ToRound(0);
                            //已有可加工数量 与 产能
                            var put = need.Have < number ? need.Have : number;
                            need.HavePut += put;
                            need.Have -= put;
                            var sc = new SmartTaskOrderScheduleDetail(t, task, cList, pCapacity)
                            {
                                MarkedDateTime = time,
                                Put = put,
                                Process = need.Process,
                                //Target = (int)Math.Floor(put * pCapacity.Rate / 100),
                                Target = (int)(put * pCapacity.Rate / 100).ToRound(0),
                                CapacityIndex = ((decimal)put / capacity).ToRound(4)
                            };
                            need.DoneTarget = sc.Target;
                            newSchedules[t].AddTaskOrderSchedule(sc);
                            var next = task.Needs.ElementAtOrDefault(i + 1);
                            if (next != null)
                            {
                                next.Have = sc.Target;
                                //next.Put = sc.Target;
                            }
                            else
                            {
                                task.DoneTarget += sc.Target;
                                task.EndTime = t;
                            }
                            //if (need.DoneTarget == need.Target)
                            //{
                            //    need.EstimatedCompleteTime = t;
                            //}
                            if (task.AllDone(need))
                            {
                                need.EstimatedCompleteTime = t;
                            }
                        }
                    }
                }

                //doneCount = tasks.Sum(x => x.AllDone() ? 1 : 0);
                doneCount = tasks.Count(x => x.AllDone());
                if (!newSchedules[t].HaveArranged())
                {
                    break;
                }

                t = t.AddDays(1);
            }
        }

        /// <summary>
        /// 最早交货期
        /// </summary>
        /// <param name="arrangeTasks">本次安排任务</param>
        /// <param name="allTasks">本次安排 + 已安排未完成任务</param>
        /// <param name="schedules">已排生产</param>
        /// <param name="productCapacities"></param>
        /// <param name="smartCapacityLists"></param>
        /// <param name="deviceList"></param>
        /// <param name="modelCounts"></param>
        /// <param name="operatorList"></param>
        /// <param name="operatorCounts"></param>
        /// <param name="time"></param>
        /// <param name="arrangeDevice">按设备产能计算</param>
        /// <returns></returns>
        private static Dictionary<DateTime, SmartTaskOrderScheduleDay> EarliestDeliveryDate(
            IEnumerable<SmartTaskOrderConfirm> arrangeTasks,
            ref List<SmartTaskOrderConfirm> allTasks,
            IEnumerable<SmartTaskOrderScheduleDetail> schedules,
            IEnumerable<SmartProductCapacityDetail> productCapacities,
            IEnumerable<SmartCapacityListDetail> smartCapacityLists,
            IEnumerable<SmartDevice> deviceList,
            IEnumerable<SmartDeviceModelCount> modelCounts,
            IEnumerable<SmartOperator> operatorList,
            IEnumerable<SmartOperatorCount> operatorCounts,
            DateTime time,
            bool arrangeDevice = false)
        {
            //S级任务  排产不可修改
            //var superTasks = tasks.Where(x => x.LevelId == 0).OrderBy(x => x.EndTime).ThenBy(x => x.Id);
            var superTasks = allTasks.Where(x => x.LevelId == 1).OrderBy(x => x.EndTime).ThenBy(x => x.Id);
            //非S级任务  排产可修改
            var normalTasks = allTasks.Where(x => x.LevelId != 1);
            //有时间要求的任务 先按生产天数从小到大排，再按截止时间从小到大排
            var timeLimitTasks = normalTasks.Where(x => x.EndTime != default(DateTime)).OrderBy(y => y.CapacityCostDay).ThenBy(y => y.EndTime).ToList();
            //没有时间要求的任务 先按生产天数从小到大排，再按目标量从小到大排
            var notTimeLimitTasks = normalTasks.Where(x => x.EndTime == default(DateTime)).OrderBy(y => y.CapacityCostDay).ThenBy(y => y.Target).ToList();
            var newSchedules = new Dictionary<DateTime, SmartTaskOrderScheduleDay>();
            var minTime = time.Date;
            var setStartTime = allTasks.Where(x => x.StartTime != default(DateTime));
            if (setStartTime.Any() && setStartTime.Min(y => y.StartTime) < minTime)
            {
                minTime = setStartTime.Min(y => y.StartTime);
            }
            var setEndTime = allTasks.Where(x => x.EndTime != default(DateTime));
            //最大时间
            var maxTime = !setEndTime.Any() ? minTime.AddDays(Day) : setEndTime.Max(x => x.EndTime);
            if (maxTime < minTime)
            {
                maxTime = minTime.AddDays(Day);
            }
            newSchedules.AddRange(AddDay(newSchedules, maxTime).ToDictionary(x => x.Key, x => x.Value));
            foreach (var schedule in schedules)
            {
                //S级任务且已排产
                var task = superTasks.FirstOrDefault(x => x.Id == schedule.TaskOrderId);
                if (task != null && task.Arranged)
                {
                    if (newSchedules.ContainsKey(schedule.ProcessTime))
                    {
                        newSchedules[schedule.ProcessTime].AddTaskOrderSchedule(schedule);
                    }
                }
            }

            EarliestDeliveryDateCal(ref timeLimitTasks, ref newSchedules, productCapacities, smartCapacityLists,
                deviceList, modelCounts, operatorList, operatorCounts, time, arrangeDevice);
            EarliestDeliveryDateCal(ref notTimeLimitTasks, ref newSchedules, productCapacities, smartCapacityLists,
                deviceList, modelCounts, operatorList, operatorCounts, time, arrangeDevice);

            return newSchedules;
        }

        /// <summary>
        /// 最早交货期计算
        /// </summary>
        /// <param name="tasks"></param>
        /// <param name="newSchedules"></param>
        /// <param name="productCapacities"></param>
        /// <param name="smartCapacityLists"></param>
        /// <param name="deviceList"></param>
        /// <param name="modelCounts"></param>
        /// <param name="operatorList"></param>
        /// <param name="operatorCounts"></param>
        /// <param name="time"></param>
        /// <param name="arrangeDevice"></param>
        private static void EarliestDeliveryDateCal(
            ref List<SmartTaskOrderConfirm> tasks,
            ref Dictionary<DateTime, SmartTaskOrderScheduleDay> newSchedules,
            IEnumerable<SmartProductCapacityDetail> productCapacities,
            IEnumerable<SmartCapacityListDetail> smartCapacityLists,
            IEnumerable<SmartDevice> deviceList,
            IEnumerable<SmartDeviceModelCount> modelCounts,
            IEnumerable<SmartOperator> operatorList,
            IEnumerable<SmartOperatorCount> operatorCounts,
            DateTime time,
            bool arrangeDevice = false)
        {
            var count = tasks.Count();
            var t = time.Date;
            var doneCount = 0;
            while (doneCount < count)
            {
                foreach (var task in tasks)
                {
                    if (task.Left == 0)
                    {
                        continue;
                    }
                    var productId = task.ProductId;
                    var capacityId = task.CapacityId;
                    //产能配置
                    var capacityList = smartCapacityLists.Where(x => x.CapacityId == capacityId);
                    //计划号单日工序实际产能列表
                    var pCapacities = productCapacities.Where(x => x.ProductId == productId);
                    if (pCapacities.Any(x => x.DeviceNumber == 0 || x.OperatorNumber == 0))
                    {
                        continue;
                    }

                    for (var i = 0; i < task.Needs.Count; i++)
                    {
                        var need = task.Needs.ElementAt(i);
                        if (need.Have == 0 && need.LeftPut == 0)
                        {
                            continue;
                        }

                        if (need.LeftPut < 0)
                        {
                            continue;
                        }
                        var processId = need.ProcessId;
                        //工序单日产能配置
                        var cList = capacityList.FirstOrDefault(x => x.ProcessId == processId);
                        need.PId = cList.PId;
                        need.Process = cList.Process;
                        if (cList.CategoryId != 0)
                        {
                            var devices = cList.DeviceList;
                            foreach (var device in devices)
                            {
                                device.Count = modelCounts.FirstOrDefault(x => (int)x.ModelId == device.ModelId) != null
                                    ? (int)modelCounts.FirstOrDefault(x => (int)x.ModelId == device.ModelId).Count : 0;
                            }
                            cList.DNumber = devices.Sum(x => x.Total);
                        }
                        var operators = cList.OperatorList;
                        foreach (var op in operators)
                        {
                            op.Count = operatorCounts.FirstOrDefault(x => (int)x.ProcessId == need.PId && (int)x.LevelId == op.LevelId) != null
                                ? (int)operatorCounts.FirstOrDefault(x => (int)x.ProcessId == need.PId && (int)x.LevelId == op.LevelId).Count : 0;
                        }
                        cList.ONumber = operators.Sum(x => x.Total);
                        if (newSchedules[t].HaveProcessLeftCapacity(need.PId))
                        {
                            if (need.DoneTarget == 0 && need.EstimatedStartTime == default(DateTime))
                            {
                                need.EstimatedStartTime = t;
                                if (task.StartTime == default(DateTime))
                                {
                                    task.StartTime = t;
                                }
                            }
                            //计划号工序单日产能
                            var pCapacity = pCapacities.FirstOrDefault(x => x.ProcessId == processId);
                            //剩余产能指数
                            var left = newSchedules[t].ProcessLeftCapacityIndex(need.PId);
                            //根据安排设备还是产能计算
                            var capacity = arrangeDevice ? (cList.CategoryId == 0 ? cList.ONumber : cList.DNumber) : cList.ONumber;
                            //剩余产能
                            var number = (int)(capacity * left).ToRound(0);
                            //已有可加工数量 与 产能
                            var put = need.Have < number ? need.Have : number;
                            need.HavePut += put;
                            need.Have -= put;
                            var sc = new SmartTaskOrderScheduleDetail(t, task, cList, pCapacity)
                            {
                                MarkedDateTime = time,
                                Put = put,
                                Process = need.Process,
                                //Target = (int)Math.Floor(put * pCapacity.Rate / 100),
                                Target = (int)(put * pCapacity.Rate / 100).ToRound(0),
                                CapacityIndex = ((decimal)put / capacity).ToRound(4)
                            };
                            need.DoneTarget = sc.Target;
                            newSchedules[t].AddTaskOrderSchedule(sc);
                            var next = task.Needs.ElementAtOrDefault(i + 1);
                            if (next != null)
                            {
                                next.Have = sc.Target;
                                //next.Put = sc.Target;
                            }
                            else
                            {
                                task.DoneTarget += sc.Target;
                                task.EndTime = t;
                            }
                            //if (need.DoneTarget == need.Target)
                            //{
                            //    need.EstimatedCompleteTime = t;
                            //}
                            if (task.AllDone(need))
                            {
                                need.EstimatedCompleteTime = t;
                            }
                        }
                    }
                }

                //doneCount = tasks.Sum(x => x.AllDone() ? 1 : 0);
                doneCount = tasks.Count(x => x.AllDone());
                if (!newSchedules[t].HaveArranged())
                {
                    break;
                }

                t = t.AddDays(1);
            }
        }

        /// <summary>
        /// 工期和交货期之间的距离
        /// </summary>
        /// <param name="arrangeTasks">本次安排任务</param>
        /// <param name="allTasks">本次安排 + 已安排未完成任务</param>
        /// <param name="schedules">已排生产</param>
        /// <param name="productCapacities"></param>
        /// <param name="smartCapacityLists"></param>
        /// <param name="deviceList"></param>
        /// <param name="modelCounts"></param>
        /// <param name="operatorList"></param>
        /// <param name="operatorCounts"></param>
        /// <param name="time"></param>
        /// <param name="arrangeDevice">按设备产能计算</param>
        /// <returns></returns>
        private static Dictionary<DateTime, SmartTaskOrderScheduleDay> WorkingPeriodAndDeliveryDate(
            IEnumerable<SmartTaskOrderConfirm> arrangeTasks,
            ref List<SmartTaskOrderConfirm> allTasks,
            IEnumerable<SmartTaskOrderScheduleDetail> schedules,
            IEnumerable<SmartProductCapacityDetail> productCapacities,
            IEnumerable<SmartCapacityListDetail> smartCapacityLists,
            IEnumerable<SmartDevice> deviceList,
            IEnumerable<SmartDeviceModelCount> modelCounts,
            IEnumerable<SmartOperator> operatorList,
            IEnumerable<SmartOperatorCount> operatorCounts,
            DateTime time,
            bool arrangeDevice = false)
        {
            //S级任务  排产不可修改
            //var superTasks = tasks.Where(x => x.LevelId == 0).OrderBy(x => x.EndTime).ThenBy(x => x.Id);
            var superTasks = allTasks.Where(x => x.LevelId == 1).OrderBy(x => x.EndTime).ThenBy(x => x.Id);
            //非S级任务  排产可修改
            var normalTasks = allTasks.Where(x => x.LevelId != 1);
            //有时间要求的任务 先按生产天数从小到大排，再按截止时间从小到大排
            var timeLimitTasks = normalTasks.Where(x => x.EndTime != default(DateTime)).OrderBy(y => y.CapacityCostDay).ThenBy(y => y.EndTime).ToList();
            //没有时间要求的任务 先按生产天数从小到大排，再按目标量从小到大排
            var notTimeLimitTasks = normalTasks.Where(x => x.EndTime == default(DateTime)).OrderBy(y => y.CapacityCostDay).ThenBy(y => y.Target).ToList();
            var newSchedules = new Dictionary<DateTime, SmartTaskOrderScheduleDay>();
            var minTime = time.Date;
            var setStartTime = allTasks.Where(x => x.StartTime != default(DateTime));
            if (setStartTime.Any() && setStartTime.Min(y => y.StartTime) < minTime)
            {
                minTime = setStartTime.Min(y => y.StartTime);
            }
            var setEndTime = allTasks.Where(x => x.EndTime != default(DateTime));
            //最大时间
            var maxTime = !setEndTime.Any() ? minTime.AddDays(Day) : setEndTime.Max(x => x.EndTime);
            if (maxTime < minTime)
            {
                maxTime = minTime.AddDays(Day);
            }
            newSchedules.AddRange(AddDay(newSchedules, maxTime).ToDictionary(x => x.Key, x => x.Value));
            foreach (var schedule in schedules)
            {
                //S级任务且已排产
                var task = superTasks.FirstOrDefault(x => x.Id == schedule.TaskOrderId);
                if (task != null && task.Arranged)
                {
                    if (newSchedules.ContainsKey(schedule.ProcessTime))
                    {
                        newSchedules[schedule.ProcessTime].AddTaskOrderSchedule(schedule);
                    }
                }
            }

            WorkingPeriodAndDeliveryDateCal(ref timeLimitTasks, ref newSchedules, productCapacities, smartCapacityLists,
                deviceList, modelCounts, operatorList, operatorCounts, time, arrangeDevice);
            WorkingPeriodAndDeliveryDateCal(ref notTimeLimitTasks, ref newSchedules, productCapacities, smartCapacityLists,
                deviceList, modelCounts, operatorList, operatorCounts, time, arrangeDevice);

            return newSchedules;
        }

        /// <summary>
        /// 最早交货期计算
        /// </summary>
        /// <param name="tasks"></param>
        /// <param name="newSchedules"></param>
        /// <param name="productCapacities"></param>
        /// <param name="smartCapacityLists"></param>
        /// <param name="deviceList"></param>
        /// <param name="modelCounts"></param>
        /// <param name="operatorList"></param>
        /// <param name="operatorCounts"></param>
        /// <param name="time"></param>
        /// <param name="arrangeDevice"></param>
        private static void WorkingPeriodAndDeliveryDateCal(
            ref List<SmartTaskOrderConfirm> tasks,
            ref Dictionary<DateTime, SmartTaskOrderScheduleDay> newSchedules,
            IEnumerable<SmartProductCapacityDetail> productCapacities,
            IEnumerable<SmartCapacityListDetail> smartCapacityLists,
            IEnumerable<SmartDevice> deviceList,
            IEnumerable<SmartDeviceModelCount> modelCounts,
            IEnumerable<SmartOperator> operatorList,
            IEnumerable<SmartOperatorCount> operatorCounts,
            DateTime time,
            bool arrangeDevice = false)
        {
            var count = tasks.Count();
            var t = time.Date;
            var doneCount = 0;
            while (doneCount < count)
            {
                foreach (var task in tasks)
                {
                    if (task.Left == 0)
                    {
                        continue;
                    }
                    var productId = task.ProductId;
                    var capacityId = task.CapacityId;
                    //产能配置
                    var capacityList = smartCapacityLists.Where(x => x.CapacityId == capacityId);
                    //计划号单日工序实际产能列表
                    var pCapacities = productCapacities.Where(x => x.ProductId == productId);
                    if (pCapacities.Any(x => x.DeviceNumber == 0 || x.OperatorNumber == 0))
                    {
                        continue;
                    }

                    for (var i = 0; i < task.Needs.Count; i++)
                    {
                        var need = task.Needs.ElementAt(i);
                        if (need.Have == 0 && need.LeftPut == 0)
                        {
                            continue;
                        }

                        if (need.LeftPut < 0)
                        {
                            continue;
                        }
                        var processId = need.ProcessId;
                        //工序单日产能配置
                        var cList = capacityList.FirstOrDefault(x => x.ProcessId == processId);
                        need.PId = cList.PId;
                        need.Process = cList.Process;
                        if (cList.CategoryId != 0)
                        {
                            var devices = cList.DeviceList;
                            foreach (var device in devices)
                            {
                                device.Count = modelCounts.FirstOrDefault(x => (int)x.ModelId == device.ModelId) != null
                                    ? (int)modelCounts.FirstOrDefault(x => (int)x.ModelId == device.ModelId).Count : 0;
                            }
                            cList.DNumber = devices.Sum(x => x.Total);
                        }
                        var operators = cList.OperatorList;
                        foreach (var op in operators)
                        {
                            op.Count = operatorCounts.FirstOrDefault(x => (int)x.ProcessId == need.PId && (int)x.LevelId == op.LevelId) != null
                                ? (int)operatorCounts.FirstOrDefault(x => (int)x.ProcessId == need.PId && (int)x.LevelId == op.LevelId).Count : 0;
                        }
                        cList.ONumber = operators.Sum(x => x.Total);
                        if (newSchedules[t].HaveProcessLeftCapacity(need.PId))
                        {
                            if (need.DoneTarget == 0 && need.EstimatedStartTime == default(DateTime))
                            {
                                need.EstimatedStartTime = t;
                                if (task.StartTime == default(DateTime))
                                {
                                    task.StartTime = t;
                                }
                            }
                            //计划号工序单日产能
                            var pCapacity = pCapacities.FirstOrDefault(x => x.ProcessId == processId);
                            //剩余产能指数
                            var left = newSchedules[t].ProcessLeftCapacityIndex(need.PId);
                            //根据安排设备还是产能计算
                            var capacity = arrangeDevice ? (cList.CategoryId == 0 ? cList.ONumber : cList.DNumber) : cList.ONumber;
                            //剩余产能
                            var number = (int)(capacity * left).ToRound(0);
                            //已有可加工数量 与 产能
                            var put = need.Have < number ? need.Have : number;
                            need.HavePut += put;
                            need.Have -= put;
                            var sc = new SmartTaskOrderScheduleDetail(t, task, cList, pCapacity)
                            {
                                MarkedDateTime = time,
                                Put = put,
                                Process = need.Process,
                                //Target = (int)Math.Floor(put * pCapacity.Rate / 100),
                                Target = (int)(put * pCapacity.Rate / 100).ToRound(0),
                                CapacityIndex = ((decimal)put / capacity).ToRound(4)
                            };
                            need.DoneTarget = sc.Target;
                            newSchedules[t].AddTaskOrderSchedule(sc);
                            var next = task.Needs.ElementAtOrDefault(i + 1);
                            if (next != null)
                            {
                                next.Have = sc.Target;
                                //next.Put = sc.Target;
                            }
                            else
                            {
                                task.DoneTarget += sc.Target;
                                task.EndTime = t;
                            }
                            //if (need.DoneTarget == need.Target)
                            //{
                            //    need.EstimatedCompleteTime = t;
                            //}
                            if (task.AllDone(need))
                            {
                                need.EstimatedCompleteTime = t;
                            }
                        }
                    }
                }

                //doneCount = tasks.Sum(x => x.AllDone() ? 1 : 0);
                doneCount = tasks.Count(x => x.AllDone());
                if (!newSchedules[t].HaveArranged())
                {
                    break;
                }

                t = t.AddDays(1);
            }
        }

        /// <summary>
        /// CR值
        /// CR是英文critical ratio的缩写，可以翻译为重要比率。它的计算方法：交期减去目前日期之差额,再除以工期，数值越小表示紧急程度越高，排程优先级高。
        /// </summary>
        /// <param name="arrangeTasks">本次安排任务</param>
        /// <param name="allTasks">本次安排 + 已安排未完成任务</param>
        /// <param name="schedules">已排生产</param>
        /// <param name="productCapacities"></param>
        /// <param name="smartCapacityLists"></param>
        /// <param name="deviceList"></param>
        /// <param name="modelCounts"></param>
        /// <param name="operatorList"></param>
        /// <param name="operatorCounts"></param>
        /// <param name="time"></param>
        /// <param name="arrangeDevice">按设备产能计算</param>
        /// <returns></returns>
        private static Dictionary<DateTime, SmartTaskOrderScheduleDay> CR(
            IEnumerable<SmartTaskOrderConfirm> arrangeTasks,
            ref List<SmartTaskOrderConfirm> allTasks,
            IEnumerable<SmartTaskOrderScheduleDetail> schedules,
            IEnumerable<SmartProductCapacityDetail> productCapacities,
            IEnumerable<SmartCapacityListDetail> smartCapacityLists,
            IEnumerable<SmartDevice> deviceList,
            IEnumerable<SmartDeviceModelCount> modelCounts,
            IEnumerable<SmartOperator> operatorList,
            IEnumerable<SmartOperatorCount> operatorCounts,
            DateTime time,
            bool arrangeDevice = false)
        {
            //S级任务  排产不可修改
            //var superTasks = tasks.Where(x => x.LevelId == 0).OrderBy(x => x.EndTime).ThenBy(x => x.Id);
            var superTasks = allTasks.Where(x => x.LevelId == 1).OrderBy(x => x.EndTime).ThenBy(x => x.Id);
            //非S级任务  排产可修改
            var normalTasks = allTasks.Where(x => x.LevelId != 1);
            //有时间要求的任务 先按生产天数从小到大排，再按截止时间从小到大排
            var timeLimitTasks = normalTasks.Where(x => x.EndTime != default(DateTime)).OrderBy(y => y.CapacityCostDay).ThenBy(y => y.EndTime).ToList();
            //没有时间要求的任务 先按生产天数从小到大排，再按目标量从小到大排
            var notTimeLimitTasks = normalTasks.Where(x => x.EndTime == default(DateTime)).OrderBy(y => y.CapacityCostDay).ThenBy(y => y.Target).ToList();
            var newSchedules = new Dictionary<DateTime, SmartTaskOrderScheduleDay>();
            var minTime = time.Date;
            var setStartTime = allTasks.Where(x => x.StartTime != default(DateTime));
            if (setStartTime.Any() && setStartTime.Min(y => y.StartTime) < minTime)
            {
                minTime = setStartTime.Min(y => y.StartTime);
            }
            var setEndTime = allTasks.Where(x => x.EndTime != default(DateTime));
            //最大时间
            var maxTime = !setEndTime.Any() ? minTime.AddDays(Day) : setEndTime.Max(x => x.EndTime);
            if (maxTime < minTime)
            {
                maxTime = minTime.AddDays(Day);
            }
            newSchedules.AddRange(AddDay(newSchedules, maxTime).ToDictionary(x => x.Key, x => x.Value));
            foreach (var schedule in schedules)
            {
                //S级任务且已排产
                var task = superTasks.FirstOrDefault(x => x.Id == schedule.TaskOrderId);
                if (task != null && task.Arranged)
                {
                    if (newSchedules.ContainsKey(schedule.ProcessTime))
                    {
                        newSchedules[schedule.ProcessTime].AddTaskOrderSchedule(schedule);
                    }
                }
            }

            CRCal(ref timeLimitTasks, ref newSchedules, productCapacities, smartCapacityLists,
                deviceList, modelCounts, operatorList, operatorCounts, time, arrangeDevice);
            CRCal(ref notTimeLimitTasks, ref newSchedules, productCapacities, smartCapacityLists,
                deviceList, modelCounts, operatorList, operatorCounts, time, arrangeDevice);

            return newSchedules;
        }

        /// <summary>
        /// CR值计算
        /// </summary>
        /// <param name="tasks"></param>
        /// <param name="newSchedules"></param>
        /// <param name="productCapacities"></param>
        /// <param name="smartCapacityLists"></param>
        /// <param name="deviceList"></param>
        /// <param name="modelCounts"></param>
        /// <param name="operatorList"></param>
        /// <param name="operatorCounts"></param>
        /// <param name="time"></param>
        /// <param name="arrangeDevice"></param>
        private static void CRCal(
            ref List<SmartTaskOrderConfirm> tasks,
            ref Dictionary<DateTime, SmartTaskOrderScheduleDay> newSchedules,
            IEnumerable<SmartProductCapacityDetail> productCapacities,
            IEnumerable<SmartCapacityListDetail> smartCapacityLists,
            IEnumerable<SmartDevice> deviceList,
            IEnumerable<SmartDeviceModelCount> modelCounts,
            IEnumerable<SmartOperator> operatorList,
            IEnumerable<SmartOperatorCount> operatorCounts,
            DateTime time,
            bool arrangeDevice = false)
        {
            var count = tasks.Count();
            var t = time.Date;
            var doneCount = 0;
            while (doneCount < count)
            {
                foreach (var task in tasks)
                {
                    if (task.Left == 0)
                    {
                        continue;
                    }
                    var productId = task.ProductId;
                    var capacityId = task.CapacityId;
                    //产能配置
                    var capacityList = smartCapacityLists.Where(x => x.CapacityId == capacityId);
                    //计划号单日工序实际产能列表
                    var pCapacities = productCapacities.Where(x => x.ProductId == productId);
                    if (pCapacities.Any(x => x.DeviceNumber == 0 || x.OperatorNumber == 0))
                    {
                        continue;
                    }

                    for (var i = 0; i < task.Needs.Count; i++)
                    {
                        var need = task.Needs.ElementAt(i);
                        if (need.Have == 0 && need.LeftPut == 0)
                        {
                            continue;
                        }

                        if (need.LeftPut < 0)
                        {
                            continue;
                        }
                        var processId = need.ProcessId;
                        //工序单日产能配置
                        var cList = capacityList.FirstOrDefault(x => x.ProcessId == processId);
                        need.PId = cList.PId;
                        need.Process = cList.Process;
                        if (cList.CategoryId != 0)
                        {
                            var devices = cList.DeviceList;
                            foreach (var device in devices)
                            {
                                device.Count = modelCounts.FirstOrDefault(x => (int)x.ModelId == device.ModelId) != null
                                    ? (int)modelCounts.FirstOrDefault(x => (int)x.ModelId == device.ModelId).Count : 0;
                            }
                            cList.DNumber = devices.Sum(x => x.Total);
                        }
                        var operators = cList.OperatorList;
                        foreach (var op in operators)
                        {
                            op.Count = operatorCounts.FirstOrDefault(x => (int)x.ProcessId == need.PId && (int)x.LevelId == op.LevelId) != null
                                ? (int)operatorCounts.FirstOrDefault(x => (int)x.ProcessId == need.PId && (int)x.LevelId == op.LevelId).Count : 0;
                        }
                        cList.ONumber = operators.Sum(x => x.Total);
                        if (newSchedules[t].HaveProcessLeftCapacity(need.PId))
                        {
                            if (need.DoneTarget == 0 && need.EstimatedStartTime == default(DateTime))
                            {
                                need.EstimatedStartTime = t;
                                if (task.StartTime == default(DateTime))
                                {
                                    task.StartTime = t;
                                }
                            }
                            //计划号工序单日产能
                            var pCapacity = pCapacities.FirstOrDefault(x => x.ProcessId == processId);
                            //剩余产能指数
                            var left = newSchedules[t].ProcessLeftCapacityIndex(need.PId);
                            //根据安排设备还是产能计算
                            var capacity = arrangeDevice ? (cList.CategoryId == 0 ? cList.ONumber : cList.DNumber) : cList.ONumber;
                            //剩余产能
                            var number = (int)(capacity * left).ToRound(0);
                            //已有可加工数量 与 产能
                            var put = need.Have < number ? need.Have : number;
                            need.HavePut += put;
                            need.Have -= put;
                            var sc = new SmartTaskOrderScheduleDetail(t, task, cList, pCapacity)
                            {
                                MarkedDateTime = time,
                                Put = put,
                                Process = need.Process,
                                //Target = (int)Math.Floor(put * pCapacity.Rate / 100),
                                Target = (int)(put * pCapacity.Rate / 100).ToRound(0),
                                CapacityIndex = ((decimal)put / capacity).ToRound(4)
                            };
                            need.DoneTarget = sc.Target;
                            newSchedules[t].AddTaskOrderSchedule(sc);
                            var next = task.Needs.ElementAtOrDefault(i + 1);
                            if (next != null)
                            {
                                next.Have = sc.Target;
                                //next.Put = sc.Target;
                            }
                            else
                            {
                                task.DoneTarget += sc.Target;
                                task.EndTime = t;
                            }
                            //if (need.DoneTarget == need.Target)
                            //{
                            //    need.EstimatedCompleteTime = t;
                            //}
                            if (task.AllDone(need))
                            {
                                need.EstimatedCompleteTime = t;
                            }
                        }
                    }
                }

                //doneCount = tasks.Sum(x => x.AllDone() ? 1 : 0);
                doneCount = tasks.Count(x => x.AllDone());
                if (!newSchedules[t].HaveArranged())
                {
                    break;
                }

                t = t.AddDays(1);
            }
        }


        private static Dictionary<DateTime, SmartTaskOrderScheduleDay> AddDay(Dictionary<DateTime, SmartTaskOrderScheduleDay> w, DateTime t)
        {
            var maxTime = w.Any() ? w.Max(x => x.Key) : DateTime.Today;
            if (t >= maxTime)
            {
                var totalDays = (t - maxTime).TotalDays + 1;
                for (var i = 0; i < totalDays; i++)
                {
                    var tt = maxTime.AddDays(i);
                    if (!w.ContainsKey(tt))
                    {
                        w.Add(tt, new SmartTaskOrderScheduleDay(tt));
                    }
                }
            }

            return w.OrderBy(x => x.Key).ToDictionary(y => y.Key, y => y.Value);
        }
    }

    public class DeviceStateWeight : IWeightRandom
    {
        public DeviceStateWeight(SmartDeviceOperateState state, int weight)
        {
            State = state;
            Weight = weight;
        }
        public SmartDeviceOperateState State { get; }

        public int Weight { get; }
    }
    public class RateWeight : IWeightRandom
    {
        public RateWeight(int rate, int weight)
        {
            Rate = rate;
            Weight = weight;
        }
        public int Rate { get; }

        public int Weight { get; }
    }

    /// <summary>
    /// 加工安排
    /// </summary>
    public class ArrangeInfo
    {
        public ArrangeInfo(int item, int id, int count)
        {
            Item = item;
            Id = id;
            Count = count;
        }
        /// <summary>
        /// 设备型号 / 人员等级
        /// </summary>
        public int Item { get; set; }
        /// <summary>
        /// 设备id / 人员id
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// 设备次数 / 人员次数
        /// </summary>
        public int Count { get; set; }
    }
}