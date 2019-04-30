using System;

namespace ApiFlowCardManagement.Models
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
        public string ProcessStepName { get; set; }
        public string ProcessStepRequirements { get; set; }
    }
}
