using Newtonsoft.Json;using ModelBase.Models.BaseModel;

namespace ApiManagement.Models.MaterialManagementModel
{
    public class MaterialPrice
    {
        public int CategoryId { get; set; }
        public string Category { get; set; }
        public int NameId { get; set; }
        public string Name { get; set; }
        public int SupplierId { get; set; }
        public string Supplier { get; set; }
        public string Specification { get; set; }
        public int SpecificationId { get; set; }
        public string Price { get; set; }
    }
}
