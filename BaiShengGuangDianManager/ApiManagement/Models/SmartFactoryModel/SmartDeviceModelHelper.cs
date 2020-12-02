using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System.Collections.Generic;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartDeviceModelHelper : DataHelper
    {
        private SmartDeviceModelHelper()
        {
            Table = "t_device_model";
            InsertSql =
                "INSERT INTO `t_device_model` (`CreateUserId`, `MarkedDateTime`, `CategoryId`, `Model`, `Remark`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @CategoryId, @Model, @Remark);";
            UpdateSql = "UPDATE `t_device_model` SET `MarkedDateTime` = @MarkedDateTime, `CategoryId` = @CategoryId, `Model` = @Model, `Remark` = @Remark WHERE `Id` = @Id;";
        }
        public static readonly SmartDeviceModelHelper Instance = new SmartDeviceModelHelper();
        #region Get
        public IEnumerable<SmartDeviceModel> GetSmartDeviceModels(int categoryId)
        {
            return ServerConfig.ApiDb.Query<SmartDeviceModel>(
                "SELECT * FROM `t_device_model` WHERE MarkedDelete = 0 AND CategoryId = @categoryId;", new { categoryId });
        }

        public IEnumerable<SmartDeviceModelDetail> GetSmartDeviceModelDetails(int categoryId)
        {
            return ServerConfig.ApiDb.Query<SmartDeviceModelDetail>(
                "SELECT a.*, b.Category FROM `t_device_model` a JOIN `t_device_category` b ON a.CategoryId = b.Id WHERE a.MarkedDelete = 0 AND CategoryId = @categoryId;", new { categoryId });
        }

        public IEnumerable<SmartDeviceModelCount> GetModelCount(IEnumerable<int> modelIds)
        {
            return ServerConfig.ApiDb.Query<SmartDeviceModelCount>(
                 "SELECT ModelId, COUNT(1) Count FROM `t_device` WHERE ModelId IN @modelIds AND MarkedDelete = 0 GROUP BY ModelId;",
                 new
                 {
                     modelIds
                 });
        }

        public IEnumerable<SmartDeviceModelCount> GetNormalModelCount(IEnumerable<int> modelIds)
        {
            return ServerConfig.ApiDb.Query<SmartDeviceModelCount>(
                "SELECT ModelId, COUNT(1) Count FROM `t_device` WHERE ModelId IN @modelIds AND MarkedDelete = 0 AND State = @state GROUP BY ModelId;",
                new
                {
                    modelIds,
                    state = SmartDeviceState.正常
                });
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