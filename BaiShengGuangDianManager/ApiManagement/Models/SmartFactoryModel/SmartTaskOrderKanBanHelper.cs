using ApiManagement.Models.BaseModel;
using ModelBase.Base.Utils;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartTaskOrderKanBanHelper : DataHelper
    {
        private SmartTaskOrderKanBanHelper()
        {
            Table = "t_task_order_level";
            InsertSql =
                "INSERT INTO  `t_task_order_need_kanban` (`CreateUserId`, `MarkedDateTime`, `WorkshopId`, `ProcessTime`, `NeedId`, `Batch`, `TaskOrderId`, `ProcessId`, `PId`, `ProductId`, `Target`, `ThisTarget`, `Put`, `ThisPut`, `HavePut`, `DoneTarget`, `Done`, `DoingCount`, `Doing`, `IssueCount`, `Issue`, `ActualRate`, `TheoreticalRate`, `Error`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @WorkshopId, @ProcessTime, @NeedId, @Batch, @TaskOrderId, @ProcessId, @PId, @ProductId, @Target, @ThisTarget, @Put, @ThisPut, @HavePut, @DoneTarget, @Done, @DoingCount, @Doing, @IssueCount, @Issue, @ActualRate, @TheoreticalRate, @Error);";
            UpdateSql = "UPDATE `t_task_order_need_kanban` SET `MarkedDateTime` = @MarkedDateTime, `ThisTarget` = @ThisTarget, `Put` = @Put, `ThisPut` = @ThisPut, `HavePut` = @HavePut, `DoneTarget` = @DoneTarget, `Done` = @Done, " +
                        "`DoingCount` = @DoingCount, `Doing` = @Doing, `IssueCount` = @IssueCount, `Issue` = @Issue, `ActualRate` = @ActualRate, " +
                        "`TheoreticalRate` = @TheoreticalRate, `Error` = @Error WHERE `Id` = @Id;";

            SameField = "Id";
            MenuFields.AddRange(new[] { "Id" });
        }
        public static readonly SmartTaskOrderKanBanHelper Instance = new SmartTaskOrderKanBanHelper();
        #region Get
        public static IEnumerable<SmartTaskOrderKanBanItem> GetDetail(int wId)
        {
            var taskOrders = SmartTaskOrderHelper.GetAllArrangedButNotDoneSmartTaskOrderDetails(wId).Select(ClassExtension.ParentCopyToChild<SmartTaskOrderDetail, SmartTaskOrderKanBanItem>).ToList();
            var needs = SmartTaskOrderNeedHelper.GetSmartTaskOrderNeedsByTaskOrderIds(wId, taskOrders.Select(x => x.Id), true).Select(ClassExtension.ParentCopyToChild<SmartTaskOrderNeedDetail, SmartTaskOrderKanBanNeed>);
            foreach (var taskOrder in taskOrders)
            {
                taskOrder.Needs.AddRange(needs.Where(y => y.TaskOrderId == taskOrder.Id).OrderBy(y => y.Order).ToList());
            }
            return taskOrders.OrderBy(x => x.DeliveryTime);
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
