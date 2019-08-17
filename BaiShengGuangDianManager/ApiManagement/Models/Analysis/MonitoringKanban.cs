﻿using ModelBase.Base.Utils;
using Newtonsoft.Json;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.Analysis
{
    public class MonitoringKanban
    {
        [JsonIgnore]
        public bool Init = false;
        [JsonIgnore]
        public int InitCount = 0;
        [JsonIgnore]
        public DateTime Date => Time.Date;
        public DateTime Time { get; set; }
        /// <summary>
        /// 总设备数量
        /// </summary>
        public int AllDevice { get; set; } = 0;
        /// <summary>
        /// 正常运行设备数量
        /// </summary>
        public int NormalDevice { get; set; } = 0;
        /// <summary>
        /// 加工中设备数量
        /// </summary>
        public int ProcessDevice { get; set; } = 0;
        /// <summary>
        /// 闲置设备数量
        /// </summary>
        public int IdleDevice => NormalDevice - ProcessDevice;
        /// <summary>
        /// 故障设备数量
        /// </summary>
        public int FaultDevice { get; set; } = 0;
        /// <summary>
        /// 连接异常设备数量
        /// </summary>
        public int ConnectErrorDevice => AllDevice - NormalDevice - FaultDevice;
        /// <summary>
        /// 日最大使用台数
        /// </summary>
        public int MaxUse { get; set; } = 0;

        /// <summary>
        /// 已使用台数
        /// </summary>
        [JsonIgnore]
        private List<int> _useList;

        public List<int> UseList
        {
            get
            {
                if (_useList == null)
                {
                    _useList = !UseListStr.IsNullOrEmpty()
                        ? JsonConvert.DeserializeObject<List<int>>(UseListStr)
                        : new List<int>();
                }
                _useList = _useList.OrderBy(x => x).ToList();

                return _useList;
            }
            set
            {
                _useList = value;
                UseListStr = _useList.OrderBy(x => x).ToJson();
            }
        }

        [JsonIgnore]
        public string UseListStr { get; set; }

        public List<string> UseCodeList { get; set; }
        /// <summary>
        /// 日最小使用台数
        /// </summary>
        public int MinUse { get; set; } = 0;
        /// <summary>
        /// 日最大使用率
        /// </summary>
        public decimal MaxUseRate => AllDevice != 0 ? (MaxUse * 1m / AllDevice).ToRound(4) : 0;
        /// <summary>
        /// 日最小使用率
        /// </summary>
        public decimal MinUseRate => AllDevice != 0 ? (MinUse * 1m / AllDevice).ToRound(4) : 0;
        /// <summary>
        /// 最大同时使用台数日
        /// </summary>
        public decimal MaxSimultaneousUseRate { get; set; } = 0;
        /// <summary>
        /// 最小同时使用台数日
        /// </summary>
        public decimal MinSimultaneousUseRate { get; set; } = 0;
        /// <summary>
        /// 单台加工利用率=加工时间/24h
        /// </summary>
        public List<ProcessUseRate> SingleProcessRate { get; set; } = new List<ProcessUseRate>();
        [JsonIgnore]
        public string SingleProcessRateStr => SingleProcessRate.ToJson();
        /// <summary>
        /// 所有利用率=总加工时间/（机台号*24h）
        /// </summary>
        public decimal AllProcessRate { get; set; } = 0;
        /// <summary>
        /// 运行时间
        /// </summary>
        public int RunTime { get; set; } = 0;
        /// <summary>
        /// 加工时间
        /// </summary>
        public int ProcessTime { get; set; } = 0;
        /// <summary>
        /// 闲置时间 = 运行时间-加工时间
        /// </summary>
        public int IdleTime => RunTime - ProcessTime;

        public bool Update(MonitoringKanban monitoringKanban)
        {
            if (!monitoringKanban.Init)
            {
                return false;
            }

            Time = monitoringKanban.Time;
            AllDevice = monitoringKanban.AllDevice;
            NormalDevice = monitoringKanban.NormalDevice;
            ProcessDevice = monitoringKanban.ProcessDevice;
            FaultDevice = monitoringKanban.FaultDevice;
            if (MaxUse < monitoringKanban.MaxUse)
            {
                MaxUse = monitoringKanban.MaxUse;
            }
            UseList = monitoringKanban.UseList;
            UseCodeList = monitoringKanban.UseCodeList;
            if (MinUse == 0)
            {
                MinUse = MaxUse;
            }
            if (MinUse > monitoringKanban.MinUse)
            {
                MinUse = monitoringKanban.MinUse;
            }
            if (MaxSimultaneousUseRate < monitoringKanban.MaxSimultaneousUseRate)
            {
                MaxSimultaneousUseRate = monitoringKanban.MaxSimultaneousUseRate;
            }
            if (MinSimultaneousUseRate == 0)
            {
                MinSimultaneousUseRate = MaxSimultaneousUseRate;
            }
            if (MinSimultaneousUseRate > monitoringKanban.MinSimultaneousUseRate)
            {
                MinSimultaneousUseRate = monitoringKanban.MinSimultaneousUseRate;
            }

            SingleProcessRate = monitoringKanban.SingleProcessRate;
            AllProcessRate = monitoringKanban.AllProcessRate;
            RunTime = monitoringKanban.RunTime;
            ProcessTime = monitoringKanban.ProcessTime;
            return true;
        }
    }

    public class ProcessUseRate
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public decimal Rate { get; set; }
    }
}