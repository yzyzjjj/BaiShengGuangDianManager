using ApiManagement.Models.DeviceManagementModel;
using ModelBase.Base.Logger;
using ModelBase.Base.Utils;
using Newtonsoft.Json;
using ServiceStack;
using System;

namespace ApiManagement.Models.StatisticManagementModel
{

    public class RedisMonitoringProcess
    {
        public DateTime Time { get; set; }
        public int DeviceId { get; set; }
        public int State { get; set; }
    }
    public class MonitoringProcess : ICloneable
    {
        //public string TimeStr { get; set; }
        //private DateTime time { get; set; }
        //[JsonIgnore]
        //public DateTime Time 

        //{
        //    get => Convert.ToDateTime(TimeStr);
        //    set
        //    {
        //        time = value;
        //        TimeStr = time.ToStr();
        //    }
        //}
        public DateTime Time { get; set; }
        public int DeviceId { get; set; }
        public string Code { get; set; }
        public int ScriptId { get; set; }
        public int DeviceCategoryId { get; set; }
        public string CategoryName { get; set; }
        public int State { get; set; }
        /// <summary>
        /// 加工类型
        /// </summary>
        public ProcessType ProcessType { get; set; }
        /// <summary>
        /// 当前流程卡
        /// </summary>
        public int FlowCardId { get; set; }
        /// <summary>
        /// 当前流程卡
        /// </summary>
        public string FlowCard { get; set; } = "";
        /// <summary>
        /// 今日加工次数
        /// </summary>
        public int ProcessCount { get; set; }
        /// <summary>
        /// 总加工次数
        /// </summary>
        public int TotalProcessCount { get; set; }
        /// <summary>
        /// 今日加工时间(秒）
        /// </summary>
        public int ProcessTime { get; set; }
        /// <summary>
        /// 总加工时间(秒）
        /// </summary>
        public int TotalProcessTime { get; set; }
        /// <summary>
        /// 今日运行时间(秒）
        /// </summary>
        public int RunTime { get; set; }
        /// <summary>
        /// 总运行时间(秒）
        /// </summary>
        public int TotalRunTime { get; set; }


        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; }
        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime EndTime { get; set; }
        /// <summary>
        /// 其他数据
        /// </summary>
        public string Data { get; set; }
        private ExtraData _extraData { get; set; }
        [JsonIgnore]
        public ExtraData ExtraData
        {
            get
            {
                try
                {
                    _extraData = _extraData ?? (Data.IsNullOrEmpty() ? new ExtraData() : Data.ToClass<ExtraData>());
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }

                return _extraData;
            }
            set
            {
                _extraData = value;
            }
        }


        /// <summary>
        /// 使用台数
        /// </summary>
        public int Use { get; set; }
        /// <summary>
        /// 总台数
        /// </summary>
        public int Total { get; set; }
        /// <summary>
        /// 使用率%
        /// </summary>
        public decimal Rate { get; set; }
        /// <summary>
        /// 样本时间
        /// </summary>
        public DateTime SampleTime { get; set; }
        /// <summary>
        /// 加工日志Id  npc_monitoring_process_log
        /// </summary>
        public int LogId { get; set; }
        /// <summary>
        /// 加工日志Id时间  npc_monitoring_process_log
        /// </summary>
        public DateTime LogTime { get; set; }
        /// <summary>
        /// 是否有插入加工日志  npc_monitoring_process_log
        /// </summary>
        public bool NewLog => LogId == 0;

        public object Clone()
        {
            return MemberwiseClone();
        }

        public bool HaveChange(MonitoringProcess old)
        {
            //"UPDATE npc_proxy_link SET `Time` = @Time, `State` = @State, `ProcessCount` = @ProcessCount, `TotalProcessCount` = @TotalProcessCount, " +
            //    "`ProcessTime` = @ProcessTime, `TotalProcessTime` = @TotalProcessTime, `RunTime` = @RunTime, `TotalRunTime` = @TotalRunTime WHERE `DeviceId` = @DeviceId;",
            return Time != old.Time
                   || State != old.State
                   || ProcessCount != old.ProcessCount
                   || TotalProcessCount != old.TotalProcessCount
                   || ProcessTime != old.ProcessTime
                   || TotalProcessTime != old.TotalProcessTime
                   || RunTime != old.RunTime
                   || TotalRunTime != old.TotalRunTime;
        }
        public void UpdateExtraData()
        {
            if (ProcessType == ProcessType.Idle)
            {
                ExtraData.IdleTime = StartTime;
                ExtraData.IdleEndTime = EndTime;
            }
            else if (ProcessType == ProcessType.Wash)
            {
                ExtraData.WashTime = StartTime;
                ExtraData.WashEndTime = EndTime;
            }
            else if (ProcessType == ProcessType.Repair)
            {
                ExtraData.RepairTime = StartTime;
                ExtraData.RepairEndTime = EndTime;
            }
            else if (ProcessType == ProcessType.Process)
            {
                ExtraData.ProcessTime = StartTime;
                ExtraData.ProcessEndTime = EndTime;
            }
            Data = _extraData.ToJSON();
        }
    }

    public class ExtraData
    {
        /// <summary>
        /// 待机开始时间
        /// </summary>
        public DateTime IdleTime { get; set; }
        /// <summary>
        /// 待机结束时间
        /// </summary>
        public DateTime IdleEndTime { get; set; }
        /// <summary>
        /// 加工开始时间
        /// </summary>
        public DateTime ProcessTime { get; set; }
        /// <summary>
        /// 加工结束时间
        /// </summary>
        public DateTime ProcessEndTime { get; set; }
        /// <summary>
        /// 洗盘开始时间
        /// </summary>
        public DateTime WashTime { get; set; }
        /// <summary>
        /// 洗盘结束时间
        /// </summary>
        public DateTime WashEndTime { get; set; }
        /// <summary>
        /// 修盘开始时间
        /// </summary>
        public DateTime RepairTime { get; set; }
        /// <summary>
        /// 修盘结束时间
        /// </summary>
        public DateTime RepairEndTime { get; set; }
    }
}
