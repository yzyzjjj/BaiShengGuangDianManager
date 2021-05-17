using ApiManagement.Models.DeviceManagementModel;
using ModelBase.Base.Utils;
using Newtonsoft.Json;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.OtherModel
{
    public class FlowCardReportGet
    {
        public FlowCardReportGet()
        {
        }
        public FlowCardReportGet(ErpFlowCardReportGet report, DeviceProcessStepDetail step, DateTime now)
        {
            MarkedDateTime = now;
            InsertTime = now;
            Step = step.Id;
            StepName = step.StepName;
            StepAbbrev = step.Abbrev;

            OtherId = report.f_id;
            Time = report.f_inserttime;
            OldFlowCard = report.f_lckh0 ?? "";
            FlowCard = report.f_lckh ?? "";
            Total = report.jgqty;
            HeGe = report.hgqty;
            Code = report.jth ?? "";
            Processor = report.jgr ?? "";
            Reason = "";
            Back = true;
        }

        public int Id { get; set; }
        /// <summary>
        /// 数据源Id
        /// </summary>
        public int OtherId { get; set; }
        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTime MarkedDateTime { get; set; }
        /// <summary>
        /// 插入时间
        /// </summary>
        public DateTime InsertTime { get; set; }
        public int MarkedDelete { get; set; }
        /// <summary>
        /// 上报时间
        /// </summary>
        public DateTime Time { get; set; }
        /// <summary>
        /// 工序
        /// </summary>
        public int Step { get; set; }
        /// <summary>
        /// 工序
        /// </summary>
        public string StepName { get; set; }
        /// <summary>
        /// 工序
        /// </summary>
        public string StepAbbrev { get; set; }
        /// <summary>
        /// 流程卡ID
        /// </summary>
        public int FlowCardId { get; set; }
        /// <summary>
        /// 流程卡
        /// </summary>
        public string FlowCard { get; set; }
        /// <summary>
        /// 最初流程卡ID
        /// </summary>
        public int OldFlowCardId { get; set; }
        /// <summary>
        /// 最初流程卡
        /// </summary>
        public string OldFlowCard { get; set; }
        /// <summary>
        /// 计划号ID
        /// </summary>
        public int ProductionId { get; set; }
        /// <summary>
        /// 计划号
        /// </summary>
        public string Production { get; set; }
        public int DeviceId { get; set; }
        /// <summary>
        /// 机台号
        /// </summary>
        public string Code { get; set; }
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
        public decimal Total { get; set; }
        public decimal HeGe { get; set; }
        public decimal LiePian { get; set; }
        /// <summary>
        /// 0 未处理  1 已处理  2 未查询到流程卡 3 未查询到加工人 4 未查询到设备
        /// </summary>
        public int State { get; set; }
        /// <summary>
        /// 次品原因
        /// </summary>
        public string Reason { get; set; }
        public IEnumerable<BadTypeCount> ReasonList => Reason.IsNullOrEmpty() ? new List<BadTypeCount>()
            : JsonConvert.DeserializeObject<List<BadTypeCount>>(Reason).OrderByDescending(x => x.count).ToList();

        /// <summary>
        /// 是否是末道工序
        /// </summary>
        public bool Last { get; set; }
        /// <summary>
        /// 单次合格率(%)
        /// </summary>
        public decimal QualifiedRate => Total != 0 ? ((decimal)HeGe * 100 / Total).ToRound() : (HeGe != 0 ? 100 : 0);
        /// <summary>
        /// 单次次品率(%)
        /// </summary>
        public decimal UnqualifiedRate => Total != 0 ? ((decimal)LiePian * 100 / Total).ToRound() : (HeGe != 0 ? 100 : 0);

        /// <summary>
        /// 加工日志的Id
        /// </summary>
        public int Id1 { get; set; }
        public ProcessType ProcessType { get; set; }
        public DateTime StartTime { get; set; }
        /// <summary>
        /// 更新
        /// </summary>
        [JsonIgnore]
        public bool Update { get; set; }
        /// <summary>
        /// 是否需要更新
        /// </summary>
        [JsonIgnore]
        public bool NeedUpdate => FlowCardId == 0 || OldFlowCardId == 0 || ProductionId == 0 || DeviceId == 0 || ProcessorId == 0;
    }


    public class ErpFlowCardReportGet
    {
        /// <summary>
        /// 自增id
        /// </summary>
        public int f_id { get; set; }
        /// <summary>
        /// 时间
        /// </summary>
        public DateTime f_inserttime { get; set; }
        /// <summary>
        /// 源流程卡
        /// </summary>
        public string f_lckh0 { get; set; }
        /// <summary>
        /// 流程卡
        /// </summary>
        public string f_lckh { get; set; }
        /// <summary>
        /// 单次加工数
        /// </summary>
        public decimal jgqty { get; set; }
        /// <summary>
        /// 单次合格数
        /// </summary>
        public decimal hgqty { get; set; }
        /// <summary>
        /// 机台号
        /// </summary>
        public string jth { get; set; }
        /// <summary>
        /// 加工人
        /// </summary>
        public string jgr { get; set; }
        /// <summary>
        /// 不良数据
        /// </summary>
        public object bl { get; set; }

        /// <summary>
        /// 在加工前上报还是加工后上报
        /// </summary>
        public bool back { get; set; } = true;
    }

    public class ErpUpdateFlowCardGet
    {
        /// <summary>
        /// 流程卡id
        /// </summary>
        public int Id { get; set; }
        public DateTime MarkedDateTime { get; set; }
        /// <summary>
        /// 总发出
        /// </summary>
        public decimal FaChu { get; set; }
        /// <summary>
        /// 合格
        /// </summary>
        public decimal HeGe { get; set; }
        /// <summary>
        /// 裂片
        /// </summary>
        public decimal LiePian { get; set; }
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
