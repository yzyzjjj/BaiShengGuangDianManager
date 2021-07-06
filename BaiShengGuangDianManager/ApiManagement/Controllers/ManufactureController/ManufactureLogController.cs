using ApiManagement.Base.Server;
using ApiManagement.Models.ManufactureModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Models.Result;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.ManufactureController
{
    /// <summary>
    /// 生产计划
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class ManufactureLogController : ControllerBase
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="planId">计划id</param>
        /// <param name="itemId">任务id</param>
        /// <returns></returns>
        // GET: api/ManufactureLog?qId=0&item=false
        [HttpGet]
        public DataResult GetManufactureLog([FromQuery] int planId, int itemId)
        {
            if (itemId != 0 && planId == 0)
            {
                return Result.GenError<DataResult>(Error.ManufactureLogSelectPlan);
            }

            var result = new DataResult();
            var data = new List<ManufactureLog>();
            string sql;
            if (planId == 0 && itemId == 0)
            {
                sql =
                   $"SELECT a.*, IFNULL(b.Plan, '') Plan, IFNULL(c.`Task`, '') `Task`, IFNULL(d.Item, '') Item,   IF(ISNULL(e.Account), a.Account, e.Name) AccountName FROM `manufacture_log` a " +
                   $"LEFT JOIN `manufacture_plan` b ON a.PlanId = b.Id " +
                   $"LEFT JOIN `manufacture_task` c ON a.TaskId = c.Id " +
                   $"LEFT JOIN `manufacture_plan_item` d ON a.ItemId = d.Id " +
                   $"LEFT JOIN `accounts` e ON a.Account = e.Account " +
                   $"WHERE a.IsAssign = @assign;";
                data.AddRange(ServerConfig.ApiDb.Query<ManufactureLog>(sql, new { assign = false }));

                sql =
                    $"SELECT a.*, IFNULL(b.Plan, '') Plan, IFNULL(c.`Task`, '') `Task`, IFNULL(d.Item, '') Item,   IF(ISNULL(e.Account), a.Account, e.Name) AccountName FROM `manufacture_log` a " +
                    $"LEFT JOIN `manufacture_plan` b ON a.PlanId = b.Id " +
                    $"LEFT JOIN `manufacture_task` c ON a.TaskId = c.Id " +
                    $"LEFT JOIN `manufacture_plan_task` d ON a.ItemId = d.Id " +
                    $"LEFT JOIN `accounts` e ON a.Account = e.Account " +
                    $"WHERE a.IsAssign = @assign AND b.State != @state;";
                data.AddRange(ServerConfig.ApiDb.Query<ManufactureLog>(sql, new { assign = true, state = ManufacturePlanState.Wait }));
            }
            else if (planId != 0 && itemId == 0)
            {
                var plan =
                    ServerConfig.ApiDb.Query<ManufacturePlan>("SELECT * FROM `manufacture_plan` WHERE Id = @planId;",
                        new { planId }).FirstOrDefault();
                if (plan == null)
                {
                    return Result.GenError<DataResult>(Error.ManufacturePlanNotExist);
                }

                sql =
                    $"SELECT a.*, IFNULL(b.Plan, '') Plan, IFNULL(c.`Task`, '') `Task`, IFNULL(d.Item, '') Item,   IF(ISNULL(e.Account), a.Account, e.Name) AccountName FROM `manufacture_log` a " +
                    $"LEFT JOIN `manufacture_plan` b ON a.PlanId = b.Id " +
                    $"LEFT JOIN `manufacture_task` c ON a.TaskId = c.Id " +
                    $"LEFT JOIN `manufacture_plan_item` d ON a.ItemId = d.Id " +
                    $"LEFT JOIN `accounts` e ON a.Account = e.Account " +
                    $"WHERE a.PlanId = @planId AND a.IsAssign = @assign AND a.`Type` <= @type;";
                data.AddRange(ServerConfig.ApiDb.Query<ManufactureLog>(sql, new { planId, assign = false, type = ManufactureLogType.PlanUpdateItem }));
                if (plan.State != ManufacturePlanState.Wait)
                {
                    sql =
                        $"SELECT a.*, IFNULL(b.Plan, '') Plan, IFNULL(c.`Task`, '') `Task`, IFNULL(d.Item, '') Item,   IF(ISNULL(e.Account), a.Account, e.Name) AccountName FROM `manufacture_log` a " +
                        $"LEFT JOIN `manufacture_plan` b ON a.PlanId = b.Id " +
                        $"LEFT JOIN `manufacture_task` c ON a.TaskId = c.Id " +
                        $"LEFT JOIN `manufacture_plan_task` d ON a.ItemId = d.Id " +
                        $"LEFT JOIN `accounts` e ON a.Account = e.Account " +
                        $"WHERE a.PlanId = @planId AND a.IsAssign = @assign AND b.State != @state AND a.`Type` <= @type;";
                    data.AddRange(ServerConfig.ApiDb.Query<ManufactureLog>(sql, new { planId, assign = true, state = ManufacturePlanState.Wait, type = ManufactureLogType.PlanUpdateItem }));
                }
            }
            else if (planId != 0 && itemId != 0)
            {
                var plan =
                    ServerConfig.ApiDb.Query<ManufacturePlan>("SELECT * FROM `manufacture_plan` WHERE Id = @planId;",
                        new { planId }).FirstOrDefault();
                if (plan == null)
                {
                    return Result.GenError<DataResult>(Error.ManufacturePlanNotExist);
                }

                var planTaskId = 0;
                if (plan.State != ManufacturePlanState.Wait)
                {
                    planTaskId = itemId;
                    itemId =
                        ServerConfig.ApiDb.Query<int>("SELECT OldId FROM `manufacture_plan_task` WHERE Id = @itemId;", new { itemId }).FirstOrDefault();
                }

                if (itemId != 0)
                {
                    sql =
                        $"SELECT a.*, IFNULL(b.Plan, '') Plan, IFNULL(c.`Task`, '') `Task`, IFNULL(d.Item, '') Item,   IF(ISNULL(e.Account), a.Account, e.Name) AccountName FROM `manufacture_log` a " +
                        $"LEFT JOIN `manufacture_plan` b ON a.PlanId = b.Id " +
                        $"LEFT JOIN `manufacture_task` c ON a.TaskId = c.Id " +
                        $"LEFT JOIN `manufacture_plan_item` d ON a.ItemId = d.Id " +
                        $"LEFT JOIN `accounts` e ON a.Account = e.Account " +
                        $"WHERE a.PlanId = @planId AND a.ItemId = @itemId AND a.IsAssign = @assign AND a.`Type` >= @type;";
                    data.AddRange(ServerConfig.ApiDb.Query<ManufactureLog>(sql, new { planId, itemId, assign = false, type = ManufactureLogType.TaskCreate }));
                }
                if (plan.State != ManufacturePlanState.Wait)
                {
                    if (planTaskId != 0)
                    {
                        sql =
                            $"SELECT a.*, IFNULL(b.Plan, '') Plan, IFNULL(c.`Task`, '') `Task`, IFNULL(d.Item, '') Item,   IF(ISNULL(e.Account), a.Account, e.Name) AccountName FROM `manufacture_log` a " +
                            $"LEFT JOIN `manufacture_plan` b ON a.PlanId = b.Id " +
                            $"LEFT JOIN `manufacture_task` c ON a.TaskId = c.Id " +
                            $"LEFT JOIN `manufacture_plan_task` d ON a.ItemId = d.Id " +
                            $"LEFT JOIN `accounts` e ON a.Account = e.Account " +
                            $"WHERE a.PlanId = @planId AND a.ItemId = @planTaskId AND a.IsAssign = @assign AND b.State != @state AND a.`Type` >= @type;";
                        data.AddRange(ServerConfig.ApiDb.Query<ManufactureLog>(sql, new { planId, planTaskId, assign = true, state = ManufacturePlanState.Wait, type = ManufactureLogType.TaskCreate }));
                    }
                }
            }
            result.datas.AddRange(data.OrderBy(x => x.Time).ThenBy(y => y.Id));
            return result;
        }

    }
}