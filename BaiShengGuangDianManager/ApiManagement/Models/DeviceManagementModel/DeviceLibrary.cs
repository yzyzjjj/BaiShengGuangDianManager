using System;
using ApiManagement.Models.BaseModel;
using ModelBase.Base.EnumConfig;
using Newtonsoft.Json;

namespace ApiManagement.Models.DeviceManagementModel
{
    public class DeviceLibrary : CommonBase
    {
        public string Code { get; set; }
        public string DeviceName { get; set; }
        public string MacAddress { get; set; }
        public string Ip { get; set; }
        public int Port { get; set; }
        public string Identifier { get; set; }
        public int DeviceModelId { get; set; }
        public int FirmwareId { get; set; }
        public int HardwareId { get; set; }
        public int ApplicationId { get; set; }
        public int SiteId { get; set; }
        public int ScriptId { get; set; }
        public string Administrator { get; set; }
        public string AdministratorName { get; set; }
        public string Remark { get; set; }

    }

    public class DeviceLibraryDetail : DeviceLibrary
    {
        public int DeviceCategoryId { get; set; }
        public string ModelName { get; set; }
        public string CategoryName { get; set; }
        public string FirmwareName { get; set; }
        public string ApplicationName { get; set; }
        public string HardwareName { get; set; }
        public string SiteName { get; set; }
        public string RegionDescription { get; set; }
        public string ScriptName { get; set; }
        /// <summary>
        /// 状态 0 已报修 1 已确认 2 维修中
        /// </summary>
        [JsonIgnore]
        public int RepairState { get; set; } = -1;
        [JsonIgnore]
        public SocketState State { get; set; } = SocketState.UnInit;
        public string StateStr => State == SocketState.Connected ? "连接正常" : "连接异常";
        [JsonIgnore]
        public DeviceState DeviceState { get; set; } = DeviceState.UnInit;

        public string DeviceStateStr
        {
            get
            {
                if (RepairState == -1)
                {
                    switch (DeviceState)
                    {
                        case DeviceState.Waiting: return "待加工";
                        case DeviceState.Processing: return "加工中";
                    }
                }
                else
                {
                    switch (RepairState)
                    {
                        case 0: return "已报修";
                        case 1: return "已确认";
                        case 2: return "维修中";
                    }
                }
                return State == SocketState.Connected ? "数据异常" : "连接异常";
            }
        }

        /// <summary>
        /// 当前加工流程卡号
        /// </summary>
        public string FlowCard { get; set; } = string.Empty;

        /// <summary>
        /// 加工时间
        /// </summary>
        public string ProcessTime { get; set; }
        /// <summary>
        /// 剩余加工时间
        /// </summary>
        public string LeftTime { get; set; }
    }
}
