using ApiManagement.Models.BaseModel;
using Newtonsoft.Json;

namespace ApiManagement.Models.MaterialManagementModel
{
    public class MaterialBill : CommonBase
    {
        public int SpecificationId { get; set; }
        public int SiteId { get; set; }
        public string Code { get; set; }
        public string Unit { get; set; }
        public decimal Price { get; set; }
        public decimal Stock { get; set; }
        public bool UpdateImage { get; set; }
        public string Images { get; set; }
        public string[] ImageList => Images != null ? JsonConvert.DeserializeObject<string[]>(Images) : new string[0];
        public string Remark { get; set; }
    }
    public class MaterialBillDetail : MaterialBill
    {
        public int CategoryId { get; set; }
        /// <summary>
        /// 类型
        /// </summary>
        public string Category { get; set; }
        public int NameId { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        public int SupplierId { get; set; }
        /// <summary>
        /// 供应商
        /// </summary>
        public string Supplier { get; set; }
        /// <summary>
        /// 规格型号
        /// </summary>
        public string Specification { get; set; }
        /// <summary>
        /// 场地
        /// </summary>
        public string Site { get; set; }
    }
}
