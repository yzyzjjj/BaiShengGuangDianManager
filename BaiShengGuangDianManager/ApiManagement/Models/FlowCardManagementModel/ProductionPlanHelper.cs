using ApiManagement.Models.BaseModel;
using ModelBase.Base.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

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
                "INSERT INTO `production_library_plan` (`Date`, `ProductionId`, `StepId`, `CreateUserId`, `MarkedDateTime`, `Plan`, `Change`, `Final`) " +
                "VALUES (@Date, @ProductionId, @StepId, @CreateUserId, @MarkedDateTime, @Plan, @Change, @Final);";
            UpdateSql =
                "UPDATE `production_library_plan` SET `MarkedDateTime` = @MarkedDateTime, `Plan` = @Plan, `Change` = @Change, `Final` = @Final " +
                "WHERE `Date` = @Date AND `ProductionId` = @ProductionId AND `StepId` = @StepId;";

            SameField = "Date";
            MenuFields.AddRange(new[] { "Date", "ProductionId", "StepId", "Plan", "Change", "Final", "Final", "Final" });
        }
        public static readonly ProductionPlanHelper Instance = new ProductionPlanHelper();
        #region Get











        public static IEnumerable<ProductionPlan> GetDetails(DateTime startTime, DateTime endTime, int pId, int sId, IEnumerable<int> pIds, IEnumerable<int> sIds)
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
            if (pId != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("ProductionId", "=", pId));
            }
            return Instance.CommonGet<ProductionPlan>(args).OrderByDescending(x => x.Date);
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
