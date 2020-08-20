using System.Collections.Generic;
using ModelBase.Models.Result;

namespace ApiManagement.Models.MaterialManagementModel
{
    public class MaterialDataResult : DataResult
    {
        public decimal Count { get; set; }
        public decimal Sum { get; set; }
    }
    public class MaterialChooseResult : Result
    {
        public List<dynamic> Categories = new List<dynamic>();
        public List<dynamic> Names = new List<dynamic>();
        public List<dynamic> Suppliers = new List<dynamic>();
        public List<dynamic> Specifications = new List<dynamic>();
        public List<dynamic> Sites = new List<dynamic>();
    }
}