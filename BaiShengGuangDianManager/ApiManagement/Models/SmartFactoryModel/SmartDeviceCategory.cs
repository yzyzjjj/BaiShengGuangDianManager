using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartDeviceCategory : CommonBase
    {
        /// <summary>
        /// 车间Id
        /// </summary>
        public int WorkshopId { get; set; }
        /// <summary>
        /// 设备类别
        /// </summary>
        public string Category { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
    }
}