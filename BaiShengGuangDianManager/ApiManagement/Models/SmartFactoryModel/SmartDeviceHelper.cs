using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System.Collections.Generic;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartDeviceHelper : DataHelper
    {
        private SmartDeviceHelper()
        {
            Table = "t_device";
            InsertSql =
                "INSERT INTO `t_device` (`CreateUserId`, `MarkedDateTime`, `Code`, `CategoryId`, `ModelId`, `Remark`, `Priority`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @Code, @CategoryId, @ModelId, @Remark, @Priority);";
            UpdateSql = "UPDATE `t_device` SET `MarkedDateTime` = @MarkedDateTime, `State` = @State, `Code` = @Code, `CategoryId` = @CategoryId, `ModelId` = @ModelId, `Remark` = @Remark, `Priority` = @Priority WHERE `Id` = @Id;";
        }
        public static readonly SmartDeviceHelper Instance = new SmartDeviceHelper();
        #region Get
        /// <summary>
        /// 获取状态正常的设备
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SmartDeviceDetail> GetNormalSmartDevices()
        {
            return ServerConfig.ApiDb.Query<SmartDeviceDetail>(
                "SELECT a.*, b.Model FROM `t_device` a JOIN `t_device_model` b ON a.ModelId = b.Id WHERE a.MarkedDelete = 0 AND a.State = @state;"
                , new { state = SmartDeviceState.正常 });
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