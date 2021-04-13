using ModelBase.Models.BaseModel;

namespace ApiManagement.Models.MaterialManagementModel
{
    public class MaterialName : CommonBase
    {
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public string Remark { get; set; }
    }

    public class MaterialNameDetail : MaterialName
    {
        public string Category { get; set; }
    }
}
