using ApiManagement.Models.BaseModel;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Base.Server;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartWorkshopHelper : DataHelper
    {
        private SmartWorkshopHelper()
        {
            Table = "t_user";
            InsertSql =
                "INSERT INTO `t_workshop` (`CreateUserId`, `MarkedDateTime`, `Workshop`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @Workshop);";
            UpdateSql = "UPDATE `t_workshop` SET `MarkedDateTime` = @MarkedDateTime, `ModifyId` = @ModifyId, `Workshop` = @Workshop WHERE `Id` = @Id;";
        }
        public static readonly SmartWorkshopHelper Instance = new SmartWorkshopHelper();
        #region Get
        public string GetSmartWorkshopNameById(int id)
        {
            return ServerConfig.ApiDb.Query<string>("SELECT `Name` FROM `t_user` WHERE Id = @id;", new { id }).FirstOrDefault();
        }
        public string GetSmartWorkshopAccountById(int id)
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
