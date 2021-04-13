using ModelBase.Base.Utils;
using ModelBase.Models.BaseModel;
using System;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartFlowCardProcess : CommonBase
    {
        /// <summary>
        /// 车间Id
        /// </summary>
        public int WorkshopId { get; set; }
        /// <summary>
        /// 流程卡id
        /// </summary>
        public int FlowCardId { get; set; }
        /// <summary>
        /// 计划号流程id
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
        /// 状态
        /// </summary>
        public SmartFlowCardProcessState State { get; set; }
        public string StateStr => State.ToString();
        /// <summary>
        /// 加工次数
        /// </summary>
        public int Count { get; set; }
        /// <summary>
        /// 加工人
        /// </summary>
        public int ProcessorId { get; set; }
        /// <summary>
        /// 加工设备
        /// </summary>
        public int DeviceId { get; set; }
        /// <summary>
        /// 加工前数量
        /// </summary>
        public int Before { get; set; }

        /// <summary>
        /// 剩余数量
        /// </summary>
        public int Left => Before - Doing - Qualified - Unqualified;
        /// <summary>
        /// 加工中数量
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
        /// <summary>
        /// 合格率
        /// </summary>
        public decimal Rate => Qualified + Unqualified != 0 ? ((decimal)Qualified / (Qualified + Unqualified)).ToRound(4) * 100 : 0;
        /// <summary>
        /// 是否异常
        /// </summary>
        public bool Fault { get; set; }
    }

    public class SmartFlowCardProcessDetail : SmartFlowCardProcess
    {
        /// <summary>
        /// 流程卡
        /// </summary>
        public string FlowCard { get; set; }
        /// <summary>
        /// 流程
        /// </summary>
        public string Process { get; set; }

        /// <summary>
        /// 加工人
        /// </summary>
        public string Processor { get; set; }

        /// <summary>
        /// 加工设备
        /// </summary>
        public string DeviceCode { get; set; }

        /// <summary>
        /// 交货日期
        /// </summary>
        public DateTime DeliveryTime { get; set; }
    }
    public class SmartFlowCardProcessStandard1 : SmartFlowCardProcess
    {
        public int TaskOrderId { get; set; }
        /// <summary>
        /// 流程编号类型id
        /// </summary>
        public int ProcessCodeCategoryId { get; set; }
        /// <summary>
        /// 标准流程id
        /// </summary>
        public int StandardId { get; set; }
    }


    public class SmartFlowCardProcessStandard2 : SmartFlowCardProcess
    {
        public int WorkOrderId { get; set; }
        /// <summary>
        /// 流程编号类型id
        /// </summary>
        public int ProcessCodeCategoryId { get; set; }
        /// <summary>
        /// 标准流程id
        /// </summary>
        public int StandardId { get; set; }
    }

}