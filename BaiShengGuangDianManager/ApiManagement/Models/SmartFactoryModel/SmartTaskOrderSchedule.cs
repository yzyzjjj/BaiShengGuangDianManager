using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using Newtonsoft.Json;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.SmartFactoryModel
{
    /// <summary>
    /// 工序生产数量
    /// </summary>
    public class SmartTaskOrderSchedule : SmartTaskOrderNeedDetail, ICloneable
    {
        ///// <summary>
        ///// 车间Id
        ///// </summary>
        //public int WorkshopId { get; set; }
        /// <summary>
        /// 0 设备 1 人员
        /// </summary>
        public int ProductType { get; set; } = -1;
        /// <summary>
        /// 加工时间
        /// </summary>
        public DateTime ProcessTime { get; set; }
        /// <summary>
        /// 耗时
        /// </summary>
        public int ActualCostDay { get; set; }
        /// <summary>
        /// 产能指数
        /// </summary>
        public decimal CapacityIndex { get; set; } = 0;
        /// <summary>
        /// 流程id
        /// </summary>
        //public new int PId { get; set; }
        /// <summary>
        /// 设备类型id
        /// </summary>
        //public int CategoryId { get; set; }
        /// <summary>
        /// 加工设备列表  id  次数
        /// </summary>
        public string Devices { get; set; } = string.Empty;
        private Dictionary<int, int> _deviceList { get; set; }
        /// <summary>
        /// 加工设备列表  id  次数
        /// </summary>
        public Dictionary<int, int> DeviceList
        {
            get
            {
                if (_deviceList == null)
                {
                    _deviceList = new Dictionary<int, int>();
                    if (!Devices.IsNullOrEmpty())
                    {
                        var s = Devices.Split(",").Select(int.Parse).ToArray();
                        for (var i = 0; i < s.Length / 2; i++)
                        {
                            var index = i * 2;
                            _deviceList.Add(s[index], s[index + 1]);
                        }
                    }
                }
                return _deviceList;
            }
            set
            {
                _deviceList = value;
                Devices = _deviceList.Select(x => $"{x.Key},{x.Value}").Join();
            }
        }

        /// <summary>
        /// 加工人员列表  id  次数
        /// </summary>
        public string Operators { get; set; } = string.Empty;
        private Dictionary<int, int> _operatorList { get; set; }
        /// <summary>
        /// 加工设备列表  id  次数
        /// </summary>
        public Dictionary<int, int> OperatorsList
        {
            get
            {
                if (_operatorList == null)
                {
                    _operatorList = new Dictionary<int, int>();
                    if (!Operators.IsNullOrEmpty())
                    {
                        var s = Operators.Split(",").Select(int.Parse).ToArray();
                        for (var i = 0; i < s.Length / 2; i++)
                        {
                            var index = i * 2;
                            _operatorList.Add(s[index], s[index + 1]);
                        }
                    }
                }
                return _operatorList;
            }
            set
            {
                _operatorList = value;
                Operators = _operatorList.Select(x => $"{x.Key},{x.Value}").Join();
            }
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
        /// <summary>
        /// 加工设备列表  id  次数
        /// </summary>
        public string ActualDevices { get; set; } = string.Empty;
        private Dictionary<int, int> _actualDeviceList { get; set; }
        /// <summary>
        /// 加工设备列表  id  次数
        /// </summary>
        public Dictionary<int, int> ActualDeviceList
        {
            get
            {
                if (_actualDeviceList == null)
                {
                    _actualDeviceList = new Dictionary<int, int>();
                    if (!ActualDevices.IsNullOrEmpty())
                    {
                        var s = ActualDevices.Split(",").Select(int.Parse).ToArray();
                        for (var i = 0; i < s.Length / 2; i++)
                        {
                            var index = i * 2;
                            _actualDeviceList.Add(s[index], s[index + 1]);
                        }
                    }
                }
                return _actualDeviceList;
            }
            set
            {
                _actualDeviceList = value;
                ActualDevices = _actualDeviceList.Select(x => $"{x.Key},{x.Value}").Join();
            }
        }

        /// <summary>
        /// 加工人员列表  id  次数
        /// </summary>
        public string ActualOperators { get; set; } = string.Empty;
        private Dictionary<int, int> _actualOperatorList { get; set; }
        /// <summary>
        /// 加工设备列表  id  次数
        /// </summary>
        public Dictionary<int, int> ActualOperatorsList
        {
            get
            {
                if (_actualOperatorList == null)
                {
                    _actualOperatorList = new Dictionary<int, int>();
                    if (!ActualOperators.IsNullOrEmpty())
                    {
                        var s = ActualOperators.Split(",").Select(int.Parse).ToArray();
                        for (var i = 0; i < s.Length / 2; i++)
                        {
                            var index = i * 2;
                            _actualOperatorList.Add(s[index], s[index + 1]);
                        }
                    }
                }
                return _actualOperatorList;
            }
            set
            {
                _actualOperatorList = value;
                ActualOperators = _actualOperatorList.Select(x => $"{x.Key},{x.Value}").Join();
            }
        }

    }

    public class SmartTaskOrderScheduleDetail : SmartTaskOrderSchedule
    {
        /// <summary>
        /// 加工顺序
        /// </summary>
        public int ArrangeOrder { get; set; }
        public SmartTaskOrderScheduleDetail()
        {

        }
        public SmartTaskOrderScheduleDetail(DateTime processTime, SmartTaskOrderConfirm task, SmartCapacityListDetail capacityList, SmartProductCapacity productCapacity)
        {
            ProcessTime = processTime;
            TaskOrderId = task.Id;
            TaskOrder = task.TaskOrder;
            ProductId = task.ProductId;
            Product = task.Product;
            ProcessId = capacityList.ProcessId;
            PId = capacityList.PId;
            CategoryId = capacityList.CategoryId;
            Order = capacityList.Order;
            var rate = 0m;
            var y = productCapacity;
            if (y.DeviceList.Any())
            {
                rate = y.DeviceList.First().Rate;
            }
            else if (y.OperatorList.Any())
            {
                rate = y.OperatorList.First().Rate;
            }
            Rate = rate;
        }
    }

    /// <summary>
    /// 任务单生产消耗天数
    /// </summary>
    public class SmartTaskOrderScheduleDay
    {
        public SmartTaskOrderScheduleDay(DateTime processTime)
        {
            ProcessTime = processTime;
            DeviceUse = new Dictionary<int, List<ArrangeInfo>>();
            OperatorUse = new Dictionary<int, List<ArrangeInfo>>();
        }

        /// <summary>
        /// 加工时间
        /// </summary>
        public DateTime ProcessTime { get; set; }

        /// <summary>
        /// Dictionary(工序), List(当日安排))
        /// </summary>
        public Dictionary<int, List<SmartTaskOrderScheduleDetail>> Needs { get; set; } = new Dictionary<int, List<SmartTaskOrderScheduleDetail>>();

        /// <summary>
        ///设备类型  (设备型号 设备id  (任务单 计划号 设备次数 最大次数 产能指数))
        /// </summary>
        private Dictionary<int, List<ArrangeInfo>> DeviceUse { get; set; }
        /// <summary>
        ///流程工序  (人员等级 人员id  (任务单 计划号 人员次数 最大次数 产能指数>>
        /// </summary>
        private Dictionary<int, List<ArrangeInfo>> OperatorUse { get; set; }

        public void Init(IEnumerable<SmartDevice> deviceList, IEnumerable<SmartOperatorDetail> operatorList)
        {
            if (deviceList != null && deviceList.Any())
            {
                //设备类型
                var categoryIds = deviceList.GroupBy(x => x.CategoryId).Select(y => y.Key);
                foreach (var categoryId in categoryIds)
                {
                    //设备类型  设备型号 设备id
                    if (!DeviceUse.ContainsKey(categoryId))
                    {
                        DeviceUse.Add(categoryId, new List<ArrangeInfo>());
                    }

                    var devices = deviceList.Where(x => x.CategoryId == categoryId).OrderBy(y => y.Priority);
                    foreach (var device in devices)
                    {
                        DeviceUse[categoryId].Add(new ArrangeInfo(device.ModelId, device.Id));
                    }
                }
            }

            if (operatorList != null && operatorList.Any())
            {
                //流程工序
                var processIds = operatorList.GroupBy(x => x.ProcessId).Select(y => y.Key);
                foreach (var processId in processIds)
                {
                    //流程工序  人员等级 人员id
                    if (!OperatorUse.ContainsKey(processId))
                    {
                        OperatorUse.Add(processId, new List<ArrangeInfo>());
                    }

                    var operators = operatorList.Where(x => x.ProcessId == processId).OrderBy(y => y.Order).ThenBy(y => y.Priority); ;
                    foreach (var op in operators)
                    {
                        OperatorUse[processId].Add(new ArrangeInfo(op.LevelId, op.Id));
                    }
                }
            }
        }

        /// <summary>
        /// 工序相关设备是否剩余产能
        /// </summary>
        /// <param name="categoryId"></param>
        /// <param name="pId"></param>
        /// <param name="capacityList"></param>
        /// <param name="productType">是否设备</param>
        /// <param name="deviceList">设备</param>
        /// <param name="operatorList">人员</param>
        /// <param name="waitIndex">等待消耗的产能</param>
        /// <returns>设备id或人员id  型号 剩余产能 / 流程工序  人员等级 人员id</returns>
        public Dictionary<int, Tuple<int, decimal>> ProcessLeftCapacityIndex(int categoryId, int pId,
            SmartCapacityListDetail capacityList,
            IEnumerable<SmartDevice> deviceList,
            IEnumerable<SmartOperator> operatorList,
            int productType,
            decimal waitIndex)
        {
            var defaultIndex = 1;
            //设备id或人员id  型号 剩余产能
            var result = new Dictionary<int, Tuple<int, decimal>>();
            if (productType == 0)
            {
                //设备类型  设备型号 设备id
                if (DeviceUse.ContainsKey(categoryId))
                {
                    var infos = DeviceUse[categoryId].Where(d => capacityList.DeviceList.Any(cd => cd.ModelId == d.Item));
                    foreach (var device in infos)
                    {
                        if (!result.ContainsKey(device.Id))
                        {
                            result.Add(device.Id, new Tuple<int, decimal>(device.Item, defaultIndex - waitIndex));
                        }
                        foreach (var arrange in device.Arranges)
                        {
                            result[device.Id] = new Tuple<int, decimal>(device.Item, result[device.Id].Item2 - arrange.CapacityIndex);
                        }
                    }
                }
            }
            else if (productType == 1)
            {
                //流程工序  人员等级 人员id
                if (OperatorUse.ContainsKey(pId))
                {
                    var infos = OperatorUse[pId].Where(d => capacityList.OperatorList.Any(cd => cd.LevelId == d.Item));
                    foreach (var op in infos)
                    {
                        if (!result.ContainsKey(op.Id))
                        {
                            result.Add(op.Id, new Tuple<int, decimal>(op.Item, defaultIndex - waitIndex));
                        }
                        foreach (var arrange in op.Arranges)
                        {
                            result[op.Id] = new Tuple<int, decimal>(op.Item, result[op.Id].Item2 - arrange.CapacityIndex);
                        }
                    }
                }
            }
            return result.Where(x => x.Value.Item2 > 0).ToDictionary(y => y.Key, y => new Tuple<int, decimal>(y.Value.Item1, y.Value.Item2));
        }

        /// <summary>
        /// 安排生产， 增加产能指数
        /// </summary>
        /// <param name="schedule"></param>
        /// <param name="arrangeInfos"></param>
        public SmartTaskOrderScheduleDetail AddTaskOrderSchedule(SmartTaskOrderScheduleDetail schedule, List<ArrangeInfo> arrangeInfos)
        {
            if (schedule.ProcessTime == ProcessTime)
            {
                if (!Needs.ContainsKey(schedule.PId))
                {
                    Needs.Add(schedule.PId, new List<SmartTaskOrderScheduleDetail>());
                }
                if (arrangeInfos.Any())
                {
                    var productType = schedule.ProductType;
                    int item;
                    Dictionary<int, List<ArrangeInfo>> use;
                    //是否设备
                    if (productType == 0)
                    {
                        item = schedule.CategoryId;
                        use = DeviceUse;
                    }
                    else if (productType == 1)
                    {
                        item = schedule.PId;
                        use = OperatorUse;
                    }
                    else
                    {
                        return null;
                    }
                    foreach (var arrangeInfo in arrangeInfos)
                    {
                        if (!use.ContainsKey(item))
                        {
                            use.Add(item, new List<ArrangeInfo>());
                            use[item].Add(new ArrangeInfo(arrangeInfo.Item, arrangeInfo.Id));
                        }

                        var first = use[item].FirstOrDefault(x => x.Id == arrangeInfo.Id);
                        if (first == null)
                        {
                            use[item].Add(new ArrangeInfo(arrangeInfo.Item, arrangeInfo.Id));
                            first = use[item].FirstOrDefault(x => x.Id == arrangeInfo.Id);
                        }
                        foreach (var arrange in arrangeInfo.Arranges)
                        {
                            if (arrange.Count > 0)
                            {
                                var productArrange = first.Arranges.FirstOrDefault(x => x.ProductId == arrange.ProductId && x.PId == arrange.PId);
                                if (productArrange != null)
                                {
                                    productArrange.Count += arrange.Count;
                                }
                                else
                                {
                                    first.Arranges.Add(arrange);
                                }
                            }

                        }
                    }

                    var arranges = arrangeInfos.ToDictionary(x => x.Id, info => info.Arranges.Sum(y => y.Count));
                    if (productType == 0)
                    {
                        schedule.DeviceList = arranges;
                    }
                    else if (productType == 1)
                    {
                        schedule.OperatorsList = arranges;
                    }
                }

                var oldSchedule = Needs[schedule.PId]
                    .FirstOrDefault(x => x.TaskOrderId == schedule.TaskOrderId && x.PId == schedule.PId);
                if (oldSchedule == null)
                {
                    schedule.ArrangeOrder = Needs[schedule.PId].Count + 1;
                    Needs[schedule.PId].Add(schedule);
                }
                else
                {
                    schedule.ArrangeOrder = oldSchedule.ArrangeOrder;
                    if (schedule.Target < oldSchedule.Target)
                    {
                        return oldSchedule;
                    }

                    oldSchedule.Stock = schedule.Stock;
                    oldSchedule.Target = schedule.Target;
                    oldSchedule.Put = schedule.Put;

                    if (schedule.ProductType == 0)
                    {
                        foreach (var (deviceId, count) in schedule.DeviceList)
                        {
                            if (oldSchedule.DeviceList.Any(x => x.Key == deviceId))
                            {
                                var cnt = oldSchedule.DeviceList[deviceId];
                                if (cnt < count)
                                {
                                    oldSchedule.DeviceList[deviceId] = count;
                                }
                            }
                            else
                            {
                                oldSchedule.DeviceList.Add(deviceId, count);
                            }
                        }
                    }
                    else if (schedule.ProductType == 1)
                    {
                        foreach (var (opId, count) in schedule.OperatorsList)
                        {
                            if (oldSchedule.OperatorsList.Any(x => x.Key == opId))
                            {
                                var cnt = oldSchedule.OperatorsList[opId];
                                if (cnt < count)
                                {
                                    oldSchedule.OperatorsList[opId] = count;
                                }
                            }
                            else
                            {
                                oldSchedule.OperatorsList.Add(opId, count);
                            }
                        }
                    }
                }
            }
            return null;
        }

        ///<summary>
        /// 当前产能指数
        /// 设备  流程工序  型号  产能指数 
        /// 人员  流程工序  等级  产能指数
        /// </summary>
        public IEnumerable<SmartTaskOrderScheduleIndex> CapacityIndexList
        {
            get
            {
                var list = new List<SmartTaskOrderScheduleIndex>();
                list.AddRange(DeviceUse.SelectMany(x => x.Value.Where(z => z.Arranges.Any())
                    .SelectMany(z => z.Arranges.GroupBy(a => new { a.PId, a.Order, z.Id }).Select(b =>
                        new SmartTaskOrderScheduleIndex
                        {
                            ProductType = 0,
                            ProcessTime = ProcessTime,
                            PId = b.Key.PId,
                            Order = b.Key.PId,
                            DealId = b.Key.Id,
                            Index = z.Arranges.Sum(ar => ar.CapacityIndex)
                        }))));

                list.AddRange(OperatorUse.SelectMany(x => x.Value.Where(y => y.Arranges.Any())
                    .SelectMany(z => z.Arranges.GroupBy(a => new { a.PId, z.Id }).Select(b =>
                       new SmartTaskOrderScheduleIndex
                       {
                           ProductType = 1,
                           ProcessTime = ProcessTime,
                           PId = b.Key.PId,
                           DealId = b.Key.Id,
                           Index = z.Arranges.Sum(ar => ar.CapacityIndex)
                       }))));
                return list;
            }
        }

        /// <summary>
        /// 工序是否有安排
        /// </summary>
        /// <returns></returns>
        public bool HaveArranged()
        {
            return CapacityIndexList.Sum(x => x.Index) > 0;
        }
    }

    /// <summary>
    /// 加工安排
    /// </summary>
    public class ArrangeInfo
    {
        /// <summary>
        /// 设备型号id/人员等级id    设备id/人员id
        /// </summary>
        /// <param name="item">设备型号id / 人员等级id</param>
        /// <param name="id">设备id / 人员id</param>
        public ArrangeInfo(int item, int id)
        {
            Item = item;
            Id = id;
            Arranges = new List<ArrangeDetail>();
        }
        /// <summary>
        /// 设备型号id / 人员等级id
        /// </summary>
        public int Item { get; set; }
        /// <summary>
        /// 设备id / 人员id
        /// </summary>
        public int Id { get; set; }
        public List<ArrangeDetail> Arranges { get; set; }
    }
    /// <summary>
    /// 设备/人员加工安排
    /// </summary>
    public class ArrangeDetail
    {
        public ArrangeDetail(int taskOrderId, int productId, int pId, int order, int single, int count, int maxCount)
        {
            ProductId = productId;
            PId = pId;
            Order = order;
            TaskOrderId = taskOrderId;
            Single = single;
            Count = count;
            MaxCount = maxCount;
        }
        /// <summary>
        /// 计划号
        /// </summary>
        public int ProductId { get; set; }
        /// <summary>
        /// 流程id
        /// </summary>
        public int PId { get; set; }
        /// <summary>
        /// 流程id
        /// </summary>
        public int Order { get; set; }
        /// <summary>
        /// 任务单
        /// </summary>
        public int TaskOrderId { get; set; }
        /// <summary>
        /// 单次生产数量
        /// </summary>
        public int Single { get; set; }
        /// <summary>
        /// 设备安排次数 / 人员安排次数
        /// </summary>
        public int Count { get; set; }
        /// <summary>
        /// 设备最大可安排次数 / 人员最大可安排次数
        /// </summary>
        public int MaxCount { get; set; }
        /// <summary>
        /// 产能指数
        /// </summary>
        public decimal CapacityIndex => MaxCount != 0 ? ((decimal)Count / MaxCount).ToRound(4) : 0;
    }
    /// <summary>
    /// 任务单安排情况
    /// </summary>
    public class SmartTaskOrderScheduleCostDays
    {
        /// <summary>
        /// 任务单id
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// 任务单
        /// </summary>
        public string TaskOrder { get; set; }
        /// <summary>
        /// 计划号id
        /// </summary>
        public int ProductId { get; set; }
        /// <summary>
        /// 计划号id
        /// </summary>
        public string Product { get; set; }
        /// <summary>
        /// 任务单设置开始日期
        /// </summary>
        public DateTime StartTime { get; set; }
        /// <summary>
        /// 任务单设置结束日期
        /// </summary>
        public DateTime EndTime { get; set; }
        /// <summary>
        /// 任务单预计开始日期
        /// </summary>
        public DateTime EstimatedStartTime { get; set; }
        /// <summary>
        /// 任务单预计完成日期
        /// </summary>
        public DateTime EstimatedEndTime { get; set; }
        /// <summary>
        /// 耗时
        /// </summary>
        public int CostDay
        {
            get
            {
                if (EstimatedStartTime != default(DateTime) && EstimatedEndTime != default(DateTime))
                {
                    return (int)(EstimatedEndTime - EstimatedStartTime).TotalDays + 1;
                }
                return 0;
            }
        }
        /// <summary>
        /// 逾期
        /// </summary>
        public int OverdueDay
        {
            get
            {
                if (EndTime != default(DateTime))
                {
                    var time = EstimatedEndTime != default(DateTime) ? EstimatedEndTime : DateTime.Today;
                    if (time > EndTime)
                    {
                        return (int)(time - EndTime).TotalDays;
                    }
                }
                return 0;
            }
        }
        /// <summary>
        /// 0 设备 1 人员
        /// </summary>
        public int Best { get; set; }
        /// <summary>
        /// 消耗天数
        /// </summary>
        public List<SmartTaskOrderScheduleCostDay> CostDays { get; set; } = new List<SmartTaskOrderScheduleCostDay>();
    }

    public class SmartTaskOrderScheduleCostDay
    {
        /// <summary>
        /// 标准流程id
        /// </summary>
        public int ProcessId { get; set; }
        /// <summary>
        /// 流程id
        /// </summary>
        public int PId { get; set; }
        /// <summary>
        /// 流程
        /// </summary>
        public string Process { get; set; }
        /// <summary>
        /// 顺序
        /// </summary>
        public int Order { get; set; }
        /// <summary>
        /// 设备
        /// </summary>
        public int DeviceDay { get; set; }
        /// <summary>
        /// 人员
        /// </summary>
        public int OperatorDay { get; set; }
    }
    public class SmartTaskOrderScheduleInfoResultBase
    {
        /// <summary>
        /// 
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// 任务单
        /// </summary>
        public int TaskOrderId { get; set; }
        /// <summary>
        /// 任务单
        /// </summary>
        public string TaskOrder { get; set; }
        /// <summary>
        /// 计划号
        /// </summary>
        public int ProductId { get; set; }
        /// <summary>
        /// 计划号
        /// </summary>
        public string Product { get; set; }
        /// <summary>
        /// 流程id
        /// </summary>
        public int PId { get; set; }
        /// <summary>
        /// 流程
        /// </summary>
        public string Process { get; set; }
        /// <summary>
        /// 流程id
        /// </summary>
        public int ProcessId { get; set; }
        /// <summary>
        /// 顺序
        /// </summary>
        public int Order { get; set; }
    }

    public class SmartTaskOrderScheduleSumInfoResult : SmartTaskOrderScheduleInfoResultBase
    {
        /// <summary>
        /// 目标投入
        /// </summary>
        public int Put { get; set; }
        /// <summary>
        /// 实际投入
        /// </summary>
        public int HavePut { get; set; }
        /// <summary>
        /// 目标产量
        /// </summary>
        public int Target { get; set; }
        /// <summary>
        /// 投料加工完成产量(合格品)
        /// </summary>
        public int DoneTarget { get; set; }
        /// <summary>
        /// 交货日期
        /// </summary>
        public DateTime DeliveryTime { get; set; }
        /// <summary>
        /// 安排日期
        /// </summary>
        public DateTime ArrangedTime { get; set; }
        public List<SmartTaskOrderScheduleInfoResult> Schedules { get; set; } = new List<SmartTaskOrderScheduleInfoResult>();
    }
    //public class SmartTaskOrderScheduleInfoAllResult : SmartTaskOrderScheduleInfoResultBase
    public class SmartTaskOrderScheduleInfoResult : SmartTaskOrderScheduleInfoResultBase
    {
        /// <summary>
        /// 0 设备 1 人员
        /// </summary>
        public int ProductType { get; set; } = -1;
        /// <summary>
        /// 时间
        /// </summary>
        public DateTime ProcessTime { get; set; }
        /// <summary>
        /// 目标投入
        /// </summary>
        public int Put { get; set; }
        /// <summary>
        /// 实际投入
        /// </summary>
        public int HavePut { get; set; }
        /// <summary>
        /// 目标产量
        /// </summary>
        public int Target { get; set; }
        /// <summary>
        /// 投料加工完成产量(合格品)
        /// </summary>
        public int DoneTarget { get; set; }
    }

    /// <summary>
    /// 投料详情
    /// </summary>
    public class SmartTaskOrderSchedulePutInfoResult : SmartTaskOrderScheduleInfoResultBase
    {
        /// <summary>
        /// 0 设备 1 人员
        /// </summary>
        public int ProductType { get; set; } = -1;
        /// <summary>
        /// 时间
        /// </summary>
        public DateTime ProcessTime { get; set; }
        /// <summary>
        /// 目标投入
        /// </summary>
        public int Put { get; set; }
        /// <summary>
        /// 实际投入
        /// </summary>
        public int HavePut { get; set; }
        /// <summary>
        /// 加工设备列表  id  次数
        /// </summary>
        [JsonIgnore]
        public string Devices { get; set; }
        private Dictionary<int, int> _deviceList { get; set; }
        /// <summary>
        /// 加工设备列表  id  次数
        /// </summary>
        [JsonIgnore]
        public Dictionary<int, int> DeviceList
        {
            get
            {
                if (_deviceList == null)
                {
                    _deviceList = new Dictionary<int, int>();
                    if (!Devices.IsNullOrEmpty())
                    {
                        var s = Devices.Split(",").Select(int.Parse).ToArray();
                        for (var i = 0; i < s.Length / 2; i++)
                        {
                            var index = i * 2;
                            _deviceList.Add(s[index], s[index + 1]);
                        }
                    }
                }
                return _deviceList;
            }
            set
            {
                _deviceList = value;
                Devices = _deviceList.Select(x => $"{x.Key},{x.Value}").Join();
            }
        }
        /// <summary>
        /// 加工人员列表  id  次数
        /// </summary>
        [JsonIgnore]
        public string Operators { get; set; }
        private Dictionary<int, int> _operatorList { get; set; }
        /// <summary>
        /// 加工人员列表  id  次数
        /// </summary>
        [JsonIgnore]
        public Dictionary<int, int> OperatorsList
        {
            get
            {
                if (_operatorList == null)
                {
                    _operatorList = new Dictionary<int, int>();
                    if (!Operators.IsNullOrEmpty())
                    {
                        var s = Operators.Split(",").Select(int.Parse).ToArray();
                        for (var i = 0; i < s.Length / 2; i++)
                        {
                            var index = i * 2;
                            _operatorList.Add(s[index], s[index + 1]);
                        }
                    }
                }
                return _operatorList;
            }
            set
            {
                _deviceList = value;
                Operators = _operatorList.Select(x => $"{x.Key},{x.Value}").Join();
            }
        }
        public Dictionary<int, Tuple<string, int>> Arranges { get; set; } = new Dictionary<int, Tuple<string, int>>();
    }

    /// <summary>
    /// 入库详情
    /// </summary>
    public class SmartTaskOrderScheduleWarehouseInfoResult : SmartTaskOrderScheduleInfoResultBase
    {
        /// <summary>
        /// 0 设备 1 人员
        /// </summary>
        public int ProductType { get; set; } = -1;
        /// <summary>
        /// 时间
        /// </summary>
        public DateTime ProcessTime { get; set; }
        /// <summary>
        /// 目标产量
        /// </summary>
        public int Target { get; set; }
        /// <summary>
        /// 投料加工完成产量(合格品)
        /// </summary>
        public int DoneTarget { get; set; }
    }
    /// <summary>
    /// 投料入库详情
    /// </summary>
    public class SmartTaskOrderSchedulePutAndWarehouseInfoResult : SmartTaskOrderSchedulePutInfoResult
    {
        /// <summary>
        /// 目标产量
        /// </summary>
        public int Target { get; set; }
        /// <summary>
        /// 投料加工完成产量(合格品)
        /// </summary>
        public int DoneTarget { get; set; }
    }

    /// <summary>
    /// 流程顺序结果
    /// </summary>
    public class SmartTaskOrderNeedWithOrderResult : DataResult
    {
        /// <summary>
        /// 顺序
        /// </summary>
        public List<SmartTaskOrderNeedWithOrder> Orders { get; set; } = new List<SmartTaskOrderNeedWithOrder>();
    }
    /// <summary>
    /// 流程顺序结果
    /// </summary>
    public class SmartTaskOrderNeedOrderTimeResult : SmartTaskOrderNeedWithOrderResult
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        /// <summary>
        /// 顺序
        /// </summary>
        public List<SmartTaskOrderScheduleIndex> Indexes { get; set; } = new List<SmartTaskOrderScheduleIndex>();
    }

    public class SmartTaskOrderNeedWithOrder
    {
        /// <summary>
        /// 流程id
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// 流程
        /// </summary>
        public string Process { get; set; }
        /// <summary>
        /// 顺序
        /// </summary>
        public int Order { get; set; }
        /// <summary>
        /// 设备类型
        /// </summary>
        public int CategoryId { get; set; }
    }

    /// <summary>
    /// 流程顺序结果
    /// </summary>
    public class SmartTaskOrderNeedWithOrderPreviewResult : DataResult
    {
        /// <summary>
        /// 顺序
        /// </summary>
        public List<SmartTaskOrderNeedWithOrderPreview> Orders { get; set; } = new List<SmartTaskOrderNeedWithOrderPreview>();
    }

    public class SmartTaskOrderNeedWithOrderPreview : SmartTaskOrderNeedWithOrder
    {
        /// <summary>
        /// 库存
        /// </summary>
        public int Stock { get; set; }
        /// <summary>
        /// 需生产
        /// </summary>
        public int Target { get; set; }
        /// <summary>
        /// 需投料
        /// </summary>
        public int Put { get; set; }

        public List<SmartTaskOrderNeedCapacityPreview> Devices { get; set; } = new List<SmartTaskOrderNeedCapacityPreview>();
        public List<SmartTaskOrderNeedCapacityPreview> Operators { get; set; } = new List<SmartTaskOrderNeedCapacityPreview>();
    }

    public class SmartTaskOrderNeedCapacityPreview
    {
        public int Id { get; set; }
        public string Name { get; set; }
        /// <summary>
        /// 需求
        /// </summary>
        public decimal NeedCapacity { get; set; }
        /// <summary>
        /// 现有
        /// </summary>
        public decimal HaveCapacity { get; set; }
        /// <summary>
        /// 班次
        /// </summary>
        public decimal Times => HaveCapacity != 0 ? (NeedCapacity / HaveCapacity).ToRound() : 0;
    }
}
