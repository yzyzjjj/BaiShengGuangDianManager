using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.OtherModel
{
    public class FlowCardReportGetHelper : DataHelper
    {
        private FlowCardReportGetHelper()
        {
            Table = "flowcard_report_get";

            InsertSql =
                "INSERT INTO `flowcard_report_get` (`MarkedDateTime`, `OtherId`, `InsertTime`, `UpdateTime`, `Time`, `Step`, `StepName`, `StepAbbrev`, `FlowCardId`, `FlowCard`, `OldFlowCardId`, `OldFlowCard`, `ProductionId`, `Production`, `DeviceId`, `Code`, `Back`, `ProcessorId`, `Processor`, `Total`, `HeGe`, `LiePian`, `Reason`, `State`) " +
                "VALUES (@MarkedDateTime, @OtherId, @InsertTime, @UpdateTime, @Time, @Step, @StepName, @StepAbbrev, @FlowCardId, @FlowCard, @OldFlowCardId, @OldFlowCard, @ProductionId, @Production, @DeviceId, @Code, @Back, @ProcessorId, @Processor, @Total, @HeGe, @LiePian, @Reason, @State);";
            UpdateSql =
                "UPDATE `flowcard_report_get` SET `MarkedDateTime` = @MarkedDateTime, `Total` = @Total, `HeGe` = @HeGe, `LiePian` = @LiePian, `Reason` = @Reason WHERE `Id` = @Id;";

            SameField = "FlowCard";
            MenuFields.AddRange(new[] { "Id", "FlowCard" });
        }
        public static readonly FlowCardReportGetHelper Instance = new FlowCardReportGetHelper();
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

        //    return Instance.CommonGet<FlowCardReportGet>(args, true).Select(x => new { x.Id, x.Name });
        //}
        public static IEnumerable<FlowCardReportGet> GetDetails(int deviceId)
        {
            return ServerConfig.ApiDb.Query<FlowCardReportGet>(
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

        public static IEnumerable<FlowCardReportGet> GetReport(int workshopId, DateTime startTime = default(DateTime), DateTime endTime = default(DateTime),
            int stepId = 0, IEnumerable<int> stepIds = null,
            int deviceId = 0, IEnumerable<int> deviceIds = null,
            int flowCardId = 0, IEnumerable<int> flowCardIds = null,
            int oldFlowCardId = 0, IEnumerable<int> oldFlowCardIds = null,
            int productionId = 0, IEnumerable<int> productionIds = null,
            int processorId = 0, IEnumerable<int> processorIds = null,
            int otherId = 0, IEnumerable<int> otherIds = null)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            args.Add(new Tuple<string, string, dynamic>("WorkshopId", "=", workshopId));
            args.Add(new Tuple<string, string, dynamic>("State", "!=", 0));
            if (startTime != default(DateTime))
            {
                args.Add(new Tuple<string, string, dynamic>("Time", ">=", startTime));
            }
            if (endTime != default(DateTime))
            {
                args.Add(new Tuple<string, string, dynamic>("Time", "<=", endTime));
            }
            if (stepId != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("Step", "=", stepId));
            }
            if (stepIds != null && stepIds.Any())
            {
                args.Add(new Tuple<string, string, dynamic>("Step", "IN", stepIds));
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
            if (oldFlowCardId != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("OldFlowCardId", "=", oldFlowCardId));
            }
            if (oldFlowCardIds != null && oldFlowCardIds.Any())
            {
                args.Add(new Tuple<string, string, dynamic>("OldFlowCardId", "IN", oldFlowCardIds));
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
            if (otherId != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("OtherId", "=", otherId));
            }
            if (otherIds != null && otherIds.Any())
            {
                args.Add(new Tuple<string, string, dynamic>("OtherId", "IN", otherIds));
            }
            return Instance.CommonGet<FlowCardReportGet>(args, false, 1000);
        }

        public static DateTime GetMaxTime()
        {
            return ServerConfig.ApiDb.Query<FlowCardReportGet>("SELECT UpdateTime FROM `flowcard_report_get` ORDER BY UpdateTime DESC LIMIT 1;", null, 1000)
                .FirstOrDefault()?.UpdateTime ?? DateTime.Now;
        }
        #endregion

        #region Add
        #endregion

        #region Update
        public static void Update(IEnumerable<FlowCardReportGet> flowCardReports)
        {
            ServerConfig.ApiDb.Execute(
                "UPDATE `flowcard_report_get` SET `MarkedDateTime` = @MarkedDateTime, `FlowCardId` = @FlowCardId, `OldFlowCardId` = @OldFlowCardId, `ProductionId` = @ProductionId, `Production` = @Production, `DeviceId` = @DeviceId, `ProcessorId` = @ProcessorId, `State` = @State  WHERE `Id` = @Id;",
                flowCardReports);
        }

        public static void UpdateReport(IEnumerable<FlowCardReportGet> flowCardReports)
        {
            ServerConfig.ApiDb.Execute(
                "UPDATE `flowcard_report_get` SET `MarkedDateTime` = @MarkedDateTime, `UpdateTime` = @UpdateTime, `State` = @State, `FlowCardId` = @FlowCardId, `FlowCardId` = @FlowCardId, `OldFlowCardId` = @OldFlowCardId, " +
                "`ProductionId` = @ProductionId, `Production` = @Production, `DeviceId` = @DeviceId, `Code` = @Code, `ProcessorId` = @ProcessorId, `Processor` = @Processor, " +
                "`Total` = @Total, `HeGe` = @HeGe, `LiePian` = @LiePian, `Reason` = @Reason  WHERE `Id` = @Id;",
                flowCardReports);
        }
        #endregion

        #region Delete
        #endregion
    }
}
