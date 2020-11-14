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
                "INSERT INTO `t_device` (`CreateUserId`, `MarkedDateTime`, `Code`, `CategoryId`, `ModelId`, `Remark`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @Code, @CategoryId, @ModelId, @Remark);";
            UpdateSql = "UPDATE `t_device` SET `MarkedDateTime` = @MarkedDateTime, `State` = @State, `Code` = @Code, `CategoryId` = @CategoryId, `ModelId` = @ModelId, `Remark` = @Remark WHERE `Id` = @Id;";
        }
        public static readonly SmartDeviceHelper Instance = new SmartDeviceHelper();
        #region Get
        #endregion

        #region Add
        #endregion

        #region Update
        #endregion

        #region Delete
        #endregion
    }
}