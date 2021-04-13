using System;
using ModelBase.Models.BaseModel;

namespace ApiManagement.Models.DeviceManagementModel
{
    public class ApplicationLibrary : CommonBase
    {
        public string ApplicationName { get; set; }
        public string FilePath { get; set; }
        public string Description { get; set; }
    }
}
