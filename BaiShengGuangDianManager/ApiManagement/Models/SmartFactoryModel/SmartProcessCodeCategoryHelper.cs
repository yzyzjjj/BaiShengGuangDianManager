using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System.Collections.Generic;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartProcessCodeCategoryHelper : DataHelper
    {
        private SmartProcessCodeCategoryHelper()
        {
            Table = "t_process_code_category";
            InsertSql =
                "INSERT INTO `t_process_code_category` (`CreateUserId`, `MarkedDateTime`, `Category`, `Remark`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @Category, @Remark);";
            UpdateSql =
                "UPDATE `t_process_code_category` SET `MarkedDateTime` = @MarkedDateTime, `Category` = @Category, `Remark` = @Remark WHERE `Id` = @Id;";
        }
        public static readonly SmartProcessCodeCategoryHelper Instance = new SmartProcessCodeCategoryHelper();
        #region Get
        public static IEnumerable<SmartProcessCodeCategory> GetSmartProcessCodeCategoriesByCategories(IEnumerable<string> categories)
        {
            return ServerConfig.ApiDb.Query<SmartProcessCodeCategory>("SELECT * FROM `t_process_code_category` WHERE MarkedDelete = 0 AND Category IN @categories;", new { categories });
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