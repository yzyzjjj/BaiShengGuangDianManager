using System;
using ModelBase.Models.BaseModel;

namespace ApiManagement.Models.DeviceManagementModel
{
    public class DeviceCategory : CommonBase
    {
        public string CategoryName { get; set; }
        public string Description { get; set; }
    }
}
