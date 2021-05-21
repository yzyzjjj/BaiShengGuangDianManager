using ModelBase.Models.BaseModel;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public int Shifts { get; set; }
        /// <summary>
        /// 班次名称
        /// 例子：1班次1个名称
        /// 例子：2班次2个名称
        /// 例子：3班次3个名称
        /// </summary>
        public string ShiftNames { get; set; }
        public List<string> ShiftNameList => ShiftNames.IsNullOrEmpty() ? new List<string>()
            : ShiftNames.Split(",").ToList();
        /// <summary>
        /// 班次时间
        /// 例子：1班次2个时间（t1,t2），上班时间为(t1,t2)
        /// 例子：2班次2个时间（t1,t2），上班时间为(t1,t2)(t2,下一天的t1) 
        /// 例子：3班次3个时间（t1,t2,t3），上班时间为(t1,t2)(t2,t3)(t3,下一天的t1) 
        /// </summary>
        public string ShiftTimes { get; set; }
        public List<TimeSpan> ShiftTimeList => ShiftTimes.IsNullOrEmpty() ? new List<TimeSpan>()
            : ShiftTimes.Split(",").Select(x => TimeSpan.TryParse(x, out var a) ? a : default(TimeSpan)).Where(y => y != default(TimeSpan)).OrderBy(x => x).ToList();
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
        /// <summary>
        /// 设置是否正确
        /// </summary>
        /// <returns></returns>
        public bool ValidShifts()
        {
            return Shifts != 0 || (Shifts == 1 && (ShiftNameList.Count == 1 && ShiftTimeList.Count == 2)) ||
                   (Shifts >= 2 && (Shifts == ShiftTimeList.Count && Shifts == ShiftTimeList.Count));
        }
    }
}
