using ModelBase.Base.ServerConfig.Enum;
using Newtonsoft.Json;
using System;

namespace ApiDeviceManagement.Models
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
        public string AdministratorUser { get; set; }
        public string Remark { get; set; }
    }

    public class DeviceLibraryDetail : DeviceLibrary
    {
        public string ModelName { get; set; }
        public string FirmwareName { get; set; }
        public string ProcessName { get; set; }
        public string HardwareName { get; set; }
        public string SiteName { get; set; }
        [JsonIgnore]
        public SocketState State { get; set; } = SocketState.UnInit;

        public string StateStr => State == SocketState.Connected ? "连接正常" : "连接异常";
    }
}
