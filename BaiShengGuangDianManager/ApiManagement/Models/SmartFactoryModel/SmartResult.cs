using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using Newtonsoft.Json;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartResult : DataResult
    {
        /// <summary>
        /// 总数
        /// </summary>
        public int Count { get; set; }
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
    public class SmartTaskOrderNeedWithOrderResult : SmartResult
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
}
