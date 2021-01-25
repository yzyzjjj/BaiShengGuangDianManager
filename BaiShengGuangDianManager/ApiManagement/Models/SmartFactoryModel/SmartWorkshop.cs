using ApiManagement.Models.BaseModel;

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
    }
    public class SmartWorkshopDetail : SmartWorkshop
    {
    }
}
