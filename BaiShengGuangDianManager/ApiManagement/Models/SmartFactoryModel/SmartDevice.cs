using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartDevice : CommonBase
    {
        /// <summary>
        /// 车间Id
        /// </summary>
        public int WorkshopId { get; set; }
        /// <summary>
        /// 全部 = 0,正常 = 1,故障 = 2,报废 = 3,
        /// </summary>
        public SmartDeviceState State { get; set; }
        public string StateStr => State.ToString();
        /// <summary>
        /// 机台号
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// 设备类别
        /// </summary>
        public int CategoryId { get; set; }
        /// <summary>
        /// 设备类别
        /// </summary>
        public int ModelId { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
        /// <summary>
        /// 被使用优先级/好用等级  越小越好
        /// </summary>
        public int Priority { get; set; }
    }

    public class SmartDeviceDetail : SmartDevice
    {
        /// <summary>
        /// 设备类别
        /// </summary>
        public string Category { get; set; }
        /// <summary>
        /// 设备型号
        /// </summary>
        public string Model { get; set; }
    }

    public class SmartDeviceProcess : SmartDevice
    {
        /// <summary>
        /// 设备流程
        /// </summary>
        public string Process { get; set; }
        /// <summary>
        /// 设备型号
        /// </summary>
        public string Model { get; set; }
    }

    public class SmartDeviceModelCount
    {
        /// <summary>
        /// 设备类别id
        /// </summary>
        public int CategoryId { get; set; }
        /// <summary>
        /// 设备型号
        /// </summary>
        public int ModelId { get; set; }
        /// <summary>
        /// 数量
        /// </summary>
        public int Count { get; set; }
    }
}
