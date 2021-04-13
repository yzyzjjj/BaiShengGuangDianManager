using ApiManagement.Base.Server;
using ModelBase.Base.Logger;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.Warning
{
    public class WarningStatisticHelper : DataHelper
    {
        public static Dictionary<WarningStatisticTime, string> Tables = new Dictionary<WarningStatisticTime, string>
        {
            { WarningStatisticTime.分, "warning_statistic_min" },
            { WarningStatisticTime.时, "warning_statistic_hour" },
            { WarningStatisticTime.天, "warning_statistic_day" },
        };
        private WarningStatisticHelper()
        {
            Table = "";
            InsertSql =
                "";
            UpdateSql =
                "";

            SameField = "";
            //MenuFields.AddRange(new[] { "Id", "Item" });
        }
        public static readonly WarningStatisticHelper Instance = new WarningStatisticHelper();
        #region Get
        /// <summary>
        /// 获取统计
        /// </summary>
        /// <param name="timeType"> 分 时 天</param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="itemIds"></param>
        /// <param name="dataType">数据分类0默认,1设备数据, 2生产数据,3 故障数据,</param>
        /// <param name="deviceIds"></param>
        /// <param name="itemTypes"></param>
        /// <returns></returns>
        public static IEnumerable<WarningStatistic> GetWarningStatistic(WarningStatisticTime timeType, DateTime startTime, DateTime endTime,
            IEnumerable<int> itemIds, WarningDataType dataType = WarningDataType.默认, IEnumerable<int> deviceIds = null, IEnumerable<WarningItemType> itemTypes = null)
        {
            var res = new List<WarningStatistic>();
            if (Tables.ContainsKey(timeType))
            {
                var table = Tables[timeType];
                var sql = "";
                if (dataType == WarningDataType.默认)
                {
                    sql = $"SELECT * FROM `{table}` " +
                          $"WHERE Time >= @startTime AND Time < @endTime" +
                          $" AND ItemId IN @itemIds;";
                }
                else
                {
                    var param = new List<string>();
                    if (itemIds != null && itemIds.Any())
                    {
                        param.Add("b.ItemId IN @ItemId");
                    }
                    if (deviceIds != null && deviceIds.Any())
                    {
                        param.Add("b.DeviceId IN @deviceIds");
                    }
                    if (itemTypes != null && itemTypes.Any())
                    {
                        param.Add("b.ItemType IN @itemTypes");
                    }
                    sql = $"SELECT a.* FROM `{table}` a JOIN `warning_set_item` b ON a.ItemId = b.Id " +
                          $"WHERE Time >= @startTime AND Time < @endTime" +
                          $" AND ItemId IN @itemIds" +
                          $"{(param.Any() ? $" AND {param.Join(" AND ")}" : "")};";
                }
                res.AddRange(ServerConfig.ApiDb.Query<WarningStatistic>(sql,
                    new { startTime, endTime, dataType, itemIds, deviceIds, itemTypes }));
            }
            return res;
        }

        /// <summary>
        /// 获取统计
        /// </summary>
        /// <param name="timeType"> 分 时 天</param>
        /// <param name="time"></param>
        /// <param name="itemIds"></param>
        /// <param name="dataType">数据分类0默认,1设备数据, 2生产数据,3 故障数据,</param>
        /// <param name="deviceIds"></param>
        /// <param name="itemTypes"></param>
        /// <returns></returns>
        public static IEnumerable<WarningStatistic> GetWarningStatistic(WarningStatisticTime timeType, DateTime time,
            IEnumerable<int> itemIds = null, WarningDataType dataType = WarningDataType.默认, IEnumerable<int> deviceIds = null, IEnumerable<WarningItemType> itemTypes = null)
        {
            var res = new List<WarningStatistic>();
            if (Tables.ContainsKey(timeType))
            {
                var table = Tables[timeType];
                var sql = "";
                if (dataType == WarningDataType.默认)
                {
                    sql = $"SELECT a.*, b.SetId, b.SetName, b.Item FROM `{table}` a " +
                          $"JOIN (SELECT a.*, b.`Name` SetName FROM `warning_set_item` a JOIN warning_set b ON a.SetId = b.Id) b ON a.ItemId = b.Id " +
                          $"WHERE Time = @time{(itemIds != null && itemIds.Any() ? " AND ItemId IN @itemIds" : "")} GROUP BY Time, a.ItemId;";
                }
                else
                {
                    var param = new List<string>();
                    if (deviceIds != null && deviceIds.Any())
                    {
                        param.Add("c.DeviceId IN @deviceIds");
                    }
                    if (itemTypes != null && itemTypes.Any())
                    {
                        param.Add("b.ItemType IN @itemTypes");
                    }
                    sql = $"SELECT a.*, b.SetId, b.SetName, b.Item, b.`Range` FROM `{table}` a " +
                          $"JOIN (SELECT a.*, b.`Name` SetName FROM `warning_set_item` a JOIN warning_set b ON a.SetId = b.Id) b ON a.ItemId = b.Id " +
                          $"JOIN `warning_current` c ON c.ItemId = a.ItemId " +
                          $"WHERE Time = @time" +
                          $"{(itemIds != null && itemIds.Any() ? " AND ItemId IN @itemIds" : "")}" +
                          //$"{(param.Any() ? $" AND {param.Join(" AND ")}" : "")} AND b.MarkedDelete = 0 GROUP BY Time, a.ItemId;";
                          $"{(param.Any() ? $" AND {param.Join(" AND ")}" : "")} AND b.MarkedDelete = 0 GROUP BY Time, a.ItemId;";
                }
                res.AddRange(ServerConfig.ApiDb.Query<WarningStatistic>(sql, new { time, dataType, deviceIds, itemTypes }));
            }
            return res;
        }

        #endregion

        #region Add
        /// <summary>
        /// 获取统计
        /// </summary>
        /// <returns></returns>
        public static void Add(WarningStatisticTime timeType, IEnumerable<WarningStatistic> statistics)
        {
            if (Tables.ContainsKey(timeType))
            {
                var table = Tables[timeType];
                var sql = $"INSERT INTO  `{table}` (`Time`, `ItemId`, `Count`) VALUES (@Time, @ItemId, @Count);";
                ServerConfig.ApiDb.Execute(sql, statistics);
            }
            else
            {
                Log.Error($"Add Error, {timeType}");
            }
        }
        #endregion

        #region Update
        /// <summary>
        /// 获取统计
        /// </summary>
        /// <returns></returns>
        public static void Update(WarningStatisticTime timeType, IEnumerable<WarningStatistic> statistics)
        {
            if (Tables.ContainsKey(timeType))
            {
                var table = Tables[timeType];
                var sql = $"UPDATE `{table}` SET `Count` = @Count WHERE `Time` = @Time AND `ItemId` = @ItemId;";
                ServerConfig.ApiDb.Execute(sql, statistics);
            }
            else
            {
                Log.Error($"Update Error, {timeType}");
            }
        }
        #endregion

        #region Delete
        #endregion
    }
}
