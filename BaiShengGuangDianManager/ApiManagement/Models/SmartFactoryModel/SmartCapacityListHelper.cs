using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartCapacityListHelper : DataHelper
    {
        private SmartCapacityListHelper()
        {
            Table = "t_capacity_list";
            ParentField = "CapacityId";
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
        public static SmartCapacityListDetail GetDetail(int id = 0)
        {
            return ServerConfig.ApiDb.Query<SmartCapacityListDetail>($"SELECT a.*, b.DeviceCategoryId CategoryId FROM `t_capacity_list` a " +
                                                                     "JOIN (SELECT a.*, b.Process, b.DeviceCategoryId FROM `t_process_code_category_process` a " +
                                                                     $"JOIN `t_process` b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id " +
                                                                     "WHERE a.MarkedDelete = 0 AND a.Id = @id;", new { id }).FirstOrDefault();
        }
        public static IEnumerable<SmartCapacityListCol> GetDetail(int id, int capacityId, int categoryId)
        {
            var sql = "";
            if (capacityId != 0 && categoryId != 0)
            {
                sql =
                    "SELECT c.*, IFNULL(c.Id, 0) Id, a.Id ProcessId, b.Process, b.DeviceCategoryId, b.Category" +
                    " " +
                    //"IFNULL(c.DeviceModel, '') DeviceModel, IFNULL(c.DeviceNumber, '') DeviceNumber, IFNULL(c.DeviceSingle, '') DeviceSingle, IFNULL(c.DeviceSingleCount, '') DeviceSingleCount, " +
                    //"IFNULL(c.OperatorLevel, '') OperatorLevel, IFNULL(c.OperatorNumber, '') OperatorNumber, IFNULL(c.OperatorSingle, '') OperatorSingle, IFNULL(c.OperatorSingleCount, '') OperatorSingleCount " +
                    "FROM `t_process_code_category_process` a " +
                    "JOIN (SELECT a.*, IFNULL(b.Category, '') Category FROM `t_process` a " +
                    "LEFT JOIN `t_device_category` b ON a.DeviceCategoryId = b.Id) b ON a.ProcessId = b.Id " +
                    "LEFT JOIN (SELECT * FROM `t_capacity_list` WHERE  MarkedDelete = 0 AND CapacityId = @capacityId) c ON a.Id = c.ProcessId " +
                    "WHERE a.MarkedDelete = 0 AND ProcessCodeCategoryId = @categoryId;";
            }
            else if (capacityId != 0 && categoryId == 0)
            {
                var capacity = SmartCapacityHelper.Instance.Get<SmartCapacity>(capacityId);
                if (capacity == null)
                {
                    return new List<SmartCapacityListCol>();
                }

                categoryId = capacity.CategoryId;
                sql =
                    "SELECT c.*, IFNULL(c.Id, 0) Id, a.Id ProcessId, b.Process, b.DeviceCategoryId, b.Category" +
                    " " +
                    //"IFNULL(c.DeviceModel, '') DeviceModel, IFNULL(c.DeviceNumber, '') DeviceNumber, IFNULL(c.DeviceSingle, '') DeviceSingle, IFNULL(c.DeviceSingleCount, '') DeviceSingleCount, " +
                    //"IFNULL(c.OperatorLevel, '') OperatorLevel, IFNULL(c.OperatorNumber, '') OperatorNumber, IFNULL(c.OperatorSingle, '') OperatorSingle, IFNULL(c.OperatorSingleCount, '') OperatorSingleCount " +
                    "FROM `t_process_code_category_process` a " +
                    "JOIN (SELECT a.*, IFNULL(b.Category, '') Category FROM `t_process` a " +
                    "LEFT JOIN `t_device_category` b ON a.DeviceCategoryId = b.Id) b ON a.ProcessId = b.Id " +
                    "LEFT JOIN (SELECT * FROM `t_capacity_list` WHERE  MarkedDelete = 0 AND CapacityId = @capacityId) c ON a.Id = c.ProcessId " +
                    "WHERE a.MarkedDelete = 0 AND ProcessCodeCategoryId = @categoryId;";
            }
            else if (capacityId == 0 && categoryId != 0)
            {
                sql =
                    "SELECT a.Id ProcessId, b.Process, b.DeviceCategoryId, b.Category FROM `t_process_code_category_process` a " +
                    "JOIN (SELECT a.*, IFNULL(b.Category, '') Category FROM `t_process` a " +
                    "LEFT JOIN `t_device_category` b ON a.DeviceCategoryId = b.Id) b ON a.ProcessId = b.Id " +
                    "WHERE a.MarkedDelete = 0 AND b.MarkedDelete = 0 AND ProcessCodeCategoryId = @categoryId;";
            }
            else
            {
                sql = $"SELECT a.*, b.Capacity, c.Process, c.DeviceCategoryId, c.Category FROM `t_capacity_list` a " +
                      $"JOIN `t_capacity` b ON a.CapacityId = b.Id " +
                      $"JOIN (SELECT a.*, b.Process, b.DeviceCategoryId, b.Category FROM `t_process_code_category_process` a " +
                      $"JOIN (SELECT a.*, b.Category FROM `t_process` a " +
                      $"JOIN `t_device_category` b ON a.DeviceCategoryId = b.Id) b ON a.ProcessId = b.Id) c ON a.ProcessId = c.Id " +
                      $"WHERE a.MarkedDelete = 0{(id == 0 ? "" : " AND a.Id = @qId")};";
            }

            return ServerConfig.ApiDb.Query<SmartCapacityListCol>(sql, new { id, capacityId, categoryId });
        }
        public static IEnumerable<SmartCapacityList> GetSmartCapacityLists(IEnumerable<int> capacityIds)
        {
            return ServerConfig.ApiDb.Query<SmartCapacityList>(
                "SELECT * FROM `t_capacity_list` WHERE MarkedDelete = 0 AND CapacityId IN @capacityIds;", new { capacityIds });
        }
        /// <summary>
        /// 按顺序获取 顺序 标准流程id  流程id
        /// </summary>
        /// <param name="capacityIds"></param>
        /// <returns></returns>
        public static IEnumerable<SmartCapacityListDetail> GetSmartCapacityListsWithOrder(IEnumerable<int> capacityIds)
        {
            return ServerConfig.ApiDb.Query<SmartCapacityListDetail>(
                "SELECT a.*, b.Process, b.`Order`, b.DeviceCategoryId CategoryId, b.ProcessId PId FROM `t_capacity_list` a JOIN (SELECT a.Id, b.Process, b.`Order`, b.DeviceCategoryId, a.ProcessId FROM `t_process_code_category_process` a JOIN `t_process` b ON a.ProcessId = b.Id WHERE a.MarkedDelete = 0) b ON a.ProcessId = b.Id WHERE a.MarkedDelete = 0 AND a.CapacityId IN @capacityIds ORDER BY a.CapacityId, b.`Order`", new { capacityIds });
        }
        /// <summary>
        /// 按顺序获取 顺序 标准流程id  流程id
        /// </summary>
        /// <param name="capacityIds"></param>
        /// <returns></returns>
        public static IEnumerable<SmartCapacityListDetail> GetAllSmartCapacityListsWithOrder(IEnumerable<int> capacityIds)
        {
            return ServerConfig.ApiDb.Query<SmartCapacityListDetail>(
                "SELECT a.*, b.Process, b.`Order`, b.DeviceCategoryId CategoryId, b.ProcessId PId FROM `t_capacity_list` a JOIN (SELECT a.Id, b.Process, b.`Order`, b.DeviceCategoryId, a.ProcessId FROM `t_process_code_category_process` a JOIN `t_process` b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id WHERE a.CapacityId IN @capacityIds ORDER BY a.CapacityId, b.`Order`", new { capacityIds });
        }
        public static IEnumerable<SmartCapacityList> GetSmartCapacityLists(int capacityId)
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
        #endregion
    }
}
