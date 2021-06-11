using ApiManagement.Base.Helper;
using ModelBase.Base.Logic;
using System;
using System.Collections.Generic;
using ModelBase.Models.BaseModel;

namespace ApiManagement.Models.MaterialManagementModel
{
    public enum MaterialPurchaseStateEnum
    {
        正常,
        中止,
        审核完成,
        开始采购,
        仓库到货,
        订单完成,
        撤销,
        已入库,
        采购中,
        部分入库,
        完全入库,
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

        [IgnoreChange]
        public DateTime Time { get; set; }
        [IgnoreChange]
        public bool IsErp { get; set; }
        [IgnoreChange]
        public int ErpId { get; set; }
        [IgnoreChange]
        public int DepartmentId { get; set; }
        [IgnoreChange]
        public string Department { get; set; }
        public string Purchase { get; set; }
        /// <summary>
        /// 发起人编号
        /// </summary>
        public string Number { get; set; }
        /// <summary>
        /// 发起人姓名
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 核价人姓名
        /// </summary>
        public string Valuer { get; set; } = string.Empty;
        public string Step { get; set; }
        public MaterialPurchaseStateEnum State { get; set; }
        [IgnoreChange]
        public string StateStr => State.ToString();
        public bool IsDesign { get; set; }
        public MaterialPurchasePriorityEnum Priority { get; set; }
        [IgnoreChange]
        public string PriorityStr => Priority.ToString();
        /// <summary>
        /// 是否引用
        /// </summary>
        [IgnoreChange]
        public bool IsQuote { get; set; }
        [IgnoreChange]
        public List<MaterialPurchaseQuote> Items = new List<MaterialPurchaseQuote>();
        //public bool HaveChange(MaterialPurchase materialPurchase)
        //{
        //    var thisProperties = GetType().GetProperties();
        //    var properties = materialPurchase.GetType().GetProperties();
        //    foreach (var propInfo in typeof(MaterialPurchase).GetProperties())
        //    {
        //        var attr = (DescriptionAttribute)propInfo.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault();
        //        if (attr == null)
        //        {
        //            continue;
        //        }
        //        var thisValue = thisProperties.First(x => x.Name == propInfo.Name).GetValue(this);
        //        var value = properties.First(x => x.Name == propInfo.Name).GetValue(materialPurchase);
        //        if (propInfo.PropertyType == typeof(DateTime))
        //        {
        //            return (DateTime)thisValue != (DateTime)value;
        //        }

        //        if (propInfo.PropertyType == typeof(decimal))
        //        {
        //            return (decimal)thisValue != (decimal)value;
        //        }

        //        var oldValue = thisValue?.ToString() ?? "";
        //        var newValue = value?.ToString() ?? "";
        //        if (oldValue != newValue)
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}
    }
}