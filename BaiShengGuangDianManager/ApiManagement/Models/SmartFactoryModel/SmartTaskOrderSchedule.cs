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
        public new int PId { get; set; }
        /// <summary>
        /// 加工设备列表  id  次数
        /// </summary>
        public string Devices { get; set; }
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
        public string Operators { get; set; }
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
                _deviceList = value;
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
            Order = capacityList.Order;
            Rate = productCapacity.Rate;
        }
    }

    /// <summary>
    /// 任务单生产消耗天数
    /// </summary>
    public class SmartTaskOrderScheduleDay
    {
        public SmartTaskOrderScheduleDay(DateTime processTime, int type)
        {
            ProcessTime = processTime;
            Type = type;
        }
        public SmartTaskOrderScheduleDay(DateTime processTime)
        {
            ProcessTime = processTime;
            Type = 0;
        }
        /// <summary>
        /// 0 设备 1 人员
        /// </summary>
        public int Type { get; set; }
        /// <summary>
        /// 加工时间
        /// </summary>
        public DateTime ProcessTime { get; set; }
        /// <summary>
        /// 当前产能指数
        /// 设备  流程工序  型号  产能指数 
        /// 人员  流程工序  等级  产能指数
        /// </summary>
        //public Dictionary<int, Dictionary<int, decimal>> CapacityIndex
        //    => Needs.GroupBy(x => x.Key).ToDictionary(y => y.Key,
        //        y => y.SelectMany(z => z.Value).GroupBy(a => a.PId).ToDictionary(b => b.Key, b => b.Sum(c => c.CapacityIndex)));
        public Dictionary<int, decimal> CapacityIndex
            => Needs.GroupBy(x => x.Key).ToDictionary(y => y.Key, y => y.Sum(c => c.Value.Sum(a => a.CapacityIndex)));

        /// <summary>
        /// 是否剩余产能
        /// </summary>
        //private Dictionary<int, Dictionary<int, decimal>> LeftCapacityIndex
        //    => CapacityIndex.ToDictionary(x => x.Key, x => x.Value.ToDictionary(y => y.Key, y => 1 - y.Value));
        private Dictionary<int, decimal> LeftCapacityIndex
            => CapacityIndex.ToDictionary(x => x.Key, x => 1 - x.Value);

        /// <summary>
        /// 工序是否剩余产能
        /// </summary>
        /// <param name="pId"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        //public bool HaveProcessLeftCapacity(int pId, int type)
        //{
        //    return !LeftCapacityIndex.ContainsKey(pId) || !LeftCapacityIndex[pId].ContainsKey(type) || LeftCapacityIndex[pId][type] > 0;
        //}
        /// <summary>
        /// 工序是否剩余产能
        /// </summary>
        /// <param name="pId"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        //public bool HaveProcessLeftCapacity(int pId, int type)
        //{
        //    return !LeftCapacityIndex.ContainsKey(pId) || !LeftCapacityIndex[pId].ContainsKey(type) || LeftCapacityIndex[pId][type] > 0;
        //}
        public bool HaveProcessLeftCapacity(int pId)
        {
            return !LeftCapacityIndex.ContainsKey(pId) || LeftCapacityIndex[pId] > 0;
        }
        ///// <summary>
        ///// 工序剩余产能指数
        ///// </summary>
        ///// <param name="pId"></param>
        ///// <returns></returns>
        //public decimal ProcessLeftCapacityIndex(int pId, int type)
        //{
        //    return LeftCapacityIndex.ContainsKey(pId) ? LeftCapacityIndex[pId].ContainsKey(type) ? LeftCapacityIndex[pId][type] : 1 : 1;
        //}
        /// <summary>
        /// 工序剩余产能指数
        /// </summary>
        /// <param name="pId"></param>
        /// <returns></returns>
        public decimal ProcessLeftCapacityIndex(int pId)
        {
            return LeftCapacityIndex.ContainsKey(pId) ? LeftCapacityIndex[pId] : 1;
        }
        /// <summary>
        /// 产能指数  Dictionary(工序), List(当日安排))
        /// </summary>
        public Dictionary<int, List<SmartTaskOrderScheduleDetail>> Needs { get; set; } = new Dictionary<int, List<SmartTaskOrderScheduleDetail>>();

        /// <summary>
        /// 安排生产， 增加产能指数
        /// </summary>
        /// <param name="schedule"></param>
        public void AddTaskOrderSchedule(SmartTaskOrderScheduleDetail schedule)
        {
            if (schedule.ProcessTime == ProcessTime)
            {
                if (!Needs.ContainsKey(schedule.PId))
                {
                    Needs.Add(schedule.PId, new List<SmartTaskOrderScheduleDetail>());
                }
                Needs[schedule.PId].Add(schedule);
            }
        }

        /// <summary>
        /// 工序是否有安排
        /// </summary>
        /// <returns></returns>
        public bool HaveArranged()
        {
            //return CapacityIndex.Sum(x => x.Value.Sum(y => y.Value)) > 0;
            return CapacityIndex.Sum(x => x.Value) > 0;
        }
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
                    var time = MustCompleteTime != default(DateTime) ? MustCompleteTime : DateTime.Today;
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

    public class SmartTaskOrderScheduleInfoResult1
    {
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

    public class SmartTaskOrderScheduleInfoResult2 : SmartTaskOrderScheduleInfoResult1
    {
        /// <summary>
        /// 投入
        /// </summary>
        public int Put { get; set; }
        /// <summary>
        /// 已投入
        /// </summary>
        public int HavePut { get; set; }
    }

    public class SmartTaskOrderScheduleInfoResult2Detail : SmartTaskOrderScheduleInfoResult2
    {
        /// <summary>
        /// 标准流程id
        /// </summary>
        public List<SmartTaskOrderScheduleInfoResult21> Tasks { get; set; } = new List<SmartTaskOrderScheduleInfoResult21>();
    }
    /// <summary>
    /// 投料详情
    /// </summary>
    public class SmartTaskOrderScheduleInfoResult21
    {
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
    }
    public class SmartTaskOrderScheduleInfoResult3 : SmartTaskOrderScheduleInfoResult1
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
