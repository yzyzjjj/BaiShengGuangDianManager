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
                "INSERT INTO `t_flow_card_process` (`CreateUserId`, `MarkedDateTime`, `FlowCardId`, `ProcessId`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @FlowCardId, @ProcessId);";
            UpdateSql = "UPDATE `t_device` SET `MarkedDateTime` = @MarkedDateTime, `Code` = @Code, `CategoryId` = @CategoryId, `Remark` = @Remark WHERE `Id` = @Id";
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