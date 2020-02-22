using ApiManagement.Base.Server;
using ApiManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using System;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Models.BaseModel;
using ApiManagement.Models.DeviceSpotCheckModel;

namespace ApiManagement.Controllers.DeviceSpotCheckController
{
    /// <summary>
    /// 点检记录
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class SpotCheckLogController : ControllerBase
    {
        // GET: api/SpotCheckLog
        /// <summary>
        /// 点检记录
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="deviceId">设备Id</param>
        /// <param name="planId"></param>
        /// <returns></returns>
        [HttpGet]
        public DataResult GetSpotCheckLog([FromQuery] DateTime startTime, DateTime endTime, int deviceId, int planId)
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.ApiDb.Query<SpotCheckLog>("SELECT a.* , b.SurveyorName FROM `spot_check_log` a JOIN `surveyor` b ON a.SurveyorId = b.Id " +
                                                                         "WHERE a.MarkedDelete = 0" +
                                                                         $"{(deviceId != 0 ? " AND a.DeviceId = @DeviceId" : "")}" +
                                                                         $"{(planId != 0 ? " AND a.PlanId = @PlanId" : "")}" +
                                                                         $"{(startTime != default(DateTime) && startTime != default(DateTime) ? " AND (a.PlannedTime >= @PlannedTime1 AND a.PlannedTime <= @PlannedTime2)" : "")};"
                , new
                {
                    DeviceId = deviceId,
                    PlanId = planId,
                    PlannedTime1 = startTime,
                    PlannedTime2 = endTime,
                }));
            return result;
        }

        // PUT: api/SpotCheckLog
        /// <summary>
        /// 更新点检记录
        /// </summary>
        /// <returns></returns>
        [HttpPut]
        public Result PutSpotCheckLog([FromBody] IEnumerable<SpotCheckLog> spotCheckLogs)
        {
            var now = DateTime.Now;
            ServerConfig.ApiDb.Execute("UPDATE spot_check_log SET `MarkedDateTime` = @MarkedDateTime, `SurveyorId` = @SurveyorId, `CheckTime` = @CheckTime, `Actual` = @Actual, `Desc` = @Desc WHERE `Id` = @Id;", spotCheckLogs.Select(
                x =>
                {
                    x.MarkedDateTime = now;
                    return x;
                }));

            return Result.GenError<Result>(Error.Success);
        }

        // PUT: api/SpotCheckLog/Image
        /// <summary>
        /// 更新点检记录图片
        /// </summary>
        /// <returns></returns>
        [HttpPut("Image")]
        public Result PutSpotCheckLogImage([FromBody] SpotCheckLog spotCheckLog)
        {
            spotCheckLog.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "UPDATE spot_check_log SET `MarkedDateTime` = @MarkedDateTime, `Images` = @Images WHERE `Id` = @Id;",
                spotCheckLog);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SpotCheckLog
        /// <summary>
        /// 添加点检记录
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public Result PostSpotCheckLog([FromBody] SpotCheckLog spotCheckLog)
        {
            var data = ServerConfig.ApiDb.Query<SpotCheckLog>("SELECT * FROM `spot_check_log` WHERE MarkedDelete = 0 AND DeviceId = @DeviceId AND PlanId = @PlanId AND PlannedTime = @PlannedTime;"
                , spotCheckLog);
            if (data == null)
            {
                return Result.GenError<Result>(Error.SpotCheckLogNotExist);
            }
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            spotCheckLog.CreateUserId = createUserId;
            spotCheckLog.MarkedDateTime = markedDateTime;
            ServerConfig.ApiDb.Execute(
                "INSERT INTO spot_check_log (`CreateUserId`, `MarkedDateTime`, `PlannedTime`, `DeviceId`, `ItemId`, `Item`, `PlanId`, `Plan`, `Enable`, `Remind`, `Min`, `Max`, `Unit`, `Reference`, `Remarks`, `Interval`, `Day`, `Month`, `NormalHour`, `Week`, `WeekHour`, `SurveyorId`, `CheckTime`, `Actual`, `Desc`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @PlannedTime, @DeviceId, @ItemId, @Item, @PlanId, @Plan, @Enable, @Remind, @Min, @Max, @Unit, @Reference, @Remarks, @Interval, @Day, @Month, @NormalHour, @Week, @WeekHour, @SurveyorId, @CheckTime, @Actual, @Desc);",
                spotCheckLog);

            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/SpotCheckLog/5
        /// <summary>
        /// 删除点检记录
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteSpotCheckLog([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            ServerConfig.ApiDb.Execute(
                "UPDATE `spot_check_log` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`IN @ids;", new { ids });

            return Result.GenError<Result>(Error.Success);
        }
    }
}