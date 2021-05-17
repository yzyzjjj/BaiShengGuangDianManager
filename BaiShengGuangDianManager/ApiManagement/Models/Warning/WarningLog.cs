using Newtonsoft.Json;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.Warning
{
    public class WarningLog : WarningCurrent
    {
        /// <summary>
        /// 是否成功预警
        /// </summary>
        public bool IsWarning { get; set; }
        public DateTime WarningTime { get; set; }
        public string Code { get; set; }
        public string CategoryName { get; set; }
        public WarningLog()
        {
        }

        /// <summary>
        /// 额外Id字段
        /// </summary>
        public string ExtraIds { get; set; } = string.Empty;
        public List<int> ExtraIdList => ExtraIds.IsNullOrEmpty() ? new List<int>() : ExtraIds.Split(",").Select(x => int.TryParse(x, out var a) ? a : 0).ToList();
        public void AddExtraId(int id)
        {
            if (ExtraIdList.Any())
            {
                ExtraIds += $",{id}";
            }
            else
            {
                ExtraIds += $"{id}";
            }
        }
    }

    public class WarningLogWeb : WarningLog
    {
        [JsonIgnore]
        public new string Param { get; set; }
        [JsonIgnore]
        public new string Values { get; set; }

    }
}