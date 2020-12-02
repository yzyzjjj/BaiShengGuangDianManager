using ApiManagement.Models.BaseModel;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Base.Server;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartTaskOrderLevelHelper : DataHelper
    {
        private SmartTaskOrderLevelHelper()
        {
            Table = "t_task_order_level";
            InsertSql =
                "INSERT INTO `t_task_order_level` (`CreateUserId`, `MarkedDateTime`, `Level`, `Order`, `Remark`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @Level, @Order, @Remark);";
            UpdateSql = "UPDATE `t_task_order_level` SET `MarkedDateTime` = @MarkedDateTime, `Level` = @Level, `Order` = @Order, `Remark` = @Remark WHERE `Id` = @Id;";
        }
        public static readonly SmartTaskOrderLevelHelper Instance = new SmartTaskOrderLevelHelper();
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
