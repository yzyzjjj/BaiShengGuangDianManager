using ApiManagement.Models.BaseModel;
using ApiManagement.Models.MaterialManagementModel;

namespace ApiManagement.Models.PlanManagementModel
{
    /// <summary>
    /// 计划所用物料
    /// </summary>
    public class ProductionPlanBill : CommonBase
    {
        public int PlanId { get; set; }
        public int BillId { get; set; }
        public int PlannedConsumption { get; set; }
        public int ActualConsumption { get; set; }
        public bool Extra { get; set; }
    }

    public class ProductionPlanBillDetail : MaterialBillDetail
    {
        public int PlanId { get; set; }
        public int BillId { get; set; }
        public int PlannedConsumption { get; set; }
        public int ActualConsumption { get; set; }
        public bool Extra { get; set; }
    }

    public class ProductionPlanBillStockDetail : MaterialBillDetail
    {
        public int PlanId { get; set; }
        public int BillId { get; set; }
        public int PlannedConsumption { get; set; }
        public int ActualConsumption { get; set; }
        public bool Extra { get; set; }
        public int Number { get; set; }
    }
}
