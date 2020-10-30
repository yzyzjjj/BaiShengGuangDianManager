using ApiManagement.Models.BaseModel;
using System.Collections.Generic;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartDeviceCategoryHelper : DataHelper
    {
        private SmartDeviceCategoryHelper()
        {
            Table = "t_device_category";
            InsertSql =
                "INSERT INTO `t_device_category` (`CreateUserId`, `MarkedDateTime`, `Category`, `Remark`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @Category, @Remark);";
            UpdateSql = "UPDATE `t_device_category` SET `MarkedDateTime` = @MarkedDateTime, `Category` = @Category, `Remark` = @Remark WHERE `Id` = @Id;";
        }
        public static readonly SmartDeviceCategoryHelper Instance = new SmartDeviceCategoryHelper();
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