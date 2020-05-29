using ApiManagement.Base.Server;
using ApiManagement.Models.DeviceManagementModel;
using ApiManagement.Models.ManufactureModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Linq;

namespace ApiManagement.Controllers.ManufactureController
{
    /// <summary>
    /// 生产统计
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class ManufactureStatisticController : ControllerBase
    {
        /// <summary>
        /// 获取绩效排行榜
        /// </summary>
        /// <param name="sTime">开始时间</param>
        /// <param name="eTime">结束时间</param>
        /// <param name="gId">分组id</param>
        /// <returns></returns>
        // GET: api/ManufactureStatistic?sTime=time
        [HttpGet("Score")]
        public DataResult Score([FromQuery] DateTime sTime, DateTime eTime, int gId)
        {
            if (sTime == default(DateTime) || eTime == default(DateTime))
            {
                return Result.GenError<DataResult>(Error.ParamError);
            }
            var result = new DataResult();
            if (gId != 0)
            {
                var manufactureGroup =
                    ServerConfig.ApiDb.Query<ManufactureGroup>("SELECT * FROM `manufacture_group` WHERE Id = @gId AND MarkedDelete = 0;",
                        new { gId }).FirstOrDefault();
                if (manufactureGroup == null)
                {
                    return result;
                }
            }

            var sql = "";
            if (gId != 0)
            {
                sql = "SELECT a.Id, a.GroupId, IFNULL(a.ProcessorName, b.Person) Processor, IFNULL(b.Score, 0) Score " +
                        "FROM (SELECT a.*, b.ProcessorName FROM `manufacture_processor` a JOIN `processor` b ON a.ProcessorId = b.Id WHERE a.MarkedDelete = 0 AND a.GroupId = @gId) a " +
                        "LEFT JOIN ( SELECT Person, SUM(ActualScore) Score FROM manufacture_plan_task WHERE MarkedDelete = 0 AND State = @state AND ActualEndTime >= @sTime AND ActualEndTime <= @eTime GROUP BY Person) b ON a.Id = b.Person ORDER BY b.Score DESC, b.Person;";
            }
            else
            {
                sql = "SELECT a.Id, IFNULL(a.ProcessorName, b.Person) Processor, SUM(IFNULL(b.Score, 0)) Score FROM (SELECT a.*, b.ProcessorName " +
                      "FROM `manufacture_processor` a JOIN `processor` b ON a.ProcessorId = b.Id WHERE a.MarkedDelete = 0) a " +
                      "LEFT JOIN (SELECT Person, SUM(ActualScore) Score FROM manufacture_plan_task WHERE MarkedDelete = 0 AND State = @state AND ActualEndTime >= @sTime AND ActualEndTime <= @eTime GROUP BY Person) b ON a.Id = b.Person GROUP BY a.ProcessorId ORDER BY b.Score DESC, a.Id;";
            }
            var data = ServerConfig.ApiDb.Query<dynamic>(sql, new { state = ManufacturePlanTaskState.Done, gId, sTime, eTime });
            result.datas.AddRange(data);
            return result;
        }

        /// <summary>
        /// 计划实际用时统计表
        /// </summary>
        /// <param name="planId">计划id,多个逗号隔开</param>
        /// <param name="sTime">开始时间</param>
        /// <param name="eTime">结束时间</param>
        /// <param name="gId">分组id</param>
        /// <returns></returns>
        // GET: api/ManufactureStatistic?qId=0&item=false
        [HttpGet("PlanActualConsume")]
        public DataResult PlanActualConsume([FromQuery] string planId, DateTime sTime, DateTime eTime, int gId)
        {
            if (planId.IsNullOrEmpty() || sTime == default(DateTime) || eTime == default(DateTime))
            {
                return Result.GenError<DataResult>(Error.ParamError);
            }
            var planIdList = planId.Split(",").Select(int.Parse);
            if (!planIdList.Any())
            {
                return Result.GenError<DataResult>(Error.ManufactureLogSelectPlan);
            }

            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `manufacture_plan` WHERE `Id` IN @qId AND MarkedDelete = 0;",
                    new { qId = planIdList }).FirstOrDefault();
            if (cnt != planIdList.Count())
            {
                return Result.GenError<DataResult>(Error.ManufacturePlanNotExist);
            }

            var result = new DataResult();
            string sql;
            if (gId == 0)
            {
                sql =
                   $"SELECT a.Id, a.Plan, SUM(b.ActualHour * 60 + b.ActualMin) Consume FROM `manufacture_plan` a " +
                   $"JOIN `manufacture_plan_task` b ON a.Id = b.PlanId " +
                   $"WHERE a.`Id` IN @planId AND b.State = @state AND b.FirstStartTime >= @sTime AND b.FirstStartTime <= @eTime AND a.MarkedDelete = 0 AND b.MarkedDelete = 0 GROUP BY a.Id ORDER BY a.Id;";
            }
            else
            {
                sql =
                    $"SELECT a.Id, a.Plan, SUM(b.ActualHour * 60 + b.ActualMin) Consume FROM `manufacture_plan` a " +
                    $"JOIN (SELECT a.* FROM `manufacture_plan_task` a JOIN `manufacture_processor` b ON a.Person = b.Id WHERE b.GroupId = @gId) b ON a.Id = b.PlanId " +
                    $"WHERE a.`Id` IN @planId AND b.State = @state AND b.FirstStartTime >= @sTime AND b.FirstStartTime <= @eTime AND a.MarkedDelete = 0 AND b.MarkedDelete = 0 GROUP BY a.Id ORDER BY a.Id;";
            }
            result.datas.AddRange(ServerConfig.ApiDb.Query<dynamic>(sql, new { gId, planId = planIdList, state = ManufacturePlanTaskState.Done, sTime, eTime}));
            return result;
        }

        /// <summary>
        /// 任务平均用时统计表
        /// </summary>
        /// <param name="planId">计划id,多个逗号隔开</param>
        /// <param name="sTime">开始时间</param>
        /// <param name="eTime">结束时间</param>
        /// <param name="gId">分组id</param>
        /// <returns></returns>
        // GET: api/ManufactureStatistic?qId=0&item=false
        [HttpGet("TaskAverageConsume")]
        public DataResult TaskAverageConsume([FromQuery] string planId, DateTime sTime, DateTime eTime, int gId)
        {
            if (planId.IsNullOrEmpty() || sTime == default(DateTime) || eTime == default(DateTime))
            {
                return Result.GenError<DataResult>(Error.ParamError);
            }
            var planIdList = planId.Split(",").Select(int.Parse);
            if (!planIdList.Any())
            {
                return Result.GenError<DataResult>(Error.ManufactureLogSelectPlan);
            }

            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `manufacture_plan` WHERE `Id` IN @qId AND MarkedDelete = 0;",
                    new { qId = planIdList }).FirstOrDefault();
            if (cnt != planIdList.Count())
            {
                return Result.GenError<DataResult>(Error.ManufacturePlanNotExist);
            }

            var result = new DataResult();
            string sql;
            if (gId == 0)
            {
                sql =
                   $"SELECT a.Id, b.Item, SUM(b.ActualHour * 60 + b.ActualMin) Consume, COUNT(1) Cnt, SUM(b.ActualHour * 60 + b.ActualMin)/COUNT(1) Avg FROM `manufacture_plan` a " +
                   $"JOIN `manufacture_plan_task` b ON a.Id = b.PlanId " +
                   $"WHERE a.`Id` IN @planId AND b.State = @state AND b.FirstStartTime >= @sTime AND b.FirstStartTime <= @eTime AND a.MarkedDelete = 0 AND b.MarkedDelete = 0 GROUP BY b.Item ORDER BY a.Id;";
            }
            else
            {
                sql =
                    $"SELECT a.Id, b.Item, SUM(b.ActualHour * 60 + b.ActualMin) Consume, COUNT(1) Cnt, SUM(b.ActualHour * 60 + b.ActualMin)/COUNT(1) Avg FROM `manufacture_plan` a " +
                    $"JOIN (SELECT a.* FROM `manufacture_plan_task` a JOIN `manufacture_processor` b ON a.Person = b.Id WHERE b.GroupId = @gId) b ON a.Id = b.PlanId " +
                    $"WHERE a.`Id` IN @planId AND b.State = @state AND b.FirstStartTime >= @sTime AND b.FirstStartTime <= @eTime AND a.MarkedDelete = 0 AND b.MarkedDelete = 0 GROUP BY b.Item ORDER BY a.Id;";
            }
            result.datas.AddRange(ServerConfig.ApiDb.Query<dynamic>(sql, new { planId = planIdList, state = ManufacturePlanTaskState.Done, sTime, eTime, gId }));
            return result;
        }

        /// <summary>
        /// 任务完成情况汇总表
        /// </summary>
        /// <param name="planId">计划id,多个逗号隔开</param>
        /// <param name="sTime">开始时间</param>
        /// <param name="eTime">结束时间</param>
        /// <param name="account"></param>
        /// <param name="gId">分组id</param>
        /// <returns></returns>
        // GET: api/ManufactureStatistic?qId=0&item=false
        [HttpGet("TaskFinishSummary")]
        public DataResult TaskFinishSummary([FromQuery] int gId, string account, DateTime sTime, DateTime eTime)
        {
            if (account.IsNullOrEmpty() || sTime == default(DateTime) || eTime == default(DateTime))
            {
                return Result.GenError<DataResult>(Error.ParamError);
            }
            var accountList = account.Split(",");
            if (accountList.Length == 0)
            {
                return Result.GenError<DataResult>(Error.ManufactureLogSelectPlan);
            }

            var processors =
                ServerConfig.ApiDb.Query<Processor>("SELECT a.Id, b.Account FROM `manufacture_processor` a JOIN `processor` b ON a.ProcessorId = b.Id " +
                                                    $"WHERE b.Account IN @account AND a.`MarkedDelete` = 0 {(gId == 0 ? "" : " AND GroupId = @gId")};", new { account = accountList, gId });
            if (!processors.Any())
            {
                return Result.GenError<DataResult>(Error.ManufactureProcessorNotExist);
            }

            var result = new DataResult();
            var sql =
                   $"SELECT a.Id, a.Item, a.ActualEndTime, a.ActualHour, a.ActualMin FROM `manufacture_plan_task` a " +
                   $"JOIN `manufacture_plan` b ON  a.PlanId = b.Id " +
                   $"WHERE a.State = @state AND a.Person IN @pId AND a.ActualEndTime >= @sTime AND a.ActualEndTime <= @eTime AND a.MarkedDelete = 0 AND b.MarkedDelete = 0 ORDER BY a.ActualEndTime;";

            result.datas.AddRange(ServerConfig.ApiDb.Query<dynamic>(sql, new { state = ManufacturePlanTaskState.Done, pId = processors.Select(x => x.Id), sTime, eTime }));
            return result;
        }
    }
}