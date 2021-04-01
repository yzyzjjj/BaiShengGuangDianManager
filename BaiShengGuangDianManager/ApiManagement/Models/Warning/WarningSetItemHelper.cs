using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using ApiManagement.Models.DeviceManagementModel;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.Warning
{
    public class WarningSetItemHelper : DataHelper
    {
        private WarningSetItemHelper()
        {
            Table = "warning_set_item";

            InsertSql =
                "INSERT INTO `warning_set_item` (`CreateUserId`, `MarkedDateTime`, `SetId`, `Item`, `ItemType`, `Frequency`, `Interval`, `Count`, `Condition1`, `Value1`, `Logic`, `Condition2`, `Value2`, `DictionaryId`, `Range`) " +
                "VALUES(@CreateUserId, @MarkedDateTime, @SetId, @Item, @ItemType, @Frequency, @Interval, @Count, @Condition1, @Value1, @Logic, @Condition2, @Value2, @DictionaryId, @Range); ";
            UpdateSql =
                "UPDATE `warning_set_item` SET `MarkedDateTime` = @MarkedDateTime, `Item` = @Item, `ItemType` = @ItemType, `Frequency` = @Frequency, `Interval` = @Interval, `Count` = @Count, " +
                "`Condition1` = @Condition1, `Value1` = @Value1, `Logic` = @Logic, `Condition2` = @Condition2, `Value2` = @Value2, `DictionaryId` = @DictionaryId, `Range` = @Range WHERE `Id` = @Id;";

            SameField = "Item";
            MenuFields.AddRange(new[] { "Id", "Item" });
        }
        public static readonly WarningSetItemHelper Instance = new WarningSetItemHelper();
        #region Get
        public static IEnumerable<WarningSetItem> GetWarningSetItemsBySetId(int setId)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("SetId", "=", setId),
            };
            return Instance.CommonGet<WarningSetItem>(args);
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
