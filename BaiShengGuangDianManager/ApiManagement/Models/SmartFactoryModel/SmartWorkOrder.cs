using ModelBase.Base.Utils;
using ModelBase.Models.BaseModel;
using System;

namespace ApiManagement.Models.SmartFactoryModel
{

    public class SmartWorkOrder : CommonBase
    {
        /// <summary>
        /// 车间Id
        /// </summary>
        public int WorkshopId { get; set; }
        /// <summary>
        /// 工单
        /// </summary>
        public string WorkOrder { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public SmartWorkOrderState State { get; set; }

        public string StateStr => State.ToString();
        /// <summary>
        /// 目标产量
        /// </summary>
        public int Target { get; set; }
        /// <summary>
        /// 已完成产量
        /// </summary>
        public int DoneTarget { get; set; }
        /// <summary>
        /// 剩余产量
        /// </summary>
        public int Left => Target - DoneTarget;
        /// <summary>
        /// 进度
        /// </summary>
        public decimal Progress => Target != 0 ? ((decimal)DoneTarget / Target).ToRound(4) * 100 : 0;
        /// <summary>
        /// 已完成卡数
        /// </summary>
        public int DoneCount { get; set; }
        /// <summary>
        /// 已完成数量
        /// </summary>
        public int Done { get; set; }
        /// <summary>
        /// 加工中卡数
        /// </summary>
        public int DoingCount { get; set; }
        /// <summary>
        /// 加工中数量
        /// </summary>
        public int Doing { get; set; }
        /// <summary>
        /// 已发流程卡 卡数量
        /// </summary>
        public int IssueCount { get; set; }
        /// <summary>
        /// 已发流程卡 加工数量
        /// </summary>
        public int Issue { get; set; }
        /// <summary>
        /// 发货数量
        /// </summary>
        public int Delivery { get; set; }
        /// <summary>
        /// 交货日期
        /// </summary>
        public DateTime DeliveryTime { get; set; }
        /// <summary>
        /// 完成日期
        /// </summary>
        public DateTime CompleteTime { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
    }

    public class SmartWorkOrderDetail : SmartWorkOrder
    {

        /// <summary>
        /// 耗时
        /// </summary>
        public int Consume { get; set; }
        /// <summary>
        /// 按时率
        /// </summary>
        public decimal OnTimeRate { get; set; } = 100;
        /// <summary>
        /// 按时率
        /// </summary>
        public RiskLevelState RiskLevel { get; set; }
        /// <summary>
        /// 按时率
        /// </summary>
        public string RiskLevelStr => RiskLevel.ToString();
    }
}
