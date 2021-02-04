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
            //InsertSql =
            //    "INSERT INTO `t_product_capacity` (`CreateUserId`, `MarkedDateTime`, `ProductId`, `ProcessId`, `Rate`, `Day`, `Hour`, `Min`, `Sec`) " +
            //    "VALUES (@CreateUserId, @MarkedDateTime, @ProductId, @ProcessId, @Rate, @Day, @Hour, @Min, @Sec);";
            //UpdateSql =
            //    "UPDATE `t_product_capacity` SET `MarkedDateTime` = @MarkedDateTime, `Rate` = @Rate, " +
            //    "`Day` = @Day, `Hour` = @Hour, `Min` = @Min, `Sec` = @Sec WHERE `Id` = @Id;";
            InsertSql =
                "INSERT INTO  `t_product_capacity` (`CreateUserId`, `MarkedDateTime`, `WorkshopId`, `ProductId`, `ProcessId`, `DeviceModel`, `DeviceSingle`, `DeviceRate`, " +
                "`DeviceWorkTime`, `DeviceProductTime`, `DeviceSingleCount`, `DeviceNumber`, `OperatorLevel`, `OperatorSingle`, `OperatorRate`, `OperatorWorkTime`, " +
                "`OperatorProductTime`, `OperatorSingleCount`, `OperatorNumber`, `Error`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @WorkshopId, @ProductId, @ProcessId, @DeviceModel, @DeviceSingle, @DeviceRate, " +
                "@DeviceWorkTime, @DeviceProductTime, @DeviceSingleCount, @DeviceNumber, @OperatorLevel, @OperatorSingle, @OperatorRate, @OperatorWorkTime, " +
                "@OperatorProductTime, @OperatorSingleCount, @OperatorNumber, @Error);";
            UpdateSql =
                "UPDATE `t_product_capacity` SET `MarkedDateTime` = @MarkedDateTime, `DeviceModel` = @DeviceModel, `DeviceSingle` = @DeviceSingle, `DeviceRate` = @DeviceRate, " +
                "`DeviceWorkTime` = @DeviceWorkTime, `DeviceProductTime` = @DeviceProductTime, `DeviceSingleCount` = @DeviceSingleCount, `DeviceNumber` = @DeviceNumber, " +
                "`OperatorLevel` = @OperatorLevel, `OperatorSingle` = @OperatorSingle, `OperatorRate` = @OperatorRate, `OperatorWorkTime` = @OperatorWorkTime, " +
                "`OperatorProductTime` = @OperatorProductTime, `OperatorSingleCount` = @OperatorSingleCount, `OperatorNumber` = @OperatorNumber, `Error` = @Error WHERE `Id` = @Id;;";
        }
        public static readonly SmartProductCapacityHelper Instance = new SmartProductCapacityHelper();
        #region Get
        public static IEnumerable<SmartProductCapacityDetail> GetDetail(int cId, int pId, int pccId)
        {
            return ServerConfig.ApiDb.Query<SmartProductCapacityDetail>(
                "SELECT IFNULL(d.Id, 0) Id, IFNULL(c.Id, 0) ListId, a.Id ProcessId, b.Id PId, b.Process, b.DeviceCategoryId, b.Category, IFNULL(d.Rate, 0) Rate, IFNULL(d.`Day`, 0) `Day`, IFNULL(d.`Hour`, 0) `Hour`, IFNULL(d.`Min`, 0) `Min`, IFNULL(d.`Sec`, 0) `Sec` FROM `t_process_code_category_process` a " +
                "JOIN (SELECT a.*, IFNULL(b.Category, '') Category FROM `t_process` a LEFT JOIN `t_device_category` b ON a.DeviceCategoryId = b.Id) b ON a.ProcessId = b.Id " +
                "LEFT JOIN (SELECT * FROM `t_capacity_list` WHERE  MarkedDelete = 0 AND CapacityId = @cId) c ON a.Id = c.ProcessId  " +
                "LEFT JOIN (SELECT * FROM `t_product_capacity` WHERE MarkedDelete = 0 AND ProductId = @pId) d ON a.Id = d.ProcessId " +
                "WHERE ProcessCodeCategoryId = @pccId AND a.MarkedDelete = 0 ORDER BY a.`Order`;"
                , new { cId, pId, pccId });
        }
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
