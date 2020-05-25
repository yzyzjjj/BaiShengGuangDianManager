using System;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Models.BaseModel;
using Newtonsoft.Json;
using ServiceStack;

namespace ApiManagement.Models.DeviceManagementModel
{
    public class ScriptVersion : CommonBase
    {
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
        public string ScriptFile { get; set; }
    }
    public class ScriptVersionDetail : ScriptVersion
    {

    }
}
