using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using ApiManagement.Models.DeviceManagementModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.OtherModel
{
    public class FlowCardReportUpdateHelper : DataHelper
    {
        private FlowCardReportUpdateHelper()
        {
            Table = "flowcard_report_update";

            InsertSql =
                "INSERT INTO `flowcard_report_update` (`MarkedDateTime`, `OtherId`, `InsertTime`, `UpdateTime`, `Time`, `Step`, `StepName`, `StepAbbrev`, `FlowCardId`, `FlowCard`, `OldFlowCardId`, `OldFlowCard`, `ProductionId`, `Production`, `DeviceId`, `Code`, `Back`, `ProcessorId`, `Processor`, `Total`, `HeGe`, `LiePian`, `Reason`, `State`, `IsUpdate`, `OldData`) " +
                "VALUES (@MarkedDateTime, @OtherId, @InsertTime, @UpdateTime, @Time, @Step, @StepName, @StepAbbrev, @FlowCardId, @FlowCard, @OldFlowCardId, @OldFlowCard, @ProductionId, @Production, @DeviceId, @Code, @Back, @ProcessorId, @Processor, @Total, @HeGe, @LiePian, @Reason, @State, @IsUpdate, @OldData);";
            UpdateSql =
                "UPDATE `flowcard_report_update` SET `MarkedDateTime` = @MarkedDateTime, `Total` = @Total, `HeGe` = @HeGe, `LiePian` = @LiePian, `Reason` = @Reason WHERE `Id` = @Id;";

            SameField = "FlowCard";
            MenuFields.AddRange(new[] { "Id", "FlowCard" });
        }
        public static readonly FlowCardReportUpdateHelper Instance = new FlowCardReportUpdateHelper();
        #region Get

        public static IEnumerable<FlowCardReportUpdate> GetReport(DateTime startTime, DateTime endTime,
            int stepId = 0, IEnumerable<int> stepIds = null,
            int deviceId = 0, IEnumerable<int> deviceIds = null,
            int flowCardId = 0, IEnumerable<int> flowCardIds = null,
            int oldFlowCardId = 0, IEnumerable<int> oldFlowCardIds = null,
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
            return Instance.CommonGet<FlowCardReportUpdate>(args, false, 1000);
        }

        public static DateTime GetMaxTime()
        {
            return ServerConfig.ApiDb.Query<FlowCardReportUpdate>("SELECT UpdateTime FROM `flowcard_report_update` ORDER BY UpdateTime DESC LIMIT 1;", null, 1000)
                       .FirstOrDefault()?.UpdateTime ?? DateTime.Now;
        }
        #endregion

        #region Add
        #endregion

        #region Update
        public static void Update(IEnumerable<FlowCardReportUpdate> flowCardReports)
        {
            ServerConfig.ApiDb.Execute(
                "UPDATE `flowcard_report_update` SET `MarkedDateTime` = @MarkedDateTime, `FlowCardId` = @FlowCardId, `OldFlowCardId` = @OldFlowCardId, `ProductionId` = @ProductionId, `Production` = @Production, `DeviceId` = @DeviceId, `ProcessorId` = @ProcessorId, `State` = @State  WHERE `Id` = @Id;",
                flowCardReports);
        }

        public static void Update(IEnumerable<FlowCardReportGet> flowCardReports)
        {
            ServerConfig.ApiDb.Execute(
                "UPDATE `flowcard_report_update` SET `MarkedDateTime` = @MarkedDateTime, `FlowCardId` = @FlowCardId, `OldFlowCardId` = @OldFlowCardId, `ProductionId` = @ProductionId, `Production` = @Production, `DeviceId` = @DeviceId, `ProcessorId` = @ProcessorId, `State` = @State  WHERE `Id` = @Id;",
                flowCardReports);
        }

        public static void IsUpdate(IEnumerable<int> ids)
        {
            ServerConfig.ApiDb.Execute(
                "UPDATE `flowcard_report_update` SET `MarkedDateTime` = NOW(), `IsUpdate` = 1  WHERE `Id` IN @ids;",
                new{ ids });
        }
        #endregion

        #region Delete
        #endregion
    }
}
