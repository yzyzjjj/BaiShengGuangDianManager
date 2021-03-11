using System.ComponentModel;

namespace ApiManagement.Models.DeviceManagementModel
{
    /// <summary>
    /// 设备启动类型
    /// </summary>
    public enum ProcessType
    {
        /// <summary>
        /// 无
        /// </summary>
        [Description("待机")]
        Idle = 0,
        /// <summary>
        /// 加工
        /// </summary>
        [Description("加工")]
        Process,
        /// <summary>
        /// 洗盘
        /// </summary>
        [Description("洗盘")]
        Wash,
        /// <summary>
        /// 修盘
        /// </summary>
        [Description("修盘")]
        Repair,
        /// <summary>
        /// 换沙
        /// </summary>
        [Description("换沙")]
        Sand,
    }
}