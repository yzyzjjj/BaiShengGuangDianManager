using ModelBase.Base.Utils;
using System;
using ApiManagement.Models.DeviceManagementModel;

namespace ApiManagement.Models.OtherModel
{
    public class FlowCardReport
    {
        public ProcessType ProcessType { get; set; }
        public DateTime StartTime { get; set; }
        public int Id { get; set; }
        public int Id1 { get; set; }
        public DateTime Time { get; set; }
        /// <summary>
        /// 流程卡ID
        /// </summary>
        public int FlowCardId { get; set; }
        /// <summary>
        /// 流程卡
        /// </summary>
        public string FlowCard { get; set; }
        /// <summary>
        /// 计划号ID
        /// </summary>
        public int ProductionId { get; set; }
        /// <summary>
        /// 计划号
        /// </summary>
        public string Production { get; set; } = "";
        /// <summary>
        /// 机台号ID
        /// </summary>
        public int DeviceId { get; set; }
        /// <summary>
        /// 机台号
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// 工序
        /// </summary>
        public int Step { get; set; }
        /// <summary>
        /// 在加工前上报还是加工后上报
        /// </summary>
        public bool Back { get; set; }
        /// <summary>
        /// 是否是末道工序
        /// </summary>
        public bool Last { get; set; }
        /// <summary>
        /// 加工人
        /// </summary>
        public int ProcessorId { get; set; }
        /// <summary>
        /// 加工人
        /// </summary>
        public string Processor { get; set; }
        /// <summary>
        /// 单次加工数
        /// </summary>
        public int Total { get; set; }
        /// <summary>
        /// 单次合格数
        /// </summary>
        public int HeGe { get; set; }
        /// <summary>
        /// 单次次品数
        /// </summary>
        public int LiePian { get; set; }
        /// <summary>
        /// 单次合格率(%)
        /// </summary>
        public decimal QualifiedRate => Total == 0 ? 0 : ((decimal)HeGe * 100 / Total).ToRound();
        /// <summary>
        /// 单次次品率(%)
        /// </summary>
        public decimal UnqualifiedRate => Total == 0 ? 0 : ((decimal)LiePian * 100 / Total).ToRound();
        /// <summary>
        /// 0 未处理  1 已处理  2 未查询到流程卡 3 未查询到加工人 4 未查询到设备
        /// </summary>
        public int State { get; set; }
        /// <summary>
        /// 次品原因
        /// </summary>
        public string Reason { get; set; }
    }


    public class ErpFlowCardReport
    {
        public DateTime time { get; set; }
        /// <summary>
        /// 流程卡
        /// </summary>
        public string lck { get; set; }
        /// <summary>
        /// 机台号
        /// </summary>
        public string jth { get; set; }
        /// <summary>
        /// 工序
        /// </summary>
        public int gx { get; set; }
        /// <summary>
        /// 加工人
        /// </summary>
        public string jgr { get; set; }
        /// <summary>
        /// 单次加工数
        /// </summary>
        public int jgqty { get; set; }
        /// <summary>
        /// 单次合格数
        /// </summary>
        public int qty { get; set; }
        /// <summary>
        /// 单次次品数
        /// </summary>
        public int lpqty { get; set; }
        /// <summary>
        /// 在加工前上报还是加工后上报
        /// </summary>
        public bool back { get; set; }
        /// <summary>
        /// 是否是末道工序
        /// </summary>
        public bool last { get; set; }
        /// <summary>
        /// 次品原因
        /// </summary>
        public string reason { get; set; }
    }

    public class ErpUpdateFlowCard
    {
        /// <summary>
        /// 流程卡id
        /// </summary>
        public int Id { get; set; }
        public DateTime MarkedDateTime { get; set; }
        /// <summary>
        /// 总发出
        /// </summary>
        public int FaChu { get; set; }
        /// <summary>
        /// 合格
        /// </summary>
        public int HeGe { get; set; }
        /// <summary>
        /// 裂片
        /// </summary>
        public int LiePian { get; set; }
        /// <summary>
        /// 机台号
        /// </summary>
        public int DeviceId { get; set; }
        public DateTime Time { get; set; }
        /// <summary>
        /// 加工人
        /// </summary>
        public string JiaGongRen { get; set; }
    }
}
