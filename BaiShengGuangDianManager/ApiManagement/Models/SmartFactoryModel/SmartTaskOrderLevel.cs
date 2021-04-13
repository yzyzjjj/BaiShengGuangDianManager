using ModelBase.Models.BaseModel;
namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartTaskOrderLevel : CommonBase
    {
        /// <summary>
        /// 车间Id
        /// </summary>
        public int WorkshopId { get; set; }
        /// <summary>
        /// 任务等级
        /// </summary>
        public string Level { get; set; }
        /// <summary>
        /// 顺序 0 最大   默认9999
        /// </summary>
        public int Order { get; set; } = 9999;
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
    }
}
