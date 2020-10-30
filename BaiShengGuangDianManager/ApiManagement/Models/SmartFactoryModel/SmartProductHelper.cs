using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System.Collections.Generic;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartProductHelper : DataHelper
    {
        private SmartProductHelper()
        {
            Table = "t_product";
            InsertSql =
                "INSERT INTO `t_product` (`CreateUserId`, `MarkedDateTime`, `Product`, `Remark`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @Product, @Remark);";
            UpdateSql =
                "UPDATE `t_product` SET `MarkedDateTime` = @MarkedDateTime, `Product` = @Product, `Remark` = @Remark WHERE `Id` = @Id;";
        }
        public static readonly SmartProductHelper Instance = new SmartProductHelper();
        #region Get
        public IEnumerable<SmartProduct> GetSameSmartProducts(IEnumerable<string> products, IEnumerable<int> productIds)
        {
            return ServerConfig.ApiDb.Query<SmartProduct>(
                "SELECT Id, Product FROM `t_product` WHERE MarkedDelete = 0 AND Product IN @products AND Id NOT IN @productIds;"
                , new { products, productIds });
        }

        public IEnumerable<SmartProduct> GetSmartProductsByProducts(IEnumerable<string> products)
        {
            return ServerConfig.ApiDb.Query<SmartProduct>(
                "SELECT * FROM `t_product` WHERE MarkedDelete = 0 AND Product IN @products;", new { products });
        }
        //public IEnumerable<SmartProduct> GetSmartProducts(int taskOrderId, int processCodeId)
        //{
        //    return ServerConfig.ApiDb.Query<SmartProduct>(
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
