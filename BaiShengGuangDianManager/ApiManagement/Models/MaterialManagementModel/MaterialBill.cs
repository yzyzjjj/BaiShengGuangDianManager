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
        public string Price { get; set; }
        public decimal Stock { get; set; }
        public bool UpdateImage { get; set; }
        public string Images { get; set; }
        public string[] ImageList => Images != null ? JsonConvert.DeserializeObject<string[]>(Images) : new string[0];
        public string Remark { get; set; }
    }
    public class MaterialBillDetail : MaterialBill
    {
        public int CategoryId { get; set; }
        public string Category { get; set; }
        public int NameId { get; set; }
        public string Name { get; set; }
        public int SupplierId { get; set; }
        public string Supplier { get; set; }
        public string Specification { get; set; }
        public string Site { get; set; }
    }
}
