using System;

namespace ApiProcessManagement.Models
{
    public class DeviceLibrary
    {
        public int Id { get; set; }
        public string CreateUserId { get; set; }
        public DateTime MarkedDateTime { get; set; }
        public bool MarkedDelete { get; set; }
        public int ModifyId { get; set; }
        public string Code { get; set; }
        public string DeviceName { get; set; }
        public string MacAddress { get; set; }
        public string Ip { get; set; }
        public int Port { get; set; }
        public string Identifier { get; set; }
        public int DeviceModelId { get; set; }
        public int FirmwareId { get; set; }
        public int ProcessId { get; set; }
        public int HardwareId { get; set; }
        public int SiteId { get; set; }
        public string AdministratorUserId { get; set; }
        public string Remark { get; set; }

    }
}
