using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System;
using System.Collections.Generic;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartTaskOrderScheduleHelper : DataHelper
    {
        private SmartTaskOrderScheduleHelper()
        {
            Table = "t_task_order_schedule";
            InsertSql =
                "INSERT INTO `t_task_order_schedule` (`CreateUserId`, `MarkedDateTime`, `Batch`, `ArrangeOrder`, `ProductType`, `ProcessTime`, `TaskOrderId`, `ProcessId`, `PId`, `ProductId`, `Target`, `Put`, `HavePut`, `Rate`, `Devices`, `Operators`, `Stock`, `DoneTarget`, `Done`, `DoingCount`, `Doing`, `IssueCount`, `Issue`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @Batch, @ArrangeOrder, @ProductType, @ProcessTime, @TaskOrderId, @ProcessId, @PId, @ProductId, @Target, @Put, @HavePut, @Rate, @Devices, @Operators, @Stock, @DoneTarget, @Done, @DoingCount, @Doing, @IssueCount, @Issue);";
            UpdateSql = "UPDATE `t_task_order_schedule` SET `MarkedDateTime` = @MarkedDateTime, `Target` = @Target, `Put` = @Put, `Rate` = @Rate WHERE `Id` = @Id;";
        }
        public static readonly SmartTaskOrderScheduleHelper Instance = new SmartTaskOrderScheduleHelper();
        #region Get
        /// <summary>
        /// 获取最新安排
        /// </summary>
        /// <param name="taskOrderIds"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        public static IEnumerable<SmartTaskOrderScheduleDetail> GetSmartTaskOrderSchedule(IEnumerable<int> taskOrderIds, DateTime startTime = default(DateTime), DateTime endTime = default(DateTime))
        {
            return ServerConfig.ApiDb.Query<SmartTaskOrderScheduleDetail>(
                $"SELECT a.*, b.`Order`, b.Process, c.TaskOrder, d.Product FROM `t_task_order_schedule` a " +
                "JOIN `t_process` b ON a.PId = b.Id " +
                "JOIN `t_task_order` c ON a.TaskOrderId = c.Id " +
                "JOIN `t_product` d ON a.ProductId = d.Id " +
                "JOIN `t_product_capacity` e ON a.ProductId = e.ProductId AND a.ProcessId = e.ProcessId " +
                $"JOIN (SELECT * FROM (SELECT Id, ProcessTime, Batch, TaskOrderId, PId, ProcessId FROM `t_task_order_schedule` WHERE TaskOrderId IN @taskOrderIds{(startTime != default(DateTime) && endTime != default(DateTime) ? " AND ProcessTime >= @startTime AND ProcessTime <= @endTime" : "")} ORDER BY Batch DESC, ProcessTime DESC) a GROUP BY a.ProcessTime, a.TaskOrderId, a.PId) f ON a.Id = f.Id ORDER BY a.ProcessTime, a.Id",
                new { taskOrderIds, startTime, endTime });
        }
        /// <summary>
        /// 获取最新安排
        /// </summary>
        /// <param name="taskOrderId"></param>
        /// <param name="pId"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        public static IEnumerable<SmartTaskOrderScheduleDetail> GetSmartTaskOrderSchedule(int taskOrderId, int pId, DateTime startTime = default(DateTime), DateTime endTime = default(DateTime))
        {
            return ServerConfig.ApiDb.Query<SmartTaskOrderScheduleDetail>(
                $"SELECT a.*, b.`Order`, b.Process, c.TaskOrder, d.Product FROM `t_task_order_schedule` a " +
                "JOIN `t_process` b ON a.PId = b.Id " +
                "JOIN `t_task_order` c ON a.TaskOrderId = c.Id " +
                "JOIN `t_product` d ON a.ProductId = d.Id " +
                "JOIN `t_product_capacity` e ON a.ProductId = e.ProductId AND a.ProcessId = e.ProcessId " +
                $"JOIN (SELECT * FROM (SELECT Id, ProcessTime, Batch, TaskOrderId, PId, ProcessId FROM `t_task_order_schedule` WHERE TaskOrderId = @taskOrderId {(pId == 0 ? "" : " AND PId = @pId")} {(startTime != default(DateTime) && endTime != default(DateTime) ? " AND ProcessTime >= @startTime AND ProcessTime <= @endTime" : "")} ORDER BY Batch DESC, ProcessTime DESC) a GROUP BY a.ProcessTime, a.TaskOrderId, a.PId) f ON a.Id = f.Id ORDER BY a.ProcessTime",
                new { taskOrderId, startTime, endTime, pId });
        }
        /// <summary>
        /// 获取最新安排
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        public static IEnumerable<SmartTaskOrderScheduleDetail> GetSmartTaskOrderSchedule(DateTime startTime = default(DateTime), DateTime endTime = default(DateTime))
        {
            return ServerConfig.ApiDb.Query<SmartTaskOrderScheduleDetail>(
                $"SELECT a.*, b.`Order`, b.Process, c.TaskOrder, d.Product FROM `t_task_order_schedule` a " +
                "JOIN `t_process` b ON a.PId = b.Id " +
                "JOIN `t_task_order` c ON a.TaskOrderId = c.Id " +
                "JOIN `t_product` d ON a.ProductId = d.Id " +
                "JOIN `t_product_capacity` e ON a.ProductId = e.ProductId AND a.ProcessId = e.ProcessId " +
                $"JOIN (SELECT * FROM (SELECT Id, ProcessTime, Batch, TaskOrderId, PId, ProcessId FROM `t_task_order_schedule`{(startTime != default(DateTime) && endTime != default(DateTime) ? " WHERE ProcessTime >= @startTime AND ProcessTime <= @endTime" : "")} ORDER BY Batch DESC, ProcessTime DESC) a GROUP BY a.ProcessTime, a.TaskOrderId, a.PId) f ON a.Id = f.Id ORDER BY a.ProcessTime, a.Id",
                new { startTime, endTime });
        }
        #endregion

        #region Add
        #endregion

        #region Update
        public static void UpdateSmartTaskOrderSchedule(IEnumerable<SmartTaskOrderSchedule> schedules)
        {
            ServerConfig.ApiDb.Execute($"UPDATE `t_task_order_schedule` SET `MarkedDateTime` = @MarkedDateTime, `DoneTarget` = @DoneTarget, `Done` = @Done, `DoingCount` = @DoingCount, `Doing` = @Doing, `IssueCount` = @IssueCount, `Issue` = @Issue, `CompleteTime` = @CompleteTime WHERE `Id` = @Id;", new { schedules });
        }
        #endregion

        #region Delete

        #endregion
    }
}