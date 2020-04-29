using System;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Base.Server;
using ApiManagement.Models.DeviceSpotCheckModel;
using ApiManagement.Models.OtherModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;

namespace ApiManagement.Controllers.DeviceSpotCheckController
{
    /// <summary>
    /// 设备点检
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class SpotCheckController : ControllerBase
    {
        // GET: api/SpotCheck
        /// <summary>
        /// 设备点检情况
        /// </summary>
        /// <param name="ids">设备Id, 例子:1,2,3,4</param>
        /// <param name="plans">计划Id, 例子:1,2,3,4</param>
        /// <returns></returns>
        [HttpGet]
        public DataResult GetSpotCheck([FromQuery] string ids, string plans)
        {
            try
            {
                var result = new DataResult();

                var sql = "SELECT d.*, a.*, b.Plan FROM `spot_check_device` a JOIN `spot_check_plan` b ON a.PlanId = b.Id JOIN `spot_check_item` c ON a.ItemId = c.Id LEFT JOIN `spot_check_log` d ON a.LogId = d.Id WHERE a.MarkedDelete = 0 AND b.MarkedDelete = 0 AND c.MarkedDelete = 0 AND c.`Interval` != 0";
                if (!ids.IsNullOrEmpty())
                {
                    sql += " AND FIND_IN_SET(a.DeviceId, @ids) > 0";
                }
                if (!plans.IsNullOrEmpty())
                {
                    sql += " AND FIND_IN_SET(a.PlanId, @plans) > 0";
                }

                var spotCheckLogs = ServerConfig.ApiDb.Query<SpotCheckLog>(sql, new { ids, plans, }).GroupBy(x => x.DeviceId).ToDictionary(y => y.Key, y => y.Select(z => z));
                var r = new List<SpotCheckProcess>();
                foreach (var spotCheckLog in spotCheckLogs)
                {
                    var d = new SpotCheckProcess(spotCheckLog.Key);
                    var pls = spotCheckLog.Value.GroupBy(x => x.PlanId).ToDictionary(y => y.Key, y => y.Select(z => z));
                    d.Data.AddRange(pls.Select(x => new SpotCheckProcessInfo
                    {
                        PlanId = x.Key,
                        Plan = x.Value.First().Plan,
                        Done = x.Value.Count(y => y.LogId != 0),
                        Total = x.Value.Count(),
                        NotPass = x.Value.Count(y => y.LogId != 0 && (y.Actual >= y.Min && y.Actual <= y.Max)),
                        Pass = x.Value.Count(y => y.LogId != 0 && !(y.Actual >= y.Min && y.Actual <= y.Max))
                    }));
                    r.Add(d);
                }
                result.datas.AddRange(r);
                return result;
            }
            catch (Exception)
            {
                return Result.GenError<DataResult>(Error.ParamError);
            }
        }

        // GET: api/SpotCheck/Detail
        /// <summary>
        /// 设备点检详情
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="planId"></param>
        /// <param name="account">账号</param>
        /// <returns></returns>
        [HttpGet("Detail")]
        public DataResult GetSpotCheckDetail([FromQuery] int deviceId, int planId, string account)
        {
            var sql = "SELECT c.*, d.*, b.Plan, a.*, IFNULL(e.SurveyorName, '') SurveyorName FROM `spot_check_device` a " +
                      "JOIN `spot_check_plan` b ON a.PlanId = b.Id " +
                      "JOIN `spot_check_item` c ON a.ItemId = c.Id " +
                      "LEFT JOIN `spot_check_log` d ON a.LogId = d.Id " +
                      "LEFT JOIN `surveyor` e ON a.SurveyorId = e.Id " +
                      "WHERE a.MarkedDelete = 0 AND b.MarkedDelete = 0 AND c.MarkedDelete = 0 AND IFNULL(d.MarkedDelete, 0) = 0 AND IFNULL(e.MarkedDelete, 0) = 0";
            if (deviceId != 0)
            {
                sql += " AND a.DeviceId = @deviceId";
            }
            if (planId != 0)
            {
                sql += " AND a.PlanId = @planId";
            }

            if (!account.IsNullOrEmpty())
            {
                sql += " AND IFNULL(e.Account, '') = @account";
            }

            var result = new DataResult();
            result.datas.AddRange(ServerConfig.ApiDb.Query<SpotCheckLog>(sql, new { deviceId, planId, account }));
            return result;
        }

        // GET: api/SpotCheck/Next
        /// <summary>
        /// 下次设备点检详情
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="plans"></param>
        /// <param name="account">账号</param>
        /// <returns></returns>
        [HttpGet("Next")]
        public DataResult GetSpotCheckNext([FromQuery]  string ids, string plans, string account)
        {
            try
            {
                var sql = "SELECT c.*, b.Plan, a.*, d.`Code`, IFNULL(e.SurveyorName, '') SurveyorName FROM `spot_check_device` a " +
                          "JOIN `spot_check_plan` b ON a.PlanId = b.Id " +
                          "JOIN `spot_check_item` c ON a.ItemId = c.Id " +
                          "JOIN `device_library` d ON a.DeviceId = d.Id " +
                          "LEFT JOIN `surveyor` e ON a.SurveyorId = e.Id " +
                          "WHERE a.MarkedDelete = 0 AND b.MarkedDelete = 0 AND c.MarkedDelete = 0";

                if (!ids.IsNullOrEmpty())
                {
                    sql += " AND FIND_IN_SET(a.DeviceId, @ids) > 0";
                }
                if (!plans.IsNullOrEmpty())
                {
                    sql += " AND FIND_IN_SET(a.PlanId, @plans) > 0";
                }
                if (!account.IsNullOrEmpty())
                {
                    sql += " AND IFNULL(e.Account, '') = @account";
                }

                var spotCheckDeviceNexts = ServerConfig.ApiDb.Query<SpotCheckDeviceNext>(sql, new { ids, plans, account });

                foreach (var checkDeviceNext in spotCheckDeviceNexts)
                {
                    var time = default(DateTime);
                    switch (checkDeviceNext.Interval)
                    {
                        case IntervalEnum.Day:
                            time = checkDeviceNext.PlannedTime.Date.AddDays(checkDeviceNext.Day).AddMonths(checkDeviceNext.Month).AddHours(checkDeviceNext.NormalHour);
                            break;
                        case IntervalEnum.Week:
                            time = checkDeviceNext.PlannedTime.Date.AddDays(checkDeviceNext.Week * 7).AddHours(checkDeviceNext.WeekHour);
                            break;
                        default:
                            break;
                    }

                    checkDeviceNext.PlannedTime = time;
                    checkDeviceNext.Devices = spotCheckDeviceNexts.Where(x => x.PlanId == checkDeviceNext.PlanId && x.ItemId == checkDeviceNext.ItemId).OrderBy(y => y.DeviceId).Select(z => z.Code).Join(",");
                }
                var r = new List<SpotCheckDeviceNext>();
                foreach (var checkDeviceNext in spotCheckDeviceNexts)
                {
                    if (!r.Any(x => x.PlanId == checkDeviceNext.PlanId && x.ItemId == checkDeviceNext.ItemId))
                    {
                        r.Add(checkDeviceNext);
                    }
                }
                var result = new DataResult();
                result.datas.AddRange(r);
                return result;
            }
            catch (Exception)
            {
                return Result.GenError<DataResult>(Error.ParamError);
            }
        }

        // POST: api/SpotCheck
        /// <summary>
        /// 更新设备点检项目
        /// </summary>
        /// <returns></returns>
        [HttpPut]
        public Result PostSpotCheck([FromBody] IEnumerable<SpotCheckLog> spotCheckLogs)
        {
            if (spotCheckLogs == null)
            {
                return Result.GenError<Result>(Error.SpotCheckItemNotExist);
            }

            var data = ServerConfig.ApiDb.Query<SpotCheckDevice>("SELECT * FROM `spot_check_device` WHERE MarkedDelete = 0 AND Id IN @Id;",
                new { Id = spotCheckLogs.Select(x => x.Id) });

            //if (data.Where(x => spotCheckLogs.Any(y => y.Id == x.Id)).Any(y => y.Expired))
            //{
            //    return Result.GenError<Result>(Error.SpotCheckDeviceExpired);
            //}

            //var planIdList = spotCheckLogs.GroupBy(x => x.PlanId).Select(y => y.Key);
            //var cnt =
            //    //ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `spot_check_plan` WHERE Id IN @planIdList AND `MarkedDelete` = 0;", new { planIdList }).FirstOrDefault();
            //    ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `spot_check_plan` WHERE Id IN @planIdList;", new { planIdList }).FirstOrDefault();
            //if (cnt != planIdList.Count())
            //{
            //    return Result.GenError<Result>(Error.SpotCheckPlanNotExist);
            //}
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            #region 更新
            var checkedLogs = spotCheckLogs.Where(x => data.Any(y => y.Id == x.Id && !y.Expired && y.LogId != 0));
            if (checkedLogs.Any())
            {
                ServerConfig.ApiDb.Execute("UPDATE spot_check_log SET `MarkedDateTime` = @MarkedDateTime, `CheckTime` = @CheckTime, `Actual` = @Actual, `Desc` = @Desc WHERE `Id` = @Id;",
                    checkedLogs.Select(x =>
                    {
                        x.MarkedDateTime = markedDateTime;
                        x.Id = data.First(y => y.Id == x.Id && !y.Expired && y.LogId != 0).LogId;
                        return x;
                    }));
            }
            #endregion
            #region 新增
            var newLogs = spotCheckLogs.Where(x => data.Any(y => y.Id == x.Id && !y.Expired && y.LogId == 0));
            if (newLogs.Any())
            {
                var sql = "SELECT c.*, a.*, b.Plan FROM `spot_check_device` a " +
                          "JOIN `spot_check_plan` b ON a.PlanId = b.Id " +
                          "JOIN `spot_check_item` c ON a.ItemId = c.Id " +
                          "WHERE a.MarkedDelete = 0 " +
                          "AND b.MarkedDelete = 0 " +
                          "AND c.MarkedDelete = 0 " +
                          "AND c.`Interval` != 0 " +
                          "AND a.LogId = 0 " +
                          "AND a.Id IN @ids;";
                var details = ServerConfig.ApiDb.Query<SpotCheckLog>(sql, new { ids = newLogs.Select(x => x.Id) });
                ServerConfig.ApiDb.Execute(
                    "INSERT INTO spot_check_log (`CreateUserId`, `MarkedDateTime`, `PlannedTime`, `DeviceId`, `ItemId`, `Item`, `PlanId`, `Plan`, `Enable`, `Remind`, `Min`, `Max`, `Unit`, `Reference`, `Remarks`, `Interval`, `Day`, `Month`, `NormalHour`, `Week`, `WeekHour`, `SurveyorId`, `CheckTime`, `Actual`, `Desc`) " +
                    "VALUES (@CreateUserId, @MarkedDateTime, @PlannedTime, @DeviceId, @ItemId, @Item, @PlanId, @Plan, @Enable, @Remind, @Min, @Max, @Unit, @Reference, @Remarks, @Interval, @Day, @Month, @NormalHour, @Week, @WeekHour, @SurveyorId, @CheckTime, @Actual, @Desc);",
                    details.Select(x =>
                    {
                        x.CreateUserId = createUserId;
                        x.MarkedDateTime = markedDateTime;
                        x.CheckTime = newLogs.FirstOrDefault(y => y.Id == x.Id)?.CheckTime ?? default(DateTime);
                        x.Actual = newLogs.FirstOrDefault(y => y.Id == x.Id)?.Actual ?? 0;
                        x.Desc = newLogs.FirstOrDefault(y => y.Id == x.Id)?.Desc ?? string.Empty;
                        return x;
                    }));

                var logs = ServerConfig.ApiDb.Query<SpotCheckLog>("SELECT * FROM `spot_check_log` WHERE ItemId IN @ItemId AND PlannedTime IN @PlannedTime;",
                   new { ItemId = details.Select(x => x.ItemId).Distinct(), PlannedTime = details.Select(x => x.PlannedTime).Distinct() });

                ServerConfig.ApiDb.Execute("UPDATE `spot_check_device` SET `MarkedDateTime`= @MarkedDateTime, `LogId` = @LogId WHERE `Id` = @Id;", details.Select(x => new
                {
                    MarkedDateTime = markedDateTime,
                    LogId = logs.FirstOrDefault(y => y.DeviceId == x.DeviceId && y.ItemId == x.ItemId && !y.Expired)?.Id ?? 0,
                    x.Id
                }));
            }
            #endregion
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SpotCheck/Image
        /// <summary>
        /// 更新设备点检项目
        /// </summary>
        /// <returns></returns>
        [HttpPut("Image")]
        public Result PostSpotCheckImage([FromBody] SpotCheckLog spotCheckLog)
        {
            if (spotCheckLog == null)
            {
                return Result.GenError<Result>(Error.SpotCheckItemNotExist);
            }

            //var cnt =
            //    //ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `spot_check_plan` WHERE Id = @PlanId AND `MarkedDelete` = 0;", new { spotCheckLog.PlanId }).FirstOrDefault();
            //    ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `spot_check_plan` WHERE Id = @PlanId;", new { spotCheckLog.PlanId }).FirstOrDefault();
            //if (cnt == 0)
            //{
            //    return Result.GenError<Result>(Error.SpotCheckPlanNotExist);
            //}

            var data = ServerConfig.ApiDb.Query<SpotCheckDevice>("SELECT * FROM `spot_check_device` WHERE Id = @Id AND `MarkedDelete` = 0;",
                new { spotCheckLog.Id }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.SpotCheckItemNotExist);
            }

            //if (data.Expired)
            //{
            //    return Result.GenError<Result>(Error.SpotCheckDeviceExpired);
            //}

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            if (data.LogId != 0)
            {
                #region 更新
                spotCheckLog.LogId = data.LogId;
                spotCheckLog.MarkedDateTime = markedDateTime;
                ServerConfig.ApiDb.Execute("UPDATE spot_check_log SET `MarkedDateTime` = @MarkedDateTime, `Images` = @Images WHERE `Id` = @LogId;",
                    spotCheckLog);
                #endregion
            }
            else
            {
                #region 新增
                var sql = "SELECT c.*, a.*, b.Plan FROM `spot_check_device` a " +
                          "JOIN `spot_check_plan` b ON a.PlanId = b.Id " +
                          "JOIN `spot_check_item` c ON a.ItemId = c.Id " +
                          "WHERE a.MarkedDelete = 0 " +
                          "AND b.MarkedDelete = 0 " +
                          "AND c.MarkedDelete = 0 " +
                          "AND c.`Interval` != 0 " +
                          "AND a.LogId = 0 " +
                          "AND a.Id = @Id;";
                var detail = ServerConfig.ApiDb.Query<SpotCheckLog>(sql, new { spotCheckLog.Id }).FirstOrDefault();
                if (detail != null)
                {
                    detail.CreateUserId = createUserId;
                    detail.MarkedDateTime = markedDateTime;
                    detail.Images = spotCheckLog.Images;

                    var id = ServerConfig.ApiDb.Query<int>(
                        "INSERT INTO spot_check_log (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `PlannedTime`, `DeviceId`, `ItemId`, `Item`, `PlanId`, `Plan`, `Enable`, `Remind`, `Min`, `Max`, `Unit`, `Reference`, `Remarks`, `Interval`, `Day`, `Month`, `NormalHour`, `Week`, `WeekHour`, `SurveyorId`, `Images`) " +
                        "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @PlannedTime, @DeviceId, @ItemId, @Item, @PlanId, @Plan, @Enable, @Remind, @Min, @Max, @Unit, @Reference, @Remarks, @Interval, @Day, @Month, @NormalHour, @Week, @WeekHour, @SurveyorId, @Images);" +
                        "SELECT LAST_INSERT_ID();",
                        detail).FirstOrDefault();
                    spotCheckLog.MarkedDateTime = markedDateTime;
                    spotCheckLog.LogId = id;
                    ServerConfig.ApiDb.Execute("UPDATE `spot_check_device` SET `MarkedDateTime`= @MarkedDateTime, `LogId` = @LogId WHERE `Id` = @Id;", spotCheckLog);
                }
                #endregion
            }

            return Result.GenError<Result>(Error.Success);
        }
    }
}