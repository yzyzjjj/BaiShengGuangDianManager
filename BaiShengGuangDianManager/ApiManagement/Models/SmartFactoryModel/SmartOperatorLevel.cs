using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartOperatorLevel : CommonBase
    {
        /// <summary>
        /// 车间Id
        /// </summary>
        public int WorkshopId { get; set; }
        /// <summary>
        /// 人员等级
        /// </summary>
        public string Level { get; set; }
        /// <summary>
        /// 顺序 0 最小
        /// </summary>
        public int Order { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
    }
}
