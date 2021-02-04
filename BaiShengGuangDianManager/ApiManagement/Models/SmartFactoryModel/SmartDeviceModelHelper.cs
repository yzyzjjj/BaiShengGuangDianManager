using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartDeviceModelHelper : DataHelper
    {
        private SmartDeviceModelHelper()
        {
            Table = "t_device_model";
            InsertSql =
                "INSERT INTO `t_device_model` (`CreateUserId`, `MarkedDateTime`, `WorkshopId`, `CategoryId`, `Model`, `Remark`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @WorkshopId, @CategoryId, @Model, @Remark);";
            UpdateSql = "UPDATE `t_device_model` SET `MarkedDateTime` = @MarkedDateTime, `CategoryId` = @CategoryId, `Model` = @Model, `Remark` = @Remark WHERE `Id` = @Id;";

            SameField = "Model";
            MenuFields.AddRange(new[] { "Id", "Model", "CategoryId" });
            //MenuQueryFields.AddRange(new[] { "Id", "CategoryId" });
            //SameQueryFields.AddRange(new[] { "CategoryId", "Model" });
            //SameQueryFieldConditions.AddRange(new[] { "=", "IN" });
        }
        public static readonly SmartDeviceModelHelper Instance = new SmartDeviceModelHelper();
        #region Get
        /// <summary>
        /// 菜单
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cId"></param>
        /// <param name="wId"></param>
        /// <returns></returns>
        public static IEnumerable<dynamic> GetMenu(int id = 0, int cId = 0, int wId = 0)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (id != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "=", id));
            }
            if (cId != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("CategoryId", "=", cId));
            }
            if (wId != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("WorkshopId", "=", wId));
            }

            return Instance.CommonGet<SmartDeviceModel>(args, true).Select(x => new { x.Id, x.Model, x.CategoryId });
        }
        public static IEnumerable<SmartDeviceModelDetail> GetDetail(int id = 0, int cId = 0, int wId = 0)
        {
            return ServerConfig.ApiDb.Query<SmartDeviceModelDetail>(
                $"SELECT a.*, b.`Category` FROM `t_device_model` a JOIN `t_device_category` b ON a.CategoryId = b.Id " +
                $"WHERE {(id == 0 ? "" : "a.Id = @id AND ")}{(cId == 0 ? "" : "a.CategoryId = @cId AND ")}{(wId == 0 ? "" : "a.WorkshopId = @wId AND ")}a.MarkedDelete = 0 ORDER BY a.CategoryId;",
                new { id, cId, wId });
        }
        public static bool GetHaveSame(int cId, IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("CategoryId", "=", cId),
                new Tuple<string, string, dynamic>("Model", "IN", sames)
            };
            if (ids != null)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "NOT IN", ids));
            }
            return Instance.CommonHaveSame(args);
        }
        public static IEnumerable<SmartDeviceModel> GetSmartDeviceModel(int id = 0, int cId = 0)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (id != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "=", id));
            }

            if (cId != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("CategoryId", "=", cId));
            }
            return Instance.CommonGet<SmartDeviceModel>(args);
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