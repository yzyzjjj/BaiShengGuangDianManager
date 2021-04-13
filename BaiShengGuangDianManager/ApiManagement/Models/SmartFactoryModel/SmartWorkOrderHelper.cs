using ApiManagement.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartWorkOrderHelper : DataHelper
    {
        private SmartWorkOrderHelper()
        {
            Table = "t_work_order";
            InsertSql =
                "INSERT INTO `t_work_order` (`CreateUserId`, `MarkedDateTime`, `WorkshopId`, `WorkOrder`, `Target`, `DeliveryTime`, `Remark`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @WorkshopId, @WorkOrder, @Target, @DeliveryTime, @Remark);";
            UpdateSql = "UPDATE `t_work_order` SET `MarkedDateTime` = @MarkedDateTime, `WorkOrder` = @WorkOrder, `Target` = @Target, `DeliveryTime` = @DeliveryTime, `Remark` = @Remark WHERE `Id` = @Id;";

            SameField = "WorkOrder";
            MenuFields.AddRange(new[] { "Id", "WorkOrder" });
        }
        public static readonly SmartWorkOrderHelper Instance = new SmartWorkOrderHelper();
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

            return Instance.CommonGet<SmartWorkOrder>(args, true).Select(x => new { x.Id, x.WorkOrder });
        }
        public static IEnumerable<SmartWorkOrder> GetDetail(int id = 0, int wId = 0)
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
            return Instance.CommonGet<SmartWorkOrder>(args);
        }
        public static bool GetHaveSame(int wId, IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("WorkshopId", "=", wId),
                new Tuple<string, string, dynamic>("WorkOrder", "IN", sames)
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
