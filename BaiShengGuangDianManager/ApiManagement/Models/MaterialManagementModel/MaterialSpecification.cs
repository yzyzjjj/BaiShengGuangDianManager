using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.MaterialManagementModel
{
    public class MaterialSpecification : CommonBase
    {
        public int SupplierId { get; set; }
        public string Specification { get; set; }
        public string Remark { get; set; }
    }
    public class MaterialSpecificationDetail : MaterialSpecification
    {
        public int CategoryId { get; set; }
        public string Category { get; set; }
        public int NameId { get; set; }
        public string Name { get; set; }
        public string Supplier { get; set; }
    }
}
