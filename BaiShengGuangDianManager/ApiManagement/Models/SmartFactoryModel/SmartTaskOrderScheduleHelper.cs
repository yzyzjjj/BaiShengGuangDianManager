using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartTaskOrderScheduleHelper : DataHelper
    {
        private SmartTaskOrderScheduleHelper()
        {
            Table = "t_task_order_schedule";
            InsertSql =
                "INSERT INTO `t_task_order_schedule` (`CreateUserId`, `MarkedDateTime`, `Batch`, `IsDevice`, `ProcessTime`, `TaskOrderId`, `ProcessId`, `PId`, `ProductId`, `Target`, `Put`, `HavePut`, `Rate`, `Devices`, `Operators`, `Stock`, `DoneTarget`, `Done`, `DoingCount`, `Doing`, `IssueCount`, `Issue`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @Batch, @IsDevice, @ProcessTime, @TaskOrderId, @ProcessId, @PId, @ProductId, @Target, @Put, @HavePut, @Rate, @Devices, @Operators, @Stock, @DoneTarget, @Done, @DoingCount, @Doing, @IssueCount, @Issue);";
            UpdateSql = "UPDATE `t_task_order_schedule` SET `MarkedDateTime` = @MarkedDateTime, `Target` = @Target, `Put` = @Put, `Rate` = @Rate WHERE `Id` = @Id;";
        }
        public static readonly SmartTaskOrderScheduleHelper Instance = new SmartTaskOrderScheduleHelper();
        #region Get
        public IEnumerable<SmartTaskOrderScheduleDetail> GetSmartTaskOrderScheduleByBatch(int batch, int taskOrderId = 0)
        {
            return ServerConfig.ApiDb.Query<SmartTaskOrderScheduleDetail>($"SELECT a.*, b.`Order`, b.Process, c.TaskOrder, d.Product FROM `t_task_order_schedule` a " +
                                                                          "JOIN `t_process` b ON a.PId = b.Id " +
                                                                          "JOIN `t_task_order` c ON a.TaskOrderId = c.Id " +
                                                                          "JOIN `t_product` d ON a.ProductId = d.Id " +
                                                                          "JOIN `t_product_capacity` e ON a.ProductId = e.ProductId AND a.ProcessId = e.ProcessId " +
                                                                          $"WHERE a.MarkedDelete = 0 AND a.Batch = @batch{(taskOrderId == 0 ? "" : " AND a.TaskOrderId = @taskOrderId")};", new { batch, taskOrderId });
        }

        public IEnumerable<SmartTaskOrderScheduleDetail> GetSmartTaskOrderScheduleByTaskOrderIds(IEnumerable<int> taskOrderIds)
        {
            return ServerConfig.ApiDb.Query<SmartTaskOrderScheduleDetail>($"SELECT a.*, b.`Order`, b.Process, c.TaskOrder, d.Product FROM `t_task_order_schedule` a " +
                                                                          "JOIN `t_process` b ON a.PId = b.Id " +
                                                                          "JOIN `t_task_order` c ON a.TaskOrderId = c.Id " +
                                                                          "JOIN `t_product` d ON a.ProductId = d.Id " +
                                                                          "JOIN `t_product_capacity` e ON a.ProductId = e.ProductId AND a.ProcessId = e.ProcessId " +
                                                                          "JOIN (SELECT * FROM (SELECT ProcessTime, Batch FROM `t_task_order_schedule` ORDER BY Batch DESC, ProcessTime DESC) a GROUP BY a.ProcessTime) f ON a.ProcessTime = f.ProcessTime AND a.Batch = f.Batch " +
                                                                          $"WHERE a.MarkedDelete = 0 AND a.TaskOrderId IN @taskOrderIds", new { taskOrderIds });
        }

        public int GetSmartTaskOrderScheduleBatch(int taskOrderId = 0)
        {
            return ServerConfig.ApiDb.Query<int>($"SELECT IFNULL(MAX(Batch), 0) FROM `t_task_order_schedule` " +
                                                 $"WHERE MarkedDelete = 0{(taskOrderId == 0 ? "" : " AND TaskOrderId = @taskOrderId")};", new { taskOrderId }).FirstOrDefault();
        }

        #endregion

        #region Add
        #endregion

        #region Update
        public void UpdateSmartTaskOrderSchedule(IEnumerable<SmartTaskOrderSchedule> schedules)
        {
            ServerConfig.ApiDb.Execute($"UPDATE `t_task_order_schedule` SET `MarkedDateTime` = @MarkedDateTime, `DoneTarget` = @DoneTarget, `Done` = @Done, `DoingCount` = @DoingCount, `Doing` = @Doing, `IssueCount` = @IssueCount, `Issue` = @Issue, `CompleteTime` = @CompleteTime WHERE `Id` = @Id;", new { schedules });
        }
        #endregion

        #region Delete

        #endregion
    }
}