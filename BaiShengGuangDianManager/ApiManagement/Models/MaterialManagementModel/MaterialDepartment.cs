using ApiManagement.Base.Helper;
using ApiManagement.Models.BaseModel;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace ApiManagement.Models.MaterialManagementModel
{
    /// <summary>
    /// 核价人
    /// </summary>
    public class MaterialValuer : CommonBase
    {
        public string Valuer { get; set; }
        public string Remark { get; set; }
        public bool IsErp { get; set; }
    }
    public class MaterialDepartment : CommonBase
    {
        public string Department { get; set; }
        public string Remark { get; set; }
        public bool IsErp { get; set; }
        public bool Get { get; set; }
    }
    public class MaterialDepartmentMember : CommonBase
    {
        public int DepartmentId { get; set; }
        public string Member { get; set; }
        public bool IsErp { get; set; }
    }


    public enum MaterialPurchaseStateEnum
    {
        正常,
        中止,
        审核完成,
        开始采购,
        仓库到货,
        订单完成,
        撤销,
    }
    public enum MaterialPurchasePriorityEnum
    {
        普通,
        紧急,
        直通,
    }
    public class MaterialPurchase : CommonBase
    {
        public MaterialPurchase()
        { }
        public MaterialPurchase(int depId, TimerHelper.ErpPurchase erpPurchase, string createUserId, DateTime now, MaterialPurchaseStateEnum state, MaterialPurchasePriorityEnum priority)
        {
            CreateUserId = createUserId;
            MarkedDateTime = now;
            Time = DateTime.Parse(erpPurchase.f_date);
            IsErp = true;
            ErpId = erpPurchase.f_id;
            DepartmentId = depId;
            Purchase = erpPurchase.f_title;
            Number = erpPurchase.f_name;
            Name = erpPurchase.f_ygxm;
            Valuer = erpPurchase.f_hjry ?? "";
            Step = erpPurchase.f_bz;
            State = state;
            IsDesign = erpPurchase.f_istz == "是";
            Priority = priority;
        }

        public DateTime Time { get; set; }
        public bool IsErp { get; set; }
        public int ErpId { get; set; }
        public int DepartmentId { get; set; }
        [Description]
        public string Purchase { get; set; }
        /// <summary>
        /// 发起人编号
        /// </summary>
        [Description]
        public string Number { get; set; }
        /// <summary>
        /// 发起人姓名
        /// </summary>
        [Description]
        public string Name { get; set; }
        /// <summary>
        /// 核价人姓名
        /// </summary>
        [Description]
        public string Valuer { get; set; } = string.Empty;
        [Description]
        public string Step { get; set; }
        [Description]
        public MaterialPurchaseStateEnum State { get; set; }
        public string StateStr => State.ToString();
        [Description]
        public bool IsDesign { get; set; }
        [Description]
        public MaterialPurchasePriorityEnum Priority { get; set; }
        public string PriorityStr => Priority.ToString();
        /// <summary>
        /// 是否引用
        /// </summary>
        public bool IsQuote { get; set; }
        public List<MaterialPurchaseQuote> Items = new List<MaterialPurchaseQuote>();
        public bool HaveChange(MaterialPurchase materialPurchase)
        {
            var thisProperties = GetType().GetProperties();
            var properties = materialPurchase.GetType().GetProperties();
            foreach (var propInfo in typeof(MaterialPurchase).GetProperties())
            {
                var attr = (DescriptionAttribute)propInfo.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault();
                if (attr == null)
                {
                    continue;
                }
                var thisValue = thisProperties.First(x => x.Name == propInfo.Name).GetValue(this);
                var value = properties.First(x => x.Name == propInfo.Name).GetValue(materialPurchase);
                if (propInfo.PropertyType == typeof(DateTime))
                {
                    return (DateTime)thisValue != (DateTime)value;
                }

                if (propInfo.PropertyType == typeof(decimal))
                {
                    return (decimal)thisValue != (decimal)value;
                }

                var oldValue = thisValue.ToString();
                var newValue = value.ToString();
                if (oldValue != newValue)
                {
                    return true;
                }
            }
            return false;
        }
    }

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
            Specification = good.f_gg;
            Number = decimal.Parse(good.f_num);
            Unit = good.f_dw;
            Remark = good.f_node;
            Purchaser = good.f_cgname ?? string.Empty;
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

        public DateTime Time { get; set; }
        public bool IsErp { get; set; }
        public int ErpId { get; set; }
        public int PurchaseId { get; set; }
        public string Code { get; set; }
        [Description]
        public string Class { get; set; }
        [Description]
        public string Category { get; set; }
        [Description]
        public string Name { get; set; }
        [Description]
        public string Supplier { get; set; }
        [Description]
        public string Specification { get; set; }
        [Description]
        public decimal Number { get; set; }
        [Description]
        public string Unit { get; set; }
        [Description]
        public string Remark { get; set; }
        [Description]
        public string Purchaser { get; set; }
        [Description]
        public string Order { get; set; }
        /// <summary>
        /// 预计到位日期
        /// </summary>
        [Description]
        public DateTime EstimatedTime { get; set; }
        /// <summary>
        /// 到货日期
        /// </summary>
        [Description]
        public DateTime ArrivalTime { get; set; }
        [Description]
        public string File { get; set; }
        [Description]
        public string FileUrl { get; set; }
        /// <summary>
        /// 是否来料检验
        /// </summary>
        [Description]
        public bool IsInspection { get; set; }
        /// <summary>
        /// 货币币种 f_hbbz
        /// </summary>
        [Description]
        public string Currency { get; set; }
        /// <summary>
        /// 付款方式 f_fkfs
        /// </summary>
        [Description]
        public string Payment { get; set; }
        /// <summary>
        /// 交易方式 f_jyfs
        /// </summary>
        [Description]
        public string Transaction { get; set; }
        /// <summary>
        /// 发票 f_fpiao
        /// </summary>
        [Description]
        public string Invoice { get; set; }
        public decimal TaxTate
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
        [Description]
        public decimal TaxPrice { get; set; }
        /// <summary>
        /// 含税金额 f_hsje
        /// </summary>
        [Description]
        public decimal TaxAmount { get; set; }
        /// <summary>
        /// 未税单价 f_wsdj
        /// </summary>
        [Description]
        public decimal Price { get; set; }
        /// <summary>
        /// 已入库
        /// </summary>
        public decimal Stock { get; set; }
        /// <summary>
        /// 本次入库
        /// </summary>
        public decimal Count { get; set; }
        /// <summary>
        /// 入库编码
        /// </summary>
        public int BillId { get; set; }
        public bool HaveChange(MaterialPurchaseItem materialPurchase)
        {
            var thisProperties = GetType().GetProperties();
            var properties = materialPurchase.GetType().GetProperties();
            foreach (var propInfo in typeof(MaterialPurchaseItem).GetProperties())
            {
                var attr = (DescriptionAttribute)propInfo.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault();
                if (attr == null)
                {
                    continue;
                }
                var thisValue = thisProperties.First(x => x.Name == propInfo.Name).GetValue(this);
                var value = properties.First(x => x.Name == propInfo.Name).GetValue(materialPurchase);
                if (propInfo.PropertyType == typeof(DateTime))
                {
                    return (DateTime)thisValue != (DateTime)value;
                }

                if (propInfo.PropertyType == typeof(decimal))
                {
                    return (decimal)thisValue != (decimal)value;
                }

                var oldValue = thisValue.ToString();
                var newValue = value.ToString();
                if (oldValue != newValue)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class MaterialPurchaseQuote : CommonBase
    {
        public MaterialPurchaseQuote()
        { }

        public string Purchase { get; set; }
        public DateTime Time { get; set; }
        public int ItemId { get; set; }
        public int PurchaseId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Specification { get; set; }
        public decimal Number { get; set; }
        public string Unit { get; set; }
        /// <summary>
        /// 含税单价 f_hsdj
        /// </summary>
        [Description]
        public decimal TaxPrice { get; set; }
        /// <summary>
        /// 未税单价 f_wsdj
        /// </summary>
        [Description]
        public decimal Price { get; set; }
        /// <summary>
        /// 含税金额 f_hsje
        /// </summary>
        [Description]
        public decimal TaxAmount { get; set; }
        /// <summary>
        ///  税率
        /// </summary>
        [Description]
        public decimal Tax { get; set; }

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
