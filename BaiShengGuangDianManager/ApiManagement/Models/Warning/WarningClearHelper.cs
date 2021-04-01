using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using ApiManagement.Models.DeviceManagementModel;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Models.AccountModel;
using ApiManagement.Models.SmartFactoryModel;

namespace ApiManagement.Models.Warning
{
    public class WarningClearHelper : DataHelper
    {
        private WarningClearHelper()
        {
            Table = "warning_clear";

            InsertSql =
                "INSERT INTO `warning_clear` (`CreateUserId`, `MarkedDateTime`, `DealTime`, `SetId`, `DeviceIds`) " +
                "VALUES(@CreateUserId, @MarkedDateTime, @DealTime, @SetId, @DeviceIds); ";
            UpdateSql =
                "UPDATE `warning_clear` SET `MarkedDateTime` = @MarkedDateTime, `OpTime` = @OpTime, `IsDeal` = @IsDeal WHERE `Id` = @Id;";

            SameField = "SetId";
            MenuFields.AddRange(new[] { "Id", "SetId", "DeviceIds" });
        }
        public static readonly WarningClearHelper Instance = new WarningClearHelper();
        #region Get
        public static IEnumerable<WarningClearDetail> GetWarningClears(DateTime startTime, DateTime endTime, int setId, 
            WarningType warningType, WarningDataType dataType, IEnumerable<int> deviceIds)
        {
            var clears = new List<WarningClearDetail>();
            var param = new List<string> { "DealTime >= @startTime", "DealTime <= @endTime" };
            if (warningType != WarningType.默认)
            {
                param.Add("b.WarningType = @warningType");
            }
            if (dataType != WarningDataType.默认)
            {
                param.Add("b.DataType = @dataType");
            }
            if (setId != 0)
            {
                param.Add("SetId = @setId");
            }
            if (deviceIds != null && deviceIds.Any())
            {
                var t = new List<WarningClearDetail>();
                param.Add("FIND_IN_SET(@deviceId, a.DeviceIds)");
                foreach (var deviceId in deviceIds)
                {
                    t.AddRange(ServerConfig.ApiDb.Query<WarningClearDetail>(
                        $"SELECT a.* FROM `warning_clear` a " +
                        $"JOIN `warning_set` b ON a.SetId = b.Id {(param.Any() ? $" WHERE {param.Join(" AND ")}" : "")} ORDER BY DealTime DESC;",
                        new { startTime, endTime, setId, deviceId }));
                }
                clears.AddRange(t.GroupBy(x => x.SetId).Select(x => x.First()));
            }
            else
            {
                clears.AddRange(ServerConfig.ApiDb.Query<WarningClearDetail>(
                    $"SELECT a.* FROM `warning_clear` a " +
                    $"JOIN `warning_set` b ON a.SetId = b.Id {(param.Any() ? $" WHERE {param.Join(" AND ")}" : "")} ORDER BY DealTime DESC;",
                    new { startTime, endTime, setId, warningType, dataType }));
            }

            if (clears.Any())
            {
                var sets = WarningSetHelper.GetMenus(clears.Select(x => x.SetId).Distinct()).ToDictionary(x => x.Id);
                var devices = DeviceLibraryHelper.GetMenus(clears.SelectMany(x => x.DeviceIdList).Distinct()).ToDictionary(x => x.Id);
                var createUserIds = AccountInfoHelper.GetAccountByAccounts(clears.Select(x => x.CreateUserId).Distinct()).ToDictionary(x => x.Account);
                foreach (var d in clears)
                {
                    d.Name = createUserIds.ContainsKey(d.CreateUserId)? createUserIds[d.CreateUserId].Name : "";
                    d.SetName = sets.ContainsKey(d.SetId) ? sets[d.SetId].Name : "";
                    d.DeviceList.AddRange(d.DeviceIdList.Select(x => devices.ContainsKey(x) ? devices[x].Code :  x.ToString()));
                }
            }

            return clears;
        }

        /// <summary>
        /// isDeal -1 所有 0 未处理  1 已处理
        /// </summary>
        /// <param name="isDeal">-1 所有 0 未处理  1 已处理</param>
        /// <returns></returns>
        public static IEnumerable<WarningClear> GetWarningClears(int isDeal = -1)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (isDeal != -1)
            {
                args.Add(new Tuple<string, string, dynamic>("IsDeal", "=", isDeal));
            }
            return Instance.CommonGet<WarningClear>(args);
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
