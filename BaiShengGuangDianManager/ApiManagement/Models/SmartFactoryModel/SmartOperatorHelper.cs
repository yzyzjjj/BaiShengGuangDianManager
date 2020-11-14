﻿using ApiManagement.Models.BaseModel;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Base.Server;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartOperatorHelper : DataHelper
    {
        private SmartOperatorHelper()
        {
            Table = "t_operator";
            InsertSql =
                "INSERT INTO `t_operator` (`CreateUserId`, `MarkedDateTime`, `UserId`, `State`, `ProcessId`, `LevelId`, `Remark`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @UserId, @State, @ProcessId, @LevelId, @Remark);";
            UpdateSql = "UPDATE `t_operator` SET `MarkedDateTime` = @MarkedDateTime, `State` = @State, `ProcessId` = @ProcessId, `LevelId` = @LevelId, `Remark` = @Remark WHERE `Id` = @Id;";
        }
        public static readonly SmartOperatorHelper Instance = new SmartOperatorHelper();
        #region Get
        public string GetSmartOperatorNameById(int id)
        {
            return ServerConfig.ApiDb.Query<string>("SELECT b.`Name` FROM `t_operator` a JOIN `t_user` b ON a.UserId = b.Id WHERE a.Id = @id AND a.`MarkedDelete` = 0;", new { id }).FirstOrDefault();
        }
        public string GetSmartOperatorAccountById(int id)
        {
            return ServerConfig.ApiDb.Query<string>("SELECT b.`Account` FROM `t_operator` a JOIN `t_user` b ON a.UserId = b.Id WHERE a.Id = @id AND a.`MarkedDelete` = 0;", new { id }).FirstOrDefault();
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
