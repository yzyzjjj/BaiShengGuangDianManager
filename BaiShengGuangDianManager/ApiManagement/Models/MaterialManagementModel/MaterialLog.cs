using System;

namespace ApiManagement.Models.MaterialManagementModel
{
    public class MaterialLog : MaterialBillDetail
    {
        public DateTime Time { get; set; }
        public int BillId { get; set; }
        /// <summary>
        /// 1 入库; 2 出库;3 冲正;
        /// </summary>
        public int Type { get; set; }
        /// <summary>
        /// 0 增加  1 减少 
        /// </summary>
        public int Mode { get; set; }
        //public int Mode
        //{
        //    get
        //    {
        //        switch (Type)
        //        {
        //            case 1:
        //                Mode = 0;
        //                break;
        //            case 2:
        //                Mode = 1;
        //                break;
        //        }

        //        return Mode;
        //    }
        //    set => Mode = value;
        //}
        public string Purpose { get; set; }
        public int PlanId { get; set; } = 0;
        public string Plan { get; set; } = "";
        public decimal Number { get; set; }
        public decimal OldNumber { get; set; }
        public string RelatedPerson { get; set; }
        public string Manager { get; set; }
    }
}
