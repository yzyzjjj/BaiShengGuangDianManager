using System;

namespace ApiManagement.Models
{
    public class ProductionProcessStep
    {
        public int Id { get; set; }
        public string CreateUserId { get; set; }
        public DateTime MarkedDateTime { get; set; }
        public bool MarkedDelete { get; set; }
        public int ModifyId { get; set; }
        public int ProductionProcessId { get; set; }
        public int ProcessStepOrder { get; set; }
        public int ProcessStepId { get; set; }
        public string ProcessStepRequirements { get; set; }
        public decimal ProcessStepRequirementMid { get; set; }
        public string CategoryName { get; set; }
        public string StepName { get; set; }
    }
    public class ProductionProcessStepDetail : ProductionProcessStep
    {
        public string ProductionProcessName { get; set; }
    }
}
