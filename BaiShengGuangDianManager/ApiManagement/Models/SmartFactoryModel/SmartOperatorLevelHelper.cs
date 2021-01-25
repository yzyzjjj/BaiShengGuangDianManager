using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartOperatorLevelHelper : DataHelper
    {
        private SmartOperatorLevelHelper()
        {
            Table = "t_operator_level";
            InsertSql =
                "INSERT INTO `t_operator_level` (`CreateUserId`, `MarkedDateTime`, `Level`, `Order`, `Remark`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @Level, @Order, @Remark);";
            UpdateSql = "UPDATE `t_operator_level` SET `MarkedDateTime` = @MarkedDateTime, `Level` = @Level, `Order` = @Order, `Remark` = @Remark WHERE `Id` = @Id;";
        }
        public static readonly SmartOperatorLevelHelper Instance = new SmartOperatorLevelHelper();
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
