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
            InsertSql =
                "INSERT INTO `t_product_capacity` (`CreateUserId`, `MarkedDateTime`, `ProductId`, `ProcessId`, `Rate`, `Day`, `Hour`, `Min`, `Sec`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @ProductId, @ProcessId, @Rate, @Day, @Hour, @Min, @Sec);";
            UpdateSql =
                "UPDATE `t_product_capacity` SET `MarkedDateTime` = @MarkedDateTime, `Rate` = @Rate, " +
                "`Day` = @Day, `Hour` = @Hour, `Min` = @Min, `Sec` = @Sec WHERE `Id` = @Id;";
        }
        public static readonly SmartProductCapacityHelper Instance = new SmartProductCapacityHelper();
        #region Get
        public IEnumerable<SmartProductCapacity> GetSmartProductCapacities(IEnumerable<int> productIds)
        {
            return ServerConfig.ApiDb.Query<SmartProductCapacity>(
                "SELECT * FROM `t_product_capacity` WHERE MarkedDelete = 0 AND ProductId IN @productIds;", new { productIds });
        }
        #endregion

        #region Add
        #endregion

        #region Update
        #endregion

        #region Delete
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <param name="productId"></param>
        public void DeleteByProductId(int productId)
        {
            ServerConfig.ApiDb.Execute($"UPDATE `{Table}` SET `MarkedDateTime`= NOW(), `MarkedDelete`= true WHERE `ProductId` = @productId;", new { productId });
        }
        public void DeleteByProductId(IEnumerable<int> productIds)
        {
            ServerConfig.ApiDb.Execute($"UPDATE `{Table}` SET `MarkedDateTime`= NOW(), `MarkedDelete`= true WHERE `ProductId` IN @productIds;", new { productIds });
        }
        #endregion
    }
}
