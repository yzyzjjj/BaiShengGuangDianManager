using System;
using ModelBase.Models.BaseModel;

namespace ApiManagement.Models.RepairManagementModel
{
    public class FaultType : CommonBase
    {
        public string FaultTypeName { get; set; }
        public string FaultDescription { get; set; }
    }
}
