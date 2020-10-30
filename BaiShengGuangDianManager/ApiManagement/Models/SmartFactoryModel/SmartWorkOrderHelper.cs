using ApiManagement.Models.BaseModel;
using System.Collections.Generic;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartWorkOrderHelper : DataHelper
    {
        private SmartWorkOrderHelper()
        {
            Table = "t_work_order";
            InsertSql =
                "INSERT INTO `t_work_order` (`CreateUserId`, `MarkedDateTime`, `WorkOrder`, `Target`, `DeliveryTime`, `Remark`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @WorkOrder, @Target, @DeliveryTime, @Remark);";
            UpdateSql = "UPDATE `t_work_order` SET `MarkedDateTime` = @MarkedDateTime, `Number` = @Number, `Remark` = @Remark WHERE `Id` = @Id;";
        }
        public static readonly SmartWorkOrderHelper Instance = new SmartWorkOrderHelper();
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
