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
        public string Production { get; set; }
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
        /// 描述
        /// </summary>
        public bool Back { get; set; }
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
    }
}
