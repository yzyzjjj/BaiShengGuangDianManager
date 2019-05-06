using System;
using Newtonsoft.Json;

namespace ApiManagement.Models
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
        public int ValueNumber { get; set; }
        public int InputNumber { get; set; }
        public int OutputNumber { get; set; }
        [JsonIgnore]
        public string HeartPacket { get; set; }
    }
    public class ScriptVersionDetail: ScriptVersion
    {
        public string ModelName { get; set; }
    }
}
