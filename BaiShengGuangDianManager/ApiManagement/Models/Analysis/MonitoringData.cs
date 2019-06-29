using System;
using Newtonsoft.Json;

namespace ApiManagement.Models.Analysis
{
    public class MonitoringData
    {
        public int Id { get; set; }
        public DateTime SendTime { get; set; }
        public DateTime ReceiveTime { get; set; }
        public int DealTime { get; set; }
        public int DeviceId { get; set; }
        public string Ip { get; set; }
        public int Port { get; set; }
        public string Data { get; set; }
        public bool UserSend { get; set; }
        public int ScriptId { get; set; }
        public int ValNum { get; set; }
        public int InNum { get; set; }
        public int OutNum { get; set; }
        public DeviceData AnalysisData { get; set; }
    }
}
