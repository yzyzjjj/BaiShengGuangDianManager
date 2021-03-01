using ApiManagement.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.StatisticManagementModel
{
    public class MonitoringKanBanSetHelper : DataHelper
    {
        private MonitoringKanBanSetHelper()
        {
            Table = "npc_monitoring_kanban_set";

            InsertSql =
                "INSERT INTO `npc_monitoring_kanban_set` (`CreateUserId`, `MarkedDateTime`, `Name`, `IsShow`, `Type`, `DeviceIds`, `Order`, `Second`, `Row`, `Col`, `Variables`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @Name, @IsShow, @Type, @DeviceIds, @Order, @Second, @Row, @Col, @Variables);";
            UpdateSql =
                "UPDATE `npc_monitoring_kanban_set` SET `MarkedDateTime` = @MarkedDateTime, `Name` = @Name, `IsShow` = @IsShow, " +
                    "`DeviceIds` = @DeviceIds, `Order` = @Order, `Second` = @Second, `Row` = @Row, `Row` = @Row, `Col` = @Col, `Variables` = @Variables WHERE `Id` = @Id;";

            SameField = "Name";
            MenuFields.AddRange(new[] { "Id", "Name", "Type" });
        }
        public static readonly MonitoringKanBanSetHelper Instance = new MonitoringKanBanSetHelper();
        #region Get
        /// <summary>
        /// 菜单
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static IEnumerable<dynamic> GetMenu(int id = 0)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (id != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "=", id));
            }

            return Instance.CommonGet<MonitoringKanBanSet>(args, true).Select(x => new { x.Id, x.Name });
        }
        public static IEnumerable<MonitoringKanBanSet> GetDetail(int id = 0)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (id != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "=", id));
            }

            return Instance.CommonGet<MonitoringKanBanSet>(args).OrderBy(x => x.Order);
        }
        public static bool GetHaveSame(int type, IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("Type", "=", type),
                new Tuple<string, string, dynamic>("Name", "IN", sames)
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
