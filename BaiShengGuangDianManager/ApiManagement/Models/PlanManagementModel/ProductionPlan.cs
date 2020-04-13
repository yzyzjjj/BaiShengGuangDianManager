using System.Collections.Generic;
using ApiManagement.Models.BaseModel;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;

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
        public decimal PlannedConsumption { get; set; }
        public decimal ActualConsumption { get; set; }
        public decimal ExtraConsumption { get; set; }
        public decimal PlannedCost { get; set; }
        public decimal ActualCost { get; set; }
        public List<ProductionPlanBillStockDetail> FirstBill  = new List<ProductionPlanBillStockDetail>();
    }
    public class OpProductionPlan : ProductionPlan
    {
        public IEnumerable<ProductionPlanBill> Bill { get; set; }
    }
}
