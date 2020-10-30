using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System.Collections.Generic;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartProductProcessHelper : DataHelper
    {
        private SmartProductProcessHelper()
        {
            Table = "t_product_process";
            InsertSql =
                "INSERT INTO `t_product_process` (`CreateUserId`, `MarkedDateTime`, `ProductId`, `ProcessCodeId`, `ProcessId`, `ProcessRepeat`, `ProcessNumber`, `ProcessData`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @ProductId, @ProcessCodeId, @ProcessId, @ProcessRepeat, @ProcessNumber, @ProcessData);";
            UpdateSql =
                "UPDATE `t_product_process` SET `MarkedDateTime` = @MarkedDateTime, `ProcessRepeat` = @ProcessRepeat, `ProcessNumber` = @ProcessNumber, `ProcessData` = @ProcessData " +
                "WHERE `Id` = @Id;";
        }
        public static readonly SmartProductProcessHelper Instance = new SmartProductProcessHelper();
        #region Get
        public IEnumerable<SmartProductProcess> GetSameSmartProductProcesses(IEnumerable<int> productIds)
        {
            return ServerConfig.ApiDb.Query<SmartProductProcess>(
                "SELECT * FROM `t_product_process` WHERE MarkedDelete = 0 AND ProductId IN @productIds;", new { productIds });
        }
        //public IEnumerable<SmartProductProcess> GetSameSmartProductProcesss(IEnumerable<string> products, IEnumerable<int> productIds)
        //{
        //    return ServerConfig.ApiDb.Query<SmartProductProcess>(
        //        "SELECT Id, Product FROM `t_product` WHERE MarkedDelete = 0 AND Product IN @products AND Id NOT IN @productIds;"
        //        , new { products, productIds });
        //}

        public IEnumerable<SmartProductProcess> GetSmartProductProcesses(int taskOrderId, int processCodeId)
        {
            return ServerConfig.ApiDb.Query<SmartProductProcess>(
                "SELECT a.* FROM `t_product_process` a JOIN `t_task_order` b ON a.ProductId = b.ProductId WHERE a.MarkedDelete = 0 AND b.Id = @taskOrderId AND a.ProcessCodeId = @processCodeId"
                , new { taskOrderId, processCodeId });
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
