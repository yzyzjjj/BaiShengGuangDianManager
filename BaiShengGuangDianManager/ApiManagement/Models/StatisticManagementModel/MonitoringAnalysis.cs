using System;
using ModelBase.Models.Device;
using Newtonsoft.Json;using ModelBase.Models.BaseModel;

namespace ApiManagement.Models.StatisticManagementModel
{
    public class MonitoringAnalysis
    {
        public int Id { get; set; }
        public DateTime SendTime { get; set; }
        public int DeviceId { get; set; }
        public int ScriptId { get; set; }
        public string Ip { get; set; }
        public int Port { get; set; }
        public string Data { get; set; }
        public DeviceData AnalysisData => JsonConvert.DeserializeObject<DeviceData>(Data);
    }
}
