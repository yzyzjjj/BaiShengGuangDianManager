using ApiManagement.Models.BaseModel;
using System.Collections.Generic;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartProcessCodeCategory : CommonBase
    {
        /// <summary>
        /// 车间Id
        /// </summary>
        public int WorkshopId { get; set; }
        /// <summary>
        /// 流程编号类别
        /// </summary>
        public string Category { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
        /// <summary>
        /// 标准流程
        /// </summary>
        public string List { get; set; }
    }

    public class SmartProcessCodeCategoryDetail : SmartProcessCodeCategory
    {
        /// <summary>
        /// 标准流程
        /// </summary>
        public IEnumerable<SmartProcessCodeCategoryProcess> Processes { get; set; }
    }
}
