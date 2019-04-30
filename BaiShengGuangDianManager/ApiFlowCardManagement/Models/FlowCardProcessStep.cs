using System;

namespace ApiFlowCardManagement.Models
{
    public class FlowCardProcessStep
    {
        public int Id { get; set; }
        public string CreateUserId { get; set; }
        public DateTime MarkedDateTime { get; set; }
        public bool MarkedDelete { get; set; }
        public int ModifyId { get; set; }
        public int FlowCardId { get; set; }
        public int ProcessStepOrder { get; set; }
        public string ProcessStepName { get; set; }
        public string ProcessStepRequirements { get; set; }
        public int ProcessorId { get; set; }
        public DateTime ProcessTime { get; set; }
        public int SurveyorId { get; set; }
        public DateTime SurveyTime { get; set; }
        public int QualifiedNumber { get; set; }
        public int UnqualifiedNumber { get; set; }
        public int DeviceId { get; set; }

    }

    public class FlowCardProcessStepDetail : FlowCardProcessStep
    {
        public string ProcessorName { get; set; }
        public string SurveyorName { get; set; }
        public string Code { get; set; } = string.Empty;
    }

}
