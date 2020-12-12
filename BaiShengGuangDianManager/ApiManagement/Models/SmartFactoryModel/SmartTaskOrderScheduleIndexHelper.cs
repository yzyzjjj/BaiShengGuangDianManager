


using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
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
                "INSERT INTO `t_task_order_schedule_index` (`CreateUserId`, `MarkedDateTime`, `Batch`, `IsDevice`, `ProcessTime`, `PId`, `DealId`, `Index`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @Batch, @IsDevice, @ProcessTime, @PId, @DealId, @Index);";
            UpdateSql = "UPDATE `t_task_order_schedule_index` SET `MarkedDateTime` = @MarkedDateTime, `Index` = @Index WHERE `Id` = @Id;";
        }
        public static readonly SmartTaskOrderScheduleIndexHelper Instance = new SmartTaskOrderScheduleIndexHelper();
        #region Get
        public IEnumerable<SmartTaskOrderScheduleIndex> GetSmartTaskOrderScheduleIndexByBatch(int batch, int taskOrderId = 0)
        {
            return ServerConfig.ApiDb.Query<SmartTaskOrderScheduleIndex>($"SELECT * FROM `t_task_order_schedule_index` " +
                                                                          $"WHERE MarkedDelete = 0 AND Batch = @batch{(taskOrderId == 0 ? "" : " AND TaskOrderId = @taskOrderId")};", new { batch, taskOrderId });
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
