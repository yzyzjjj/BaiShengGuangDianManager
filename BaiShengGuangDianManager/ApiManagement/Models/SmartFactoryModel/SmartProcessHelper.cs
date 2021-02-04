using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartProcessHelper : DataHelper
    {
        private SmartProcessHelper()
        {
            Table = "t_process";
            InsertSql =
                "INSERT INTO `t_process` (`CreateUserId`, `MarkedDateTime`, `WorkshopId`, `Process`, `DeviceCategoryId`, `Order`, `Remark`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @WorkshopId, @Process, @DeviceCategoryId, @Order, @Remark);";
            UpdateSql =
                "UPDATE `t_process` SET `MarkedDateTime` = @MarkedDateTime, `Process` = @Process, `DeviceCategoryId` = @DeviceCategoryId, `Order` = @Order, `Remark` = @Remark WHERE `Id` = @Id;";

            SameField = "Process";
            MenuFields.AddRange(new[] { "Id", "Process", "Order" });
        }
        public static readonly SmartProcessHelper Instance = new SmartProcessHelper();
        #region Get
        /// <summary>
        /// 菜单
        /// </summary>
        /// <param name="id"></param>
        /// <param name="wId"></param>
        /// <returns></returns>
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

            return Instance.CommonGet<SmartProcess>(args, true).Select(x => new { x.Id, x.Process, x.Order });
        }
        public static IEnumerable<SmartProcessDetail> GetDetail(int id = 0, int wId = 0)
        {
            return ServerConfig.ApiDb.Query<SmartProcessDetail>(
                $"SELECT a.*, IFNULL(b.Category, '') DeviceCategory FROM `t_process` a LEFT JOIN t_device_category b ON a.DeviceCategoryId = b.Id " +
                $"WHERE {(id == 0 ? "" : "a.Id = @id AND ")}{(wId == 0 ? "" : "a.WorkshopId = @wId AND ")}a.MarkedDelete = 0;",
                new { id, wId });
        }
        public static bool GetHaveSame(int wId, IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("WorkshopId", "=", wId),
                new Tuple<string, string, dynamic>("Process", "IN", sames)
            };
            if (ids != null)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "NOT IN", ids));
            }
            return Instance.CommonHaveSame(args);
        }
        public static bool GetHaveSame(int wId, IEnumerable<int> cIds, IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("WorkshopId", "=", wId),
                new Tuple<string, string, dynamic>("Process", "IN", sames),
                new Tuple<string, string, dynamic>("DeviceCategoryId", "IN", cIds)
            };
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
