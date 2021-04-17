using System;
using System.Collections.Generic;
using ModelBase.Base.Logic;
using ModelBase.Base.Utils;
using ModelBase.Models.BaseModel;

namespace ApiManagement.Models.FlowCardManagementModel
{
    /// <summary>
    /// 计划号生产计划
    /// </summary>
    public class ProductionPlan : CommonBase
    {
        public DateTime Date { get; set; }
        /// <summary>
        /// 计划号Id
        /// </summary>
        public int ProductionId { get; set; }
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
    }

    public class ProductionPlanDetail : ProductionPlan
    {
        /// <summary>
        /// 计划号
        /// </summary>
        public string ProductionProcessName { get; set; }
        /// <summary>
        /// 步骤Id
        /// </summary>
        public string StepName { get; set; }
    }
}
