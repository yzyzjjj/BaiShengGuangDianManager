using System;
using ModelBase.Models.BaseModel;

namespace ApiManagement.Models.ManufactureModel
{
    /// <summary>
    /// 生产分组
    /// </summary>
    public class ManufactureGroup : CommonBase
    {
        public string Group { get; set; }
        /// <summary>
        /// 更新名字
        /// </summary>
        public bool IsName { get; set; }
        /// <summary>
        /// 显示时段 0 天 1周  2月
        /// </summary>
        public int Interval { get; set; }
        /// <summary>
        /// 周期第一天
        /// </summary>
        public int ScoreTime { get; set; }
    }
}
