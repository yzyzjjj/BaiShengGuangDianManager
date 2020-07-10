using ApiManagement.Models.RepairManagementModel;
using ModelBase.Base.Utils;
using System.Collections.Generic;
using System.ComponentModel;

namespace ApiManagement.Models.Notify
{
    /// <summary>
    /// 0 不通知 1 主群 2 维修群 3 测试群  
    /// </summary>
    public enum NotifyTypeEnum
    {
        Default,
        Main,
        Repair,
        Test,
    }

    /// <summary>
    /// 0 未知 1 钉钉 2 企业微信 
    /// </summary>
    public enum NotifyPlatformEnum
    {
        Default,
        DingDing,
        WeiXin,
    }

    /// <summary>
    /// 钉钉msgtype
    /// https://ding-doc.dingtalk.com/doc#/serverapi2/qf2nxq
    /// </summary>
    public enum NotifyMsgTypeEnum
    {
        text,
        link,
        markdown,
        actionCard,
        feedCard,
    }
    /// <summary>
    /// 0 默认 1 普通消息 2 故障上报提醒 3 故障指派提醒
    /// </summary>
    public enum NotifyMsgEnum
    {
        Default,
        [Description("普通消息")]
        Common,
        [Description("故障上报")]
        FaultReport,
        [Description("故障指派")]
        FaultAssign,
    }

    public class NotifyFormat
    {
        private static readonly Dictionary<NotifyMsgEnum, string> NotifyFormats = new Dictionary<NotifyMsgEnum, string>
        {
            { NotifyMsgEnum.Default, "{0}"},
            { NotifyMsgEnum.Common,"{0}"},
            { NotifyMsgEnum.FaultReport, "故障设备：{0}，故障时间：{1}，报修人：{2}，故障类型：{3}"},
            { NotifyMsgEnum.FaultAssign, "故障设备：{0}，故障时间：{1}，报修人：{2}，故障类型：{3}，优先级：{4}，故障等级：{5}"},
        };

        public static string Format(NotifyMsgEnum msgEnum)
        {
            return NotifyFormats.ContainsKey(msgEnum) ? NotifyFormats[msgEnum] : "";
        }

        public static string Format(FaultDeviceDetail faultDevice, NotifyMsgEnum msgEnum)
        {
            switch (msgEnum)
            {
                case NotifyMsgEnum.FaultReport:
                    return string.Format(Format(msgEnum), faultDevice.DeviceCode, faultDevice.FaultTime.ToStr(), faultDevice.Proposer, faultDevice.FaultTypeName);
                case NotifyMsgEnum.FaultAssign:
                    return string.Format(Format(msgEnum), faultDevice.DeviceCode, faultDevice.FaultTime.ToStr(), faultDevice.Proposer, faultDevice.FaultTypeName,
                        faultDevice.Priority == 0 ? "低" : (faultDevice.Priority == 1 ? "中" : "高"), faultDevice.Grade == 0 ? "小修" : "大修");
            }

            return "";
        }
    }
}
