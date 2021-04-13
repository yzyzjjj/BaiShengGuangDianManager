using ModelBase.Models.BaseModel;
using System;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartProcessFault : CommonBase
    {
        /// <summary>
        /// 时间
        /// </summary>
        public DateTime FaultTime { get; set; }
        /// <summary>
        /// 错误类型
        /// </summary>
        public ProcessFault Type { get; set; }
        /// <summary>
        /// 错误描述
        /// </summary>
        public string Fault => Type.ToString();
        /// <summary>
        /// 机台号id
        /// </summary>
        public int DeviceId { get; set; }
        /// <summary>
        /// 流程卡id
        /// </summary>
        public int FlowCardId { get; set; }
        /// <summary>
        /// 流程卡流程id
        /// </summary>
        public int ProcessId { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; } = "";
        /// <summary>
        /// 是否处理
        /// </summary>
        public bool IsDeal { get; set; }
        /// <summary>
        /// 处理时间
        /// </summary>
        public DateTime DealTime { get; set; }
        /// <summary>
        /// 处理方式
        /// </summary>
        public ProcessFaultDeal DealType { get; set; }
        /// <summary>
        /// 处理方式
        /// </summary>
        public string Deal => DealType.ToString();
    }

    public class SmartProcessFaultDetail : SmartProcessFault
    {
        /// <summary>
        /// 机台号
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// 流程卡
        /// </summary>
        public string FlowCard { get; set; }
        /// <summary>
        /// 流程
        /// </summary>
        public string Process { get; set; }
    }

    public class SmartWorkProcessFaultCount
    {
        /// <summary>
        /// 工单id
        /// </summary>
        public int WorkOrderId { get; set; }
        /// <summary>
        /// 工单
        /// </summary>
        public string WorkOrder { get; set; }
        /// <summary>
        /// 数量
        /// </summary>
        public int Count { get; set; }
    }
    public class SmartTaskProcessFaultCount
    {
        /// <summary>
        /// 任务单id
        /// </summary>
        public int TaskOrderId { get; set; }
        /// <summary>
        /// 任务单
        /// </summary>
        public string TaskOrder { get; set; }
        /// <summary>
        /// 数量
        /// </summary>
        public int Count { get; set; }
    }
    public class SmartFlowCardProcessFaultCount
    {
        /// <summary>
        /// 流程卡id
        /// </summary>
        public int FlowCardId { get; set; }
        /// <summary>
        /// 流程卡
        /// </summary>
        public string FlowCard { get; set; }
        /// <summary>
        /// 数量
        /// </summary>
        public int Count { get; set; }
    }

    public class SmartWorkOrderFault : SmartProcessFault
    {
        /// <summary>
        /// 工单
        /// </summary>
        public string WorkOrder { get; set; }
        /// <summary>
        /// 流程卡
        /// </summary>
        public string FlowCard { get; set; }
        /// <summary>
        /// 流程
        /// </summary>
        public string Process { get; set; }
    }
    public class SmartTaskOrderFault : SmartProcessFault
    {
        /// <summary>
        /// 任务单
        /// </summary>
        public string TaskOrder { get; set; }
        /// <summary>
        /// 流程卡
        /// </summary>
        public string FlowCard { get; set; }
        /// <summary>
        /// 流程
        /// </summary>
        public string Process { get; set; }
    }
    public class SmartFlowCardFault : SmartProcessFault
    {
        /// <summary>
        /// 流程卡
        /// </summary>
        public string FlowCard { get; set; }
        /// <summary>
        /// 流程
        /// </summary>
        public string Process { get; set; }
    }
}
