using System;

namespace ApiFlowCardManagement.Models
{
    public class ProcessStep
    {
        public int Id { get; set; }
        public string CreateUserId { get; set; }
        public DateTime MarkedDateTime { get; set; }
        public bool MarkedDelete { get; set; }
        public int ModifyId { get; set; }
        public int ProductionProcessId { get; set; }
        public int ProcessStepOrder { get; set; }
        public string ProcessStepName { get; set; }
        public string ProcessSteprequirements { get; set; }
        public int ProcessorId { get; set; }
        public DateTime ProcessorTime { get; set; }
        public int SurveyorId { get; set; }
        public DateTime SurveyTime { get; set; }
        public int QualifiedNumber { get; set; }
        public int UnqualifiedNumber { get; set; }
        public int DeviceId { get; set; }

    }

    public class ProcessStepDetail : ProcessStep
    {
        public string ProcessorName { get; set; }
        public string SurveyorName { get; set; }
        public string Code { get; set; } = string.Empty;
    }

}
