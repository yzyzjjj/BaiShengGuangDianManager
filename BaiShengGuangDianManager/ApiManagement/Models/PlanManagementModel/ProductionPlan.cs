using System.Collections.Generic;
using System.Linq;
using ApiManagement.Models.BaseModel;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using ServiceStack;

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

    public class ProductionPlanMove : CommonBase
    {
        /// <summary>
        /// 转出
        /// </summary>
        public int FromId { get; set; }
        /// <summary>
        /// 转移到
        /// </summary>
        public int ToId { get; set; }
        /// <summary>
        /// 转移id
        /// </summary>
        public IEnumerable<int> Bill { get; set; }
        public string List => Bill != null ? Bill.Join() : "";
    }
}
