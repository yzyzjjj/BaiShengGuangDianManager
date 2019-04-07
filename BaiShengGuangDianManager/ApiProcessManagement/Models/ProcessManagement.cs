using System;

namespace ApiProcessManagement.Models
{
    public class ProcessManagement
    {
        public int Id { get; set; }
        public string CreateUserId { get; set; }
        public DateTime MarkedDateTime { get; set; }
        public bool MarkedDelete { get; set; }
        public int ModifyId { get; set; }
        /// <summary>
        /// 工艺编号
        /// </summary>
        public string ProcessNumber { get; set; } = string.Empty;
        /// <summary>
        /// 适用设备型号（自增Id, 英文逗号隔开）
        /// </summary>
        public string DeviceModels { get; set; }
        /// <summary>
        /// 适用产品型号（计划号）（自增Id, 英文逗号隔开）
        /// </summary>
        public string ProductModels { get; set; }
        /// <summary>
        /// 适用机台号（自增Id, 英文逗号隔开）
        /// </summary>
        public string DeviceIds { get; set; }
    }

    public class ProcessManagementDetail : ProcessManagement
    {
        /// <summary>
        /// 适用设备型号 英文逗号隔开
        /// </summary>
        public string ModelName { get; set; } = string.Empty;
        /// <summary>
        /// 适用产品型号 英文逗号隔开
        /// </summary>
        public string ProductionProcessName { get; set; } = string.Empty;
        /// <summary>
        /// 适用机台号 英文逗号隔开
        /// </summary>
        public string Code { get; set; } = string.Empty;
    }
}
