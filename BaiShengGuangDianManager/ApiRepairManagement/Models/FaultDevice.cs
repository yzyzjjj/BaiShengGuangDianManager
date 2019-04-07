using System;
using System.Collections.Generic;

namespace ApiRepairManagement.Models
{
    public partial class FaultDevice
    {
        public int Id { get; set; }
        public string CreateUserId { get; set; }
        public DateTime MarkedDateTime { get; set; }
        public byte MarkedDelete { get; set; }
        public int ModifyId { get; set; }
        public string DeviceCode { get; set; }
        public DateTime FaultTime { get; set; }
        public string Proposer { get; set; }
        public string FaultDescription { get; set; }
        public int Priority { get; set; }
    }
}
