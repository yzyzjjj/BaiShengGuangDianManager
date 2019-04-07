using System;
using System.Collections.Generic;

namespace ApiDeviceManagement.Models
{
    public class HardwareLibrary
    {
        public int Id { get; set; }
        public string CreateUserId { get; set; }
        public DateTime MarkedDateTime { get; set; }
        public bool MarkedDelete { get; set; }
        public int ModifyId { get; set; }
        public string HardwareName { get; set; }
        public int InputNumber { get; set; }
        public int OutputNumber { get; set; }
        public int DacNumber { get; set; }
        public int AdcNumber { get; set; }
        public int AxisNumber { get; set; }
        public int ComNumber { get; set; }
        public string Description { get; set; }
    }
}
