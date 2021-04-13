using ModelBase.Base.Utils;
using System;
using System.Collections.Generic;
using ModelBase.Models.BaseModel;

namespace ApiManagement.Models.SmartFactoryModel
{
    /// <summary>
    /// 任务单生产线单步流程
    /// </summary>
    public class SmartLineTaskOrder : CommonBase
    {
        /// <summary>
        /// 任务单id
        /// </summary>
        public int TaskOrderId { get; set; }
        /// <summary>
        /// 流程编号类型id
        /// </summary>
        public int ProcessCodeCategoryId { get; set; }
        /// <summary>
        /// 标准流程id
        /// </summary>
        public int ProcessId { get; set; }
        /// <summary>
        /// 加工时间
        /// </summary>
        public DateTime StartTime { get; set; }
        /// <summary>
        /// 加工时间
        /// </summary>
        public DateTime EndTime { get; set; }
        /// <summary>
        /// 加工前
        /// </summary>
        public int Before { get; set; }
        /// <summary>
        /// 加工中
        /// </summary>
        public int Doing { get; set; }
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

        public SmartLineState State { get; set; }
        public string StateStr => State.ToString();
    }

    /// <summary>
    /// 任务单生产线单步流程
    /// </summary>
    public class SmartLineTaskOrderDetail : SmartLineTaskOrder
    {
        /// <summary>
        /// 任务单
        /// </summary>
        public string TaskOrder { get; set; }
        /// <summary>
        /// 流程编号类型
        /// </summary>
        public string ProcessCodeCategory { get; set; }
        /// <summary>
        /// 流程
        /// </summary>
        public string Process { get; set; }
        /// <summary>
        /// 流程故障列表
        /// </summary>
        public List<SmartProcessFaultDetail> Faults { get; set; } = new List<SmartProcessFaultDetail>();
    }

    /// <summary>
    /// 任务单生产线
    /// </summary>
    public class SmartTaskOrderLine
    {
        public SmartTaskOrderLine()
        {
            Processes = new List<SmartLineTaskOrderDetail>();
        }
        /// <summary>
        /// 流程列表
        /// </summary>
        public List<SmartLineTaskOrderDetail> Processes { get; set; }
    }

}
