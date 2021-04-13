using ApiManagement.Base.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.OtherModel
{
    public class FlowCardReportHelper : DataHelper
    {
        private FlowCardReportHelper()
        {
            Table = "flowcard_report";

            InsertSql =
                "INSERT INTO `flowcard_report` (`Time`, `FlowCardId`, `FlowCard`, `ProductionId`, `Production`, `DeviceId`, `Code`, `Step`, `Back`, `ProcessorId`, `Processor`, `Total`, `HeGe`, `LiePian`, `Reason`) " +
                "VALUES (@Time, @FlowCardId, @FlowCard, @ProductionId, @Production, @DeviceId, @Code, @Step, @Back, @ProcessorId, @Processor, @Total, @HeGe, @LiePian, @Reason);";
            UpdateSql =
                "UPDATE `flowcard_report` SET `Time` = @Time, `FlowCardId` = @FlowCardId, `FlowCard` = @FlowCard, `ProductionId` = @ProductionId, `Production` = @Production, " +
                "`DeviceId` = @DeviceId, `Code` = @Code, `Step` = @Step, `Back` = @Back, `ProcessorId` = @ProcessorId, `Processor` = @Processor, `Total` = @Total, `HeGe` = @HeGe, `LiePian` = @LiePian WHERE `Id` = @Id;";

            SameField = "FlowCard";
            MenuFields.AddRange(new[] { "Id", "FlowCard" });
        }
        public static readonly FlowCardReportHelper Instance = new FlowCardReportHelper();
        #region Get
        ///// <summary>
        ///// 菜单
        ///// </summary>
        ///// <param name="id"></param>
        ///// <returns></returns>
        //public static IEnumerable<dynamic> GetMenu(int id = 0)
        //{
        //    var args = new List<Tuple<string, string, dynamic>>();
        //    if (id != 0)
        //    {
        //        args.Add(new Tuple<string, string, dynamic>("Id", "=", id));
        //    }

        //    return Instance.CommonGet<FlowCardReport>(args, true).Select(x => new { x.Id, x.Name });
        //}
        public static IEnumerable<FlowCardReport> GetDetail(int deviceId)
        {
            return ServerConfig.ApiDb.Query<FlowCardReport>(
                "SELECT * FROM `flowcard_report` WHERE DeviceId = @deviceId ORDER BY Id DESC LIMIT 1;", new { deviceId });
        }
        //public static bool GetHaveSame(int type, IEnumerable<string> sames, IEnumerable<int> ids = null)
        //{
        //    var args = new List<Tuple<string, string, dynamic>>
        //    {
        //        new Tuple<string, string, dynamic>("Type", "=", type),
        //        new Tuple<string, string, dynamic>("Name", "IN", sames)
        //    };
        //    if (ids != null)
        //    {
        //        args.Add(new Tuple<string, string, dynamic>("Id", "NOT IN", ids));
        //    }
        //    return Instance.CommonHaveSame(args);
        //}
        public static IEnumerable<FlowCardReport> GetReport(DateTime startTime, DateTime endTime,
            int deviceId = 0, IEnumerable<int> deviceIds = null,
            int flowCardId = 0, IEnumerable<int> flowCardIds = null,
            int productionId = 0, IEnumerable<int> productionIds = null,
            int processorId = 0, IEnumerable<int> processorIds = null)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (startTime != default(DateTime))
            {
                args.Add(new Tuple<string, string, dynamic>("Time", ">=", startTime));
            }
            if (endTime != default(DateTime))
            {
                args.Add(new Tuple<string, string, dynamic>("Time", "<", endTime));
            }
            if (deviceId != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("DeviceId", "=", deviceId));
            }
            if (deviceIds != null && deviceIds.Any())
            {
                args.Add(new Tuple<string, string, dynamic>("DeviceId", "IN", deviceIds));
            }
            if (flowCardId != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("FlowCardId", "=", flowCardId));
            }
            if (flowCardIds != null && flowCardIds.Any())
            {
                args.Add(new Tuple<string, string, dynamic>("FlowCardId", "IN", flowCardIds));
            }
            if (productionId != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("ProductionId", "=", productionId));
            }
            if (productionIds != null && productionIds.Any())
            {
                args.Add(new Tuple<string, string, dynamic>("ProductionId", "IN", productionIds));
            }
            if (processorId != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("ProcessorId", "=", processorId));
            }
            if (processorIds != null && processorIds.Any())
            {
                args.Add(new Tuple<string, string, dynamic>("ProcessorId", "IN", processorIds));
            }
            return Instance.CommonGet<FlowCardReport>(args);
        }
        #endregion

        #region Add
        #endregion

        #region Update

        public static void Update(IEnumerable<FlowCardReport> flowCardReports)
        {
            ServerConfig.ApiDb.Execute(
                "UPDATE `flowcard_report` SET `FlowCardId` = @FlowCardId, `ProductionId` = @ProductionId, `Production` = @Production, `DeviceId` = @DeviceId, `ProcessorId` = @ProcessorId, `State` = @State  WHERE `Id` = @Id;",
                flowCardReports);
        }
        #endregion

        #region Delete
        #endregion
    }
}
