using System;

namespace ApiManagement.Models.MaterialManagementModel
{
    public class MaterialLog
    {
        public int Id { get; set; }
        public DateTime Time { get; set; }
        public int BillId { get; set; }
        public string Code { get; set; }
        /// <summary>
        /// 1 入库; 2 出库;
        /// </summary>
        public int Type { get; set; }
        public string Purpose { get; set; }
        public int PlanId { get; set; }
        public int Number { get; set; }
        public string RelatedPerson { get; set; }
        public string Manager { get; set; }
    }
}
