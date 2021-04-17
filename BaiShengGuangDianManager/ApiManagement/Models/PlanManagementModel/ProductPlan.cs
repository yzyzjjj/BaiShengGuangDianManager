using ModelBase.Models.BaseModel;
using ServiceStack;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.PlanManagementModel
{
    /// <summary>
    /// 计划管理
    /// </summary>
    public class ProductPlan : CommonBase
    {
        public string Plan { get; set; }
        public string Remark { get; set; }
    }
    public class ProductPlanDetail : ProductPlan
    {
        public decimal PlannedConsumption { get; set; }
        public decimal ActualConsumption { get; set; }
        public decimal ExtraConsumption { get; set; }
        public decimal PlannedCost { get; set; }
        public decimal ActualCost { get; set; }
        public List<ProductPlanBillStockDetail> FirstBill = new List<ProductPlanBillStockDetail>();
    }
    public class OpProductPlan : ProductPlan
    {
        public IEnumerable<ProductPlanBill> Bill { get; set; }
    }

    public class ProductPlanMove : CommonBase
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
