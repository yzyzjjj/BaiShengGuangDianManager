using System.Collections.Generic;
using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.PlanManagementModel
{
    /// <summary>
    /// 计划管理
    /// </summary>
    public class ProductionPlan : CommonBase
    {
        public string Plan { get; set; }
        public string Remark { get; set; }
    }
    public class ProductionPlanDetail : ProductionPlan
    {
        public int PlannedConsumption { get; set; }
        public int ActualConsumption { get; set; }
        public int ExtraConsumption { get; set; }
        public decimal PlannedCost { get; set; }
        public decimal ActualCost { get; set; }
        public List<ProductionPlanBillStockDetail> FirstBill  = new List<ProductionPlanBillStockDetail>();
    }
    public class OpProductionPlan : ProductionPlan
    {
        public IEnumerable<ProductionPlanBill> Bill { get; set; }
    }
}
