using ModelBase.Models.BaseModel;

namespace ApiManagement.Models.DeviceManagementModel
{
    public class HardwareLibrary : CommonBase
    {
        public string HardwareName { get; set; }
        public int InputNumber { get; set; }
        public int OutputNumber { get; set; }
        public int DacNumber { get; set; }
        public int AdcNumber { get; set; }
        public int AxisNumber { get; set; }
        public int ComNumber { get; set; }
        public string Description { get; set; }
    }
}
