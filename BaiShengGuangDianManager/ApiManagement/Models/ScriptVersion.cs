using Newtonsoft.Json;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models
{
    public class ScriptVersion
    {
        public int Id { get; set; }
        public string CreateUserId { get; set; }
        public DateTime MarkedDateTime { get; set; }
        public bool MarkedDelete { get; set; }
        public int ModifyId { get; set; }
        public string DeviceModelId { get; set; }
        [JsonIgnore]
        public IEnumerable<int> DeviceModelIds => DeviceModelId.IsNullOrEmpty() ? new List<int>() : DeviceModelId.Split(",").Select(int.Parse);

        public string ScriptName { get; set; }
        public int ValueNumber { get; set; }
        public int InputNumber { get; set; }
        public int OutputNumber { get; set; }
        public int MaxValuePointerAddress { get; set; }
        public int MaxInputPointerAddress { get; set; }
        public int MaxOutputPointerAddress { get; set; }
        [JsonIgnore]
        public string HeartPacket { get; set; }
    }
    public class ScriptVersionDetail : ScriptVersion
    {

    }
}
