using System;

namespace ApiManagement.Models.StatisticManagementModel
{
    /// <summary>
    /// 计划号进度表
    /// </summary>
    public class ProductionSchedule
    {
        public int ProductionId { get; set; }
        public string Production { get; set; }
        public decimal Plan { get; set; }
        public decimal Actual { get; set; }
    }
    /// <summary>
    /// 计划号进度表
    /// </summary>
    public class KanBanProductProcess : ProductionSchedule
    {
        public DateTime Time { get; set; }
        public int StepId { get; set; }
        public string StepName { get; set; }
    }
}