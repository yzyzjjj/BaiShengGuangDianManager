using System;
using System.Collections.Generic;

namespace ApiDeviceManagement.Models
{
    public class NpcProxyLink
    {
        public int Id { get; set; }
        public int DeviceId { get; set; }
        public int ServerId { get; set; }
        public int GroupId { get; set; }
        public short Monitoring { get; set; }
        public int Frequency { get; set; }
        public string Instruction { get; set; }
        public short Storage { get; set; }
    }
}
