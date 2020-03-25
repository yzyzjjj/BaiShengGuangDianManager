using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.ManufactureModel
{
    /// <summary>
    /// 生产检验配置
    /// </summary>
    public class ManufactureCheckItem : CommonBase
    {
        public int CheckId { get; set; }
        public string Item { get; set; }
        public string Method { get; set; }
    }
    public class ManufactureCheckItemDetail : ManufactureCheckItem
    {
        public string Check { get; set; }
    }
}
