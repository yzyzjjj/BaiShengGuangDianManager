using ApiManagement.Models.BaseModel;
using ServiceStack.DataAnnotations;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartWorkshop : CommonBase
    {
        /// <summary>
        /// 车间
        /// </summary>
        public string Workshop { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
        /// <summary>
        /// 刷新频率
        /// </summary>
        public int Frequency { get; set; }
        /// <summary>
        /// 显示条数
        /// </summary>
        public int Length { get; set; } = 20;
        /// <summary>
        /// 0 秒  1 分  2 小时
        /// </summary>
        public SmartKanBanUnit Unit { get; set; }
    }
    public class SmartWorkshopDetail : SmartWorkshop
    {
    }
}
