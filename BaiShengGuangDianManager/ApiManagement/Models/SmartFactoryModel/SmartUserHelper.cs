using ApiManagement.Models.BaseModel;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Base.Server;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartUserHelper : DataHelper
    {
        private SmartUserHelper()
        {
            Table = "t_user";
            InsertSql =
                "INSERT INTO  `t_user` (`CreateUserId`, `MarkedDateTime`, `Number`, `Account`, `Name`, `Remark`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @Number, @Account, @Name, @Remark);";
            UpdateSql = "UPDATE `t_user` SET `MarkedDateTime` = @MarkedDateTime, `Number` = @Number, `Account` = @Account, `Name` = @Name, `Remark` = @Remark WHERE `Id` = @Id;";
        }
        public static readonly SmartUserHelper Instance = new SmartUserHelper();
        #region Get
        public string GetSmartUserNameById(int id)
        {
            return ServerConfig.ApiDb.Query<string>("SELECT `Name` FROM `t_user` WHERE Id = @id;", new { id }).FirstOrDefault();
        }
        public string GetSmartUserAccountById(int id)
        {
            return ServerConfig.ApiDb.Query<string>("SELECT Account FROM `t_user` WHERE Id = @id;", new { id }).FirstOrDefault();
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
