using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartDevice : CommonBase
    {
        /// <summary>
        /// 机台号
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// 设备类别
        /// </summary>
        public int CategoryId { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
    }

    public class SmartDeviceDetail : SmartDevice
    {
        /// <summary>
        /// 设备类别
        /// </summary>
        public string Category { get; set; }
    }

}
