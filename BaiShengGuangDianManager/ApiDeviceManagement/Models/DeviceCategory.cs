using System;

namespace ApiDeviceManagement.Models
{
    public class DeviceCategory
    {
        public int Id { get; set; }
        public string CreateUserId { get; set; }
        public DateTime MarkedDateTime { get; set; }
        public bool MarkedDelete { get; set; }
        public int ModifyId { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }
    }
}
