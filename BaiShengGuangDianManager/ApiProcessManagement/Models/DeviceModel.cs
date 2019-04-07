using System;

namespace ApiProcessManagement.Models
{
    public class DeviceModel
    {
        public int Id { get; set; }
        public string CreateUserId { get; set; }
        public DateTime MarkedDateTime { get; set; }
        public bool MarkedDelete { get; set; }
        public int ModifyId { get; set; }
        public int DeviceCategoryId { get; set; }
        public string ModelName { get; set; }
        public string Description { get; set; }
    }
}
