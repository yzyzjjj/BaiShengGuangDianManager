using System;
using ApiManagement.Base.Helper;
using ApiManagement.Models.BaseModel;
using ModelBase.Base.Logic;

namespace ApiManagement.Models.MaterialManagementModel
{
    public class MaterialPurchaseItem : CommonBase
    {
        public MaterialPurchaseItem()
        { }

        public MaterialPurchaseItem(int purchaseId, TimerHelper.ErpPurchaseItem good, string createUserId, DateTime now, string urlFile)
        {
            CreateUserId = createUserId;
            MarkedDateTime = now;
            Time = good.f_time;
            IsErp = true;
            ErpId = good.f_id;
            PurchaseId = purchaseId;
            Code = good.f_wlbm;
            Class = good.f_lx;
            Category = good.f_xflmc;
            Name = good.f_wlpc;
            Supplier = good.f_gys ?? string.Empty;
            SupplierFull = good.f_nickname ?? string.Empty;
            Specification = good.f_gg;
            Number = decimal.TryParse(good.f_cgddnum, out var a) ? a : decimal.Parse(good.f_num);
            Unit = good.f_dw;
            Remark = good.f_node;
            Purchaser = good.f_cgname ?? string.Empty;
            PurchasingCompany = good.f_gsmc ?? string.Empty;
            Order = good.f_cgddh ?? string.Empty;
            EstimatedTime = DateTime.TryParse(good.f_dwdate, out var dwDate) ? dwDate : default(DateTime);
            ArrivalTime = DateTime.TryParse(good.f_dhdate, out var dhDate) ? dhDate : default(DateTime);
            File = good.f_file ?? string.Empty;
            FileUrl = good.f_file != null ? urlFile + good.f_file : string.Empty;
            IsInspection = good.f_llj == "是";
            Currency = good.f_hbbz ?? string.Empty;
            Payment = good.f_fkfs ?? string.Empty;
            Transaction = good.f_jyfs ?? string.Empty;
            Invoice = good.f_fpiao ?? string.Empty;
            TaxPrice = decimal.TryParse(good.f_hsdj, out var hsdj) ? hsdj : 0;
            TaxAmount = decimal.TryParse(good.f_hsje, out var hsje) ? hsje : 0;
            Price = decimal.TryParse(good.f_wsdj, out var wsdj) ? wsdj : 0;
        }

        /// <summary>
        /// 是否首次入库
        /// </summary>
        [IgnoreChange]
        public bool IsFirst { get; set; }
        /// <summary>
        /// 入库批次
        /// </summary>
        [IgnoreChange]
        public string Batch { get; set; } = "";
        /// <summary>
        /// 入库时间
        /// </summary>
        [IgnoreChange]
        public DateTime IncreaseTime { get; set; }
        [IgnoreChange]
        public DateTime Time { get; set; }
        [IgnoreChange]
        public bool IsErp { get; set; }
        [IgnoreChange]
        public int ErpId { get; set; }
        [IgnoreChange]
        public int PurchaseId { get; set; }
        [IgnoreChange]
        public string Code { get; set; }

        public string Class { get; set; }

        public string Category { get; set; }

        public string Name { get; set; }

        public string Supplier { get; set; }

        public string SupplierFull { get; set; }

        public string Specification { get; set; }

        public decimal Number { get; set; }

        public string Unit { get; set; }

        public string Remark { get; set; }

        public string Purchaser { get; set; }
        /// <summary>
        /// 采购公司
        /// </summary>

        public string PurchasingCompany { get; set; }

        public string Order { get; set; }
        /// <summary>
        /// 预计到位日期
        /// </summary>

        public DateTime EstimatedTime { get; set; }
        /// <summary>
        /// 到货日期
        /// </summary>

        public DateTime ArrivalTime { get; set; }

        public string File { get; set; }

        public string FileUrl { get; set; }
        /// <summary>
        /// 是否来料检验
        /// </summary>

        public bool IsInspection { get; set; }
        /// <summary>
        /// 货币币种 f_hbbz
        /// </summary>

        public string Currency { get; set; }
        /// <summary>
        /// 付款方式 f_fkfs
        /// </summary>

        public string Payment { get; set; }
        /// <summary>
        /// 交易方式 f_jyfs
        /// </summary>

        public string Transaction { get; set; }
        /// <summary>
        /// 发票 f_fpiao
        /// </summary>

        public string Invoice { get; set; }
        [IgnoreChange]
        public decimal TaxRate
        {
            get
            {
                if (Invoice != null && Invoice.Contains("%"))
                {
                    var taxStr = Invoice.Split("%");
                    return decimal.TryParse(taxStr[0], out var r) ? r : 0;
                }
                return 0;
            }
        }
        /// <summary>
        /// 含税单价 f_hsdj
        /// </summary>

        public decimal TaxPrice { get; set; }
        /// <summary>
        /// 含税金额 f_hsje
        /// </summary>

        public decimal TaxAmount { get; set; }
        /// <summary>
        /// 未税单价 f_wsdj
        /// </summary>

        public decimal Price { get; set; }
        /// <summary>
        /// 已入库
        /// </summary>
        [IgnoreChange]
        public decimal Stock { get; set; }
        /// <summary>
        /// 本次入库
        /// </summary>
        [IgnoreChange]
        public decimal Count { get; set; }
        /// <summary>
        /// 入库编码
        /// </summary>
        [IgnoreChange]
        public int BillId { get; set; }
        /// <summary>
        /// 入库编码
        /// </summary>
        [IgnoreChange]
        public string ThisCode { get; set; }
        public bool IsSame(MaterialPurchaseItem materialPurchase)
        {
            return Code == materialPurchase.Code
                   && Category == materialPurchase.Category
                   && Name == materialPurchase.Name
                   && Supplier == materialPurchase.Supplier
                   && Specification == materialPurchase.Specification;
        }
    }
}