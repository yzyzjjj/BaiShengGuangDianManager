using ApiManagement.Models.BaseModel;
using ModelBase.Models.Result;
using System;
using System.Collections.Generic;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartTaskOrderNeed : CommonBase
    {
        /// <summary>
        /// 批号
        /// </summary>
        public int Batch { get; set; }
        /// <summary>
        /// 任务单id
        /// </summary>
        public int TaskOrderId { get; set; }
        /// <summary>
        /// 标准流程id
        /// </summary>
        public int ProcessId { get; set; }
        /// <summary>
        /// 流程id
        /// </summary>
        public int PId { get; set; }
        /// <summary>
        /// 计划号id
        /// </summary>
        public int ProductId { get; set; }
        /// <summary>
        /// 总目标产量
        /// </summary>
        public int Target { get; set; }
        /// <summary>
        /// 目标投料数量
        /// </summary>
        public int Put { get; set; }
        /// <summary>
        /// 当前可加工数量
        /// </summary>
        public int Have { get; set; }
        /// <summary>
        /// 已投料数量
        /// </summary>
        public int HavePut { get; set; }
        /// <summary>
        /// 剩余投料数量
        /// </summary>
        public int LeftPut => Put - HavePut;
        /// <summary>
        /// 合格率
        /// </summary>
        public decimal Rate { get; set; }
        /// <summary>
        /// 库存
        /// </summary>
        public int Stock { get; set; }
        /// <summary>
        /// 已完成 含库存
        /// </summary>
        public int TotalHave => Stock + DoneTarget;
        /// <summary>
        /// 剩余未完成产量
        /// </summary>
        public int LeftTarget => Target > TotalHave ? Target - TotalHave : 0;
        /// <summary>
        /// 投料加工已完成产量
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
        /// 预计开始日期
        /// </summary>
        public DateTime EstimatedStartTime { get; set; }
        /// <summary>
        /// 预计完成日期
        /// </summary>
        public DateTime EstimatedCompleteTime { get; set; }
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
        /// 实际完成时间
        /// </summary>
        public DateTime ActualTime { get; set; }

        /// <summary>
        /// 前道工序
        /// </summary>
        public int PreProcessId { get; set; }
        /// <summary>
        /// 后道工序
        /// </summary>
        public int NextProcessId { get; set; }
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
        /// 流程
        /// </summary>
        public string Process { get; set; }
        /// <summary>
        /// 顺序
        /// </summary>
        public int Order { get; set; }
    }

    public class SmartTaskOrderNeedDetailResult : DataResult
    {
        /// <summary>
        /// 顺序
        /// </summary>
        public List<SmartTaskOrderNeedOrder> Orders { get; set; } = new List<SmartTaskOrderNeedOrder>();
    }
    public class SmartTaskOrderNeedOrder
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
    }
}
