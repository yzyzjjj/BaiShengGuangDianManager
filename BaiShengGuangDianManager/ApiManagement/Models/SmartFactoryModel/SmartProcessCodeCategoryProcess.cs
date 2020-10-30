using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartProcessCodeCategoryProcess : CommonBase
    {
        /// <summary>
        /// 流程编号类型id
        /// </summary>
        public int ProcessCodeCategoryId { get; set; }
        /// <summary>
        /// 顺序
        /// </summary>
        public int Order { get; set; }
        /// <summary>
        /// 流程id
        /// </summary>
        public int ProcessId { get; set; }
    }

    public class SmartProcessCodeCategoryProcessDetail : SmartProcessCodeCategoryProcess
    {
        /// <summary>
        /// 流程
        /// </summary>
        public string Process { get; set; }
    }

}
