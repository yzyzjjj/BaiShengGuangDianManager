using ApiManagement.Models.BaseModel;
using ModelBase.Base.Utils;
using System;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartFlowCardProcessLog : CommonBase
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="wId"></param>
        /// <param name="createUserId"></param>
        /// <param name="markedDateTime"></param>
        /// <param name="processId">流程id</param>
        /// <param name="processorId">加工人</param>
        /// <param name="deviceId">设备</param>
        /// <param name="startTime">加工开始时间</param>
        /// <param name="endTime">加工结束时间</param>
        /// <param name="before">发出数量</param>
        /// <param name="count">加工次数</param>
        /// <param name="qualified">合格</param>
        /// <param name="unqualified">不合格</param>
        public SmartFlowCardProcessLog(int wId, string createUserId, DateTime markedDateTime, int processId, int processorId, int deviceId,
            DateTime startTime, DateTime endTime, int before, int count, int qualified, int unqualified)
        {
            WorkshopId = wId;
            MarkedDateTime = markedDateTime;
            ProcessId = processId;
            ProcessorId = processorId;
            DeviceId = deviceId;
            StartTime = startTime;
            EndTime = endTime;
            Count = count;
            Before = before;
            Qualified = qualified;
            Unqualified = unqualified;
        }

        public SmartFlowCardProcessLog(int wId, string createUserId, DateTime markedDateTime, SmartFlowCardProcess process, int qualified, int unqualified)
        {
            WorkshopId = wId;
            CreateUserId = createUserId;
            MarkedDateTime = markedDateTime;
            ProcessId = process.Id;
            DeviceId = process.DeviceId;
            StartTime = process.StartTime;
            EndTime = process.EndTime;
            Count = process.Count;
            Before = process.Before;
            Qualified = qualified;
            Unqualified = unqualified;
        }
        /// <summary>
        /// 车间Id
        /// </summary>
        public int WorkshopId { get; set; }
        /// <summary>
        /// 计划号流程卡id
        /// </summary>
        public int ProcessId { get; set; }
        /// <summary>
        /// 加工人
        /// </summary>
        public int ProcessorId { get; set; }
        /// <summary>
        /// 加工设备
        /// </summary>
        public int DeviceId { get; set; }
        /// <summary>
        /// 加工开始时间
        /// </summary>
        public DateTime StartTime { get; set; }
        /// <summary>
        /// 加工结束时间
        /// </summary>
        public DateTime EndTime { get; set; }
        /// <summary>
        /// 加工次数
        /// </summary>
        public int Count { get; set; }
        /// <summary>
        /// 加工前数量
        /// </summary>
        public int Before { get; set; }
        /// <summary>
        /// 加工数量
        /// </summary>
        public int Doing => Qualified + Unqualified;
        /// <summary>
        /// 合格数量
        /// </summary>
        public int Qualified { get; set; }
        /// <summary>
        /// 不合格数量
        /// </summary>
        public int Unqualified { get; set; }
        /// <summary>
        /// 合格率
        /// </summary>
        public decimal Rate => Doing != 0 ? ((decimal)Qualified / Doing).ToRound(4) * 100 : 0;

    }
}