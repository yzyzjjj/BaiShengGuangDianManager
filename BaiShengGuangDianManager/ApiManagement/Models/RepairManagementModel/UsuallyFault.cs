using System;
using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.RepairManagementModel
{
    public class UsuallyFault : CommonBase
    {
        public string UsuallyFaultDesc { get; set; }
        public string SolverPlan { get; set; }
    }
}
