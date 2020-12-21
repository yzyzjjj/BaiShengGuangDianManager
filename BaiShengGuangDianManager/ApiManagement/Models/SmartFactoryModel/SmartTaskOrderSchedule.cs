using ModelBase.Base.Utils;
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
        /// <summary>
        /// 0 设备 1 人员
        /// </summary>
        public int IsDevice { get; set; }
        /// <summary>
        /// true 设备 false 人员
        /// </summary>
        public bool Device => IsDevice == 0;
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
        public int CategoryId { get; set; }
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
    }

    public class SmartTaskOrderScheduleDetail : SmartTaskOrderSchedule
    {
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
            Rate = productCapacity.Rate;
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
        /// <param name="arrangeDevice">是否设备</param>
        /// <param name="deviceList">设备</param>
        /// <param name="operatorList">人员</param>
        /// <param name="waitIndex">等待消耗的产能</param>
        /// <returns>设备id或人员id  型号 剩余产能 / 流程工序  人员等级 人员id</returns>
        public Dictionary<int, Tuple<int, decimal>> ProcessLeftCapacityIndex(int categoryId, int pId,
            SmartCapacityListDetail capacityList,
            IEnumerable<SmartDevice> deviceList,
            IEnumerable<SmartOperator> operatorList,
            bool arrangeDevice,
            decimal waitIndex)
        {
            var defaultIndex = 1;
            //设备id或人员id  型号 剩余产能
            var result = new Dictionary<int, Tuple<int, decimal>>();
            if (arrangeDevice)
            {
                //设备类型  设备型号 设备id
                if (DeviceUse.ContainsKey(categoryId))
                {
                    var infos = DeviceUse[categoryId];
                    foreach (var deviceCapacity in capacityList.DeviceList)
                    {
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

                        var canUseDevices =
                            deviceList.Where(x => x.CategoryId == categoryId && x.ModelId == deviceCapacity.ModelId);
                        foreach (var device in canUseDevices)
                        {
                            if (!result.ContainsKey(device.Id))
                            {
                                result.Add(device.Id, new Tuple<int, decimal>(device.ModelId, defaultIndex - waitIndex));
                            }
                        }
                    }
                }
            }
            else
            {
                //流程工序  人员等级 人员id
                if (OperatorUse.ContainsKey(pId))
                {
                    var infos = OperatorUse[pId];
                    foreach (var operatorCapacity in capacityList.OperatorList)
                    {
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

                        var canUseOperators =
                            operatorList.Where(x => x.ProcessId == pId && x.LevelId == operatorCapacity.LevelId);
                        foreach (var op in canUseOperators)
                        {
                            if (!result.ContainsKey(op.Id))
                            {
                                result.Add(op.Id, new Tuple<int, decimal>(op.LevelId, defaultIndex - waitIndex));
                            }
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
        public void AddTaskOrderSchedule(SmartTaskOrderScheduleDetail schedule, List<ArrangeInfo> arrangeInfos)
        {
            if (!arrangeInfos.Any())
            {
                return;
            }

            if (schedule.ProcessTime == ProcessTime)
            {
                //是否设备
                var arrangeDevice = schedule.Device;
                if (!Needs.ContainsKey(schedule.PId))
                {
                    Needs.Add(schedule.PId, new List<SmartTaskOrderScheduleDetail>());
                }
                var item = arrangeDevice ? schedule.CategoryId : schedule.PId;
                var use = arrangeDevice ? DeviceUse : OperatorUse;
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
                if (arrangeDevice)
                {
                    schedule.DeviceList = arranges;
                }
                else
                {
                    schedule.OperatorsList = arranges;
                }

                Needs[schedule.PId].Add(schedule);
            }
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
                list.AddRange(DeviceUse.SelectMany(x => x.Value.Where(z => z.Arranges.Any()).SelectMany(y => y.Arranges.Select(z =>
                    new SmartTaskOrderScheduleIndex
                    {
                        IsDevice = true,
                        ProcessTime = ProcessTime,
                        PId = z.PId,
                        DealId = y.Id,
                        Index = z.CapacityIndex,
                    }))));

                list.AddRange(OperatorUse.SelectMany(x => x.Value.Where(z => z.Arranges.Any()).SelectMany(y => y.Arranges.Select(z =>
                      new SmartTaskOrderScheduleIndex
                      {
                          IsDevice = false,
                          ProcessTime = ProcessTime,
                          PId = z.PId,
                          DealId = y.Id,
                          Index = z.CapacityIndex,
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
        public ArrangeDetail(int taskOrderId, int productId, int pId, int single, int count, int maxCount)
        {
            ProductId = productId;
            PId = pId;
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
        /// 预计开始日期
        /// </summary>
        public DateTime EstimatedStartTime { get; set; }
        /// <summary>
        /// 预计完成日期
        /// </summary>
        public DateTime EstimatedCompleteTime { get; set; }
        /// <summary>
        /// 必须完成日期
        /// </summary>
        public DateTime MustCompleteTime { get; set; }
        /// <summary>
        /// 耗时
        /// </summary>
        public int CostDay
        {
            get
            {
                if (EstimatedStartTime != default(DateTime) && EstimatedCompleteTime != default(DateTime))
                {
                    return (int)(EstimatedCompleteTime - EstimatedStartTime).TotalDays + 1;
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
                if (MustCompleteTime != default(DateTime))
                {
                    var time = EstimatedCompleteTime != default(DateTime) ? EstimatedCompleteTime : DateTime.Today;
                    if (time > MustCompleteTime)
                    {
                        return (int)(time - MustCompleteTime).TotalDays;
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
        /// 0 设备 1 人员
        /// </summary>
        public int IsDevice { get; set; }
        /// <summary>
        /// 时间
        /// </summary>
        public DateTime ProcessTime { get; set; }
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
        /// 顺序
        /// </summary>
        public int Order { get; set; }
    }

    public class SmartTaskOrderScheduleInfoAllResult : SmartTaskOrderScheduleInfoResultBase
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
        /// 实际产量
        /// </summary>
        public int DoneTarget { get; set; }
    }

    public class SmartTaskOrderScheduleInfoResult1 : SmartTaskOrderScheduleInfoResultBase
    {
        /// <summary>
        /// 目标投入
        /// </summary>
        public int Put { get; set; }
        /// <summary>
        /// 实际投入
        /// </summary>
        public int HavePut { get; set; }
    }

    /// <summary>
    /// 投料详情
    /// </summary>
    public class SmartTaskOrderScheduleInfoResult11
    {
        /// <summary>
        /// 0 设备 1 人员
        /// </summary>
        public int IsDevice { get; set; }
        /// <summary>
        /// true 设备 false 人员
        /// </summary>
        public bool Device => IsDevice == 0;
        /// <summary>
        /// 时间
        /// </summary>
        public DateTime ProcessTime { get; set; }
        /// <summary>
        /// 任务单id
        /// </summary>
        public int TaskOrderId { get; set; }
        /// <summary>
        /// 任务单
        /// </summary>
        public string TaskOrder { get; set; }
        /// <summary>
        /// 投入
        /// </summary>
        public int Put { get; set; }
        /// <summary>
        /// 已投入
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
        /// 加工设备列表  id  次数
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

    public class SmartTaskOrderScheduleInfoResult2 : SmartTaskOrderScheduleInfoResultBase
    {
        /// <summary>
        /// 标准流程id
        /// </summary>
        public int Target { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int DoneTarget { get; set; }
    }
}
