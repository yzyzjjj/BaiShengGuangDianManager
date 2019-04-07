using System;

namespace ApiDeviceManagement.Models
{
    public class ScriptVersion
    {
        public int Id { get; set; }
        public string CreateUserId { get; set; }
        public DateTime MarkedDateTime { get; set; }
        public bool MarkedDelete { get; set; }
        public int ModifyId { get; set; }
        public int DeviceModelId { get; set; }
        public string ScriptName { get; set; }

    }
}
