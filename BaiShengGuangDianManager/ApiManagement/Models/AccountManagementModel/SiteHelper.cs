using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.AccountManagementModel
{
    public class SiteHelper : DataHelper
    {
        private SiteHelper()
        {
            Table = "site";
            InsertSql =
                "INSERT INTO `site` (`CreateUserId`, `MarkedDateTime`, `WorkshopId`, `Region`, `Remark`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @WorkshopId, @Region, @Remark);";
            UpdateSql = "UPDATE `site` SET `MarkedDateTime` = @MarkedDateTime, `WorkshopId` = @WorkshopId, `Region` = @Region, `Remark` = @Remark WHERE `Id` = @Id;";

            SameField = "Region";
            MenuFields.AddRange(new[] { "Id", "WorkshopId", "Region" });
        }
        public static readonly SiteHelper Instance = new SiteHelper();
        #region Get
        public static IEnumerable<dynamic> GetMenu(int id = 0, int wId = 0)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (id != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "=", id));
            }
            if (wId != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("WorkshopId", "=", wId));
            }
            return Instance.CommonGet<Site>(args, true).Select(x => new { x.Id, x.WorkshopId, x.Region }).OrderByDescending(x => x.Id);
        }

        public static IEnumerable<Site> GetDetails(int id = 0, int wId = 0)
        {
            return ServerConfig.ApiDb.Query<Site>("SELECT a.*, b.`Name` WorkshopName FROM `site` a " +
                                                  "JOIN `workshop` b ON a.WorkshopId = b.Id " +
                                                  "WHERE " +
                                                  $"{(id == 0 ? "" : "a.Id = @id AND ")}" +
                                                  $"{(wId == 0 ? "" : "a.WorkshopId = @wId AND ")}" +
                                                  "a.MarkedDelete = 0;", new { id, wId }).OrderByDescending(x => x.Id);
        }
        public static Site GetDetail(int id)
        {
            return Instance.Get<Site>(id);
        }
        public static Site GetDetail(string region)
        {
            var args = new List<Tuple<string, string, dynamic>> { new Tuple<string, string, dynamic>("Region", "=", region) };
            return Instance.CommonGet<Site>(args).FirstOrDefault();
        }

        public static bool GetHaveSame(int wId, IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("Region", "IN", sames)
            };
            if (wId != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("WorkshopId", "=", wId));
            }
            if (ids != null)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "NOT IN", ids));
            }
            return Instance.CommonHaveSame(args);
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
