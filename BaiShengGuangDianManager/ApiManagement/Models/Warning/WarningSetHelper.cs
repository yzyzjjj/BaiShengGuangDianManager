using ApiManagement.Models.BaseModel;
using System;
using System.Collections.Generic;

namespace ApiManagement.Models.Warning
{
    public class WarningSetHelper : DataHelper
    {
        private WarningSetHelper()
        {
            Table = "warning_set";

            InsertSql =
                "INSERT INTO `warning_set` (`Id`, `CreateUserId`, `MarkedDateTime`, `WarningType`, `DataType`, `Name`, `Enable`, `StepId`, `ClassId`, `ScriptId`, `CategoryId`, `DeviceIds`) " +
                "VALUES (@Id, @CreateUserId, @MarkedDateTime, @WarningType, @DataType, @Name, @Enable, @StepId, @ClassId, @ScriptId, @CategoryId, @DeviceIds);";
            UpdateSql =
                "UPDATE `warning_set` SET `MarkedDateTime` = @MarkedDateTime, `Name` = @Name, `Enable` = @Enable, `StepId` = @StepId, `ClassId` = @ClassId, `ScriptId` = @ScriptId, " +
                "`DeviceIds` = @DeviceIds WHERE `Id` = @Id";

            SameField = "Name";
            MenuFields.AddRange(new[] { "Id", "Name" });
        }
        public static readonly WarningSetHelper Instance = new WarningSetHelper();
        #region Get
        /// <summary>
        /// 菜单
        /// </summary>
        public static IEnumerable<WarningSet> GetMenus(int wId, IEnumerable<int> ids)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (wId != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("WorkshopId", "=", wId));
            }
            if (ids != null)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "IN", ids));
            }

            return Instance.CommonGet<WarningSet>(args, true);
        }
        public static bool GetHaveSame(int wId, IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("WorkshopId", "=", wId),
                new Tuple<string, string, dynamic>("Name", "IN", sames)
            };
            if (ids != null)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "NOT IN", ids));
            }
            return Instance.CommonHaveSame(args);
        }
        public static bool GetHaveSame(int wId, WarningType warningType, WarningDataType dataType, IEnumerable<string> sames, IEnumerable<int> ids)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("WorkshopId", "=", wId),
                new Tuple<string, string, dynamic>("WarningType", "=", warningType),
                new Tuple<string, string, dynamic>("DataType", "=", dataType),
                new Tuple<string, string, dynamic>("Name", "IN", sames),
                new Tuple<string, string, dynamic>("Id", "NOT IN", ids)
            };
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