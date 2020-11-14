using ApiManagement.Models.BaseModel;
using System;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartTaskOrderNeed : CommonBase
    {
        /// <summary>
        /// 任务单id
        /// </summary>
        public int TaskOrderId { get; set; }
        /// <summary>
        /// 标准流程id
        /// </summary>
        public int ProcessId { get; set; }
        /// <summary>
        /// 计划号id
        /// </summary>
        public int ProductId { get; set; }
        /// <summary>
        /// 目标产量
        /// </summary>
        public int Target { get; set; }
        /// <summary>
        /// 库存
        /// </summary>
        public int Stock { get; set; }
        /// <summary>
        /// 加工完成产量
        /// </summary>
        public int DoneTarget { get; set; }
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
        /// 已发流程卡
        /// </summary>
        public int IssueCount { get; set; }
        /// <summary>
        /// 已发流程卡
        /// </summary>
        public int Issue { get; set; }
        /// <summary>
        /// 预计完成日期
        /// </summary>
        public DateTime EstimatedTime { get; set; }
        /// <summary>
        /// 实际完成时间
        /// </summary>
        public DateTime ActualTime { get; set; }
        /// <summary>
        /// 完成日期
        /// </summary>
        public DateTime CompleteTime { get; set; }
    }
    public class SmartTaskOrderNeedDetail : SmartTaskOrderNeed
    {
        /// <summary>
        /// 任务单
        /// </summary>
        public string TaskOrder { get; set; }
        /// <summary>
        /// 计划号
        /// </summary>
        public string Product { get; set; }
        /// <summary>
        /// 标准流程
        /// </summary>
        public string Process { get; set; }
    }
}
