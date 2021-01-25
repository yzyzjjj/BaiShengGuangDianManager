


using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartTaskOrderScheduleIndexHelper : DataHelper
    {
        private SmartTaskOrderScheduleIndexHelper()
        {
            Table = "t_task_order_schedule_index";
            InsertSql =
                "INSERT INTO `t_task_order_schedule_index` (`CreateUserId`, `MarkedDateTime`, `Batch`, `ProductType`, `ProcessTime`, `PId`, `DealId`, `Index`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @Batch, @ProductType, @ProcessTime, @PId, @DealId, @Index);";
            UpdateSql = "UPDATE `t_task_order_schedule_index` SET `MarkedDateTime` = @MarkedDateTime, `Index` = @Index WHERE `Id` = @Id;";
        }
        public static readonly SmartTaskOrderScheduleIndexHelper Instance = new SmartTaskOrderScheduleIndexHelper();
        #region Get
        public static IEnumerable<SmartTaskOrderScheduleIndex> GetSmartTaskOrderScheduleIndexByBatch(int batch, int taskOrderId = 0)
        {
            return ServerConfig.ApiDb.Query<SmartTaskOrderScheduleIndex>($"SELECT * FROM `t_task_order_schedule_index` " +
                                                                          $"WHERE MarkedDelete = 0 AND Batch = @batch{(taskOrderId == 0 ? "" : " AND TaskOrderId = @taskOrderId")};", new { batch, taskOrderId });
        }
        public static IEnumerable<SmartTaskOrderScheduleIndex> GetSmartTaskOrderScheduleIndex(DateTime startTime = default(DateTime), DateTime endTime = default(DateTime), int pId = 0)
        {
            var param = new List<string>();
            if (startTime != default(DateTime) && endTime != default(DateTime))
            {
                param.Add("ProcessTime >= @startTime AND ProcessTime <= @endTime");
            }
            if (startTime != default(DateTime) && endTime == default(DateTime))
            {
                param.Add("ProcessTime = @startTime");
            }
            if (pId != 0)
            {
                param.Add("PId = @pId");
            }
            return ServerConfig.ApiDb.Query<SmartTaskOrderScheduleIndex>(
                "SELECT * FROM (SELECT * FROM `t_task_order_schedule_index` " +
                $"{(param.Any() ? " WHERE " + param.Join(" AND ") : "")} " +
                "ORDER BY Batch DESC, ProcessTime DESC) a GROUP BY a.ProcessTime, a.PId, a.DealId;",
                new { startTime, endTime, pId });
            //return ServerConfig.ApiDb.Query<SmartTaskOrderScheduleIndex>(
            //    "SELECT a.* FROM `t_task_order_schedule_index` a " +
            //    $"JOIN (SELECT * FROM (SELECT Id, ProcessTime, Batch, TaskOrderId, PId, ProcessId FROM `t_task_order_schedule`{(param.Any() ? " WHERE " + param.Join(" AND ") : "")} ORDER BY Batch DESC, ProcessTime DESC) a GROUP BY a.ProcessTime, a.TaskOrderId, a.PId) b ON a.Id = b.Id ORDER BY a.ProcessTime, a.DealId",
            //    new { startTime, endTime, pId });
        }

        #endregion

        #region Add
        #endregion

        #region Update
        #endregion

        #region Delete
        #endregion
    }
}
