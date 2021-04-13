using ApiManagement.Base.Server;
using ApiManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using System;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Models.DeviceSpotCheckModel;
using ModelBase.Models.BaseModel;
using ApiManagement.Models.OtherModel;

namespace ApiManagement.Controllers.DeviceSpotCheckController
{
    /// <summary>
    /// 点检设备
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class SpotCheckDeviceController : ControllerBase
    {
        // GET: api/SpotCheckDevice/Bind/5
        /// <summary>
        /// 设备绑定的点检计划
        /// </summary>
        /// <param name="id">设备Id</param>
        /// <returns></returns>
        [HttpGet("Bind/{id}")]
        public DataResult GetSpotCheckDeviceBind([FromRoute] int id)
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.ApiDb.Query<SpotCheckPlan>("SELECT b.* FROM `spot_check_device_bind` a JOIN `spot_check_plan` " +
                                                                                "b ON a.PlanId = b.Id WHERE a.MarkedDelete = 0 AND b.MarkedDelete = 0 AND a.DeviceId = @id;"
                , new { id }));
            return result;
        }

        // GET: api/SpotCheckDevice
        /// <summary>
        /// 设备点检项目(配置与设置共用)
        /// </summary>
        /// <param name="deviceId">0 为所有</param>
        /// <param name="planId">0 为所有</param>
        /// <returns></returns>
        [HttpGet]
        public DataResult GetSpotCheckDevice([FromQuery] int deviceId, int planId)
        {
            var sql = "SELECT b.*, a.*, IFNULL(c.SurveyorName, '') SurveyorName FROM `spot_check_device` a JOIN ( SELECT a.Plan, b.* FROM `spot_check_plan` a JOIN `spot_check_item` b ON a.Id = b.PlanId ) b ON a.ItemId = b.Id LEFT JOIN `surveyor` c ON a.SurveyorId = c.Id WHERE a.MarkedDelete = 0 AND b.MarkedDelete = 0 AND IFNULL(c.MarkedDelete, 0) = 0";
            if (deviceId != 0)
            {
                sql += " AND a.DeviceId = @deviceId";
            }
            if (planId != 0)
            {
                sql += " AND a.PlanId = @planId";
            }
            sql += " ORDER BY a.`ItemId`";
            var result = new DataResult();
            var data = ServerConfig.ApiDb.Query<SpotCheckDeviceDetail>(sql, new { deviceId, planId });
            if (deviceId == 0)
            {
                var device = data.GroupBy(x => x.DeviceId).Select(x => x.Key).FirstOrDefault();
                result.datas.AddRange(data.Where(x => x.DeviceId == device));
            }
            else
            {
                result.datas.AddRange(data);
            }
            return result;
        }

        // POST: api/SpotCheckDevice
        /// <summary>
        /// 添加或更新设备点检项目
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public Result PostSpotCheckDevice([FromBody] SpotCheckDeviceBindPlan spotCheckDeviceBindPlan)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `spot_check_plan` WHERE Id = @PlanId AND `MarkedDelete` = 0;", new { spotCheckDeviceBindPlan.PlanId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.SpotCheckPlanNotExist);
            }

            var checkDeviceBinds =
                ServerConfig.ApiDb.Query<SpotCheckDeviceBind>("SELECT * FROM `spot_check_device_bind` WHERE PlanId = @PlanId AND MarkedDelete = 0;", new { spotCheckDeviceBindPlan.PlanId });
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            #region 更新
            var oldDevice = spotCheckDeviceBindPlan.DeviceList.Where(x => checkDeviceBinds.Any(y => y.DeviceId == x && y.PlanId == spotCheckDeviceBindPlan.PlanId));
            if (oldDevice.Any())
            {
                var data = ServerConfig.ApiDb.Query<SpotCheckDevice>("SELECT * FROM `spot_check_device` WHERE MarkedDelete = 0 AND PlanId = @PlanId AND DeviceId IN @oldDevice;",
                    new { spotCheckDeviceBindPlan.PlanId, oldDevice });
                foreach (var spotCheckDevice in spotCheckDeviceBindPlan.SpotCheckDevices)
                {

                    var a = data.Where(x => x.ItemId == spotCheckDevice.ItemId).ToList();
                    foreach (var d in a)
                    {
                        if (d != null && d.SurveyorId != spotCheckDevice.SurveyorId)
                        {
                            d.SurveyorId = spotCheckDevice.SurveyorId;
                            d.MarkedDateTime = markedDateTime;
                        }
                    }
                }

                ServerConfig.ApiDb.Execute("UPDATE spot_check_device SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `DeviceId` = @DeviceId, `ItemId` = @ItemId, " +
                                           "`PlanId` = @PlanId, `SurveyorId` = @SurveyorId, `PlannedTime` = @PlannedTime WHERE `Id` = @Id;", data);
            }

            #endregion

            #region 添加
            var newDevice = spotCheckDeviceBindPlan.DeviceList.Where(x => checkDeviceBinds.All(y => !(y.DeviceId == x && y.PlanId == spotCheckDeviceBindPlan.PlanId)));
            if (newDevice.Any())
            {
                ServerConfig.ApiDb.Execute("INSERT INTO spot_check_device_bind (`CreateUserId`, `MarkedDateTime`, `DeviceId`, `PlanId`) " +
                                           "VALUES (@CreateUserId, @MarkedDateTime, @DeviceId, @PlanId);", newDevice.Select(x => new SpotCheckDeviceBind
                                           {
                                               CreateUserId = createUserId,
                                               MarkedDateTime = markedDateTime,
                                               DeviceId = x,
                                               PlanId = spotCheckDeviceBindPlan.PlanId
                                           }));

                var items = ServerConfig.ApiDb.Query<SpotCheckItem>(
                    "SELECT * FROM `spot_check_item` WHERE PlanId = @PlanId AND `MarkedDelete` = 0 AND `Enable` = 1;", new { spotCheckDeviceBindPlan.PlanId });

                var addList = new List<SpotCheckDevice>();
                foreach (var device in newDevice)
                {
                    foreach (var item in items)
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
                            DeviceId = device,
                            ItemId = item.Id,
                            PlanId = item.PlanId,
                            SurveyorId = spotCheckDeviceBindPlan.SpotCheckDevices.FirstOrDefault(x => x.ItemId == item.Id)?.SurveyorId ?? 0,
                            PlannedTime = time
                        });
                    }
                }
                ServerConfig.ApiDb.Execute("INSERT INTO spot_check_device (`CreateUserId`, `MarkedDateTime`, `DeviceId`, `ItemId`, `PlanId`, `SurveyorId`, `PlannedTime`) " +
                                           "VALUES (@CreateUserId, @MarkedDateTime, @DeviceId, @ItemId, @PlanId, @SurveyorId, @PlannedTime);", addList);
            }
            #endregion
            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/SpotCheckDevice
        [HttpDelete]
        public Result DeleteSpotCheckDevice([FromBody] SpotCheckDeviceBindPlan spotCheckDeviceBindPlan)
        {
            ServerConfig.ApiDb.Execute(
                "UPDATE `spot_check_device_bind` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `DeviceId` = @DeviceId AND `PlanId` = @PlanId;",
                spotCheckDeviceBindPlan.DeviceList.Select(x => new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    DeviceId = x,
                    PlanId = spotCheckDeviceBindPlan.PlanId
                }));

            ServerConfig.ApiDb.Execute(
                "UPDATE `spot_check_device` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `DeviceId` = @DeviceId AND `PlanId` = @PlanId;",
                spotCheckDeviceBindPlan.DeviceList.Select(x => new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    DeviceId = x,
                    PlanId = spotCheckDeviceBindPlan.PlanId
                }));

            return Result.GenError<Result>(Error.Success);
        }
    }
}