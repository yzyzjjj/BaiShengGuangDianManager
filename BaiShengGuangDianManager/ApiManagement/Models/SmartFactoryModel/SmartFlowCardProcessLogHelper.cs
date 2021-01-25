using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartFlowCardProcessLogHelper : DataHelper
    {
        private SmartFlowCardProcessLogHelper()
        {
            Table = "t_flow_card_process_log";
            InsertSql =
                "INSERT INTO `t_flow_card_process_log` (`CreateUserId`, `MarkedDateTime`, `ProcessId`, `ProcessorId`, `DeviceId`, `StartTime`, `EndTime`, `Count`, `Before`, `Doing`, `Qualified`, `Unqualified`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @ProcessId, @ProcessorId, @DeviceId, @StartTime, @EndTime, @Count, @Before, @Doing, @Qualified, @Unqualified);";
            UpdateSql =
                "UPDATE `t_flow_card_process_log` SET `MarkedDateTime` = @MarkedDateTime, `ProcessId` = @ProcessId, `ProcessorId` = @ProcessorId, `DeviceId` = @DeviceId, " +
                "`Qualified` = @Qualified, `Unqualified` = @Unqualified WHERE `Id` = @Id;";
        }
        public static readonly SmartFlowCardProcessLogHelper Instance = new SmartFlowCardProcessLogHelper();
        #region Get
        #endregion

        #region Add
        #endregion

        #region Update
        #endregion

        #region Delete
        #endregion
    }
}