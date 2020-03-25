using System.Collections.Generic;
using ApiManagement.Models.BaseModel;
using ApiManagement.Models._6sModel;

namespace ApiManagement.Models.ManufactureModel
{
    /// <summary>
    /// 生产检验
    /// </summary>
    public class ManufactureCheck : CommonBase
    {
        public string Check { get; set; }
    }
    public class ManufactureCheckItems : ManufactureCheck
    {
        public IEnumerable<ManufactureCheckItem> Items { get; set; }
    }
    public class ManufactureCheckCopy : ManufactureCheck
    {
        public int CopyId { get; set; }
    }
}
