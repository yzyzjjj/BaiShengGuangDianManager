using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using ModelBase.Base.Logger;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using ModelBase.Base.Utils;

namespace ApiManagement.Models.Warning
{
    public class WarningStatisticHelper : DataHelper
    {
        private WarningStatisticHelper()
        {
            Table = "warning_statistic";
            InsertSql =
                "INSERT INTO `warning_statistic` (`WorkshopId`, `Type`, `Time`, `ItemId`, `Count`) VALUES (@WorkshopId, @Type, @Time, @ItemId, @Count);";
            UpdateSql =
                "UPDATE `warning_statistic` SET `Count` = @Count " +
                "WHERE `WorkshopId` = @WorkshopId AND `Type` = @Type AND `Time` = @Time AND `ItemId` = @ItemId;";

            SameField = "";
            //MenuFields.AddRange(new[] { "Id", "Item" });
        }
        public static readonly WarningStatisticHelper Instance = new WarningStatisticHelper();

        #region Get
        /// <summary>
        /// 获取统计
        /// </summary>
        /// <param name="workshopId">车间</param>
        /// <param name="workDays">时间段</param>
        /// <param name="itemIds"></param>
        /// <param name="deviceIds"></param>
        /// <param name="itemTypes"></param>
        /// <returns></returns>
        public static IEnumerable<WarningStatistic> GetWarningStatistic(int workshopId, Tuple<DateTime, DateTime> workDays,
            IEnumerable<int> itemIds, IEnumerable<int> deviceIds = null, IEnumerable<WarningItemType> itemTypes = null)
        {
            var res = new List<WarningStatistic>();
            var sql = "";
            var param = new List<string>();
            if (workshopId != 0)
            {
                param.Add("a.WorkshopId = @workshopId");
            }

            if (itemIds != null && itemIds.Any())
            {
                param.Add("a.ItemId IN @itemIds");
            }

            if (deviceIds != null && deviceIds.Any())
            {
                param.Add("b.DeviceId IN @deviceIds");
            }

            if (itemTypes != null && itemTypes.Any())
            {
                param.Add("b.ItemType IN @itemTypes");
            }

            sql = $"SELECT a.* FROM `warning_statistic` a JOIN `warning_set_item` b ON a.ItemId = b.Id " +
                  $"WHERE ((Type = @t AND Time = @st1) OR (Type != @t AND Time >= @st2 AND Time < @ed))" +
                  $"{(param.Any() ? $" AND {param.Join(" AND ")}" : "")} ORDER BY a.Type, a.Time;";
            res.AddRange(ServerConfig.ApiDb.Query<WarningStatistic>(sql,
                new {
                    workshopId,
                    t = WarningStatisticTime.天,
                    st1 = workDays.Item1.DayBeginTime(),
                    st2 = workDays.Item1.NoMinute(),
                    ed = workDays.Item2.NoMinute().AddHours(1),
                    itemIds, deviceIds, itemTypes }));
            return res;
        }
        #endregion

        #region Add
        #endregion

        #region Update
        #endregion

        #region Delete
        /// <summary>
        /// 获取统计
        /// </summary>
        /// <returns></returns>
        public static void Delete(IEnumerable<WarningStatistic> statistics)
        {
            var sql = $"Delete FROM `warning_statistic` WHERE WorkshopId = @WorkshopId AND `Type` = @Type AND Time = @Time AND ItemId = @ItemId;";
            ServerConfig.ApiDb.Execute(sql, statistics);
        }
        #endregion
    }
}
