using System;
using System.Collections.Generic;
using ApiManagement.Models.BaseModel;
using ModelBase.Base.Logic;
using ModelBase.Base.Utils;

namespace ApiManagement.Models.FlowCardManagementModel
{
    /// <summary>
    /// 计划号
    /// </summary>
    public class Production : CommonBase
    {
        /// <summary>
        /// 计划号
        /// </summary>
        public string ProductionProcessName { get; set; }
        [IgnoreChange]
        public List<ProductionSpecification> Specifications = new List<ProductionSpecification>();
        [IgnoreChange]
        public List<ProductionProcessStep> ProcessSteps = new List<ProductionProcessStep>();
    }
    public class ProductionDetail : Production
    {
        /// <summary>
        /// 总流程卡数
        /// </summary>
        public int FlowCardCount { get; set; }
        /// <summary>
        /// 已完成流程卡数量
        /// </summary>
        public int Complete { get; set; }
        /// <summary>
        /// 总原料数
        /// </summary>
        public int AllRawMaterialQuantity { get; set; }
        /// <summary>
        /// 已完成原料数
        /// </summary>
        public int RawMaterialQuantity { get; set; }
        /// <summary>
        /// 已完成产量
        /// </summary>
        public int QualifiedNumber { get; set; }
        public string PassRate => 0 == RawMaterialQuantity ? "非数字" :
            ((double)QualifiedNumber / RawMaterialQuantity * 100).ToRound() + "%";
    }
}
