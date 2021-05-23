using ModelBase.Base.Utils;
using ModelBase.Models.BaseModel;
using System;
using System.ComponentModel;

namespace ApiManagement.Models.StatisticManagementModel
{

    public enum StatisticProcessTimeEnum
    {
        [Description("小时")]
        小时 = 0,
        [Description("日")]
        日 = 1,
        [Description("周")]
        周 = 2,
        [Description("月")]
        月 = 3,
        [Description("年")]
        年 = 4,
    }
    public enum StatisticProcessTypeEnum
    {
        [Description("设备")]
        设备 = 0,
        [Description("计划号")]
        计划号 = 1,
        [Description("操作工")]
        操作工 = 2,
    }

    /// <summary>
    /// 加工统计
    /// </summary>
    public class StatisticProcessBase : CommonBase
    {
        /// <summary>
        /// 车间Id
        /// </summary>
        public int WorkshopId { get; set; }
        /// <summary>
        /// 时间类型
        /// </summary>
        public StatisticProcessTimeEnum Type { get; set; }
        /// <summary>
        /// 时间
        /// </summary>
        public DateTime Time { get; set; }
        /// <summary>
        /// 工序
        /// </summary>
        public int Step { get; set; }
        /// <summary>
        /// 工序名称
        /// </summary>
        public string StepName { get; set; }
        /// <summary>
        /// 工序
        /// </summary>
        public string StepAbbrev { get; set; }
        /// <summary>
        /// 加工数
        /// </summary>
        public int Total { get; set; }
        /// <summary>
        /// 合格数
        /// </summary>
        public int Qualified { get; set; }
        /// <summary>
        /// 次品数
        /// </summary>
        public int Unqualified { get; set; }
        /// <summary>
        /// 合格率(%)
        /// </summary>
        public decimal QualifiedRate => Total == 0 ? 0 : ((decimal)Qualified * 100 / Total).ToRound();
        /// <summary>
        /// 次品率(%)
        /// </summary>
        public decimal UnqualifiedRate => Total == 0 ? 0 : ((decimal)Unqualified * 100 / Total).ToRound();
        /// <summary>
        /// 是否是老数据（数据库数据）
        /// </summary>
        public bool Old { get; set; } = true;
        /// <summary>
        /// 是否更新（数据库数据）
        /// </summary>
        public bool Update { get; set; }
    }

    /// <summary>
    /// 操作工工序加工统计
    /// </summary>
    public class StatisticProcessAll : StatisticProcessBase
    {
        public int DeviceId { get; set; }
        /// <summary>
        /// 机台号
        /// </summary>
        public string Code { get; set; }
        public int ProductionId { get; set; }
        /// <summary>
        /// 计划号
        /// </summary>
        public string Production { get; set; }
        public int ProcessorId { get; set; }
        /// <summary>
        /// 加工人
        /// </summary>
        public string Processor { get; set; }
    }
    /// <summary>
    /// 设备工序加工统计
    /// </summary>
    public class StatisticProcessDevice : StatisticProcessBase
    {
        public int DeviceId { get; set; }
        /// <summary>
        /// 机台号
        /// </summary>
        public string Code { get; set; }
    }
    /// <summary>
    /// 计划号工序加工统计
    /// </summary>
    public class StatisticProcessProduction : StatisticProcessBase
    {
        public int ProductionId { get; set; }
        /// <summary>
        /// 计划号
        /// </summary>
        public string Production { get; set; }
    }
    /// <summary>
    /// 操作工工序加工统计
    /// </summary>
    public class StatisticProcessProcessor : StatisticProcessBase
    {
        public int ProcessorId { get; set; }
        /// <summary>
        /// 加工人
        /// </summary>
        public string Processor { get; set; }
    }




}