using System;
using ModelBase.Models.BaseModel;

namespace ApiManagement.Models.RepairManagementModel
{
    public class UsuallyFault : CommonBase
    {
        public string UsuallyFaultDesc { get; set; }
        public string SolvePlan { get; set; }
    }
}
