using ModelBase.Base.Utils;
using Newtonsoft.Json;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace ApiManagement.Models.ManufactureModel
{
    /// <summary>
    /// 生产计划检验配置
    /// </summary>
    public class ManufacturePlanCheckItem : ManufactureCheckItem
    {
        public int PlanId { get; set; }
        public int ItemId { get; set; }
        /// <summary>
        /// 检验时间
        /// </summary>
        [ManufactureDescription("检验时间", 1)]
        public DateTime CheckTime { get; set; }
        /// <summary>
        /// 检验说明
        /// </summary>
        [ManufactureDescription("检验说明", 2)]
        public string Desc { get; set; }

        /// <summary>
        /// 检验结果
        /// </summary>
        [ManufactureDescription("检验结果", 3)]
        public ManufacturePlanCheckItemState Result { get; set; } = ManufacturePlanCheckItemState.Wait;
        public string ResultDesc => Result.GetAttribute<DescriptionAttribute>()?.Description ?? "";
        [ManufactureDescription("检验图片", 4)]
        public string Images { get; set; } = "[]";
        public string[] ImageList => Images != null ? JsonConvert.DeserializeObject<string[]>(Images) : new string[0];

        public bool HaveChange(ManufacturePlanCheckItem manufacturePlanCheckItem, out ManufactureLog change)
        {
            var changeFlag = false;
            change = new ManufactureLog
            {
                Type = ManufactureLogType.UpdateCheckItem,
                ParamList = new List<ManufactureLogItem>()
            };
            var thisProperties = GetType().GetProperties();
            var properties = manufacturePlanCheckItem.GetType().GetProperties();
            var tmp = new Dictionary<int, ManufactureLogItem>();
            foreach (var propInfo in typeof(ManufacturePlanCheckItem).GetProperties())
            {
                var attr = (ManufactureDescription)propInfo.GetCustomAttributes(typeof(ManufactureDescription), false).FirstOrDefault();
                if (attr == null)
                {
                    continue;
                }

                var description = attr.Description;
                var order = attr.Order;
                var thisValue = thisProperties.First(x => x.Name == propInfo.Name).GetValue(this);
                var value = properties.First(x => x.Name == propInfo.Name).GetValue(manufacturePlanCheckItem);
                string oldValue, newValue;
                if (propInfo.PropertyType == typeof(DateTime))
                {
                    oldValue = ((DateTime)thisValue).ToStr();
                    newValue = ((DateTime)value).ToStr();
                }
                else if (propInfo.PropertyType.BaseType == typeof(Enum))
                {
                    var newEnum = Enum.Parse(propInfo.PropertyType, thisValue.ToString());
                    oldValue = ((DescriptionAttribute)newEnum.GetType().GetField(newEnum.ToString()).GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault())?.Description ?? "";
                    var oldEnum = Enum.Parse(propInfo.PropertyType, value.ToString());
                    newValue = ((DescriptionAttribute)oldEnum.GetType().GetField(oldEnum.ToString()).GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault())?.Description ?? "";
                }
                else
                {
                    oldValue = thisValue.ToString();
                    newValue = value.ToString();
                }

                if (oldValue != newValue)
                {
                    changeFlag = true;
                    if (!attr.TrueValue.IsNullOrEmpty())
                    {
                        oldValue = thisProperties.First(x => x.Name == attr.TrueValue).GetValue(this).ToString();
                        newValue = properties.First(x => x.Name == attr.TrueValue).GetValue(manufacturePlanCheckItem).ToString();
                    }

                    var item = propInfo.Name != "Images"
                        ? new ManufactureLogItem
                        {
                            Type = ManufactureLogType.UpdateFormat,
                            Old = oldValue,
                            New = newValue,
                            Field = description,
                        }
                        : new ManufactureLogItem
                        {
                            Type = ManufactureLogType.UpdateImagesFormat,
                            Old = "",
                            New = "",
                            Field = "",
                        };
                    if (!tmp.ContainsKey(order))
                    {
                        tmp.Add(order, item);
                    }
                }
            }
            change.ParamList.AddRange(tmp.OrderBy(x => x.Key).Select(y => y.Value));
            return changeFlag;
        }
    }
}
