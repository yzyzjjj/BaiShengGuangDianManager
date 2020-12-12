using ApiManagement.Base.Server;
using ApiManagement.Models.SmartFactoryModel;
using ModelBase.Base.Utils;
using Newtonsoft.Json;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static ApiManagement.Models.SmartFactoryModel.ScheduleState;
using DateTime = System.DateTime;

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
        /// <param name="schedule">最终安排结果</param>
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
                var today = DateTime.Today;
                var waitTasks = new List<SmartTaskOrderConfirm>();
                var allTasks = SmartTaskOrderHelper.Instance.GetArrangedButNotDoneSmartTaskOrders();
                tasks = tasks.Where(x => allTasks.All(y => y.Id != x.Id));
                var arrangeTasks = allTasks.Where(x => tasks.All(y => y.Id != x.Id))
                    .Select(ClassExtension.ParentCopyToChild<SmartTaskOrder, SmartTaskOrderConfirm>);
                if (arrangeTasks.Any())
                {
                    var needs =
                        SmartTaskOrderNeedHelper.Instance.GetSmartTaskOrderNeedsByTaskOrderIds(
                            arrangeTasks.Select(x => x.Id));
                    foreach (var task in arrangeTasks)
                    {
                        task.SetEndTime = task.EndTime;
                        var tNeeds = needs.Where(x => x.TaskOrderId == task.Id);
                        if (tNeeds.Any())
                        {
                            var tBatch = tNeeds.Max(x => x.Batch);
                            if (tBatch != 0)
                            {
                                task.Needs.AddRange(tNeeds.Where(x => x.Batch == tBatch).Select(ClassExtension.ParentCopyToChild<SmartTaskOrderNeed, SmartTaskOrderSchedule>).Select(y => (SmartTaskOrderSchedule)y.Clone()));
                                waitTasks.Add(task);
                            }
                        }
                    }
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
                            task.StartTime = task.StartTime;
                            task.EndTime = task.EndTime;
                            task.SetEndTime = task.EndTime;
                            task.LevelId = tTask.LevelId;
                            task.TaskOrder = tTask.TaskOrder;
                        }
                    }
                    waitTasks.AddRange(tasks);
                }

                if (!waitTasks.Any())
                {
                    return costDays;
                }
                //所有工序
                var processes = SmartProcessHelper.Instance.GetAll<SmartProcess>();
                var productIds = waitTasks.Select(x => x.ProductId);
                // 任务单计划号
                var products = SmartProductHelper.Instance.GetByIds<SmartProduct>(productIds);
                // 计划号产能
                var productCapacities = SmartProductCapacityHelper.Instance.GetAllSmartProductCapacities(productIds);
                var capacityIds = products.Select(x => x.CapacityId);
                // 产能设置
                var smartCapacityLists = SmartCapacityListHelper.Instance.GetAllSmartCapacityListsWithOrder(capacityIds);
                //设备型号数量
                var deviceList = SmartDeviceHelper.Instance.GetNormalSmartDevices();
                var modelCount = deviceList.GroupBy(x => x.ModelId).Select(y => new SmartDeviceModelCount
                {
                    ModelId = y.Key,
                    Count = y.Count()
                });
                //人员等级数量
                var operatorList = SmartOperatorHelper.Instance.GetNormalSmartOperators();
                var operatorCount = operatorList.GroupBy(x => new { x.ProcessId, x.LevelId }).Select(y => new SmartOperatorCount
                {
                    ProcessId = y.Key.ProcessId,
                    LevelId = y.Key.LevelId,
                    Count = y.Count()
                });
                var batch = SmartTaskOrderScheduleHelper.Instance.GetSmartTaskOrderScheduleBatch();
                taskIds = waitTasks.Select(x => x.Id);
                //工序已安排数量
                var schedules = SmartTaskOrderScheduleHelper.Instance.GetSmartTaskOrderScheduleByTaskOrderIds(taskIds);

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
                    task.CapacityCostDay = product.Number != 0 ? (int)Math.Ceiling((decimal)task.Left / product.Number) : 0;
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
                        var process = processes.FirstOrDefault(x => x.Id == need.PId);
                        if (process != null)
                        {
                            need.Process = process.Process ?? "";
                            need.Order = process.Order;
                        }
                        var pre = task.Needs.FirstOrDefault(x => x.ProcessId == preProcessId);
                        if (pre != null)
                        {
                            need.Have = pre.Stock;
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
                                    smartCapacityLists, deviceList, modelCount, operatorList, operatorCount, today, j == 0);
                                break;
                            //最早交货期
                            case 1:
                                sc = EarliestDeliveryDate(tasks, ref cnWaitTasks[j][i], schedules, productCapacities,
                                    smartCapacityLists, deviceList, modelCount, operatorList, operatorCount, today, j == 0);
                                break;
                            //按照工期和交货期之间的距离
                            case 2:
                                sc = WorkingPeriodAndDeliveryDate(tasks, ref cnWaitTasks[j][i], schedules, productCapacities,
                                    smartCapacityLists, deviceList, modelCount, operatorList, operatorCount, today, j == 0);
                                break;
                            //CR值
                            case 3:
                                sc = CR(tasks, ref cnWaitTasks[j][i], schedules, productCapacities,
                                    smartCapacityLists, deviceList, modelCount, operatorList, operatorCount, today, j == 0);
                                break;
                            default:
                                f = false;
                                break;
                        }

                        var t = today;
                        var last = sc.Values.LastOrDefault(x => x.HaveArranged());
                        if (last != null)
                        {
                            t = last.ProcessTime.AddDays(1);
                        }
                        cnSchedules[j][i] = sc.Where(x => x.Key < t).ToDictionary(y => y.Key, y => y.Value);
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
                        var dEstimatedStartTime = default(DateTime);
                        var dEstimatedCompleteTime = default(DateTime);
                        var oEstimatedStartTime = default(DateTime);
                        var oEstimatedCompleteTime = default(DateTime);
                        var dC = new SmartTaskOrderScheduleCostDays
                        {
                            Id = task.Id,
                            TaskOrder = task.TaskOrder,
                            ProductId = task.ProductId,
                            Product = task.Product,
                            MustCompleteTime = task.SetEndTime,
                        };
                        var oC = new SmartTaskOrderScheduleCostDays
                        {
                            Id = task.Id,
                            TaskOrder = task.TaskOrder,
                            ProductId = task.ProductId,
                            Product = task.Product,
                            MustCompleteTime = task.EndTime,
                        };
                        if (dCost == null || !dCost.Needs.Any() || oCost == null || !oCost.Needs.Any())
                        {
                            continue;
                        }
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
                                dEstimatedStartTime = dEstimatedStartTime == default(DateTime) ? estimatedStartTime :
                                    (estimatedStartTime < dEstimatedStartTime ? estimatedStartTime : dEstimatedStartTime);
                            }

                            //设备结束时间
                            var estimatedCompleteTime =
                                dCost.Needs.FirstOrDefault(x => x.ProcessId == need.ProcessId)?.EstimatedCompleteTime ??
                                DateTime.Today;

                            if (estimatedCompleteTime != default(DateTime))
                            {
                                dEstimatedCompleteTime = dEstimatedCompleteTime == default(DateTime) ? estimatedCompleteTime :
                                    (estimatedCompleteTime < dEstimatedCompleteTime ? estimatedCompleteTime : dEstimatedCompleteTime);
                            }

                            //人员开始时间
                            estimatedStartTime =
                                oCost.Needs.FirstOrDefault(x => x.ProcessId == need.ProcessId)?.EstimatedStartTime ??
                                DateTime.Today;
                            if (estimatedStartTime != default(DateTime))
                            {
                                oEstimatedStartTime = oEstimatedStartTime == default(DateTime) ? estimatedStartTime :
                                    (estimatedStartTime < oEstimatedStartTime ? estimatedStartTime : oEstimatedStartTime);
                            }

                            //人员结束时间
                            estimatedCompleteTime =
                                oCost.Needs.FirstOrDefault(x => x.ProcessId == need.ProcessId)?.EstimatedCompleteTime ??
                               DateTime.Today;

                            if (estimatedCompleteTime != default(DateTime))
                            {
                                oEstimatedCompleteTime = oEstimatedCompleteTime == default(DateTime) ? estimatedCompleteTime :
                                    (estimatedCompleteTime < oEstimatedCompleteTime ? estimatedCompleteTime : oEstimatedCompleteTime);
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

                    cnCostList[0][i] = cnCostList[0][i].OrderBy(x => x.EstimatedStartTime)
                        .ThenBy(x => x.EstimatedCompleteTime).ToList();
                    cnCostList[1][i] = cnCostList[0][i].OrderBy(x => x.EstimatedStartTime)
                        .ThenBy(x => x.EstimatedCompleteTime).ToList();
                }

                BestArrange(way, total, cnScore, waitTasks, cnCostList, cnSchedules, out costDays, out var best, out var jj, out var ii);
                foreach (var task in waitTasks)
                {
                    var arrangeTask = cnWaitTasks[ii][jj].FirstOrDefault(x => x.Id == task.Id);
                    if (arrangeTask != null)
                    {
                        task.StartTime = arrangeTask.Needs.Min(x => x.EstimatedStartTime);
                        task.EndTime = arrangeTask.Needs.Max(x => x.EstimatedCompleteTime);
                    }
                }

                schedule.AddRange(best.Values.OrderBy(x => x.ProcessTime).SelectMany(y => y.Needs).SelectMany(z => z.Value));
                //schedule.AddRange(best.Values.Where(v => v.ProcessTime >= today).OrderBy(x => x.ProcessTime).SelectMany(y => y.Needs).SelectMany(z => z.Value));
                if (jj != -1)
                {
                    if (isArrange)
                    {
                        taskIds = waitTasks.Select(x => x.Id);
                        var oldNeeds = SmartTaskOrderNeedHelper.Instance.GetSmartTaskOrderNeedsByTaskOrderIds(taskIds);
                        var newNeeds = cnWaitTasks[ii][jj].SelectMany(x => x.Needs);
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

                        SmartTaskOrderScheduleHelper.Instance.Add(schedule.Select(x =>
                        {
                            x.Batch = batch + 1;
                            x.CreateUserId = createUserId;
                            x.MarkedDateTime = markedDateTime;
                            return x;
                        }));
                        SmartTaskOrderNeedHelper.Instance.Add(newNeeds);
                        var indexes = best.Values.OrderBy(x => x.ProcessTime).SelectMany(y => y.CapacityIndexList.Select(z =>
                        {
                            z.Batch = batch + 1;
                            return z;
                        }));
                        SmartTaskOrderScheduleIndexHelper.Instance.Add(indexes.Select(x =>
                        {
                            x.Batch = batch + 1;
                            x.CreateUserId = createUserId;
                            x.MarkedDateTime = markedDateTime;
                            return x;
                        }));
                        SmartTaskOrderHelper.Instance.Arrange(waitTasks.Select(x =>
                        {
                            x.MarkedDateTime = markedDateTime;
                            return x;
                        }));
                        //排程是否改变
                        var haveChange = false;
                        var arrange = new List<int>();


                        if (haveChange)
                        {
                            WorkFlowHelper.Instance.OnTaskOrderArrangeChanged(arrange);
                        }
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
        /// <param name="list"></param>
        /// <param name="schedules"></param>
        private static void BestArrange(int way, int total,
            int[][] cnScore,
            List<SmartTaskOrderConfirm> waitTasks,
            List<SmartTaskOrderScheduleCostDays>[][] cnCostList,
            Dictionary<DateTime, SmartTaskOrderScheduleDay>[][] cnSchedules,
            out List<SmartTaskOrderScheduleCostDays> list,
            out Dictionary<DateTime, SmartTaskOrderScheduleDay> schedules,
            out int jj,
            out int ii)
        {
            list = new List<SmartTaskOrderScheduleCostDays>();
            schedules = new Dictionary<DateTime, SmartTaskOrderScheduleDay>();
            jj = -1;
            ii = -1;
            if (way != cnScore.Length)
            {
                return;
            }

            List<SmartTaskOrderScheduleCostDays> l = null;
            var b = 0;
            List<SmartTaskOrderScheduleCostDays> l1 = null;
            var j1 = -1;
            var b1 = 0;
            for (var i = 0; i < total; i++)
            {
                var dScore = cnScore[0];
                var oScore = cnScore[1];
                b = dScore[i] <= oScore[i] ? 0 : 1;
                l = cnCostList[b][i];
                if (!l.Any())
                {
                    if (i == 1)
                    {
                        jj = j1;
                        ii = b1;
                        list = cnCostList[b1][j1];
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
                        jj = i;
                        ii = b;
                        list = cnCostList[b][i];
                        schedules = cnSchedules[b][i];
                    }
                    else
                    {
                        jj = j1;
                        ii = b1;
                        list = cnCostList[b1][j1];
                        schedules = cnSchedules[b1][j1];
                    }
                }

                j1++;
                b1 = dScore[i] <= oScore[i] ? 0 : 1;
                l1 = cnCostList[b1][j1];
            }
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
            IEnumerable<SmartOperatorDetail> operatorList,
            IEnumerable<SmartOperatorCount> operatorCounts,
            DateTime time,
            bool arrangeDevice)
        {
            //S级任务  排产不可修改
            var superTasks = allTasks.Where(x => x.LevelId == 1).OrderBy(x => x.EndTime).ThenBy(x => x.Id);
            //非S级任务  排产可修改
            var normalTasks = allTasks.Where(x => x.LevelId != 1).OrderBy(y => y.Order);
            //有时间要求的任务 先按生产天数从小到大排，再按截止时间从小到大排
            var timeLimitTasks = normalTasks.Where(x => x.EndTime != default(DateTime)).OrderBy(t => t.Order).ThenBy(y => y.CapacityCostDay).ThenBy(y => y.EndTime).ThenBy(y => y.StartTime).ToList();
            //没有时间要求的任务 先按生产天数从小到大排，再按目标量从小到大排
            var notTimeLimitTasks = normalTasks.Where(x => x.EndTime == default(DateTime)).OrderBy(t => t.Order).ThenBy(y => y.CapacityCostDay).ThenBy(y => y.StartTime).ThenBy(y => y.Target).ToList();
            var newSchedules = new Dictionary<DateTime, SmartTaskOrderScheduleDay>();
            var minTime = time;
            var maxTime = time.AddDays(Day);
            var setEndTime = timeLimitTasks.Where(x => x.EndTime != default(DateTime));
            //最小时间
            minTime = !setEndTime.Any() ? minTime : setEndTime.Min(x => x.StartTime) < minTime ? setEndTime.Min(x => x.EndTime) : minTime;
            minTime = !schedules.Any() ? minTime : schedules.Min(x => x.ProcessTime) < minTime ? schedules.Min(x => x.ProcessTime) : minTime;
            //最大时间
            maxTime = !setEndTime.Any() ? maxTime : setEndTime.Max(x => x.EndTime) > maxTime ? setEndTime.Max(x => x.EndTime) : maxTime;
            maxTime = !schedules.Any() ? maxTime : schedules.Max(x => x.ProcessTime) > maxTime ? schedules.Max(x => x.ProcessTime) : maxTime;

            AddDay(ref newSchedules, minTime, maxTime, deviceList, operatorList);
            InitData(ref newSchedules, superTasks, schedules, productCapacities, smartCapacityLists, deviceList, operatorList);

            ScheduleArrangeCal(ref timeLimitTasks, ref newSchedules, productCapacities, smartCapacityLists,
                deviceList, modelCounts, operatorList, operatorCounts, time, arrangeDevice);
            ScheduleArrangeCal(ref notTimeLimitTasks, ref newSchedules, productCapacities, smartCapacityLists,
                deviceList, modelCounts, operatorList, operatorCounts, time, arrangeDevice);

            return newSchedules;
        }

        /// <summary>
        /// 最早交货期  截止日期
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
            IEnumerable<SmartOperatorDetail> operatorList,
            IEnumerable<SmartOperatorCount> operatorCounts,
            DateTime time,
            bool arrangeDevice = false)
        {
            //S级任务  排产不可修改
            var superTasks = allTasks.Where(x => x.LevelId == 1).OrderBy(x => x.EndTime).ThenBy(x => x.Id);
            //非S级任务  排产可修改
            var normalTasks = allTasks.Where(x => x.LevelId != 1).OrderBy(y => y.Order);
            //有时间要求的任务 按交货期/截止时间从早到晚排
            var timeLimitTasks = normalTasks.Where(x => x.EndTime != default(DateTime)).OrderBy(t => t.Order).ThenBy(y => y.EndTime).ThenBy(y => y.StartTime).ToList();
            //没有时间要求的任务 按目标量从小到大排
            var notTimeLimitTasks = normalTasks.Where(x => x.EndTime == default(DateTime)).OrderBy(t => t.Order).ThenBy(y => y.StartTime).ThenBy(y => y.Target).ToList();
            var newSchedules = new Dictionary<DateTime, SmartTaskOrderScheduleDay>();
            var minTime = time;
            var maxTime = time.AddDays(Day);
            var setEndTime = timeLimitTasks.Where(x => x.EndTime != default(DateTime));
            //最小时间
            minTime = !setEndTime.Any() ? minTime : setEndTime.Min(x => x.StartTime) < minTime ? setEndTime.Min(x => x.EndTime) : minTime;
            minTime = !schedules.Any() ? minTime : schedules.Min(x => x.ProcessTime) < minTime ? schedules.Min(x => x.ProcessTime) : minTime;
            //最大时间
            maxTime = !setEndTime.Any() ? maxTime : setEndTime.Max(x => x.EndTime) > maxTime ? setEndTime.Max(x => x.EndTime) : maxTime;
            maxTime = !schedules.Any() ? maxTime : schedules.Max(x => x.ProcessTime) > maxTime ? schedules.Max(x => x.ProcessTime) : maxTime;

            AddDay(ref newSchedules, minTime, maxTime, deviceList, operatorList);
            InitData(ref newSchedules, superTasks, schedules, productCapacities, smartCapacityLists, deviceList, operatorList);

            ScheduleArrangeCal(ref timeLimitTasks, ref newSchedules, productCapacities, smartCapacityLists,
                deviceList, modelCounts, operatorList, operatorCounts, time, arrangeDevice);
            ScheduleArrangeCal(ref notTimeLimitTasks, ref newSchedules, productCapacities, smartCapacityLists,
                deviceList, modelCounts, operatorList, operatorCounts, time, arrangeDevice);

            return newSchedules;
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
            IEnumerable<SmartOperatorDetail> operatorList,
            IEnumerable<SmartOperatorCount> operatorCounts,
            DateTime time,
            bool arrangeDevice = false)
        {
            //S级任务  排产不可修改
            var superTasks = allTasks.Where(x => x.LevelId == 1).OrderBy(x => x.EndTime).ThenBy(x => x.Id);
            //非S级任务  排产可修改
            var normalTasks = allTasks.Where(x => x.LevelId != 1).OrderBy(y => y.Order);
            //有时间要求的任务 先按期和交货期之间的距离从小到大排，再按截止时间从小到大排
            var timeLimitTasks = normalTasks.Where(x => x.EndTime != default(DateTime)).OrderBy(t => t.Order).ThenBy(y => y.DistanceDay).ThenBy(y => y.EndTime).ThenBy(y => y.StartTime).ToList();
            //没有时间要求的任务 按目标量从小到大排
            var notTimeLimitTasks = normalTasks.Where(x => x.EndTime == default(DateTime)).OrderBy(t => t.Order).ThenBy(y => y.StartTime).ThenBy(y => y.Target).ToList();
            var newSchedules = new Dictionary<DateTime, SmartTaskOrderScheduleDay>();
            var minTime = time;
            var maxTime = time.AddDays(Day);
            var setEndTime = timeLimitTasks.Where(x => x.EndTime != default(DateTime));
            //最小时间
            minTime = !setEndTime.Any() ? minTime : setEndTime.Min(x => x.StartTime) < minTime ? setEndTime.Min(x => x.EndTime) : minTime;
            minTime = !schedules.Any() ? minTime : schedules.Min(x => x.ProcessTime) < minTime ? schedules.Min(x => x.ProcessTime) : minTime;
            //最大时间
            maxTime = !setEndTime.Any() ? maxTime : setEndTime.Max(x => x.EndTime) > maxTime ? setEndTime.Max(x => x.EndTime) : maxTime;
            maxTime = !schedules.Any() ? maxTime : schedules.Max(x => x.ProcessTime) > maxTime ? schedules.Max(x => x.ProcessTime) : maxTime;

            AddDay(ref newSchedules, minTime, maxTime, deviceList, operatorList);
            InitData(ref newSchedules, superTasks, schedules, productCapacities, smartCapacityLists, deviceList, operatorList);

            ScheduleArrangeCal(ref timeLimitTasks, ref newSchedules, productCapacities, smartCapacityLists,
                deviceList, modelCounts, operatorList, operatorCounts, time, arrangeDevice);
            ScheduleArrangeCal(ref notTimeLimitTasks, ref newSchedules, productCapacities, smartCapacityLists,
                deviceList, modelCounts, operatorList, operatorCounts, time, arrangeDevice);

            return newSchedules;
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
            IEnumerable<SmartOperatorDetail> operatorList,
            IEnumerable<SmartOperatorCount> operatorCounts,
            DateTime time,
            bool arrangeDevice = false)
        {
            //S级任务  排产不可修改
            var superTasks = allTasks.Where(x => x.LevelId == 1).OrderBy(x => x.EndTime).ThenBy(x => x.Id);
            //非S级任务  排产可修改
            var normalTasks = allTasks.Where(x => x.LevelId != 1).OrderBy(y => y.Order);
            //有时间要求的任务 先按生产天数从小到大排，再按截止时间从小到大排
            var timeLimitTasks = normalTasks.Where(x => x.EndTime != default(DateTime)).OrderBy(t => t.Order).ThenBy(y => y.CR).ThenBy(y => y.EndTime).ThenBy(y => y.StartTime).ToList();
            //没有时间要求的任务 先按生产天数从小到大排，再按目标量从小到大排
            var notTimeLimitTasks = normalTasks.Where(x => x.EndTime == default(DateTime)).OrderBy(t => t.Order).ThenBy(y => y.StartTime).ThenBy(y => y.Target).ToList();
            var newSchedules = new Dictionary<DateTime, SmartTaskOrderScheduleDay>();
            var minTime = time;
            var maxTime = time.AddDays(Day);
            var setEndTime = timeLimitTasks.Where(x => x.EndTime != default(DateTime));
            //最小时间
            minTime = !setEndTime.Any() ? minTime : setEndTime.Min(x => x.StartTime) < minTime ? setEndTime.Min(x => x.EndTime) : minTime;
            minTime = !schedules.Any() ? minTime : schedules.Min(x => x.ProcessTime) < minTime ? schedules.Min(x => x.ProcessTime) : minTime;
            //最大时间
            maxTime = !setEndTime.Any() ? maxTime : setEndTime.Max(x => x.EndTime) > maxTime ? setEndTime.Max(x => x.EndTime) : maxTime;
            maxTime = !schedules.Any() ? maxTime : schedules.Max(x => x.ProcessTime) > maxTime ? schedules.Max(x => x.ProcessTime) : maxTime;

            AddDay(ref newSchedules, minTime, maxTime, deviceList, operatorList);
            InitData(ref newSchedules, superTasks, schedules, productCapacities, smartCapacityLists, deviceList, operatorList);

            ScheduleArrangeCal(ref timeLimitTasks, ref newSchedules, productCapacities, smartCapacityLists,
                deviceList, modelCounts, operatorList, operatorCounts, time, arrangeDevice);
            ScheduleArrangeCal(ref notTimeLimitTasks, ref newSchedules, productCapacities, smartCapacityLists,
                deviceList, modelCounts, operatorList, operatorCounts, time, arrangeDevice);

            return newSchedules;
        }

        /// <summary>
        /// 排程计算
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
        /// <param name="critical">是否紧急</param>
        private static void ScheduleArrangeCal(
            ref List<SmartTaskOrderConfirm> tasks,
            ref Dictionary<DateTime, SmartTaskOrderScheduleDay> newSchedules,
            IEnumerable<SmartProductCapacityDetail> productCapacities,
            IEnumerable<SmartCapacityListDetail> smartCapacityLists,
            IEnumerable<SmartDevice> deviceList,
            IEnumerable<SmartDeviceModelCount> modelCounts,
            IEnumerable<SmartOperatorDetail> operatorList,
            IEnumerable<SmartOperatorCount> operatorCounts,
            DateTime time,
            bool arrangeDevice,
            bool critical = false)
        {
            var count = tasks.Count;
            var t = time.Date;
            var doneCount = 0;
            while (doneCount < count)
            {
                foreach (var task in tasks)
                {
                    if (task.StartTime != default(DateTime) && task.StartTime > t)
                    {
                        continue;
                    }

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

                    decimal waitIndex = 0;
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

                        if (need.Have <= 0)
                        {
                            //当前没东西可加工
                            continue;
                        }
                        var processId = need.ProcessId;
                        //工序单日产能配置
                        var cList = capacityList.FirstOrDefault(x => x.ProcessId == processId);
                        need.PId = cList.PId;
                        need.Process = cList.Process;
                        //计划号工序单日产能
                        var pCapacity = pCapacities.FirstOrDefault(x => x.ProcessId == processId);
                        //安排设备或人员列表
                        var arrangeList = new List<ArrangeInfo>();
                        var tArrangeDevice = arrangeDevice;
                        //用设备产能计算但是不支持设备加工
                        if (arrangeDevice && cList.CategoryId == 0)
                        {
                            tArrangeDevice = false;
                        }
                        //根据arrangeDevice判断用设备产能计算还是人员产能计算， 以安排的前道工序同时生产一次产能总和来排产，等待中的产能损耗额外安排
                        ArrangeDeviceOrOperator(task, t, need.Have, out var capacity, ref waitIndex, ref arrangeList, ref newSchedules,
                            cList, pCapacity, deviceList, modelCounts, operatorList, operatorCounts, tArrangeDevice);
                        //本次剩余产能
                        if (capacity == 0)
                        {
                            //没产能
                            continue;
                        }

                        if (need.DoneTarget == 0 && need.EstimatedStartTime == default(DateTime))
                        {
                            need.EstimatedStartTime = t;
                            if (task.StartTime == default(DateTime))
                            {
                                task.StartTime = t;
                            }
                        }
                        //已有可加工数量 与 产能
                        var put = need.Have < capacity ? need.Have : capacity;
                        need.HavePut += put;
                        need.Have -= put;
                        var sc = new SmartTaskOrderScheduleDetail(t, task, cList, pCapacity)
                        {
                            IsDevice = tArrangeDevice ? 0 : 1,
                            MarkedDateTime = time,
                            Put = put,
                            Process = need.Process,
                            //Target = (int)Math.Floor(put * pCapacity.Rate / 100),
                            Target = (int)(put * pCapacity.Rate / 100).ToRound(0),
                            CapacityIndex = ((decimal)put / capacity).ToRound(4)
                        };
                        need.DoneTarget = sc.Target;
                        newSchedules[t].AddTaskOrderSchedule(sc, arrangeList);
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

                doneCount = tasks.Count(x => x.AllDone());
                if (!newSchedules[t].HaveArranged())
                {
                    break;
                }

                t = t.AddDays(1);
            }
        }

        /// <summary>
        /// 数据初始化
        /// </summary>
        /// <param name="schedules"></param>
        /// <param name="minTime">加工时间</param>
        /// <param name="maxTime">加工时间</param>
        /// <param name="deviceList"></param>
        /// <param name="operatorList"></param>
        /// <returns></returns>
        private static void AddDay(
            ref Dictionary<DateTime, SmartTaskOrderScheduleDay> schedules,
            DateTime minTime,
            DateTime maxTime,
            IEnumerable<SmartDevice> deviceList,
            IEnumerable<SmartOperatorDetail> operatorList)
        {
            if (maxTime >= minTime)
            {
                var totalDays = (maxTime - minTime).TotalDays + 1;
                for (var i = 0; i < totalDays; i++)
                {
                    var tt = minTime.AddDays(i);
                    if (!schedules.ContainsKey(tt))
                    {
                        var sc = new SmartTaskOrderScheduleDay(tt);
                        sc.Init(deviceList, operatorList);
                        schedules.Add(tt, sc);
                    }
                }
            }

            schedules = schedules.OrderBy(x => x.Key).ToDictionary(y => y.Key, y => y.Value);
        }

        /// <summary>
        /// 数据初始化
        /// </summary>
        /// <param name="newSchedules"></param>
        /// <param name="superTasks"></param>
        /// <param name="schedules"></param>
        /// <param name="productCapacities"></param>
        /// <param name="smartCapacityLists"></param>
        /// <param name="deviceList"></param>
        /// <param name="operatorList"></param>
        private static void InitData(
            ref Dictionary<DateTime, SmartTaskOrderScheduleDay> newSchedules,
            IEnumerable<SmartTaskOrderConfirm> superTasks,
            IEnumerable<SmartTaskOrderScheduleDetail> schedules,
            IEnumerable<SmartProductCapacityDetail> productCapacities,
            IEnumerable<SmartCapacityListDetail> smartCapacityLists,
            IEnumerable<SmartDevice> deviceList,
            IEnumerable<SmartOperatorDetail> operatorList)
        {
            foreach (var schedule in schedules)
            {
                var processTime = schedule.ProcessTime;
                var taskOrderId = schedule.TaskOrderId;
                var processId = schedule.ProcessId;
                var productId = schedule.ProductId;
                var capacityId = productCapacities.FirstOrDefault(x => x.ProductId == productId && x.ProcessId == processId)?.CapacityId ?? 0;
                var capacityList = smartCapacityLists.FirstOrDefault(x => x.CapacityId == capacityId && x.ProcessId == processId);
                var pId = schedule.PId;
                schedule.CategoryId = capacityList.CategoryId;
                schedule.Order = capacityList.Order;
                //S级任务且已排产
                var task = superTasks.FirstOrDefault(x => x.Id == schedule.TaskOrderId);
                if (task != null && task.Arranged)
                {
                    if (!newSchedules.ContainsKey(processTime))
                    {
                        var sc = new SmartTaskOrderScheduleDay(processTime);
                        sc.Init(deviceList, operatorList);
                        newSchedules.Add(processTime, sc);
                    }

                    var arrangeInfos = new List<ArrangeInfo>();
                    if (schedule.Device)
                    {
                        foreach (var (id, count) in schedule.DeviceList)
                        {
                            var device = deviceList.FirstOrDefault(x => x.Id == id);
                            if (device != null)
                            {
                                var cList = capacityList.DeviceList.FirstOrDefault(x => x.ModelId == device.ModelId);
                                if (cList == null)
                                {
                                    continue;
                                }

                                var single = cList.Single;
                                var singleCount = cList.SingleCount;

                                if (single == 0 || singleCount == 0)
                                {
                                    continue;
                                }

                                var first = arrangeInfos.FirstOrDefault(x => x.Id == device.Id);
                                if (first == null)
                                {
                                    arrangeInfos.Add(new ArrangeInfo(device.ModelId, device.Id));
                                    first = arrangeInfos.FirstOrDefault(x => x.Id == device.Id);
                                }
                                var productArrange = first.Arranges.FirstOrDefault(x => x.ProductId == productId && x.PId == pId);
                                if (productArrange != null)
                                {
                                    productArrange.Count += count;
                                }
                                else
                                {
                                    first.Arranges.Add(new ArrangeDetail(taskOrderId, productId, pId, single, count, singleCount));
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var (id, count) in schedule.OperatorsList)
                        {
                            var op = operatorList.FirstOrDefault(x => x.Id == id);
                            if (op != null)
                            {
                                var cList = capacityList.OperatorList.FirstOrDefault(x => x.LevelId == op.LevelId);
                                if (cList == null)
                                {
                                    continue;
                                }

                                var single = cList.Single;
                                var singleCount = cList.SingleCount;

                                if (single == 0 || singleCount == 0)
                                {
                                    continue;
                                }

                                var first = arrangeInfos.FirstOrDefault(x => x.Id == op.Id);
                                if (first == null)
                                {
                                    arrangeInfos.Add(new ArrangeInfo(op.LevelId, op.Id));
                                    first = arrangeInfos.FirstOrDefault(x => x.Id == op.Id);
                                }
                                var productArrange = first.Arranges.FirstOrDefault(x => x.ProductId == productId && x.PId == pId);
                                if (productArrange != null)
                                {
                                    productArrange.Count += count;
                                }
                                else
                                {
                                    first.Arranges.Add(new ArrangeDetail(taskOrderId, productId, pId, single, count, singleCount));
                                }
                            }
                        }
                    }
                    newSchedules[processTime].AddTaskOrderSchedule(schedule, arrangeInfos);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="task">加工任务单</param>
        /// <param name="processTime">加工日期</param>
        /// <param name="number"></param>
        /// <param name="capacity"></param>
        /// <param name="arrangeList">设备类型  设备型号 设备id  设备次数 /流程工序  人员等级 人员id  人员次数</param>
        /// <param name="newSchedules"></param>
        /// <param name="capacityList"></param>
        /// <param name="productCapacity"></param>
        /// <param name="deviceList"></param>
        /// <param name="modelCounts"></param>
        /// <param name="operatorList"></param>
        /// <param name="operatorCounts"></param>
        /// <param name="arrangeDevice"></param>
        /// <param name="waitIndex">等待消耗的产能</param>
        private static void ArrangeDeviceOrOperator(
            SmartTaskOrderConfirm task,
            DateTime processTime,
            int number,
            out int capacity,
            ref decimal waitIndex,
            ref List<ArrangeInfo> arrangeList,
            ref Dictionary<DateTime, SmartTaskOrderScheduleDay> newSchedules,
            SmartCapacityListDetail capacityList,
            SmartProductCapacityDetail productCapacity,
            IEnumerable<SmartDevice> deviceList,
            IEnumerable<SmartDeviceModelCount> modelCounts,
            IEnumerable<SmartOperatorDetail> operatorList,
            IEnumerable<SmartOperatorCount> operatorCounts,
            bool arrangeDevice)
        {
            var schedule = newSchedules[processTime];
            var taskOrderId = task.Id;
            var capacityId = capacityList.CapacityId;
            var categoryId = capacityList.CategoryId;
            var pId = capacityList.PId;
            var productId = productCapacity.ProductId;
            var processId = capacityList.ProcessId;
            var indexes = schedule.ProcessLeftCapacityIndex(categoryId, pId, capacityList, deviceList, operatorList, arrangeDevice, waitIndex);
            capacity = 0;
            if (!indexes.Any())
            {
                return;
            }
            var dy = false;
            if (dy)
            {
                #region 动态规划最优安排
                //安排设备
                if (arrangeDevice)
                {
                    var devices = capacityList.DeviceList;
                    var maxSingle = devices.Max(x => x.Single);
                    var max = devices.Sum(device => (modelCounts.FirstOrDefault(x => x.ModelId == device.ModelId)?.Count ?? 0) * device.SingleCount) + 1;
                    var C = number + maxSingle + 1;
                    var f = new int[max][];
                    for (var i = 0; i < max; i++)
                    {
                        f[i] = new int[C];
                    }
                    //占用次数
                    var w = new int[max];
                    //加工数量
                    var v = new int[max];
                    var zt = new int[max];
                    var m = 0;
                    var k = 1;
                    var lefts = new List<int>();
                    //可生产的设备型号产量及单日次数
                    foreach (var device in devices)
                    {
                        //设备单次生产数量
                        var single = device.Single;
                        //设备单日生产次数
                        var singleCount = device.SingleCount;
                        if (singleCount > 0)
                        {
                            // 设备类型  设备型号 设备id  设备次数 /流程工序  人员等级 人员id  人员次数
                            var sameMode = indexes.Values.Where(x => x.Item1 == device.ModelId);
                            for (var i = 0; i < sameMode.Count(); i++)
                            {
                                var left = (int)(singleCount * sameMode.ElementAt(i).Item2).ToRound();
                                for (var j = 0; j < left; j++)
                                {
                                    w[k] = 1;
                                    v[k] = single;
                                    k++;
                                }
                                lefts.Add(left);
                                m += left;
                            }
                        }
                    }

                    var i1 = 0;
                    var j1 = 0;
                    var ca = -1;
                    for (var i = 1; i <= m; i++)
                    {
                        //尝试放置每次安排
                        for (var j = 1; j < C; j++)
                        {
                            if (j >= w[i])
                            {
                                f[i][j] = Math.Max(f[i - 1][j - w[i]] + v[i], f[i - 1][j]);
                            }
                            else
                            {
                                f[i][j] = f[i - 1][j];
                            }

                            var cur = f[i][j];
                            if (cur >= number)
                            {
                                if (ca == -1)
                                {
                                    i1 = i;
                                    j1 = j;
                                    ca = cur - number;
                                }
                                if (ca > cur - number)
                                {
                                    i1 = i;
                                    j1 = j;
                                    ca = cur - number;
                                }
                            }

                            //倒叙是为了保证每次安排都使用一次
                            //var cur = f[j - w[i]] + v[i];
                            //var old = f[j];
                            //var cur = f[j][];
                            //if (cur >= number)
                            //{
                            //    if (ca == -1)
                            //    {
                            //        i1 = i;
                            //        j1 = j;
                            //        ca = cur - number;
                            //    }
                            //    if (ca > cur - number)
                            //    {
                            //        i1 = i;
                            //        j1 = j;
                            //        ca = cur - number;
                            //    }
                            //}
                            //if (j >= w[i] && f[j] < number)
                            //{
                            //    zt[i] = 1;
                            //    f[j] = Math.Max(f[j - w[i]] + v[i], f[j]);
                            //}
                        }
                    }
                    var s = f[i1][j1];
                    for (var i = i1; i >= 1; i--)
                    {
                        if (f[i][s] > f[i - 1][s])
                        {
                            zt[i] = 1; //装入背包
                            s -= v[i]; //物品i装入背包之前背包的容量
                        }
                        else
                        {
                            zt[i] = 0; //没有装入背包
                        }
                    }

                    var ss = 1;
                    var d = new List<int>();
                    foreach (var left in lefts)
                    {
                        d.Add(zt.Skip(ss).Take(left).Sum());
                        ss += left;
                    }
                }
                else
                {

                }


                #endregion
            }
            else
            {
                //安排设备
                if (arrangeDevice)
                {
                    var devices = capacityList.DeviceList;
                    //可生产的设备型号产量及单日次数
                    foreach (var deviceCapacity in devices)
                    {
                        //设备单次生产数量
                        var single = deviceCapacity.Single;
                        //设备单日生产次数
                        var singleCount = deviceCapacity.SingleCount;
                        if (single > 0 && singleCount > 0)
                        {
                            //设备型号 设备id  设备次数
                            var sames = indexes.Where(x => x.Value.Item1 == deviceCapacity.ModelId);
                            foreach (var (id, info) in sames)
                            {
                                var count = (int)(singleCount * info.Item2).ToRound();
                                var maxCount = (int)Math.Ceiling(((decimal)number - capacity) / single);
                                var actCount = count > maxCount ? maxCount : count;
                                capacity += actCount * single;
                                var device = deviceList.FirstOrDefault(x => x.Id == id);
                                if (device != null)
                                {
                                    var first = arrangeList.FirstOrDefault(x => x.Id == device.Id);
                                    if (first == null)
                                    {
                                        arrangeList.Add(new ArrangeInfo(device.ModelId, device.Id));
                                        first = arrangeList.FirstOrDefault(x => x.Id == device.Id);
                                    }
                                    var productArrange = first.Arranges.FirstOrDefault(x => x.ProductId == productId && x.PId == pId);
                                    if (productArrange != null)
                                    {
                                        productArrange.Count += actCount;
                                    }
                                    else
                                    {
                                        first.Arranges.Add(new ArrangeDetail(taskOrderId, productId, pId, single, actCount, singleCount));
                                    }
                                }
                                if (capacity >= number)
                                {
                                    break;
                                }
                            }
                            if (capacity >= number)
                            {
                                break;
                            }
                        }
                    }
                }
                else
                {
                    var operators = capacityList.OperatorList;
                    //可生产的人员等级产量及单日次数
                    foreach (var operatorCapacity in operators)
                    {
                        //设备单次生产数量
                        var single = operatorCapacity.Single;
                        //设备单日生产次数
                        var singleCount = operatorCapacity.SingleCount;
                        if (single > 0 && singleCount > 0)
                        {
                            // 人员等级 人员id  人员次数
                            var sames = indexes.Where(x => x.Value.Item1 == operatorCapacity.LevelId);
                            foreach (var (id, info) in sames)
                            {
                                var count = (int)(singleCount * info.Item2).ToRound();
                                var maxCount = (int)Math.Ceiling(((decimal)number - capacity) / single);
                                var actCount = count > maxCount ? maxCount : count;
                                capacity += actCount * single;
                                var op = operatorList.FirstOrDefault(x => x.Id == id);
                                if (op != null)
                                {
                                    var first = arrangeList.FirstOrDefault(x => x.Id == op.Id);
                                    if (first == null)
                                    {
                                        arrangeList.Add(new ArrangeInfo(op.LevelId, op.Id));
                                        first = arrangeList.FirstOrDefault(x => x.Id == op.Id);
                                    }
                                    var productArrange = first.Arranges.FirstOrDefault(x => x.ProductId == productId && x.PId == pId);
                                    if (productArrange != null)
                                    {
                                        productArrange.Count += actCount;
                                    }
                                    else
                                    {
                                        first.Arranges.Add(new ArrangeDetail(taskOrderId, productId, pId, single, actCount, singleCount));
                                    }
                                }
                                if (capacity >= number)
                                {
                                    break;
                                }
                            }
                            if (capacity >= number)
                            {
                                break;
                            }
                        }
                    }
                }
            }
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
