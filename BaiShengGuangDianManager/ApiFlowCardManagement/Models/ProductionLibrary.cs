using ModelBase.Base.Utils;
using System;
using System.Collections.Generic;

namespace ApiFlowCardManagement.Models
{
    /// <summary>
    /// 计划号
    /// </summary>
    public class ProductionLibrary
    {
        public int Id { get; set; }
        public string CreateUserId { get; set; }
        public DateTime MarkedDateTime { get; set; }
        public bool MarkedDelete { get; set; }
        public int ModifyId { get; set; }
        public string ProductionProcessName { get; set; }
        public List<ProductionSpecification> Specifications = new List<ProductionSpecification>();
        public List<ProductionProcessStep> ProcessSteps = new List<ProductionProcessStep>();
    }
    public class ProductionLibraryDetail : ProductionLibrary
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
