using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System.Collections.Generic;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartCapacityListHelper : DataHelper
    {
        private SmartCapacityListHelper()
        {
            Table = "t_capacity_list";
            InsertSql =
                "INSERT INTO `t_capacity_list` (`CreateUserId`, `MarkedDateTime`, `CapacityId`, `ProcessId`, `DeviceModel`, `DeviceNumber`, `OperatorLevel`, `OperatorNumber`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @CapacityId, @ProcessId, @DeviceModel, @DeviceNumber, @OperatorLevel, @OperatorNumber);";
            UpdateSql =
                "UPDATE `t_capacity_list` SET `MarkedDateTime` = @MarkedDateTime, `CapacityId` = @CapacityId, `ProcessId` = @ProcessId, `DeviceModel` = @DeviceModel, " +
                "`DeviceNumber` = @DeviceNumber, `OperatorLevel` = @OperatorLevel, `OperatorNumber` = @OperatorNumber WHERE `Id` = @Id;";
        }
        public static readonly SmartCapacityListHelper Instance = new SmartCapacityListHelper();
        #region Get
        public IEnumerable<SmartCapacityList> GetSmartCapacityLists(IEnumerable<int> capacityIds)
        {
            return ServerConfig.ApiDb.Query<SmartCapacityList>(
                "SELECT * FROM `t_capacity_list` WHERE MarkedDelete = 0 AND CapacityId IN @capacityIds;", new { capacityIds });
        }
        public IEnumerable<SmartCapacityList> GetSmartCapacityLists(int capacityId)
        {
            return ServerConfig.ApiDb.Query<SmartCapacityList>(
                "SELECT * FROM `t_capacity_list` WHERE MarkedDelete = 0 AND CapacityId = @capacityId;", new { capacityId });
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
        /// <param name="capacityId"></param>
        public void DeleteByCapacityId(int capacityId)
        {
            ServerConfig.ApiDb.Execute($"UPDATE `{Table}` SET `MarkedDateTime`= NOW(), `MarkedDelete`= true WHERE `CapacityId` = @categoryId;", new { capacityId });
        }
        public void DeleteByCapacityId(IEnumerable<int> capacityIds)
        {
            ServerConfig.ApiDb.Execute($"UPDATE `{Table}` SET `MarkedDateTime`= NOW(), `MarkedDelete`= true WHERE `CapacityId` IN @capacityIds;", new { capacityIds });
        }
        #endregion
    }
}
