using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ApiManagement.Models.MaterialManagementModel
{
    public class MaterialManagement : MaterialBill
    {
        public int BillId { get; set; }
        public DateTime InTime { get; set; }
        public DateTime OutTime { get; set; }
        public decimal Number { get; set; }

    }
    public class MaterialManagementDetail : MaterialManagement
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
    public class OpMaterialManagement : MaterialManagementDetail
    {
        public int Type { get; set; }
        public string Purpose { get; set; }
        public int PlanId { get; set; }
        public string RelatedPerson { get; set; }
    }

    public class IncreaseMaterialManagementDetail
    {
        public IEnumerable<OpMaterialManagement> Bill { get; set; }
    }
    public class ConsumeMaterialManagementDetail
    {
        public int PlanId { get; set; }
        [JsonIgnore]
        public string Plan { get; set; }
        public IEnumerable<OpMaterialManagement> Bill { get; set; }
    }
}
