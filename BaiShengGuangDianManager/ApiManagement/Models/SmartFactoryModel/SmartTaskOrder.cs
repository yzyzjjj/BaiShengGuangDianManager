﻿using ApiManagement.Models.BaseModel;
using ModelBase.Base.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartTaskOrder : CommonBase
    {
        /// <summary>
        /// 任务单
        /// </summary>
        public string TaskOrder { get; set; }
        /// <summary>
        /// 工单id
        /// </summary>
        public int WorkOrderId { get; set; }
        /// <summary>
        /// 计划号id
        /// </summary>
        public int ProductId { get; set; }
        /// <summary>
        /// 状态
        /// </summary>
        public SmartTaskOrderState State { get; set; }
        public string StateStr => State.ToString();
        /// <summary>
        /// 目标产量
        /// </summary>
        public int Target { get; set; }
        /// <summary>
        /// 已完成产量
        /// </summary>
        public int DoneTarget { get; set; }
        /// <summary>
        /// 剩余产量
        /// </summary>
        public int Left => Target > DoneTarget ? Target - DoneTarget : 0;
        /// <summary>
        /// 进度
        /// </summary>
        public decimal Progress => Target != 0 ? ((decimal)DoneTarget / Target).ToRound(4) * 100 : 0;
        /// <summary>
        /// 已完成卡数
        /// </summary>
        public int DoneCount { get; set; }
        /// <summary>
        /// 已完成数量
        /// </summary>
        public int Done { get; set; }
        /// <summary>
        /// 加工中卡数
        /// </summary>
        public int DoingCount { get; set; }
        /// <summary>
        /// 加工中数量
        /// </summary>
        public int Doing { get; set; }
        /// <summary>
        /// 已发流程卡 卡数量
        /// </summary>
        public int IssueCount { get; set; }
        /// <summary>
        /// 已发流程卡 加工数量
        /// </summary>
        public int Issue { get; set; }
        /// <summary>
        /// 发货数量
        /// </summary>
        public int Delivery { get; set; }
        /// <summary>
        /// 交货日期
        /// </summary>
        public DateTime DeliveryTime { get; set; }
        /// <summary>
        /// 实际完成日期
        /// </summary>
        public DateTime CompleteTime { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// 是否安排
        /// </summary>
        public bool Arranged { get; set; }
        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; }
        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime EndTime { get; set; }
        /// <summary>
        /// 耗时
        /// </summary>
        public int CostDay
        {
            get
            {
                if (StartTime != default(DateTime) && EndTime != default(DateTime))
                {
                    return (int)(EndTime - StartTime).TotalDays + 1;
                }
                return 0;
            }
        }
        /// <summary>
        /// 预计完成日期
        /// </summary>
        public DateTime EstimatedTime { get; set; }
        /// <summary>
        /// 等级
        /// </summary>
        public int LevelId { get; set; }
        /// <summary>
        /// 等级排序   越大越可以排程是越可以被改变
        /// </summary>
        public int Order { get; set; }

    }
    public class SmartTaskOrderDetail : SmartTaskOrder
    {
        /// <summary>
        /// 工单
        /// </summary>
        public string WorkOrder { get; set; }
        /// <summary>
        /// 计划号id
        /// </summary>
        public string Product { get; set; }
        /// <summary>
        /// 耗时
        /// </summary>
        public int Consume { get; set; }
        /// <summary>
        /// 按时率
        /// </summary>
        public decimal OnTimeRate { get; set; } = 100;
        /// <summary>
        /// 按时率
        /// </summary>
        public RiskLevelState RiskLevel { get; set; }
        /// <summary>
        /// 按时率
        /// </summary>
        public string RiskLevelStr => RiskLevel.ToString();

        /// <summary>
        /// 产能类型id
        /// </summary>
        public int CapacityId { get; set; }
        /// <summary>
        /// 产能类型
        /// </summary>
        public string Capacity { get; set; }
    }

    public class SmartTaskOrderDetailProduct : SmartTaskOrder
    {
        /// <summary>
        /// 计划号id
        /// </summary>
        public string Product { get; set; }
    }

    public class SmartTaskOrderDetailLevel : SmartTaskOrderDetailProduct
    {
        /// <summary>
        /// 等级
        /// </summary>
        public string Level { get; set; }
    }
    public class SmartTaskOrderPreview : SmartTaskOrder
    {
        /// <summary>
        /// 计划号id
        /// </summary>
        public string Product { get; set; }
        /// <summary>
        /// 等级
        /// </summary>
        public string Level { get; set; }
        public List<SmartTaskOrderNeedDetail> Needs { get; set; } = new List<SmartTaskOrderNeedDetail>();
        [JsonIgnore]
        public List<SmartTaskOrderNeedDetail> OldNeeds { get; set; } = new List<SmartTaskOrderNeedDetail>();
    }
    public class SmartTaskOrderConfirm : SmartTaskOrderDetail, ICloneable
    {
        /// <summary>
        /// 等级
        /// </summary>
        public string Level { get; set; }
        /// <summary>
        /// 按产能计算的生产天数
        /// </summary>
        public int CapacityCostDay { get; set; }
        /// <summary>
        /// 工期和交货期之间的距离
        /// </summary>
        public int DistanceDay => DeliveryDay - CapacityCostDay;
        //public int DistanceDay => DeliveryDay > CapacityCostDay ? DeliveryDay - CapacityCostDay : 0;
        /// <summary>
        /// critical ratio 重要比率, 交期减去目前日期之差额,再除以工期
        /// </summary>
        public decimal CR => CapacityCostDay != 0 ? ((decimal)DeliveryDay / CapacityCostDay).ToRound() : 0;
        /// <summary>
        /// 耗时
        /// </summary>
        public int TotalCostDay => Needs.Sum(x => x.CostDay);
        /// <summary>
        /// 按第一次生产时间计算天数
        /// </summary>
        public int DeliveryDay
        {
            get
            {
                if (EndTime == default(DateTime))
                {
                    return int.MaxValue;
                }

                var t = DateTime.Today;
                if (StartTime != default(DateTime))
                {
                    t = StartTime;
                }

                return EndTime >= t ? (int)(EndTime - t).TotalDays + 1 : 0;
            }
        }
        /// <summary>
        /// 逾期
        /// </summary>
        public int OverdueDay
        {
            get
            {
                if (EndTime != default(DateTime))
                {
                    var time = CompleteTime != default(DateTime) ? CompleteTime : DateTime.Today;
                    if (time > EndTime)
                    {
                        return (int)(time - EndTime).TotalDays;
                    }
                }
                return 0;
            }
        }
        /// <summary>
        /// 各工序加工需求
        /// </summary>
        public List<SmartTaskOrderSchedule> Needs { get; set; } = new List<SmartTaskOrderSchedule>();

        /// <summary>
        /// 检验工序是否都已完成，无可加工原料
        /// </summary>
        /// <param name="need"></param>
        /// <returns></returns>
        public bool AllDone(SmartTaskOrderSchedule need = null)
        {
            if (need == null)
            {
                return !Needs.Any() || Needs.All(x => x.Have == 0);
            }

            var index = Needs.IndexOf(need);
            var part = Needs.Take(index + 1);
            return !part.Any() || part.All(x => x.Have == 0);
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
    /// <summary>
    /// 任务单设计安排
    /// </summary>
    public class SmartTaskOrderArrange
    {
        public List<SmartTaskOrderConfirm> TaskOrders { get; set; } = new List<SmartTaskOrderConfirm>();
        public List<SmartTaskOrderScheduleDetail> Schedule { get; set; } = new List<SmartTaskOrderScheduleDetail>();
    }
}