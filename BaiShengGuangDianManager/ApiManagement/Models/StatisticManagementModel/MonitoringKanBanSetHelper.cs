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
                    new KanBanItemConfig(KanBanItemEnum.异常报警, true, true, new List<KanBanTableFieldConfig>
                    {
                        new KanBanTableFieldConfig("datetime", "time", "WarningTime", "预警时间"),
                        new KanBanTableFieldConfig("string", "SetName", "预警设置名称"),
                        new KanBanTableFieldConfig("string", "Item", "预警项名称"),
                        new KanBanTableFieldConfig("string", "Code", "机台号"),
                        new KanBanTableFieldConfig("datetime", "StartTime", "首次出现时间"),
                        new KanBanTableFieldConfig("int", "Count", "达标次数"),
                        new KanBanTableFieldConfig("string", "Range", "条件"),
                        new KanBanTableFieldConfig("int", "Current", "当前次数"),
                        new KanBanTableFieldConfig("decimal", "Value", "当前值"),
                        new KanBanTableFieldConfig("string", "Info", "信息"),
                        //new KanBanTableFieldConfig("list", "WarningData", "数据集",
                        //    new List<KanBanTableFieldConfig>
                        //    {
                        //        new KanBanTableFieldConfig("datetime", "DT", "时间"),
                        //        new KanBanTableFieldConfig("string", "V", "数值"),
                        //        new KanBanTableFieldConfig("array", "ParamList", "参数"),
                        //        new KanBanTableFieldConfig("list", "OtherParamList", "参数",
                        //            new List<KanBanTableFieldConfig>
                        //            {
                        //                new KanBanTableFieldConfig("int", "count", "数量"),
                        //                new KanBanTableFieldConfig("string", "name", "不良类型"),
                        //            }),
                        //    }),
                    }),
                    new KanBanItemConfig(KanBanItemEnum.异常统计, true, false, new List<KanBanTableFieldConfig>
                    {
                        new KanBanTableFieldConfig("datetime", "date", "Time", "预警时间"),
                        new KanBanTableFieldConfig("string", "SetName", "预警设置名称"),
                        new KanBanTableFieldConfig("string", "Item", "预警项名称"),
                        new KanBanTableFieldConfig("string", "Range", "条件"),
                        new KanBanTableFieldConfig("int", "Count", "预警次数"),
                    }),
                    new KanBanItemConfig(KanBanItemEnum.设备状态反馈, false, false, new List<KanBanTableFieldConfig>
                    {
                        new KanBanTableFieldConfig("string", "Code", "机台号"),
                        new KanBanTableFieldConfig("int", "time", "IdleSecond", "闲置时间(秒)"),
                    }),
                    new KanBanItemConfig(KanBanItemEnum.设备预警状态, false, false, new List<KanBanTableFieldConfig>
                    {
                        new KanBanTableFieldConfig("datetime", "Time", "预警时间"),
                        new KanBanTableFieldConfig("string", "Code", "机台号"),
                        new KanBanTableFieldConfig("string", "Item", "预警项名称"),
                        new KanBanTableFieldConfig("string", "SetName", "预警设置名称"),
                        new KanBanTableFieldConfig("string", "Range", "条件"),
                        new KanBanTableFieldConfig("decimal", "Value", "当前值"),
                    }),
                    new KanBanItemConfig(KanBanItemEnum.计划号日进度表, true, false, new List<KanBanTableFieldConfig>
                    {
                        new KanBanTableFieldConfig("string", "Production", "计划号"),
                        new KanBanTableFieldConfig("decimal", "Plan", "计划加工"),
                        new KanBanTableFieldConfig("decimal", "Actual", "实际加工"),
                    }),
                    new KanBanItemConfig(KanBanItemEnum.设备日进度表, true, false, new List<KanBanTableFieldConfig>
                    {
                        new KanBanTableFieldConfig("string", "Code", "机台号"),
                        new KanBanTableFieldConfig("decimal", "Plan", "计划加工"),
                        new KanBanTableFieldConfig("decimal", "Actual", "实际加工"),
                    }),
                    new KanBanItemConfig(KanBanItemEnum.操作工日进度表, true, false, new List<KanBanTableFieldConfig>
                    {
                        new KanBanTableFieldConfig("string", "Processor", "操作工"),
                        new KanBanTableFieldConfig("decimal", "Plan", "计划加工"),
                        new KanBanTableFieldConfig("decimal", "Actual", "实际加工"),
                    }),
                    new KanBanItemConfig(KanBanItemEnum.故障状态反馈, false, false, new List<KanBanTableFieldConfig>
                    {
                        new KanBanTableFieldConfig("string", "DeviceCode", "机台号"),
                        new KanBanTableFieldConfig("datetime", "FaultTime", "故障时间"),
                        new KanBanTableFieldConfig("string", "StateDesc", "故障状态"),
                        new KanBanTableFieldConfig("string", "Proposer", "报修人"),
                        new KanBanTableFieldConfig("string", "FaultTypeName", "故障类型"),
                        new KanBanTableFieldConfig("string", "FaultDescription", "故障描述"),
                        new KanBanTableFieldConfig("string", "Supplement", "故障补充"),
                        new KanBanTableFieldConfig("string", "Name", "维修工"),
                        new KanBanTableFieldConfig("string", "Phone", "手机"),
                        new KanBanTableFieldConfig("int", "time", "NoAssignTime", "未指派耗时(秒)"),
                        new KanBanTableFieldConfig("int", "time", "WaitTime", "已指派未维修耗时(秒)"),
                        new KanBanTableFieldConfig("int", "time", "RepairCostTime", "维修耗时(秒)"),
                        new KanBanTableFieldConfig("int", "time", "TotalCostTime", "总故障时间(秒)"),
                    }),
                    new KanBanItemConfig(KanBanItemEnum.计划号工序推移图, true, true, new List<KanBanTableFieldConfig>
                    {
                        new KanBanTableFieldConfig("datetime", "Time", "时间"),
                        new KanBanTableFieldConfig("string", "Production", "计划号"),
                        new KanBanTableFieldConfig("int", "Total", "加工数"),
                        new KanBanTableFieldConfig("int", "Qualified", "合格数"),
                        new KanBanTableFieldConfig("int", "Unqualified", "次品数"),
                        new KanBanTableFieldConfig("decimal", "QualifiedRate", "合格率(%)"),
                        new KanBanTableFieldConfig("decimal", "UnqualifiedRate", "次品率(%)"),
                    }, KanBanItemDisplayEnum.Chart),
                    new KanBanItemConfig(KanBanItemEnum.设备工序推移图, true, true, new List<KanBanTableFieldConfig>
                    {
                        new KanBanTableFieldConfig("datetime", "Time", "时间"),
                        new KanBanTableFieldConfig("string", "Code", "机台号"),
                        new KanBanTableFieldConfig("int", "Total", "加工数"),
                        new KanBanTableFieldConfig("int", "Qualified", "合格数"),
                        new KanBanTableFieldConfig("int", "Unqualified", "次品数"),
                        new KanBanTableFieldConfig("decimal", "QualifiedRate", "合格率(%)"),
                        new KanBanTableFieldConfig("decimal", "UnqualifiedRate", "次品率(%)"),
                    }, KanBanItemDisplayEnum.Chart),
                    new KanBanItemConfig(KanBanItemEnum.操作工工序推移图, true, true, new List<KanBanTableFieldConfig>
                    {
                        new KanBanTableFieldConfig("datetime", "Time", "时间"),
                        new KanBanTableFieldConfig("string", "Processor", "操作工"),
                        new KanBanTableFieldConfig("int", "Total", "加工数"),
                        new KanBanTableFieldConfig("int", "Qualified", "合格数"),
                        new KanBanTableFieldConfig("int", "Unqualified", "次品数"),
                        new KanBanTableFieldConfig("decimal", "QualifiedRate", "合格率(%)"),
                        new KanBanTableFieldConfig("decimal", "UnqualifiedRate", "次品率(%)"),
                    }, KanBanItemDisplayEnum.Chart),
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

        public static KanBanTableFieldSet ConvertFieldSet(KanBanTableFieldConfig config, int order)
        {
            var c = new KanBanTableFieldSet(config) { Order = order + 1 };
            if (config.FieldList.Any())
            {
                c.FieldList.AddRange(config.FieldList.Select(ConvertFieldSet));
            }
            return c;
        }

        public static KanBanTableFieldConfig ConvertFieldConfig(KanBanTableFieldConfig config, int order)
        {
            var c = new KanBanTableFieldConfig(config) { Order = order + 1 };
            if (config.FieldList.Any())
            {
                c.FieldList.AddRange(config.FieldList.Select(ConvertFieldConfig));
            }
            return c;
        }
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
        public static IEnumerable<MonitoringKanBanSet> GetDetail(int wId = 0, int id = 0)
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
