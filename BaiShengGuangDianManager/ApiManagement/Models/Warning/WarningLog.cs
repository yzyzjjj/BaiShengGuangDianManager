using System;
using System.Linq;
using ModelBase.Base.Logic;
using ModelBase.Base.Utils;

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
    }
}