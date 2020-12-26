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
                "INSERT INTO `t_capacity_list` (`CreateUserId`, `MarkedDateTime`, `CapacityId`, `ProcessId`, " +
                "`DeviceModel`, `DeviceSingle`, `DeviceRate`, `DeviceWorkTime`, `DeviceProductTime`, `DeviceSingleCount`, `DeviceNumber`, " +
                "`OperatorLevel`, `OperatorSingle`, `OperatorRate`, `OperatorWorkTime`, `OperatorProductTime`, `OperatorSingleCount`, `OperatorNumber`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @CapacityId, @ProcessId, " +
                "@DeviceModel, @DeviceSingle, @DeviceRate, @DeviceWorkTime, @DeviceProductTime, @DeviceSingleCount, @DeviceNumber, " +
                "@OperatorLevel, @OperatorSingle, @OperatorRate, @OperatorWorkTime, @OperatorProductTime, @OperatorSingleCount, @OperatorNumber);";
            UpdateSql =
                "UPDATE `t_capacity_list` SET `MarkedDateTime` = @MarkedDateTime, `CapacityId` = @CapacityId, `ProcessId` = @ProcessId, " +
                "`DeviceModel` = @DeviceModel, `DeviceSingle` = @DeviceSingle, `DeviceRate` = @DeviceRate, `DeviceWorkTime` = @DeviceWorkTime, " +
                "`DeviceProductTime` = @DeviceProductTime, `DeviceSingleCount` = @DeviceSingleCount, `DeviceNumber` = @DeviceNumber, " +
                "`OperatorLevel` = @OperatorLevel, `OperatorSingle` = @OperatorSingle, `OperatorRate` = @OperatorRate, `OperatorWorkTime` = @OperatorWorkTime, " +
                "`OperatorProductTime` = @OperatorProductTime, `OperatorSingleCount` = @OperatorSingleCount, `OperatorNumber` = @OperatorNumber " +
                "WHERE `Id` = @Id;";
        }
        public static readonly SmartCapacityListHelper Instance = new SmartCapacityListHelper();
        #region Get
        public IEnumerable<SmartCapacityList> GetSmartCapacityLists(IEnumerable<int> capacityIds)
        {
            return ServerConfig.ApiDb.Query<SmartCapacityList>(
                "SELECT * FROM `t_capacity_list` WHERE MarkedDelete = 0 AND CapacityId IN @capacityIds;", new { capacityIds });
        }
        /// <summary>
        /// 按顺序获取 顺序 标准流程id  流程id
        /// </summary>
        /// <param name="capacityIds"></param>
        /// <returns></returns>
        public IEnumerable<SmartCapacityListDetail> GetSmartCapacityListsWithOrder(IEnumerable<int> capacityIds)
        {
            return ServerConfig.ApiDb.Query<SmartCapacityListDetail>(
                "SELECT a.*, b.Process, b.`Order`, b.DeviceCategoryId CategoryId, b.ProcessId PId FROM `t_capacity_list` a JOIN (SELECT a.Id, b.Process, b.`Order`, b.DeviceCategoryId, a.ProcessId FROM `t_process_code_category_process` a JOIN `t_process` b ON a.ProcessId = b.Id WHERE a.MarkedDelete = 0) b ON a.ProcessId = b.Id WHERE a.MarkedDelete = 0 AND a.CapacityId IN @capacityIds ORDER BY a.CapacityId, b.`Order`", new { capacityIds });
        }
        /// <summary>
        /// 按顺序获取 顺序 标准流程id  流程id
        /// </summary>
        /// <param name="capacityIds"></param>
        /// <returns></returns>
        public IEnumerable<SmartCapacityListDetail> GetAllSmartCapacityListsWithOrder(IEnumerable<int> capacityIds)
        {
            return ServerConfig.ApiDb.Query<SmartCapacityListDetail>(
                "SELECT a.*, b.Process, b.`Order`, b.DeviceCategoryId CategoryId, b.ProcessId PId FROM `t_capacity_list` a JOIN (SELECT a.Id, b.Process, b.`Order`, b.DeviceCategoryId, a.ProcessId FROM `t_process_code_category_process` a JOIN `t_process` b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id WHERE a.CapacityId IN @capacityIds ORDER BY a.CapacityId, b.`Order`", new { capacityIds });
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
