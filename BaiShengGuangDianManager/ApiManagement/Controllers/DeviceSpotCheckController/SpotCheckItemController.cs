using ApiManagement.Base.Server;
using ApiManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Models.BaseModel;
using ApiManagement.Models.DeviceSpotCheckModel;
using ApiManagement.Models.OtherModel;

namespace ApiManagement.Controllers.DeviceSpotCheckController
{
    /// <summary>
    /// 点检项
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class SpotCheckItemController : ControllerBase
    {
        // GET: api/SpotCheckItem
        [HttpGet]
        public DataResult GetSpotCheckItem([FromQuery] int qId)
        {
            var result = new DataResult();
            var sql = $"SELECT * FROM `spot_check_item` WHERE {(qId == 0 ? "" : "Id = @qId AND ")}`MarkedDelete` = 0;";
            var data = ServerConfig.ApiDb.Query<SpotCheckItem>(sql, new { qId });
            if (qId != 0 && !data.Any())
            {
                return Result.GenError<DataResult>(Error.SpotCheckItemNotExist);
            }
            result.datas.AddRange(data);
            return result;
        }

        // GET: api/SpotCheckItem/Plan/
        [HttpGet("Plan")]
        public DataResult GetSpotCheckItemByPlan([FromQuery] int qId, bool enable)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `spot_check_plan` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = qId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<DataResult>(Error.SpotCheckPlanNotExist);
            }

            var result = new DataResult();
            result.datas.AddRange(ServerConfig.ApiDb.Query<SpotCheckItem>($"SELECT * FROM `spot_check_item` WHERE PlanId = @id AND `MarkedDelete` = 0{(enable ? " AND `Enable` = @Enable" : "")};",
                new { id = qId, Enable = enable ? 1 : 0 }));
            return result;
        }

        // PUT: api/SpotCheckItem/5
        [HttpPut]
        public Result PutSpotCheckItem([FromBody] IEnumerable<SpotCheckItem> spotCheckItems)
        {
            if (spotCheckItems == null)
            {
                return Result.GenError<Result>(Error.SpotCheckItemNotExist);
            }

            if (spotCheckItems.Any(x => x.Item.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SpotCheckItemNotEmpty);
            }

            var planIdList = spotCheckItems.GroupBy(x => x.PlanId).Select(y => y.Key);
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `spot_check_plan` WHERE Id IN @planIdList AND `MarkedDelete` = 0;", new { planIdList }).FirstOrDefault();
            if (cnt != planIdList.Count())
            {
                return Result.GenError<Result>(Error.SpotCheckPlanNotExist);
            }
            var data =
                ServerConfig.ApiDb.Query<SpotCheckItem>("SELECT * FROM `spot_check_item` WHERE PlanId IN @planIdList AND MarkedDelete = 0;", new { planIdList });
            spotCheckItems = spotCheckItems.Where(x => data.Any(y => y.PlanId == x.PlanId && y.Id == x.Id));
            foreach (var planId in planIdList)
            {
                var items = data.Where(x => x.PlanId == planId);
                var uItems = spotCheckItems.Where(x => x.PlanId == planId);
                if (items.Any(x => uItems.Any(y => y.Item == x.Item && y.Id != x.Id)))
                {
                    return Result.GenError<Result>(Error.SpotCheckItemIsExist);
                }
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var spotCheckItem in spotCheckItems)
            {
                spotCheckItem.MarkedDateTime = markedDateTime;
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE spot_check_item SET `MarkedDateTime` = @MarkedDateTime, `Item` = @Item, `Enable` = @Enable, `Remind` = @Remind, `Min` = @Min, `Max` = @Max, `Unit` = @Unit, `Reference` = @Reference, `Remarks` = @Remarks, " +
                "`Interval` = @Interval, `Day` = @Day, `Month` = @Month, `NormalHour` = @NormalHour, `Week` = @Week, `WeekHour` = @WeekHour WHERE `Id` = @Id;", spotCheckItems);

            var enableItems = spotCheckItems.Where(x => data.Any(y => y.Id == x.Id && y.Enable != x.Enable));
            #region 删除
            var dels = enableItems.Where(x => !x.Enable);
            if (dels.Any())
            {
                ServerConfig.ApiDb.Execute(
                    "UPDATE `spot_check_device` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `ItemId` = @Id;", dels.Select(x => new
                    {
                        MarkedDateTime = DateTime.Now,
                        MarkedDelete = true,
                        Id = x.Id
                    }));
            }

            #endregion

            #region 添加
            var news = enableItems.Where(x => x.Enable);
            if (news.Any())
            {
                var spotCheckDeviceBinds = ServerConfig.ApiDb.Query<SpotCheckDeviceBind>(
                    "SELECT * FROM `spot_check_device_bind` WHERE MarkedDelete = 0 AND PlanId IN @PlanId;", new { PlanId = enableItems.Select(x => x.PlanId) });

                foreach (var planId in planIdList)
                {
                    var deviceList = spotCheckDeviceBinds.Where(x => x.PlanId == planId);
                    if (deviceList.Any())
                    {
                        var addList = new List<SpotCheckDevice>();
                        foreach (var device in deviceList)
                        {
                            foreach (var item in news)
                            {
                                var time = default(DateTime);
                                switch (item.Interval)
                                {
                                    case IntervalEnum.Day:
                                        time = markedDateTime.Date.AddDays(item.Day).AddMonths(item.Month).AddHours(item.NormalHour);
                                        break;
                                    case IntervalEnum.Week:
                                        time = markedDateTime.Date.AddDays(item.Week * 7).AddHours(item.WeekHour);
                                        break;
                                }
                                addList.Add(new SpotCheckDevice
                                {
                                    CreateUserId = createUserId,
                                    MarkedDateTime = markedDateTime,
                                    DeviceId = device.DeviceId,
                                    ItemId = item.Id,
                                    PlanId = item.PlanId,
                                    SurveyorId = 0,
                                    PlannedTime = time
                                });
                            }
                        }
                        ServerConfig.ApiDb.Execute("INSERT INTO spot_check_device (`CreateUserId`, `MarkedDateTime`, `DeviceId`, `ItemId`, `PlanId`, `SurveyorId`, `PlannedTime`) " +
                                                   "VALUES (@CreateUserId, @MarkedDateTime, @DeviceId, @ItemId, @PlanId, @SurveyorId, @PlannedTime);", addList);
                    }
                }
            }
            #endregion
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SpotCheckItem
        [HttpPost]
        public Result PostSpotCheckItem([FromBody] IEnumerable<SpotCheckItem> spotCheckItems)
        {
            if (spotCheckItems == null)
            {
                return Result.GenError<Result>(Error.SpotCheckItemNotExist);
            }

            if (spotCheckItems.Any(x => x.Item.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SpotCheckItemNotEmpty);
            }
            var planIdList = spotCheckItems.GroupBy(x => x.PlanId).Select(y => y.Key);
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `spot_check_plan` WHERE Id IN @planIdList AND `MarkedDelete` = 0;", new { planIdList }).FirstOrDefault();
            if (cnt != planIdList.Count())
            {
                return Result.GenError<Result>(Error.SpotCheckPlanNotExist);
            }

            var data = ServerConfig.ApiDb.Query<SpotCheckItem>("SELECT * FROM `spot_check_item` WHERE PlanId IN @planIdList AND MarkedDelete = 0;", new { planIdList });

            foreach (var planId in planIdList)
            {
                var items = data.Where(x => x.PlanId == planId);
                var uItems = spotCheckItems.Where(x => x.PlanId == planId);
                if (items.Any(x => uItems.Any(y => y.Item == x.Item)))
                {
                    return Result.GenError<Result>(Error.SpotCheckItemIsExist);
                }
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var spotCheckItem in spotCheckItems)
            {
                spotCheckItem.CreateUserId = createUserId;
                spotCheckItem.MarkedDateTime = markedDateTime;
            }
            ServerConfig.ApiDb.Execute(
                "INSERT INTO spot_check_item (`CreateUserId`, `MarkedDateTime`, `Item`, `PlanId`, `Enable`, `Remind`, `Min`, `Max`, `Unit`, `Reference`, `Remarks`, `Interval`, `Day`, `Month`, `NormalHour`, `Week`, `WeekHour`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @Item, @PlanId, @Enable, @Remind, @Min, @Max, @Unit, @Reference, @Remarks, @Interval, @Day, @Month, @NormalHour, @Week, @WeekHour);",
                spotCheckItems);


            var spotCheckDeviceBinds = ServerConfig.ApiDb.Query<SpotCheckDeviceBind>(
                "SELECT * FROM `spot_check_device_bind` WHERE MarkedDelete = 0 AND PlanId IN @PlanId;", new { PlanId = planIdList });

            #region 添加
            var news = ServerConfig.ApiDb.Query<SpotCheckItem>($"SELECT * FROM `spot_check_item` WHERE {(data.Select(x => x.Id).Any() ? "Id NOT IN @ids AND " : "")}{(planIdList.Any() ? "PlanId IN @planIdList AND " : "")}MarkedDelete = 0;",
                new { ids = data.Select(x => x.Id), planIdList });

            foreach (var planId in planIdList)
            {
                var deviceList = spotCheckDeviceBinds.Where(x => x.PlanId == planId);
                if (deviceList.Any())
                {
                    var addList = new List<SpotCheckDevice>();
                    foreach (var device in deviceList)
                    {
                        foreach (var item in news)
                        {
                            var time = default(DateTime);
                            switch (item.Interval)
                            {
                                case IntervalEnum.Day:
                                    time = markedDateTime.Date.AddDays(item.Day).AddMonths(item.Month).AddHours(item.NormalHour);
                                    break;
                                case IntervalEnum.Week:
                                    time = markedDateTime.Date.AddDays(item.Week * 7).AddHours(item.WeekHour);
                                    break;
                            }
                            addList.Add(new SpotCheckDevice
                            {
                                CreateUserId = createUserId,
                                MarkedDateTime = markedDateTime,
                                DeviceId = device.DeviceId,
                                ItemId = item.Id,
                                PlanId = item.PlanId,
                                SurveyorId = 0,
                                PlannedTime = time
                            });
                        }
                    }
                    ServerConfig.ApiDb.Execute("INSERT INTO spot_check_device (`CreateUserId`, `MarkedDateTime`, `DeviceId`, `ItemId`, `PlanId`, `SurveyorId`, `PlannedTime`) " +
                                               "VALUES (@CreateUserId, @MarkedDateTime, @DeviceId, @ItemId, @PlanId, @SurveyorId, @PlannedTime);", addList);
                }
            }
            #endregion

            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/SpotCheckItem/5
        /// <summary>
        /// 批量删除 点检项
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteSpotCheckItem([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `spot_check_item` WHERE Id IN @id AND `MarkedDelete` = 0;", new { id = ids }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.SpotCheckItemNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `spot_check_item` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id` IN @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = ids
                });

            ServerConfig.ApiDb.Execute(
                "UPDATE `spot_check_device` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `ItemId`IN @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = ids
                });
            return Result.GenError<Result>(Error.Success);
        }

    }
}