using ApiManagement.Models.MaterialManagementModel;
using ModelBase.Models.BaseModel;

namespace ApiManagement.Models.PlanManagementModel
{
    /// <summary>
    /// 计划所用物料
    /// </summary>
    public class ProductionPlanBill : CommonBase
    {
        public int PlanId { get; set; }
        public int BillId { get; set; }
        public decimal PlannedConsumption { get; set; }
        public decimal ActualConsumption { get; set; }
        public bool Extra { get; set; }
    }

    public class ProductionPlanBillDetail : MaterialBillDetail
    {
        public int PlanId { get; set; }
        public int BillId { get; set; }
        public decimal PlannedConsumption { get; set; }
        public decimal ActualConsumption { get; set; }
        public bool Extra { get; set; }
    }

    public class ProductionPlanBillStockDetail : MaterialBillDetail
    {
        public int PlanId { get; set; }
        public int BillId { get; set; }
        public decimal PlannedConsumption { get; set; }
        public decimal ActualConsumption { get; set; }
        public bool Extra { get; set; }
        public decimal Number { get; set; }
    }
}
