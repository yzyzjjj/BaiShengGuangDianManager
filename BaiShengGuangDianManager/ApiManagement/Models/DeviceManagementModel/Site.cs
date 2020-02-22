using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.DeviceManagementModel
{
    public class Site : CommonBase
    {
        public string SiteName { get; set; }
        public string RegionDescription { get; set; }
        public string Manager { get; set; }
    }
}
