using ApiManagement.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.StatisticManagementModel
{
    public class MonitoringKanBanSetHelper : DataHelper
    {
        public Dictionary<KanBanEnum, List<KanBanItemConfig>> Configs = new Dictionary<KanBanEnum, List<KanBanItemConfig>>
        {
            {
                KanBanEnum.生产相关看板, new List<KanBanItemConfig>
                {
                    new KanBanItemConfig(KanBanItemEnum.合格率异常报警, true, true),
                    new KanBanItemConfig(KanBanItemEnum.合格率异常统计, true, false),
                    new KanBanItemConfig(KanBanItemEnum.设备状态反馈, false, false),
                    new KanBanItemConfig(KanBanItemEnum.设备预警状态, false, false),
                    new KanBanItemConfig(KanBanItemEnum.计划号日进度表, true, false),
                    new KanBanItemConfig(KanBanItemEnum.设备日进度表, true, false),
                    new KanBanItemConfig(KanBanItemEnum.操作工日进度表, true, false),
                }
            },
            {
                KanBanEnum.设备状态看板, new List<KanBanItemConfig>
                {
                    //KanBanItemEnum.上次加工数,
                    //KanBanItemEnum.上次合格数,
                    //KanBanItemEnum.上次次品数,
                    //KanBanItemEnum.上次合格率,
                    //KanBanItemEnum.上次次品率,
                    //KanBanItemEnum.今日加工数,
                    //KanBanItemEnum.今日加工次数,
                    //KanBanItemEnum.今日合格数,
                    //KanBanItemEnum.今日次品数,
                    //KanBanItemEnum.今日合格率,
                    //KanBanItemEnum.今日次品率,
                    //KanBanItemEnum.今日合格率预警,
                    //KanBanItemEnum.昨日加工数,
                    //KanBanItemEnum.昨日加工次数,
                    //KanBanItemEnum.昨日合格数,
                    //KanBanItemEnum.昨日次品数,
                    //KanBanItemEnum.昨日合格率,
                    //KanBanItemEnum.昨日次品率,
                    //KanBanItemEnum.昨日合格率预警,
                }
            }
        };

        private MonitoringKanBanSetHelper()
        {
            Table = "kanban_set";

            InsertSql =
                "INSERT INTO `kanban_set` (`CreateUserId`, `MarkedDateTime`, `Name`, `IsShow`, `Type`, `DeviceIds`, `Order`, `UI`, `Second`, `Row`, `Col`, `ContentCol`, `ColName`, `ColSet`, `Variables`, `Items`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @Name, @IsShow, @Type, @DeviceIds, @Order, @UI, @Second, @Row, @Col, @ContentCol, @ColName, @ColSet, @Variables, @Items);";
            UpdateSql =
                "UPDATE `kanban_set` SET `MarkedDateTime` = @MarkedDateTime, `Name` = @Name, `IsShow` = @IsShow, `DeviceIds` = @DeviceIds, `Order` = @Order, `UI` = @UI, " +
                "`Second` = @Second, `Row` = @Row, `Row` = @Row, `Col` = @Col, `ContentCol` = @ContentCol, `ColName` = @ColName, `ColSet` = @ColSet, `Variables` = @Variables, `Items` = @Items WHERE `Id` = @Id;";

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
