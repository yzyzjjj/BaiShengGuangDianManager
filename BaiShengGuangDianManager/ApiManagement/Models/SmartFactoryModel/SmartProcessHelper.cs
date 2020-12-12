using ApiManagement.Models.BaseModel;
using System.Collections.Generic;
using ApiManagement.Base.Server;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartProcessHelper : DataHelper
    {
        private SmartProcessHelper()
        {
            Table = "t_process";
            InsertSql =
                "INSERT INTO `t_process` (`CreateUserId`, `MarkedDateTime`, `Process`, `DeviceCategoryId`, `Remark`, `Order`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @Process, @DeviceCategoryId, @Remark, @Order);";
            UpdateSql =
                "UPDATE `t_process` SET `MarkedDateTime` = @MarkedDateTime, `Process` = @Process, `DeviceCategoryId` = @DeviceCategoryId, `Remark` = @Remark, `Order` = @Order WHERE `Id` = @Id;";
        }
        public static readonly SmartProcessHelper Instance = new SmartProcessHelper();
        #region Get
        //public IEnumerable<SmartProcess> GetSmartProcesses(int taskOrderId, int processCodeId)
        //{
        //    return ServerConfig.ApiDb.Query<SmartProcess>(
        //        "SELECT a.* FROM `t_product_process` a JOIN `t_task_order` b ON a.ProductId = b.ProductId WHERE a.MarkedDelete = 0 AND b.Id = @taskOrderId AND a.ProcessCodeId = @processCodeId"
        //        , new { taskOrderId, processCodeId });
        //}
        //public IEnumerable<SmartProcess> GetSmartProcessDevice(int taskOrderId, int processCodeId)
        //{
        //    return ServerConfig.ApiDb.Query<SmartProcess>(
        //        "SELECT a.* FROM `t_product_process` a JOIN `t_task_order` b ON a.ProductId = b.ProductId WHERE a.MarkedDelete = 0 AND b.Id = @taskOrderId AND a.ProcessCodeId = @processCodeId"
        //        , new { taskOrderId, processCodeId });
        //}
        #endregion

        #region Add
        #endregion

        #region Update
        #endregion

        #region Delete
        #endregion
    }
}
