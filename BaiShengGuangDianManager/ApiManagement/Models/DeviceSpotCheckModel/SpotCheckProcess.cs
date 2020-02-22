using System.Collections.Generic;

namespace ApiManagement.Models.DeviceSpotCheckModel
{
    public class SpotCheckProcess
    {
        public SpotCheckProcess(int deviceId)
        {
            DeviceId = deviceId;
            Data = new List<SpotCheckProcessInfo>();
        }
        public int DeviceId { get; set; }
        public List<SpotCheckProcessInfo> Data { get; set; }
    }

    public class SpotCheckProcessInfo
    {
        public int PlanId { get; set; }
        public string Plan { get; set; }
        public int Total { get; set; }
        public int Done { get; set; }
        public int NotPass { get; set; }
        public int Pass { get; set; }
    }
}