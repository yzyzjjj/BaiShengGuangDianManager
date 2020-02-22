using System;
using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.DeviceManagementModel
{
    public class DeviceModel : CommonBase
    {
        public int DeviceCategoryId { get; set; }
        public string ModelName { get; set; }
        public string Description { get; set; }
    }

   public class DeviceModelDetail: DeviceModel
    {
        public string CategoryName { get; set; }
    }

}
