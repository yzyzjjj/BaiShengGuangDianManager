using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.BaseModel;
using Newtonsoft.Json;
using System;
using System.ComponentModel;

namespace ApiManagement.Models.DeviceManagementModel
{
    public class DeviceLibrary : CommonBase
    {
        /// <summary>
        /// 车间Id
        /// </summary>
        public int WorkshopId { get; set; }
        /// <summary>
        /// 设备编码或机台号
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// 设备名称
        /// </summary>
        public string DeviceName { get; set; }
        /// <summary>
        /// 设备MAC地址
        /// </summary>
        public string MacAddress { get; set; }
        /// <summary>
        /// 设备IP地址
        /// </summary>
        public string Ip { get; set; }
        /// <summary>
        /// 设备端口
        /// </summary>
        public int Port { get; set; }
        /// <summary>
        /// 设备唯一识别码
        /// </summary>
        public string Identifier { get; set; }
        /// <summary>
        /// 设备分类
        /// </summary>
        public int ClassId { get; set; }
        /// <summary>
        /// 设备型号编号
        /// </summary>
        public int DeviceModelId { get; set; }
        /// <summary>
        /// 流程脚本版本编号
        /// </summary>
        public int ScriptId { get; set; }
        /// <summary>
        /// 设备固件版本编号
        /// </summary>
        public int FirmwareId { get; set; }
        /// <summary>
        /// 设备硬件版本编号
        /// </summary>
        public int HardwareId { get; set; }
        /// <summary>
        /// 应用层版本编号
        /// </summary>
        public int ApplicationId { get; set; }
        /// <summary>
        /// 设备所在场地编号
        /// </summary>
        public int SiteId { get; set; }
        /// <summary>
        /// 设备管理员用户编号
        /// </summary>
        public string Administrator { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
        /// <summary>
        /// 图标
        /// </summary>
        public string Icon { get; set; }

        public string AdministratorName { get; set; }
        public string Phone { get; set; }

    }

    public class DeviceLibraryDetail : DeviceLibrary
    {
        public string Class { get; set; }
        public int DeviceCategoryId { get; set; }
        public string ModelName { get; set; }
        public string CategoryName { get; set; }
        public string FirmwareName { get; set; }
        public string ApplicationName { get; set; }
        public string HardwareName { get; set; }
        public string WorkshopName { get; set; }
        public string Region { get; set; }
        public string ScriptName { get; set; }
        /// <summary>
        /// 状态 0 已报修 1 已确认 2 维修中
        /// </summary>
        [JsonIgnore]
        public int RepairState { get; set; } = -1;
        /// <summary>
        /// 加工类型
        /// </summary>
        public ProcessType ProcessType { get; set; }
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
                        //case DeviceState.Readying: return "准备中";
                        //case DeviceState.Waiting: return "待机";
                        //case DeviceState.Processing: return "加工中";
                        //case DeviceState.UpgradeScript: return "流程升级中";
                        //case DeviceState.UpgradeFirmware: return "固件升级中";
                        //case DeviceState.Restart: return "设备重启中";
                        case DeviceState.Readying:
                        case DeviceState.Waiting:
                        case DeviceState.Processing:
                        case DeviceState.UpgradeScript:
                        case DeviceState.UpgradeFirmware:
                        case DeviceState.Restart:
                            return DeviceState.GetAttribute<DescriptionAttribute>()?.Description ?? "";
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

                if (State == SocketState.Connected && DeviceState == DeviceState.UnInit)
                {
                    return "数据待解析中";
                }
                return State == SocketState.Connected ? "数据异常" : "连接异常";
            }
        }

        /// <summary>
        /// 当前流程卡
        /// </summary>
        public int FlowCardId { get; set; }
        /// <summary>
        /// 当前流程卡
        /// </summary>
        public string FlowCard { get; set; } = "";
        /// <summary>
        /// 上次加工流程卡
        /// </summary>
        public int LastFlowCardId { get; set; }
        /// <summary>
        /// 上次加工流程卡
        /// </summary>
        public string LastFlowCard { get; set; } = "";
        /// <summary>
        /// 当前加工计划号
        /// </summary>
        public string Production { get; set; } = string.Empty;
        /// <summary>
        /// 已加工时间
        /// </summary>
        public string ProcessTime { get; set; }
        /// <summary>
        /// 剩余加工时间
        /// </summary>
        public string LeftTime { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; }/// <summary>
                                               /// 本次过程已经历时间
                                               /// </summary>
        public int TotalTime => StartTime != default(DateTime) ? (int)(DateTime.Now - StartTime).TotalSeconds : 0;
    }
}
