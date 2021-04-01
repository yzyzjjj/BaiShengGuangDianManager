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
                "INSERT INTO  `warning_set` (`Id`, `CreateUserId`, `MarkedDateTime`, `WarningType`, `DataType`, `Name`, `Enable`, `ClassId`, `ScriptId`, `CategoryId`, `DeviceIds`) " +
                "VALUES (@Id, @CreateUserId, @MarkedDateTime, @WarningType, @DataType, @Name, @Enable, @ClassId, @ScriptId, @CategoryId, @DeviceIds);";
            UpdateSql =
                "UPDATE `warning_set` SET `MarkedDateTime` = @MarkedDateTime, `Name` = @Name, `Enable` = @Enable, `ClassId` = @ClassId, `ScriptId` = @ScriptId, " +
                "`DeviceIds` = @DeviceIds WHERE `Id` = @Id";

            SameField = "Name";
            MenuFields.AddRange(new[] { "Id", "Name" });
        }
        public static readonly WarningSetHelper Instance = new WarningSetHelper();
        #region Get
        /// <summary>
        /// 菜单
        /// </summary>
        /// <param name="ids"></param>
        public static IEnumerable<WarningSet> GetMenus(IEnumerable<int> ids)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (ids != null)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "IN", ids));
            }

            return Instance.CommonGet<WarningSet>(args, true);
        }
        public static bool GetHaveSame(IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("Name", "IN", sames)
            };
            if (ids != null)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "NOT IN", ids));
            }
            return Instance.CommonHaveSame(args);
        }
        public static bool GetHaveSame(WarningType warningType, WarningDataType dataType, IEnumerable<string> sames, IEnumerable<int> ids)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
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