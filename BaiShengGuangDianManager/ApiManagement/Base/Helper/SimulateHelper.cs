using ApiManagement.Base.Server;
using ApiManagement.Models.SmartFactoryModel;
using ModelBase.Base.Utils;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ApiManagement.Models.AccountManagementModel;
using static ApiManagement.Models.SmartFactoryModel.ScheduleState;
using DateTime = System.DateTime;

namespace ApiManagement.Base.Helper
{
    /// <summary>
    /// 生产线模拟
    /// </summary>
    public class SimulateHelper
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

        private static string RedisPre = "Schedule";
        /// <summary>
        /// 最近一次排程时间
        /// </summary>
        private static string DateKey = $"{RedisPre}:Date";
        private static string DateLock = $"{RedisPre}:DateLock";
        private static string StateKey = $"{RedisPre}:State";
        private static string ProcessorKey = $"{RedisPre}:Processor";
        private static string ProcessorLockKey = $"{RedisPre}:ProcessorLock";
        //private static List<SmartProcessDevice> _devices;
        private static List<SmartProcessor> _processors = new List<SmartProcessor>();

        private static Timer _simulateTimer;

        public static void Init()
        {
            //            if (!RedisHelper.Exists(StateKey))
            //            {
            //                RedisHelper.SetForever(StateKey, 1);
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

        private static void NeedArrange(object obj)
        {
            if (RedisHelper.SetIfNotExist(DateLock, DateTime.Now.ToStrx()))
            {
                RedisHelper.SetExpireAt(DateLock, DateTime.Now.AddMinutes(5));
                var last = RedisHelper.Get<DateTime>(DateKey);
                var now = DateTime.Now;
                if (last == default(DateTime) || last < now.Date)
                {
                    var tasks = new List<SmartTaskOrderConfirm>();
                    var schedule = new List<SmartTaskOrderScheduleDetail>();
                    //ArrangeSchedule(ref tasks, ref schedule, true);
                    RedisHelper.SetForever(DateKey, now);
                }
            }
        }

        public static void InitDevice()
        {
            var devices = new List<SmartProcessDevice>();
            var deviceList = SmartDeviceHelper.Instance.GetAll<SmartDevice>();
            var categoryIds = deviceList.GroupBy(x => x.CategoryId).Select(y => y.Key);
            foreach (var categoryId in categoryIds)
            {
                devices.Clear();
                var deviceKey = GetDeviceKey(categoryId);
                var processDevices = RedisHelper.Get<List<SmartProcessDevice>>(deviceKey);
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

        public static void InitProcessor()
        {
            var users = AccountInfoHelper.Instance.GetAll<AccountInfo>();
            var processorList = RedisHelper.Get<List<SmartProcessor>>(ProcessorKey);
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

        private static void Do()
        {
            while (true)
            {
#if !DEBUG
                if (RedisHelper.Get<int>("StateKey") != 1)
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
                            var processDevices = RedisHelper.Get<List<SmartProcessDevice>>(deviceKey);
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

        private static string GetDeviceKey(int categoryId)
        {
            return $"{RedisPre}:Device{categoryId}";
        }

        private static string GetDeviceLockKey(int categoryId)
        {
            return $"{RedisPre}:DeviceLock{categoryId}";
        }

        private static bool LockDevice(int categoryId, string op)
        {
            var deviceKey = GetDeviceLockKey(categoryId);
            return RedisHelper.SetIfNotExist(deviceKey, $"{DateTime.Now.ToStrx()}:{op}");
        }
        private static void UnLockDevice(int categoryId)
        {
            var deviceKey = GetDeviceLockKey(categoryId);
            RedisHelper.Remove(deviceKey);
        }
        private static bool LockProcessor(string op)
        {
            return RedisHelper.SetIfNotExist(ProcessorLockKey, $"{DateTime.Now.ToStrx()}:{op}");
        }
        private static void UnLockProcessor()
        {
            RedisHelper.Remove(ProcessorLockKey);
        }

        public static IEnumerable<SmartProcessDevice> Devices()
        {
            var devices = new List<SmartProcessDevice>();
            var categories = SmartDeviceCategoryHelper.Instance.GetAll<SmartDeviceCategory>();
            var categoryIds = categories.GroupBy(x => x.Id).Select(y => y.Key);
            foreach (var categoryId in categoryIds)
            {
                var deviceKey = GetDeviceKey(categoryId);
                var processDevices = RedisHelper.Get<List<SmartProcessDevice>>(deviceKey);
                devices.AddRange(processDevices);
            }
            return devices;
        }

        /// <summary>
        /// 更新设备
        /// </summary>
        /// <returns></returns>
        private static void UpdateDevices(Dictionary<int, SmartProcessDevice> devices)
        {
            foreach (var device in devices)
            {
                var deviceKey = GetDeviceKey(device.Key);
                RedisHelper.SetForever(deviceKey, device.Value);
            }
        }

        /// <summary>
        /// 更新设备
        /// </summary>
        /// <returns></returns>
        private static void UpdateDevices(int categoryId, IEnumerable<SmartProcessDevice> devices)
        {
            var deviceKey = GetDeviceKey(categoryId);
            RedisHelper.SetForever(deviceKey, devices);
        }

        /// <summary>
        /// 更新加工人
        /// </summary>
        /// <returns></returns>
        private static void UpdateProcessors(IEnumerable<SmartProcessor> processors)
        {
            RedisHelper.SetForever(ProcessorKey, processors);
        }
        /// <summary>
        ///  获取设备状况
        /// </summary>
        /// <param name="categoryId">设备类型id</param>
        /// <param name="deviceId">设备id</param>
        /// <returns></returns>
        public static SmartDeviceOperateState GetDeviceState(int categoryId, int deviceId)
        {
            var deviceKey = GetDeviceKey(categoryId);
            var devices = RedisHelper.Get<List<SmartProcessDevice>>(deviceKey);
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
        //    var processors = RedisHelper.Get<List<SmartProcessor>>(ProcessorKey);
        //    return processors.FirstOrDefault(x => x.Id == processorId);
        //}

        /// <summary>
        /// 安排加工人
        /// </summary>
        /// <returns></returns>
        private static ScheduleState ArrangeProcess(string op, out int processorId)
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

        //        var processors = RedisHelper.Get<List<SmartProcessor>>(ProcessorKey);
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
        private static void ReleaseProcess(int processorId)
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

        //        var processors = RedisHelper.Get<List<SmartProcessor>>(ProcessorKey);
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
        private static void ReleaseProcess(IEnumerable<int> processorIds)
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

        //        var processors = RedisHelper.Get<List<SmartProcessor>>(ProcessorKey);
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
        private static ScheduleState Arrange(int deviceCategoryId, int flowCardProcessId, int processNumber, int processCount, int totalSecond, out int deviceId, ref int processorId)
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
                    var devices = RedisHelper.Get<List<SmartProcessDevice>>(deviceKey);
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

        //        var processors = RedisHelper.Get<List<SmartProcessor>>(ProcessorKey);
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
        //            var devices = RedisHelper.Get<List<SmartProcessDevice>>(deviceKey);
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
        private static void UpdateDeviceState(int deviceId, int deviceCategoryId, SmartDeviceOperateState state)
        {
            while (LockDevice(deviceCategoryId, $"更新设备状态-{state}"))
            {
                var deviceKey = GetDeviceKey(deviceCategoryId);
                var devices = RedisHelper.Get<List<SmartProcessDevice>>(deviceKey);
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
        //    _devices = RedisHelper.Get<List<SmartDeviceProcess>>(DeviceKey);
        //    var 闲置设备 = _devices.Where(x=>x.State == SmartDeviceState.未加工)
        //    return results.FirstOrDefault() ?? "";
        //}

        private static void Simulate()
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
                            var 合格率 = (RateWeight)RandomSeed.GetWeightRandom(SimulateHelper.合格率);
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
                                SmartFlowCardProcessHelper.UpdateSmartFlowCardProcessNextBefore(processDevice.FlowCardId, processDevice.Id, processDevice.Qualified);
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
                            var processor = AccountInfoHelper.GetAccountInfo(processDevice.ProcessorId)?.Account ?? "";
                            //todo
                            var log = new SmartFlowCardProcessLog(0, processor, markedDateTime, processDevice, qualified, unqualified);
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
            SmartFlowCardProcessHelper.UpdateSmartFlowCardProcessArrange(processDevices.Where(x => categroy_0.Contains(x.Id)));
            SmartFlowCardProcessHelper.Instance.Update(processDevices.Where(x => categroy0.Contains(x.Id)));
            SmartProcessFaultHelper.Instance.Add<SmartProcessFault>(faults);
            WorkFlowHelper.Instance.OnSmartFlowCardProcessChanged(processDevices.Where(x => categroy0.Contains(x.Id)));
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
}
