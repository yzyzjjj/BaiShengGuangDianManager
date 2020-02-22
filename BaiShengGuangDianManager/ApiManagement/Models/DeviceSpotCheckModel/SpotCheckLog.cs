using System;
using Newtonsoft.Json;

namespace ApiManagement.Models.DeviceSpotCheckModel
{
    public class SpotCheckLog : SpotCheckDeviceDetail
    {
        public DateTime CheckTime { get; set; }
        public int Actual { get; set; }
        public string Desc { get; set; }
        public string Images { get; set; }
        public string[] ImageList => Images != null ? JsonConvert.DeserializeObject<string[]>(Images) : new string[0];
        public bool Check { get; set; }
    }
}