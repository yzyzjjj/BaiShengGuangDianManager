using System;
using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.FlowCardManagementModel
{
    public class FlowCardProcessStep : CommonBase
    {
        public int FlowCardId { get; set; }
        public int ProcessStepOrder { get; set; }
        public int ProcessStepId { get; set; }
        public string ProcessStepRequirements { get; set; }
        public decimal ProcessStepRequirementMid { get; set; }
        public int ProcessorId { get; set; }
        public DateTime ProcessTime { get; set; }
        public DateTime ProcessEndTime { get; set; }
        public int SurveyorId { get; set; }
        public DateTime SurveyTime { get; set; }
        public int QualifiedNumber { get; set; }
        public int UnqualifiedNumber { get; set; }
        public int DeviceId { get; set; }
        public bool IsReport { get; set; }
        public string QualifiedRange { get; set; }
        public decimal QualifiedMode { get; set; }
    }

    public class FlowCardProcessStepDetail : FlowCardProcessStep
    {
        public bool IsSurvey { get; set; }
        public string CategoryName { get; set; }
        public string StepName { get; set; }
        public string ProcessorName { get; set; }
        public string SurveyorName { get; set; }
        public string Code { get; set; } = string.Empty;
        public string ProcessStepOrderName { get; set; }
    }

}
