using System;
using System.Collections.Generic;
using System.Linq;
using ModelBase.Models.BaseModel;
using ServiceStack;

namespace ApiManagement.Models.AccountManagementModel
{
    public class Workshop : CommonBase
    {
        /// <summary>
        /// 车间名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 简称
        /// </summary>
        public string Abbrev { get; set; }
        /// <summary>
        /// 班次
        /// </summary>
        public int Shifts => ShiftTimeList.Count;
        /// <summary>
        /// 班次时间
        /// </summary>
        public string ShiftTimes { get; set; }
        public List<TimeSpan> ShiftTimeList => ShiftTimes.IsNullOrEmpty() ? new List<TimeSpan>() 
            : ShiftTimes.Split(",").Select(x => TimeSpan.TryParse(x, out var a) ? a : default(TimeSpan)).Where(y => y != default(TimeSpan)).OrderBy(x => x).ToList();
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
    }
}
