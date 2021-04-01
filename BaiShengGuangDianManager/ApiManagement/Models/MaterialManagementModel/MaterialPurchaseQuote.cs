using System;
using ApiManagement.Models.BaseModel;
using ServiceStack;

namespace ApiManagement.Models.MaterialManagementModel
{
    public class MaterialPurchaseQuote : CommonBase
    {
        public MaterialPurchaseQuote()
        {
        }

        /// <summary>
        /// 订单编号
        /// </summary>
        public string Purchase { get; set; }
        /// <summary>
        /// 采购日期
        /// </summary>
        public DateTime Time { get; set; }
        /// <summary>
        /// erp清单
        /// </summary>
        public int ItemId { get; set; }
        public int PurchaseId { get; set; }
        /// <summary>
        /// 物料编码
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// 品类
        /// </summary>
        public string Class { get; set; }
        /// <summary>
        /// 小分类
        /// </summary>
        public string Category { get; set; }
        /// <summary>
        /// 物料名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 供应商
        /// </summary>
        public string Supplier { get; set; }
        /// <summary>
        /// 规格
        /// </summary>
        public string Specification { get; set; }
        /// <summary>
        /// 数量
        /// </summary>
        public decimal Number { get; set; }
        /// <summary>
        /// 单位
        /// </summary>
        public string Unit { get; set; }
        /// <summary>
        /// 含税单价 f_hsdj
        /// </summary>
        public decimal TaxPrice { get; set; }
        /// <summary>
        /// 未税单价 f_wsdj
        /// </summary>
        public decimal Price { get; set; }
        /// <summary>
        /// 含税金额 f_hsje
        /// </summary>
        public decimal TaxAmount { get; set; }
        /// <summary>
        ///  税率
        /// </summary>
        public decimal TaxRate { get; set; }
        /// <summary>
        /// 采购单号
        /// </summary>
        public string Order { get; set; }
        /// <summary>
        /// 品类
        /// </summary>
        public string Purchaser { get; set; }
        /// <summary>
        /// 采购公司
        /// </summary>

        public string PurchasingCompany { get; set; }

        public bool Illegal()
        {
            //if (Code.IsNullOrEmpty())
            //{
            //    return true;
            //}

            if (Name.IsNullOrEmpty())
            {
                return true;
            }

            if (Specification.IsNullOrEmpty())
            {
                return true;
            }
            return false;
        }
    }
}