using ModelBase.Base.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace ApiManagement.Models.SmartFactoryModel
{
    /// <summary>
    /// 任务单看板
    /// </summary>
    public class SmartTaskOrderKanBanItem : SmartTaskOrderDetail
    {
        /// <summary>
        /// 状态
        /// </summary>
        public SmartKanBanError Error { get; set; } = SmartKanBanError.正常;
        public string ErrorStr => Error.GetAttribute<DescriptionAttribute>()?.Description ?? "";
        public List<SmartTaskOrderKanBanNeed> Needs { get; set; } = new List<SmartTaskOrderKanBanNeed>();

    }

    /// <summary>
    /// 任务单看板工序
    /// </summary>
    public class SmartTaskOrderKanBanNeed : SmartTaskOrderNeedDetail
    {
        /// <summary>
        /// 加工时间
        /// </summary>
        public DateTime ProcessTime { get; set; }
        /// <summary>
        /// 任务单安排工序id
        /// </summary>
        public int NeedId { get; set; }
        /// <summary>
        /// 应投料数量
        /// </summary>
        public int ShouldPut { get; set; }
        /// <summary>
        /// 应该完成产量
        /// </summary>
        public int ShouldTarget { get; set; }
        /// <summary>
        /// 本次目标产量
        /// </summary>
        public int ThisTarget { get; set; }
        /// <summary>
        /// 本次投料数量
        /// </summary>
        public int ThisPut { get; set; }
        /// <summary>
        /// 理论入库率
        /// </summary>
        public decimal TheoreticalRate => Target != 0 ? ((decimal)ShouldTarget * 100 / Target).ToRound() : 0;
        /// <summary>
        /// 实际入库率
        /// </summary>
        public decimal ActualRate => Target != 0 ? ((decimal)DoneTarget * 100 / Target).ToRound() : 0;
        /// <summary>
        /// 状态:0正常；1合格率低；
        /// </summary>
        public SmartKanBanError Error { get; set; } = SmartKanBanError.正常;

        public string ErrorStr => Error.GetAttribute<DescriptionAttribute>()?.Description ?? "";
    }
}
