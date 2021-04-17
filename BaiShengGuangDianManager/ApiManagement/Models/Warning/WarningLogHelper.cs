using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using ApiManagement.Models.DeviceManagementModel;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.Warning
{
    public class WarningLogHelper : DataHelper
    {
        private WarningLogHelper()
        {
            Table = "warning_log";

            InsertSql =
                "INSERT INTO `warning_log` (`Id`, `IsWarning`, `WarningTime`, `WarningType`, `StepId`, `ClassId`, `SetName`, `ItemId`, `Item`, `DeviceId`, `DataType`, `SetId`, `ScriptId`, `CategoryId`, `StartTime`, `EndTime`, `Frequency`, `Interval`, `Count`, `Condition1`, `Value1`, `Condition2`, `Value2`, `Range`, `DictionaryId`, `Current`, `Value`, `Param`, `Values`, `DeviceIds`) " +
                "VALUES (@Id, @IsWarning, @WarningTime, @WarningType, @StepId, @ClassId, @SetName, @ItemId, @Item, @DeviceId, @DataType, @SetId, @ScriptId, @CategoryId, @StartTime, @EndTime, @Frequency, @Interval, @Count, @Condition1, @Value1, @Condition2, @Value2, @Range, @DictionaryId, @Current, @Value, @Param, @Values, @DeviceIds);";
            UpdateSql =
                "";

            SameField = "WarningTime";
            MenuFields.AddRange(new[] { "Id", "WarningTime" });
        }
        public static readonly WarningLogHelper Instance = new WarningLogHelper();
        #region Get
        public static bool GetHaveSame(WarningType warningType, IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("WarningType", "=", warningType),
                new Tuple<string, string, dynamic>("Name", "IN", sames)
            };
            if (ids != null)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "NOT IN", ids));
            }
            return Instance.CommonHaveSame(args);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="setId"></param>
        /// <param name="itemId"></param>
        /// <param name="warningType"></param>
        /// <param name="dataType"></param>
        /// <param name="deviceIds"></param>
        /// <param name="itemTypes"></param>
        /// <param name="isWarning">-1all 0  1 </param>
        /// <returns></returns>
        public static IEnumerable<WarningLog> GetWarningLogs(DateTime startTime, DateTime endTime, int setId, int itemId,
            WarningType warningType, WarningDataType dataType, IEnumerable<int> deviceIds, IEnumerable<WarningItemType> itemTypes, int isWarning = -1)
        {
            var param = new List<string>();
            if (startTime != default(DateTime))
            {
                param.Add("WarningTime >= @startTime");
            }
            if (endTime != default(DateTime))
            {
                param.Add("WarningTime <= @endTime");
            }
            if (setId != 0)
            {
                param.Add("SetId = @setId");
            }
            if (itemId != 0)
            {
                param.Add("ItemId = @itemId");
            }
            if (warningType != WarningType.默认)
            {
                param.Add("WarningType = @warningType");
            }
            if (dataType != WarningDataType.默认)
            {
                param.Add("DataType = @dataType");
            }
            if (deviceIds != null && deviceIds.Any())
            {
                param.Add("DeviceId IN @deviceIds");
            }
            if (isWarning != -1)
            {
                param.Add("IsWarning = @isWarning");
            }

            IEnumerable<int> itemIds = null;
            if (itemTypes != null && itemTypes.Any())
            {
                var args = new List<Tuple<string, string, dynamic>>
                {
                    new Tuple<string, string, dynamic>("ItemType", "IN", itemTypes)
                };
                if (setId != 0)
                {
                    args.Add(new Tuple<string, string, dynamic>("SetId", "=", setId));
                }
                itemIds = WarningSetItemHelper.Instance.CommonGet<WarningSetItem>(args).Select(x => x.Id);
                if (!itemIds.Any())
                {
                    return new List<WarningLog>();
                }
                param.Add("ItemId IN @itemIds");
            }
            var r = ServerConfig.ApiDb.Query<WarningLog>(
                $"SELECT * FROM `warning_log`{(param.Any() ? $" WHERE {param.Join(" AND ")}" : "")} ORDER BY WarningTime DESC;",
                new { startTime, endTime, setId, itemId, warningType, dataType, deviceIds, itemIds, isWarning });

            if (r != null && r.Any())
            {
                var sets = WarningSetHelper.GetMenus(r.Select(x => x.SetId).Distinct()).ToDictionary(x => x.Id);
                var devices = DeviceLibraryHelper.GetMenus(r.Select(x => x.DeviceId).Distinct()).ToDictionary(x => x.Id);
                var categories = DeviceCategoryHelper.GetMenus(r.Select(x => x.CategoryId).Distinct()).ToDictionary(x => x.Id);
                foreach (var d in r)
                {
                    //var d = ClassExtension.ParentCopyToChild<WarningCurrent, WarningLog>(current);
                    d.SetName = sets.ContainsKey(d.SetId) ? sets[d.SetId].Name : "";
                    d.Code = devices.ContainsKey(d.DeviceId) ? devices[d.DeviceId].Code : "";
                    //d.Class = classes.FirstOrDefault(x => x.Id == d.ClassId)?.Class ?? "";
                    d.CategoryName = categories.ContainsKey(d.CategoryId) ? categories[d.CategoryId].CategoryName : "";
                }
            }

            return r;
        }

        public static IEnumerable<WarningLog> GetWarningLogs(IEnumerable<int> deviceIds, IEnumerable<int> itemIds)
        {
            var r = ServerConfig.ApiDb.Query<WarningLog>(
                $"SELECT * FROM (SELECT * FROM `warning_log` WHERE DeviceId IN @deviceIds AND ItemId IN @itemIds ORDER BY WarningTime DESC) a GROUP BY a.DeviceId, a.ItemId",
                new { deviceIds, itemIds });
            return r;
        }

        #endregion

        #region Add
        #endregion

        #region Update
        public static void DealWarningLogs(WarningClear clear)
        {
            ServerConfig.ApiDb.Execute(
                $"UPDATE warning_log SET IsDeal = true WHERE SetId = @SetId AND DeviceId IN @DeviceIdList AND IsDeal = false;", clear);
        }

        #endregion

        #region Delete
        #endregion
    }
}
