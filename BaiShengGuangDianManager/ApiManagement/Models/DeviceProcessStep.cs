using System;

namespace ApiManagement.Models
{
    public class DeviceProcessStep
    {
        public int Id { get; set; }
        public string CreateUserId { get; set; }
        public DateTime MarkedDateTime { get; set; }
        public bool MarkedDelete { get; set; }
        public int ModifyId { get; set; }
        public int DeviceCategoryId { get; set; }
        public string StepName { get; set; }
        public string Description { get; set; }
        public bool IsSurvey { get; set; }
    }

    public class DeviceProcessStepDetail : DeviceProcessStep
    {
        public string CategoryName { get; set; }
    }
}
