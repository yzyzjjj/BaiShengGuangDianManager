using ApiManagement.Models.BaseModel;
using System.Collections.Generic;
using ApiManagement.Base.Server;
using ModelBase.Models.Result;
using System.Linq;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartDeviceCategoryHelper : DataHelper
    {
        private SmartDeviceCategoryHelper()
        {
            Table = "t_device_category";
            InsertSql =
                "INSERT INTO `t_device_category` (`CreateUserId`, `MarkedDateTime`, `WorkshopId`, `Category`, `Remark`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @WorkshopId, @Category, @Remark);";
            UpdateSql = "UPDATE `t_device_category` SET `MarkedDateTime` = @MarkedDateTime, `Category` = @Category, `Remark` = @Remark WHERE `Id` = @Id;";
        }
        public static readonly SmartDeviceCategoryHelper Instance = new SmartDeviceCategoryHelper();
        #region Get
        public static IEnumerable<SmartDeviceCategory> GetSmartDeviceCategory(int id, int wId)
        {
            var sql = $"SELECT * FROM `t_device_category` " +
                      $"WHERE{(id == 0 ? "" : " Id = @qId AND ")}{(wId == 0 ? "" : " WorkshopId = @wId AND ")}MarkedDelete = 0;";

            return ServerConfig.ApiDb.Query<SmartDeviceCategory>(sql, new { id, wId });
        }
        /// <summary>
        /// 菜单
        /// </summary>
        /// <param name="id"></param>
        /// <param name="wId"></param>
        /// <returns></returns>
        public static IEnumerable<dynamic> GetSmartDeviceCategoryMenu(int id, int wId)
        {
            var sql = $"SELECT Id, `Category` FROM `t_device_category` " +
                      $"WHERE{(id == 0 ? "" : " Id = @qId AND ")}{(wId == 0 ? "" : " WorkshopId = @wId AND ")}MarkedDelete = 0;";

            return ServerConfig.ApiDb.Query<SmartDeviceCategory>(sql, new { id, wId }).Select(x => new { x.Id, x.Category });
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