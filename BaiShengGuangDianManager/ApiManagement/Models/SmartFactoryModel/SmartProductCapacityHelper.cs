using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System.Collections.Generic;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartProductCapacityHelper : DataHelper
    {
        private SmartProductCapacityHelper()
        {
            Table = "t_product_capacity";
            ParentField = "ProductId";
            InsertSql =
                "INSERT INTO `t_product_capacity` (`CreateUserId`, `MarkedDateTime`, `ProductId`, `ProcessId`, `Rate`, `Day`, `Hour`, `Min`, `Sec`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @ProductId, @ProcessId, @Rate, @Day, @Hour, @Min, @Sec);";
            UpdateSql =
                "UPDATE `t_product_capacity` SET `MarkedDateTime` = @MarkedDateTime, `Rate` = @Rate, " +
                "`Day` = @Day, `Hour` = @Hour, `Min` = @Min, `Sec` = @Sec WHERE `Id` = @Id;";
        }
        public static readonly SmartProductCapacityHelper Instance = new SmartProductCapacityHelper();
        #region Get
        public static IEnumerable<SmartProductCapacityDetail> GetSmartProductCapacities(IEnumerable<int> productIds)
        {
            return ServerConfig.ApiDb.Query<SmartProductCapacityDetail>(
                "SELECT a.*, b.CapacityId FROM `t_product_capacity` a JOIN `t_product` b ON a.ProductId = b.Id WHERE a.MarkedDelete = 0 AND a.ProductId IN @productIds;", new { productIds });
        }
        public static IEnumerable<SmartProductCapacityDetail> GetAllSmartProductCapacities(IEnumerable<int> productIds)
        {
            return ServerConfig.ApiDb.Query<SmartProductCapacityDetail>(
                "SELECT a.*, b.CapacityId FROM `t_product_capacity` a JOIN `t_product` b ON a.ProductId = b.Id WHERE a.ProductId IN @productIds;", new { productIds });
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
