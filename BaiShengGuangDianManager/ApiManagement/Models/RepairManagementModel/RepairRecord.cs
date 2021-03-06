﻿using ModelBase.Base.Utils;
using ServiceStack;
using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.RepairManagementModel
{
    public class RepairRecord : FaultDevice
    {
        /// <summary>
        /// 故障解决者
        /// </summary>
        public string FaultSolver { get; set; }
        [Ignore]
        public List<string> FaultSolvers => FaultSolver.IsNullOrEmpty() ? new List<string>() : FaultSolver.Split(",").ToList();
        /// <summary>
        /// 故障解决时间
        /// </summary>
        public DateTime SolveTime { get; set; }
        /// <summary>
        /// 故障解决方案
        /// </summary>
        public string SolvePlan { get; set; }
        /// <summary>
        /// 维修故障类型Id
        /// </summary>
        public int FaultTypeId1 { get; set; }
        /// <summary>
        /// 是否为添加记录
        /// </summary>
        public bool IsAdd { get; set; }
        /// <summary>
        /// 评分
        /// </summary>
        public string Score { get; set; }
        /// <summary>
        /// 评论
        /// </summary>
        public string Comment { get; set; }
        [Ignore]
        public List<int> Scores => Score.IsNullOrEmpty() ? new List<int>() : Score.Split(",").Select(int.Parse).ToList();
        [Ignore]
        public string CostTime => FaultTime == default(DateTime) || SolveTime == default(DateTime)
                ? "" : DateTimeExtend.ToTimeStr((int)(SolveTime.NoSecond() - FaultTime.NoSecond()).TotalSeconds);

        private static IEnumerable<string> GetMembers(IEnumerable<string> except = null)
        {
            var result = typeof(RepairRecord).GetProperties().Where(x => (IgnoreAttribute)x.GetCustomAttributes(typeof(IgnoreAttribute), false).FirstOrDefault() == null).Select(y => y.Name);
            //var result = typeof(RepairRecord).GetProperties().Select(x => x.Name);
            return except == null ? result : result.Where(x => !except.Contains(x));
        }

        public static new string GetField(IEnumerable<string> except = null, string pre = "")
        {
            return GetMembers(except).Select(x => $"{pre}`{x}`").Join(", ");
        }
        /// <summary>
        /// 维修故障类型
        /// </summary>
        [Ignore]
        public string FaultTypeName1 { get; set; }
        /// <summary>
        /// 维修故障描述
        /// </summary>
        [Ignore]
        public string FaultDescription1 { get; set; }

    }
    public class RepairRecordDetail : RepairRecord
    {
        public string SiteName { get; set; }
        public string Name { get; set; }
        public string Account { get; set; }
        public string Phone { get; set; }
        /// <summary>
        /// 故障未指派 等待耗时(秒)
        /// </summary>
        public int NoAssignTime => AssignTime == default(DateTime) ? (int)(DateTime.Now - FaultTime).TotalSeconds : (int)(AssignTime - FaultTime).TotalSeconds;
        /// <summary>
        /// 故障已指派未维修耗时(秒)
        /// </summary>
        public int WaitTime => AssignTime != default(DateTime) && State == RepairStateEnum.Default ? (int)(DateTime.Now - AssignTime).TotalSeconds : (int)(StartTime - AssignTime).TotalSeconds;
        /// <summary>
        /// 故障维修耗时(秒)
        /// </summary>
        public int RepairCostTime => AssignTime != default(DateTime) && State != RepairStateEnum.Default
            ? (State == RepairStateEnum.Complete ? (int)(SolveTime - StartTime).TotalSeconds
                : (State > RepairStateEnum.Default ? (int)(DateTime.Now - StartTime).TotalSeconds : 0)) : 0;
        /// <summary>
        /// 故障时间
        /// </summary>
        public int TotalCostTime => State != RepairStateEnum.Complete ? (int)(DateTime.Now - FaultTime).TotalSeconds : (int)(SolveTime - FaultTime).TotalSeconds;
    }
}
