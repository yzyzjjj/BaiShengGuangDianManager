using System;
using ModelBase.Models.BaseModel;

namespace ApiManagement.Models.DeviceManagementModel
{
    public class FirmwareLibrary : CommonBase
    {
        public string FirmwareName { get; set; }
        public int VarNumber { get; set; }
        public string CommunicationProtocol { get; set; }
        public string FilePath { get; set; }
        public string Description { get; set; }
    }
}
