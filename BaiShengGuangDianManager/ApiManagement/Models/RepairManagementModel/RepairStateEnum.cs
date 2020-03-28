
using System.ComponentModel;

namespace ApiManagement.Models.RepairManagementModel
{
    /// <summary>
    /// 状态 0 未确认 1 已确认 2 维修中 3 已解决
    /// </summary>
    public enum RepairStateEnum
    {

        [Description("未确认")]
        Default = 0,

        [Description("已确认")]
        Confirm = 1,

        [Description("维修中")]
        Repair = 2,

        [Description("已解决")]
        Complete = 3,
    }

}
