using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System.Collections.Generic;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartCapacityHelper : DataHelper
    {
        private SmartCapacityHelper()
        {
            Table = "t_capacity";
            InsertSql =
                "INSERT INTO `t_capacity` (`CreateUserId`, `MarkedDateTime`, `Capacity`, `CategoryId`, `Number`, `Last`, `Remark`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @Capacity, @CategoryId, @Number, @Last, @Remark);";
            UpdateSql =
                "UPDATE `t_capacity` SET `MarkedDateTime` = @MarkedDateTime, `Capacity` = @Capacity, `Remark` = @Remark WHERE `Id` = @Id;";
        }
        public static readonly SmartCapacityHelper Instance = new SmartCapacityHelper();
        #region Get
        /// <summary>
        /// 通过流程类型id获取产能类型
        /// </summary>
        /// <param name="processCodeCategoryIds">流程类型id</param>
        /// <returns></returns>
        public IEnumerable<SmartCapacity> GetSameSmartCapacities(IEnumerable<int> processCodeCategoryIds)
        {
            return ServerConfig.ApiDb.Query<SmartCapacity>(
                "SELECT * FROM `t_capacity` WHERE MarkedDelete = 0 AND CategoryId IN @processCodeCategoryIds;", new { processCodeCategoryIds });
        }

        public IEnumerable<SmartCapacity> GetSmartCapacities(IEnumerable<string> capacities)
        {
            return ServerConfig.ApiDb.Query<SmartCapacity>(
                "SELECT * FROM `t_capacity` WHERE MarkedDelete = 0 AND Capacity IN @capacities;", new { capacities });
        }
        #endregion

        #region Add
        #endregion

        #region Update
        public void UpdateSmartCapacity(SmartCapacity capacity)
        {
            ServerConfig.ApiDb.Execute(
              "UPDATE `t_capacity` SET `MarkedDateTime` = @MarkedDateTime, `CategoryId` = @CategoryId WHERE `Id` = @Id;", capacity);
        }
        #endregion

        #region Delete
        #endregion
    }
}
