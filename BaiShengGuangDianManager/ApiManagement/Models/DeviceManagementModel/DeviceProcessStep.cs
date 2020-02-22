using System;
using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.DeviceManagementModel
{
    public class DeviceProcessStep : CommonBase
    {
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
