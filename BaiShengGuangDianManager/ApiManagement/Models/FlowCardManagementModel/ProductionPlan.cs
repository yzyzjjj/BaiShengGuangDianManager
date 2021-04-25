using ApiManagement.Base.Helper;
using ApiManagement.Models.DeviceManagementModel;
using ModelBase.Models.BaseModel;
using System;

namespace ApiManagement.Models.FlowCardManagementModel
{
    /// <summary>
    /// 计划号生产计划
    /// </summary>
    public class ProductionPlan : CommonBase
    {
        public ProductionPlan()
        {
        }

        public ProductionPlan(HFlowCardHelper.ErpProductionPlan jhPlan, Production production, DeviceProcessStepDetail step, string createUserId, DateTime now)
        {
            CreateUserId = createUserId;
            MarkedDateTime = now;
            Date = jhPlan.f_jhdate;
            ProductionProcessName = jhPlan.f_jhh;
            ProductionId = production?.Id ?? 0;
            StepName = jhPlan.f_gxname;
            StepId = step?.Id ?? 0;
            Plan = jhPlan.f_yqty;
            Change = jhPlan.f_xqty ?? 0;
            Final = jhPlan.f_qty;
            Reason = jhPlan.f_xgyy ?? "";
            Remark = jhPlan.f_note ?? "";
        }

        public DateTime Date { get; set; }
        /// <summary>
        /// 计划号
        /// </summary>
        public string ProductionProcessName { get; set; }
        /// <summary>
        /// 计划号Id
        /// </summary>
        public int ProductionId { get; set; }
        /// <summary>
        /// 步骤Id
        /// </summary>
        public string StepName { get; set; }
        /// <summary>
        /// 步骤Id
        /// </summary>
        public int StepId { get; set; }
        /// <summary>
        /// 最初值
        /// </summary>
        public decimal Plan { get; set; }
        /// <summary>
        /// 修改值
        /// </summary>
        public decimal Change { get; set; }
        /// <summary>
        /// 最终值
        /// </summary>
        public decimal Final { get; set; }
        /// <summary>
        /// 修改原因
        /// </summary>
        public string Reason { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }

        public bool HaveChange(ProductionPlan plan)
        {
            return (plan.ProductionId != ProductionId && plan.ProductionId != 0 && ProductionId == 0)
                   || (plan.StepId != StepId && plan.StepId != 0 && StepId == 0)
                   || plan.Plan != Plan || plan.Change != Change || plan.Final != Final || plan.Reason != Reason ||
                   plan.Remark != Remark;
        }
    }

    public class ProductionPlanDetail : ProductionPlan
    {
    }
}
