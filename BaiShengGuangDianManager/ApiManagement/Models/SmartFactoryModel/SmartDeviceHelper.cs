using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartDeviceHelper : DataHelper
    {
        private SmartDeviceHelper()
        {
            Table = "t_device";
            InsertSql =
                "INSERT INTO `t_device` (`CreateUserId`, `MarkedDateTime`, `WorkshopId`, `Code`, `CategoryId`, `ModelId`, `Remark`, `Priority`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @WorkshopId, @Code, @CategoryId, @ModelId, @Remark, @Priority);";
            UpdateSql = "UPDATE `t_device` SET `MarkedDateTime` = @MarkedDateTime, `State` = @State, `Code` = @Code, `CategoryId` = @CategoryId, `ModelId` = @ModelId, `Remark` = @Remark, `Priority` = @Priority WHERE `Id` = @Id;";
        }
        public static readonly SmartDeviceHelper Instance = new SmartDeviceHelper();
        #region Get

        /// <summary>
        /// 获取状态正常的设备
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<SmartDeviceDetail> GetNormalSmartDevices(int wId = 0)
        {
            return ServerConfig.ApiDb.Query<SmartDeviceDetail>(
                $"SELECT a.*, b.Model FROM `t_device` a " +
                $"JOIN `t_device_model` b ON a.ModelId = b.Id " +
                $"WHERE a.MarkedDelete = 0 AND a.State = @state{(wId == 0 ? "" : " AND a.WorkshopId = @wId")};"
                , new { state = SmartDeviceState.正常 });
        }

        public static IEnumerable<SmartDeviceDetail> GetSmartDevice(int id, int wId)
        {
            var sql = $"SELECT a.*, b.Category, c.Model FROM t_device a " +
                      $"JOIN t_device_category b ON a.CategoryId = b.Id AND a.WorkshopId = b.Id " +
                      $"JOIN t_device_model c ON a.ModelId = c.Id " +
                      $"WHERE{(id == 0 ? "" : " Id = @qId AND ")}{(wId == 0 ? "" : " WorkshopId = @wId AND ")}MarkedDelete = 0;";

            return ServerConfig.ApiDb.Query<SmartDeviceDetail>(sql, new { id, wId });
        }
        /// <summary>
        /// 菜单
        /// </summary>
        /// <param name="id"></param>
        /// <param name="wId"></param>
        /// <returns></returns>
        public static IEnumerable<dynamic> GetSmartDeviceMenu(int id, int wId)
        {
            var sql = $"SELECT Id, `Code` FROM t_device " +
                      $"WHERE{(id == 0 ? "" : " Id = @qId AND ")}{(wId == 0 ? "" : " WorkshopId = @wId AND ")}MarkedDelete = 0;";

            return ServerConfig.ApiDb.Query<SmartDevice>(sql, new { id, wId }).Select(x => new { x.Id, x.Code });
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