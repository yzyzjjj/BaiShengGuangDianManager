using ApiManagement.Models.BaseModel;
using ModelBase.Base.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Base.Server;
using ServiceStack;

namespace ApiManagement.Models.FlowCardManagementModel
{
    public class ProductionPlanHelper : DataHelper
    {
        /// <summary>
        /// 计划号生产计划
        /// </summary>
        private ProductionPlanHelper()
        {
            Table = "production_library_plan";
            InsertSql =
                "INSERT INTO `production_library_plan` (`Date`, `ProductionProcessName`, `ProductionId`, `StepName`, `StepId`, `CreateUserId`, `MarkedDateTime`, `Plan`, `Change`, `Final`, `Reason`, `Remark`) " +
                "VALUES (@Date, @ProductionProcessName, @ProductionId, @StepName, @StepId, @CreateUserId, @MarkedDateTime, @Plan, @Change, @Final, @Reason, @Remark);";
            UpdateSql =
                "UPDATE `production_library_plan` SET `MarkedDateTime` = @MarkedDateTime, `ProductionId` = IF(`ProductionId` = 0, @ProductionId, `ProductionId`), `StepId` = IF(`StepId` = 0, @StepId, `StepId`), `Plan` = @Plan, `Change` = @Change, `Final` = @Final, `Reason` = @Reason, `Remark` = @Remark " +
                "WHERE `Id` = @Id;";

            SameField = "Date";
            MenuFields.AddRange(new[] { "Date", "ProductionId", "StepId", "Plan", "Change", "Final", "Final", "Final" });
        }
        public static readonly ProductionPlanHelper Instance = new ProductionPlanHelper();
        #region Get


        public static IEnumerable<ProductionPlan> GetDetails(DateTime startTime, DateTime endTime, int stepId = 0, int productId = 0, 
            IEnumerable<int> productIds = null, IEnumerable<int> stepIds = null)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (startTime != default(DateTime))
            {
                args.Add(new Tuple<string, string, dynamic>("Date", ">=", startTime.DayBeginTime()));
            }
            if (endTime != default(DateTime))
            {
                args.Add(new Tuple<string, string, dynamic>("Date", "<=", endTime.DayEndTime()));
            }
            if (stepId != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("StepId", "=", stepId));
            }
            if (productId != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("ProductionId", "=", productId));
            }
            if (productIds != null && productIds.Any())
            {
                args.Add(new Tuple<string, string, dynamic>("ProductionId", "IN", productIds));
            }
            if (stepIds != null && stepIds.Any())
            {
                args.Add(new Tuple<string, string, dynamic>("StepId", "IN", stepIds));
            }
            return Instance.CommonGet<ProductionPlan>(args).OrderByDescending(x => x.Date);
        }

        public static IEnumerable<ProductionPlan> GetDetails(DateTime startTime, DateTime endTime, string step, int productId = 0,
            IEnumerable<int> productIds = null, IEnumerable<int> stepIds = null)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (startTime != default(DateTime))
            {
                args.Add(new Tuple<string, string, dynamic>("Date", ">=", startTime.DayBeginTime()));
            }
            if (endTime != default(DateTime))
            {
                args.Add(new Tuple<string, string, dynamic>("Date", "<=", endTime.DayEndTime()));
            }
            if (!step.IsNullOrEmpty())
            {
                args.Add(new Tuple<string, string, dynamic>("StepName", "=", step));
            }
            if (productId != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("ProductionId", "=", productId));
            }
            if (productIds != null && productIds.Any())
            {
                args.Add(new Tuple<string, string, dynamic>("ProductionId", "IN", productIds));
            }
            if (stepIds != null && stepIds.Any())
            {
                args.Add(new Tuple<string, string, dynamic>("StepId", "IN", stepIds));
            }
            return Instance.CommonGet<ProductionPlan>(args).OrderByDescending(x => x.Date);
        }
        public static DateTime GetMaxDate()
        {
            return ServerConfig.ApiDb.Query<DateTime>("SELECT Date FROM `production_library_plan` ORDER BY Date DESC LIMIT 1;").FirstOrDefault();
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
