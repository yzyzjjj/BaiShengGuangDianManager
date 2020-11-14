using ApiManagement.Models.BaseModel;
using System;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartFlowCard : CommonBase
    {
        /// <summary>
        /// 生成时间
        /// </summary>
        public DateTime CreateTime { get; set; }
        /// <summary>
        /// 卡号
        /// </summary>
        public string FlowCard { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public SmartFlowCardState State { get; set; }

        public string StateStr => State.ToString();
        /// <summary>
        /// 任务单id
        /// </summary>
        public int TaskOrderId { get; set; }
        /// <summary>
        /// 流程编号id
        /// </summary>
        public int ProcessCodeId { get; set; }
        /// <summary>
        /// 加工人
        /// </summary>
        public int ProcessorId { get; set; }
        /// <summary>
        /// 批次
        /// </summary>
        public int Batch { get; set; }
        /// <summary>
        /// 加工数量
        /// </summary>
        public int Number { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
    }

    public class SmartFlowCardUI : SmartFlowCard
    {
        /// <summary>
        /// 当前工序
        /// </summary>
        public string Process { get; set; }
        /// <summary>
        /// 进度
        /// </summary>
        public decimal Progress { get; set; }
    }
    public class SmartFlowCardDetail : SmartFlowCard
    {
        /// <summary>
        /// 任务单
        /// </summary>
        public string TaskOrder { get; set; }
        /// <summary>
        /// 计划号id
        /// </summary>
        public int ProductId { get; set; }
        /// <summary>
        /// 计划号
        /// </summary>
        public string Product { get; set; }
        /// <summary>
        /// 流程编号
        /// </summary>
        public string ProcessCode { get; set; }
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
        /// <summary>
        /// 已完成数量
        /// </summary>
        public int Done { get; set; }
        /// <summary>
        /// 未完成数量
        /// </summary>
        public int Left { get; set; }
    }
}