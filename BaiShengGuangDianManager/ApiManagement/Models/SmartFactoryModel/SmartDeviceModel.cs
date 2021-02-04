using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartDeviceModel : CommonBase
    {
        /// <summary>
        /// 车间Id
        /// </summary>
        public int WorkshopId { get; set; }
        /// <summary>
        /// 设备类别id
        /// </summary>
        public int CategoryId { get; set; }
        /// <summary>
        /// 设备型号
        /// </summary>
        public string Model { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
    }

    public class SmartDeviceModelDetail : SmartDeviceModel
    {
        /// <summary>
        /// 设备类别
        /// </summary>
        public string Category { get; set; }
    }
}