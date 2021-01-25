using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartProcessCodeHelper : DataHelper
    {
        private SmartProcessCodeHelper()
        {
            Table = "t_process_code";
            InsertSql =
                "INSERT INTO `t_process_code` (`CreateUserId`, `MarkedDateTime`, `Code`, `CategoryId`, `List`, `Remark`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @Code, @CategoryId, @List, @Remark);";
            UpdateSql =
                "UPDATE `t_process_code` SET `MarkedDateTime` = @MarkedDateTime, `Code` = @Code, `CategoryId` = @CategoryId, `List` = @List, `Remark` = @Remark WHERE `Id` = @Id;";
        }
        public static readonly SmartProcessCodeHelper Instance = new SmartProcessCodeHelper();
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
