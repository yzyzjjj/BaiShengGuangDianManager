using ApiManagement.Base.Server;
using ApiManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Linq;
using ApiManagement.Models.DeviceSpotCheckModel;

namespace ApiManagement.Controllers.DeviceSpotCheckController
{
    /// <summary>
    /// 点检计划
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class SpotCheckPlanController : ControllerBase
    {

        // GET: api/SpotCheckPlan
        [HttpGet]
        public DataResult GetSpotCheckPlan([FromQuery] int qId)
        {
            var result = new DataResult();
            var sql = $"SELECT * FROM `spot_check_plan` WHERE {(qId == 0 ? "" : "Id = @qId AND ")}`MarkedDelete` = 0;";
            var data = ServerConfig.ApiDb.Query<SpotCheckPlan>(sql, new { qId });
            if (qId != 0 && !data.Any())
            {
                return Result.GenError<DataResult>(Error.SpotCheckPlanNotExist);
            }
            result.datas.AddRange(data);
            return result;
        }

        // PUT: api/SpotCheckPlan/5
        [HttpPut("{id}")]
        public Result PutSpotCheckPlan([FromRoute] int id, [FromBody] SpotCheckPlan spotCheckPlan)
        {
            if (spotCheckPlan.Plan.IsNullOrEmpty())
            {
                return Result.GenError<Result>(Error.SpotCheckPlanNotEmpty);
            }
            var data =
                ServerConfig.ApiDb.Query<SpotCheckPlan>("SELECT * FROM `spot_check_plan` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.SpotCheckPlanNotExist);
            }

            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `spot_check_plan` WHERE Id != @id AND Plan = @Plan AND MarkedDelete = 0;", new { id, spotCheckPlan.Plan }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.SpotCheckPlanIsExist);
            }

            spotCheckPlan.Id = id;
            spotCheckPlan.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "UPDATE spot_check_plan SET `MarkedDateTime` = @MarkedDateTime, `Plan` = @Plan WHERE `Id` = @Id;", spotCheckPlan);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SpotCheckPlan
        /// <summary>
        /// 添加新计划号
        /// </summary>
        /// <param name="spotCheckPlan"></param>
        /// <returns></returns>
        [HttpPost]
        public Result PostSpotCheckPlan([FromBody] SpotCheckPlan spotCheckPlan)
        {
            if (spotCheckPlan.Plan.IsNullOrEmpty())
            {
                return Result.GenError<Result>(Error.SpotCheckPlanNotEmpty);
            }
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `spot_check_plan` WHERE Plan = @Plan AND MarkedDelete = 0;", new { spotCheckPlan.Plan }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.SpotCheckPlanIsExist);
            }

            if (spotCheckPlan.SpotCheckItems != null && spotCheckPlan.SpotCheckItems.Count() != spotCheckPlan.SpotCheckItems.GroupBy(x => x.Item).Count())
            {
                return Result.GenError<Result>(Error.SpotCheckItemDuplicate);
            }

            if (spotCheckPlan.SpotCheckItems != null && spotCheckPlan.SpotCheckItems.Any(x => x.Item.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SpotCheckItemNotEmpty);
            }

            spotCheckPlan.CreateUserId = Request.GetIdentityInformation();
            spotCheckPlan.MarkedDateTime = DateTime.Now;
            var id = ServerConfig.ApiDb.Query<int>(
              "INSERT INTO spot_check_plan (`CreateUserId`, `MarkedDateTime`, `Plan`) VALUES (@CreateUserId, @MarkedDateTime, @Plan);SELECT LAST_INSERT_ID();",
                spotCheckPlan).FirstOrDefault();

            if (spotCheckPlan.SpotCheckItems != null)
            {
                foreach (var spotCheckItem in spotCheckPlan.SpotCheckItems)
                {
                    spotCheckItem.PlanId = id;
                    spotCheckItem.CreateUserId = Request.GetIdentityInformation();
                    spotCheckItem.MarkedDateTime = DateTime.Now;
                }
                ServerConfig.ApiDb.Execute(
                    "INSERT INTO spot_check_item (`CreateUserId`, `MarkedDateTime`, `Item`, `PlanId`, `Enable`, `Remind`, `Min`, `Max`, `Unit`, `Reference`, `Remarks`, `Interval`, `Day`, `Month`, `NormalHour`, `Week`, `WeekHour`) " +
                    "VALUES (@CreateUserId, @MarkedDateTime, @Item, @PlanId, @Enable, @Remind, @Min, @Max, @Unit, @Reference, @Remarks, @Interval, @Day, @Month, @NormalHour, @Week, @WeekHour);",
                    spotCheckPlan.SpotCheckItems);
            }

            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/SpotCheckPlan/5
        [HttpDelete("{id}")]
        public Result DeleteSpotCheckPlan([FromRoute] int id)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `spot_check_plan` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.SpotCheckPlanNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `spot_check_plan` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });

            ServerConfig.ApiDb.Execute(
                "UPDATE `spot_check_item` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `PlanId`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });

            ServerConfig.ApiDb.Execute(
                "UPDATE `spot_check_device_bind` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `PlanId`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });

            ServerConfig.ApiDb.Execute(
                "UPDATE `spot_check_device` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `PlanId`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}