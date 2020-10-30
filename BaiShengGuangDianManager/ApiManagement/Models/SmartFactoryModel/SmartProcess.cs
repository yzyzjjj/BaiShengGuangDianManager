using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartProcess : CommonBase
    {
        /// <summary>
        /// 流程
        /// </summary>
        public string Process { get; set; }
        /// <summary>
        /// 设备类型
        /// </summary>
        public int DeviceCategoryId { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
    }

    public class SmartProcessDetail : SmartProcess
    {
        /// <summary>
        /// 设备类别
        /// </summary>
        public string DeviceCategory { get; set; }
    }
}
