﻿using ApiManagement.Base.Server;
using ApiManagement.Models.ManufactureModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.ManufactureController
{
    /// <summary>
    /// 任务管理列表
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class ManufactureManagementController : ControllerBase
    {
        /// <summary>
        /// 获取计划状态列表
        /// </summary>
        /// <returns></returns>
        // GET: api/ManufactureManagement?qId=0&item=false
        [HttpGet("State")]
        public DataResult GetManufacturePlanTaskState()
        {
            var result = new DataResult();
            result.datas.AddRange(EnumHelper.EnumToList<ManufacturePlanTaskState>(true).Select(x => new
            {
                Id = x.EnumValue,
                State = x.Description,
            }));
            return result;
        }
        /// <summary>
        /// 获取已下发计划
        /// </summary>
        /// <param name="qId"></param>
        /// <returns></returns>
        // GET: api/ManufacturePlan?qId=0&item=false
        [HttpGet("Plan")]
        public DataResult GetManufacturePlan([FromQuery] int qId)
        {
            var result = new DataResult();
            var sql =
                 $"SELECT Id, `Plan`, `TaskId` FROM `manufacture_plan` WHERE {(qId == 0 ? "" : "Id = @qId AND ")}State != @state AND `MarkedDelete` = 0;";
            var data = ServerConfig.ApiDb.Query<dynamic>(sql, new { qId, state = ManufacturePlanState.Wait });
            result.datas.AddRange(data);
            return result;
        }

        /// <summary>
        /// 获取任务管理列表
        /// </summary>
        /// <param name="spTime">计划</param>
        /// <param name="epTime">计划结束</param>
        /// <param name="sTime">实际开始</param>
        /// <param name="eTime">实际结束</param>
        /// <param name="state">状态</param>
        /// <param name="gId">分组id</param>
        /// <param name="pId">加工人id</param>
        /// <param name="planId">计划id</param>
        /// <returns></returns>
        // GET: api/ManufacturePlan?qId=0
        [HttpGet("Item")]
        public DataResult GetManufacturePlanTask([FromQuery] DateTime spTime, DateTime epTime, DateTime sTime, DateTime eTime, int planId, int gId, string pId = "0", string state = "-1")
        {
            if (planId != 0)
            {
                var plan =
                    ServerConfig.ApiDb.Query<ManufacturePlan>("SELECT * FROM `manufacture_plan` WHERE `Id` = @planId AND MarkedDelete = 0;",
                        new { planId }).FirstOrDefault();
                if (plan == null)
                {
                    return Result.GenError<DataResult>(Error.ManufacturePlanNotExist);
                }
            }

            var result = new DataResult();
            var sql = $"SELECT a.*, IFNULL(b.Plan, '') Plan, IFNULL(c.Name, '') Processor, IFNULL(d.Module, '') Module, IFNULL(e.`Check`, '') `Check` FROM manufacture_plan_task a " +
                      "LEFT JOIN `manufacture_plan` b ON a.PlanId = b.Id " +
                      "LEFT JOIN (SELECT a.*, b.Name FROM `manufacture_processor` a JOIN `accounts` b ON a.ProcessorId = b.Id WHERE a.MarkedDelete = 0) c ON a.Person = c.Id " +
                      "LEFT JOIN `manufacture_task_module` d ON a.ModuleId = d.Id " +
                      "LEFT JOIN `manufacture_check` e ON a.CheckId = e.Id " +
                      "WHERE a.MarkedDelete = 0 " +
                      $"{(planId == 0 ? "" : " AND a.PlanId = @planId ")}" +
                      $"{((spTime == default(DateTime) || epTime == default(DateTime)) ? "" : " AND b.PlannedStartTime >= @spTime AND b.PlannedStartTime <= @epTime ")}" +
                      $"{((sTime == default(DateTime) || eTime == default(DateTime)) ? "" : " AND FirstStartTime >= @sTime AND FirstStartTime <= @eTime ")}" +
                      $"{(state.IsNullOrEmpty() || state == "-1" ? "" : " AND FIND_IN_SET(a.State, @state) ")}" +
                      $"{(gId == 0 ? "" : " AND c.GroupId = @gId ")}" +
                      $"{(pId.IsNullOrEmpty() || pId == "0" ? "" : " AND FIND_IN_SET(a.Person, @pId) ")}" +
                      $" ORDER BY a.`TotalOrder`;";
            var data = ServerConfig.ApiDb.Query<ManufacturePlanTask>(sql, new { spTime, epTime, sTime, eTime, planId, gId, pId, state });
            result.datas.AddRange(data);
            return result;
        }

        /// <summary>
        /// 获取任务详情
        /// </summary>
        /// <returns></returns>
        /// <param name="tId">任务id</param>
        // GET: api/ManufactureTaskWorkSpace
        [HttpGet("Detail")]
        public DataResult GetManufacturePlanTaskDetail([FromQuery] int tId)
        {
            var result = new DataResult();
            var sql =
                "SELECT a.*, IFNULL(b.Plan, '') Plan, c.`GroupId`, IFNULL(c.`Group`, '') `Group`, IFNULL(c.Name, '') Processor, IFNULL(d.Module, '') Module, IFNULL(e.`Check`, '') `Check`, IFNULL(f.Name, a.Assignor) Assignor FROM manufacture_plan_task a " +
                "LEFT JOIN `manufacture_plan` b ON a.PlanId = b.Id " +
                "LEFT JOIN (SELECT a.*, b.Name, c.`Group` FROM `manufacture_processor` a JOIN `accounts` b ON a.ProcessorId = b.Id JOIN `manufacture_group` c ON a.GroupId = c.Id WHERE a.MarkedDelete = 0) c ON a.Person = c.Id " +
                "LEFT JOIN `manufacture_task_module` d ON a.ModuleId = d.Id " +
                "LEFT JOIN `manufacture_check` e ON a.CheckId = e.Id " +
                "LEFT JOIN `accounts` f ON a.Assignor = f.Account " +
                "WHERE a.Id = @tId AND a.MarkedDelete = 0 ORDER BY a.`TotalOrder` LIMIT 2;";
            var task = ServerConfig.ApiDb.Query<ManufacturePlanTask>(sql, new { tId }).FirstOrDefault();

            if (task == null)
            {
                return Result.GenError<DataResult>(Error.ManufactureTaskItemNotExist);
            }

            if (task.IsCheck)
            {
                sql =
                    "SELECT a.*, IFNULL(b.Name, a.Person) Processor FROM `manufacture_plan_task` a " +
                    "LEFT JOIN (SELECT a.*, b.Name FROM `manufacture_processor` a JOIN `accounts` b ON a.ProcessorId = b.Id ) b ON a.Person = b.Id " +
                    "WHERE PlanId = @PlanId AND `Order` = @Relation;";
                var preTask = ServerConfig.ApiDb.Query<ManufacturePlanTask>(sql, new { task.PlanId, task.Relation }).FirstOrDefault();
                if (preTask != null)
                {
                    task.CheckTask = preTask.Item;
                    task.CheckProcessor = preTask.Processor;
                }

                task.Items = task.IsCheckItem ?
                    ServerConfig.ApiDb.Query<ManufacturePlanCheckItem>("SELECT * FROM `manufacture_plan_check_item` WHERE PlanId = @PlanId AND ItemId = @Id AND MarkedDelete = 0;", new { task.PlanId, task.Id })
                    : ServerConfig.ApiDb.Query<ManufacturePlanCheckItem>("SELECT * FROM `manufacture_check_item` WHERE CheckId = @CheckId AND MarkedDelete = 0;", new { task.CheckId });
            }
            result.datas.Add(task);
            return result;
        }

        /// <summary>
        /// 更新任务
        /// </summary>
        /// <returns></returns>
        // PUT: api/ManufacturePlan
        [HttpPut]
        public Result PutManufacturePlanTask([FromBody] ManufacturePlanTask task)
        {
            if (task.Id == 0)
            {
                return Result.GenError<Result>(Error.ManufactureTaskNotExist);
            }

            var oldTask =
                ServerConfig.ApiDb.Query<ManufacturePlanTask>(
                    "SELECT a.*, IFNULL(b.Name, '') Processor FROM `manufacture_plan_task` a " +
                    "LEFT JOIN (SELECT a.*, b.Name FROM `manufacture_processor` a JOIN `accounts` b ON a.ProcessorId = b.Id WHERE a.MarkedDelete = 0) b ON a.Person = b.Id WHERE a.Id = @Id AND a.MarkedDelete = 0;;",
                    new { task.PlanId, task.Id }).FirstOrDefault();
            if (oldTask == null)
            {
                return Result.GenError<Result>(Error.ManufactureTaskItemNotExist);
            }

            var changes = new List<ManufactureLog>();
            task.TotalOrder = oldTask.TotalOrder;
            task.PlanId = oldTask.PlanId;
            task.TaskId = oldTask.TaskId;
            task.Order = oldTask.Order;
            task.OldId = oldTask.OldId;
            task.Person = task.Person == 0 ? oldTask.Person : task.Person;
            task.ModuleId = oldTask.ModuleId;
            task.IsCheck = oldTask.IsCheck;
            task.CheckId = oldTask.CheckId;
            task.Relation = oldTask.Relation;
            task.FirstStartTime = oldTask.FirstStartTime;
            task.PauseTime = oldTask.PauseTime;
            task.ActualStartTime = oldTask.ActualStartTime;
            task.ActualEndTime = oldTask.ActualEndTime;
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            var changeTask = false;
            var keys = new List<string> { "Person", "EstimatedHour", "EstimatedMin", "Score", "Desc", "ActualHour", "ActualMin", "ActualScore", "CheckResult" };
            if (oldTask.HaveChange(task, out var taskChange, keys))
            {
                changeTask = true;
                taskChange.Time = markedDateTime;
                taskChange.Account = createUserId;
                taskChange.PlanId = task.PlanId;
                taskChange.IsAssign = true;
                taskChange.TaskId = task.TaskId;
                taskChange.ItemId = task.Id;
                changes.Add(taskChange);
            }

            if (oldTask.IsCheck)
            {
                ManufacturePlanTask preTask;
                if (oldTask.CheckResult != ManufacturePlanCheckState.Redo &&
                    task.CheckResult == ManufacturePlanCheckState.Redo)
                {
                    var sql =
                        "SELECT * FROM `manufacture_plan_task` WHERE PlanId = @PlanId AND `Order` = @Relation;";
                    preTask = ServerConfig.ApiDb.Query<ManufacturePlanTask>(sql, new { oldTask.PlanId, oldTask.Relation })
                        .FirstOrDefault();
                    if (preTask != null)
                    {
                        if (preTask.State == ManufacturePlanTaskState.Done)
                        {
                            var oldPreTask = (ManufacturePlanTask)preTask.Clone();
                            preTask.State = preTask.IsCheck
                                ? ManufacturePlanTaskState.WaitCheck
                                : ManufacturePlanTaskState.WaitRedo;
                            preTask.RedoCount++;
                            preTask.IsRedo = true;
                            keys = new List<string> { "State" };
                            if (oldPreTask.HaveChange(preTask, out var change, keys))
                            {
                                change.Time = markedDateTime;
                                change.Account = createUserId;
                                change.PlanId = preTask.PlanId;
                                change.IsAssign = true;
                                change.TaskId = preTask.TaskId;
                                change.ItemId = preTask.Id;
                                changes.Add(change);
                            }

                            ServerConfig.ApiDb.Execute(
                                "UPDATE manufacture_plan_task SET `State` = @State, `RedoCount` = @RedoCount, `IsRedo` = @IsRedo WHERE `Id` = @Id;",
                                preTask);
                        }
                        else
                        {
                            return Result.GenError<DataResult>(Error.ManufacturePlaneTaskNotDone);
                        }
                    }
                }

                if (oldTask.CheckResult == ManufacturePlanCheckState.Redo &&
                    task.CheckResult != ManufacturePlanCheckState.Redo)
                {
                    var sql =
                        "SELECT * FROM `manufacture_plan_task` WHERE PlanId = @PlanId AND `Order` = @Relation;";
                    preTask = ServerConfig.ApiDb.Query<ManufacturePlanTask>(sql, new { oldTask.PlanId, oldTask.Relation })
                        .FirstOrDefault();
                    if (preTask != null)
                    {
                        if (preTask.State == ManufacturePlanTaskState.WaitRedo ||
                            preTask.State == ManufacturePlanTaskState.WaitCheck)
                        {
                            var oldPreTask = (ManufacturePlanTask)preTask.Clone();
                            preTask.State = ManufacturePlanTaskState.Done;
                            preTask.RedoCount--;
                            preTask.IsRedo = preTask.RedoCount > 0;

                            keys = new List<string> { "State" };
                            if (oldPreTask.HaveChange(preTask, out var change, keys))
                            {
                                change.Time = markedDateTime;
                                change.Account = createUserId;
                                change.PlanId = preTask.PlanId;
                                change.IsAssign = true;
                                change.TaskId = preTask.TaskId;
                                change.ItemId = preTask.Id;
                                changes.Add(change);
                            }

                            ServerConfig.ApiDb.Execute(
                                "UPDATE manufacture_plan_task SET `State` = @State, `RedoCount` = @RedoCount, `IsRedo` = @IsRedo WHERE `Id` = @Id;",
                                preTask);
                        }
                        else
                        {
                            return Result.GenError<DataResult>(Error.ManufacturePlaneTaskNotWaitRedo);
                        }
                    }
                }

                var checkItemChange = new ManufactureLog
                {
                    Time = markedDateTime,
                    Account = createUserId,
                    PlanId = task.PlanId,
                    IsAssign = true,
                    TaskId = task.TaskId,
                    ItemId = task.Id,
                    Type = ManufactureLogType.UpdateCheckItem
                };

                var data =
                    ServerConfig.ApiDb.Query<ManufacturePlanCheckItem>(
                        "SELECT * FROM `manufacture_plan_check_item` WHERE PlanId = @PlanId AND ItemId = @Id AND MarkedDelete = 0;",
                        new { task.PlanId, task.Id });

                if (task.Items != null && task.Items.Any())
                {
                    var update = false;
                    foreach (var item in task.Items)
                    {
                        var d = data.FirstOrDefault(x => x.Id == item.Id);
                        if (d == null)
                        {
                            continue;
                        }

                        item.Desc = item.Desc ?? d.Desc;
                        item.Images = item.Images ?? d.Images;
                        if (d.HaveChange(item, out var change))
                        {
                            update = true;
                            change.Time = markedDateTime;
                            change.Account = createUserId;
                            change.PlanId = task.PlanId;
                            change.IsAssign = true;
                            change.TaskId = task.TaskId;
                            change.ItemId = task.Id;
                            change.Type = ManufactureLogType.UpdateCheckItem;
                            changes.Add(change);
                        }
                    }

                    if (update)
                    {
                        changes.Add(checkItemChange);
                        ServerConfig.ApiDb.Execute(
                            "UPDATE manufacture_plan_check_item SET `CheckTime` = @CheckTime, `Desc` = @Desc, `Result` = @Result, `Images` = @Images WHERE `Id` = @Id;",
                            task.Items);
                    }
                }
            }

            if (changeTask)
            {
                ServerConfig.ApiDb.Execute("UPDATE manufacture_plan_task SET `Person` = @Person, `EstimatedHour` = @EstimatedHour, `EstimatedMin` = @EstimatedMin, `Score` = @Score, `Desc` = @Desc, `ActualHour` = @ActualHour, `ActualMin` = @ActualMin, `ActualScore` = @ActualScore, `CheckResult` = @CheckResult WHERE `Id` = @Id;", task);
            }

            ManufactureLog.AddLog(changes);
            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 上移任务
        /// </summary>
        /// <returns></returns>
            // PUT: api/ManufacturePlan
        [HttpPost("Up")]
        public DataResult UpManufacturePlanTask([FromBody] ManufacturePlanTaskUp moveTask)
        {
            if (moveTask.FromOrder == 0 || moveTask.ToOrder == 0)
            {
                return Result.GenError<DataResult>(Error.ManufactureTaskItemNotExist);
            }

            var tasks =
                ServerConfig.ApiDb.Query<ManufacturePlanTask>("SELECT * FROM `manufacture_plan_task` WHERE MarkedDelete = 0 AND TotalOrder <= @FromOrder AND TotalOrder >= @ToOrder Order By TotalOrder;", moveTask);
            if (tasks.Count() < 2)
            {
                return Result.GenError<DataResult>(Error.ManufactureTaskItemNotExist);
            }

            if (tasks.Select(x => x.TotalOrder).All(y => y != moveTask.FromOrder && y != moveTask.ToOrder))
            {
                return Result.GenError<DataResult>(Error.ManufactureTaskItemNotExist);
            }
            var fromTask = (ManufacturePlanTask)tasks.First(x => x.TotalOrder == moveTask.FromOrder).Clone();
            var toTask = (ManufacturePlanTask)tasks.First(x => x.TotalOrder == moveTask.ToOrder).Clone();

            //无法上移非等待中/非待检验任务
            if ((fromTask.State != ManufacturePlanTaskState.Wait && fromTask.State != ManufacturePlanTaskState.WaitCheck && fromTask.State != ManufacturePlanTaskState.WaitRedo))
            {
                return Result.GenError<DataResult>(Error.ManufacturePlaneTaskNotWait);
            }

            var count = tasks.Count();
            var state = fromTask.IsCheck ? ManufacturePlanTaskState.Checking : ManufacturePlanTaskState.Doing;
            var res = fromTask.IsCheck ? Error.ManufacturePlaneTaskCheckAfterChecking : Error.ManufacturePlaneTaskAfterDoing;
            //不能上移到该操作工的进行中/检验中任务之前
            if (tasks.Take(count - 1).Any(x => x.Person == fromTask.Person && x.State == state))
            {
                return Result.GenError<DataResult>(res);
            }

            //不能上移到关联任务之前
            if (tasks.Take(count - 1).Any(x => x.PlanId == fromTask.PlanId && x.Order == fromTask.Relation))
            {
                return Result.GenError<DataResult>(Error.ManufacturePlaneTaskAfterDoing);
            }

            var changes = new List<ManufactureLog>();
            var newTasks = new Dictionary<int, ManufacturePlanTask>();
            newTasks.AddRange(tasks.Take(count - 1).OrderByDescending(x => x.TotalOrder).ToDictionary(x => x.Id));
            var changeOrder = new List<Tuple<int, int>>();

            foreach (var task in newTasks.Values)
            {
                if (task.PlanId == fromTask.PlanId)
                {
                    //if (task.Relation != 0)
                    //{
                    //    task.Relation++;
                    //}
                    changeOrder.Add(new Tuple<int, int>(task.Order, task.Order + 1));
                    task.Order++;
                }
                task.TotalOrder++;
            }
            newTasks = newTasks.Values.OrderBy(x => x.TotalOrder).ToDictionary(x => x.Id);
            newTasks.Add(fromTask.Id, fromTask);
            newTasks.Values.Last().TotalOrder = newTasks.Values.First().TotalOrder - 1;
            var oldOrder = fromTask.Order;
            if (fromTask.PlanId == toTask.PlanId)
            {
                newTasks.Values.Last().Order = toTask.Order;
            }

            if (newTasks.Values.Last().Order <= 0)
            {
                newTasks.Values.Last().Order = 1;
            }
            changeOrder.Add(new Tuple<int, int>(oldOrder, newTasks.Values.Last().Order));
            if (changeOrder.Any())
            {
                var changeOrderTasks =
                    ServerConfig.ApiDb.Query<ManufacturePlanTask>("SELECT * FROM `manufacture_plan_task` WHERE MarkedDelete = 0 AND `PlanId` = @PlanId AND `Relation` IN @Relation;", new
                    {
                        PlanId = fromTask.PlanId,
                        Relation = changeOrder.Select(x => x.Item2)
                    });
                var chOrderTasks = new List<Tuple<int, int>>();
                foreach (var chTuple in changeOrder)
                {
                    chOrderTasks.AddRange(changeOrderTasks.Where(x => x.Relation == chTuple.Item1).Select(y => new Tuple<int, int>(y.Id, chTuple.Item2)));
                }
                ServerConfig.ApiDb.Execute("UPDATE manufacture_plan_task SET `MarkedDateTime`= NOW(), `Relation` = @Item2 WHERE `Id` = @Item1;", chOrderTasks);
            }
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            var planChange = new ManufactureLog
            {
                Time = markedDateTime,
                Account = createUserId,
                PlanId = fromTask.PlanId,
                IsAssign = true,
                Type = ManufactureLogType.PlanUpdateItem,
                ParsingWay = 1,
            };
            foreach (var oldTask in tasks.Where(x => x.PlanId == fromTask.PlanId))
            {
                if (newTasks.ContainsKey(oldTask.Id))
                {
                    var newTask = newTasks[oldTask.Id];
                    if (oldTask.HaveChange(newTask, out var change))
                    {
                        planChange.ParamList.Add(new ManufactureLogItem
                        {
                            Type = ManufactureLogType.UpdatePlanTaskFormat,
                            Field = newTask.Order.ToString(),
                            Items = change.ParamList.Select(y =>
                            {
                                y.Type = ManufactureLogType.UpdatePlanTaskItemFormat;
                                return y;
                            })
                        });
                        changes.Add(change);
                    }
                }
            }

            changes.Add(planChange);
            ServerConfig.ApiDb.Execute("UPDATE manufacture_plan_task SET `MarkedDateTime`= NOW(), `TotalOrder` = @TotalOrder, `Order` = @Order, `Relation` = @Relation WHERE `Id` = @Id;", newTasks.Values);
            ManufactureLog.AddLog(changes);
            var result = new DataResult();
            result.datas.AddRange(newTasks.Values.Where(x => x.Id == fromTask.Id || x.Id == toTask.Id));
            return result;
        }

        /// <summary>
        /// 新增任务
        /// </summary>
        /// <returns></returns>
        // POST: api/ManufacturePlan
        [HttpPost("Add")]
        public Result AddManufactureTask([FromBody] ManufacturePlanTask task)
        {
            var plan =
                ServerConfig.ApiDb.Query<ManufacturePlan>("SELECT * FROM `manufacture_plan` WHERE `Id` = @PlanId AND MarkedDelete = 0;",
                    new { task.PlanId }).FirstOrDefault();
            if (plan == null)
            {
                return Result.GenError<Result>(Error.ManufacturePlanNotExist);
            }

            if (plan.State == ManufacturePlanState.Wait)
            {
                return Result.GenError<Result>(Error.ManufacturePlaneNotAssign);
            }

            if (task.Item.IsNullOrEmpty())
            {
                return Result.GenError<Result>(Error.ManufactureTaskItemNotEmpty);
            }
            //if (task.Order <= 0)
            //{
            //    return Result.GenError<Result>(Error.ManufactureTaskItemOrderError);
            //}
            var changes = new List<ManufactureLog>();
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            var totalOrder = task.TotalOrder;
            var manufacturePlanTasks =
                ServerConfig.ApiDb.Query<ManufacturePlanTask>("SELECT * FROM `manufacture_plan_task` WHERE PlanId = @PlanId AND MarkedDelete = 0 ORDER BY `Order`;", new { task.PlanId });

            var planChange = new ManufactureLog
            {
                Time = markedDateTime,
                Account = createUserId,
                PlanId = plan.Id,
                IsAssign = true,
                Type = ManufactureLogType.PlanUpdateItem,
                ParsingWay = 1,
            };
            var now = DateTime.Now;
            task.CreateUserId = createUserId;
            task.Desc = task.Desc ?? "";
            task.State = ManufacturePlanTaskState.Wait;
            if (!manufacturePlanTasks.Any())
            {
                task.Order = 1;
                task.MarkedDateTime = now;
                if (task.Relation != 0)
                {
                    return Result.GenError<Result>(Error.ManufactureTaskItemRelationError);
                }
                if (task.IsCheck && task.Relation == 0)
                {
                    return Result.GenError<Result>(Error.ManufactureCheckItemNoRelation);
                }
                ServerConfig.ApiDb.Execute(
                    "UPDATE manufacture_plan_task SET `MarkedDateTime`= NOW(), `TotalOrder` = `TotalOrder` + 1 WHERE `TotalOrder` > @totalOrder;",
                    new { totalOrder });
                task.TotalOrder++;
                task.Id = ServerConfig.ApiDb.Query<int>(
                    "INSERT INTO manufacture_plan_task(`CreateUserId`, `MarkedDateTime`, `TotalOrder`, `PlanId`, `Order`, `Person`, `ModuleId`, `IsCheck`, `CheckId`, `Item`, `EstimatedHour`, `EstimatedMin`, `Score`, `Relation`) " +
                                              "VALUES (@CreateUserId, @MarkedDateTime, @TotalOrder, @PlanId, @Order, @Person, @ModuleId, @IsCheck, @CheckId, @Item, @EstimatedHour, @EstimatedMin, @Score, @Relation)" +
                                                        ";SELECT LAST_INSERT_ID();", task).FirstOrDefault();

                planChange.ParamList.Add(new ManufactureLogItem
                {
                    Type = ManufactureLogType.AddPlanTaskFormat,
                    Field = task.Order.ToString()
                });
                changes.Add(planChange);
                changes.Add(new ManufactureLog
                {
                    Time = markedDateTime,
                    Account = createUserId,
                    PlanId = plan.Id,
                    IsAssign = true,
                    ItemId = task.Id,
                    Type = ManufactureLogType.TaskCreate
                });
            }
            else
            {
                if (task.Relation != 0 && manufacturePlanTasks.All(x => x.Order != task.Relation))
                {
                    return Result.GenError<Result>(Error.ManufactureTaskItemRelationError);
                }

                if (task.IsCheck && task.Relation == 0)
                {
                    return Result.GenError<Result>(Error.ManufactureCheckItemNoRelation);
                }

                var pre = manufacturePlanTasks.LastOrDefault(x => x.TotalOrder == totalOrder);
                var insertIndex = pre?.Order ?? 0;
                task.Order = insertIndex + 1;
                foreach (var manufacturePlanTask in manufacturePlanTasks.Where(x => x.TotalOrder > totalOrder))
                {
                    manufacturePlanTask.MarkedDateTime = now;
                    var oldManufacturePlanTask = (ManufacturePlanTask)manufacturePlanTask.Clone();
                    manufacturePlanTask.Order++;
                    if (manufacturePlanTask.Relation > insertIndex)
                    {
                        manufacturePlanTask.Relation++;
                    }

                    if (oldManufacturePlanTask.HaveChange(manufacturePlanTask, out var change))
                    {
                        change.Time = markedDateTime;
                        change.Account = createUserId;
                        change.PlanId = manufacturePlanTask.PlanId;
                        change.TaskId = manufacturePlanTask.TaskId;
                        change.ItemId = manufacturePlanTask.Id;
                        planChange.ParamList.Add(new ManufactureLogItem
                        {
                            Type = ManufactureLogType.UpdatePlanTaskFormat,
                            Field = oldManufacturePlanTask.Order.ToString(),
                            Items = change.ParamList.Select(y =>
                            {
                                y.Type = ManufactureLogType.UpdatePlanTaskItemFormat;
                                return y;
                            })
                        });
                        changes.Add(change);
                    }
                }

                planChange.ParamList.Add(new ManufactureLogItem
                {
                    Type = ManufactureLogType.AddPlanTaskFormat,
                    Field = task.Order.ToString()
                });
                changes.Add(planChange);
                ServerConfig.ApiDb.Execute(
                    "UPDATE manufacture_plan_task SET `MarkedDateTime`= NOW(), `TotalOrder` = `TotalOrder` + 1 WHERE `TotalOrder` > @totalOrder AND MarkedDelete = 0;",
                    new { totalOrder });

                ServerConfig.ApiDb.Execute(
                    "UPDATE manufacture_plan_task SET `MarkedDateTime`= NOW(), `Order` = @Order, `Relation` = @Relation WHERE `Id` = @Id;", manufacturePlanTasks);

                var relationTask = manufacturePlanTasks.FirstOrDefault(x => x.Order == task.Relation);
                if (relationTask != null && relationTask.State == ManufacturePlanTaskState.Done && task.IsCheck)
                {
                    task.State = ManufacturePlanTaskState.WaitCheck;
                }
                task.TotalOrder++;
                task.MarkedDateTime = markedDateTime;
                task.Id = ServerConfig.ApiDb.Query<int>("INSERT INTO manufacture_plan_task (`CreateUserId`, `MarkedDateTime`, `TotalOrder`, `PlanId`, `Order`, `Person`, `ModuleId`, `IsCheck`, `CheckId`, `Item`, `EstimatedHour`, `EstimatedMin`, `Score`, `Relation`, `State`) " +
                                "VALUES (@CreateUserId, @MarkedDateTime, @TotalOrder, @PlanId, @Order, @Person, @ModuleId, @IsCheck, @CheckId, @Item, @EstimatedHour, @EstimatedMin, @Score, @Relation, @State);SELECT LAST_INSERT_ID();", task).FirstOrDefault();

                changes.Add(new ManufactureLog
                {
                    Time = markedDateTime,
                    Account = createUserId,
                    PlanId = plan.Id,
                    IsAssign = true,
                    ItemId = task.Id,
                    Type = ManufactureLogType.TaskCreate
                });

                ServerConfig.ApiDb.Execute("UPDATE manufacture_plan_item SET `Order` = `Order` + 1 WHERE `PlanId` = @Id AND MarkedDelete = 0 AND `State` = @State;", new
                {
                    plan.Id,
                    State = ManufacturePlanItemState.WaitAssign
                });
            }

            ManufactureLog.AddLog(changes);
            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/ManufacturePlan
        /// <summary>
        /// 任务删除
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public Result DeleteManufacturePlan([FromQuery] int tId)
        {
            var sql =
                "SELECT * FROM manufacture_plan_task WHERE Id = @tId AND MarkedDelete = 0;";
            var task = ServerConfig.ApiDb.Query<ManufacturePlanTask>(sql, new { tId }).FirstOrDefault();
            if (task == null)
            {
                return Result.GenError<Result>(Error.ManufactureTaskItemNotExist);
            }
            var plan =
                ServerConfig.ApiDb.Query<ManufacturePlan>("SELECT * FROM `manufacture_plan` WHERE `Id` = @PlanId;",
                    new { task.PlanId }).FirstOrDefault();

            sql =
                "SELECT COUNT(1) FROM `manufacture_plan_task` WHERE PlanId = @PlanId AND Relation = @Order AND MarkedDelete = 0";
            var cnt = ServerConfig.ApiDb.Query<int>(sql, new { task.PlanId, task.Order }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.ManufacturePlaneTaskDeleteHaveRelation);
            }

            var manufacturePlanTasks =
                ServerConfig.ApiDb.Query<ManufacturePlanTask>("SELECT * FROM `manufacture_plan_task` WHERE PlanId = @PlanId AND MarkedDelete = 0 ORDER BY `Order`;", new { task.PlanId });

            var changes = new List<ManufactureLog>();
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            task = manufacturePlanTasks.First(x => x.Id == tId);
            task.MarkedDelete = true;

            var planChange = new ManufactureLog
            {
                Time = markedDateTime,
                Account = createUserId,
                PlanId = task.PlanId,
                IsAssign = true,
                Type = ManufactureLogType.PlanUpdateItem,
                ParsingWay = 1,
            };
            var pre = manufacturePlanTasks.LastOrDefault(x => x.TotalOrder < task.TotalOrder);
            var insertIndex = task?.Order ?? 0;
            //task.Order = insertIndex + 1;
            foreach (var manufacturePlanTask in manufacturePlanTasks.Where(x => x.TotalOrder > task.TotalOrder))
            {
                var oldManufacturePlanTask = (ManufacturePlanTask)manufacturePlanTask.Clone();
                manufacturePlanTask.Order--;
                if (manufacturePlanTask.Relation > task.Order)
                {
                    manufacturePlanTask.Relation--;
                }

                if (oldManufacturePlanTask.HaveChange(manufacturePlanTask, out var change))
                {
                    planChange.ParamList.Add(new ManufactureLogItem
                    {
                        Type = ManufactureLogType.UpdatePlanTaskFormat,
                        Field = task.Order.ToString(),
                        Items = change.ParamList.Select(y =>
                        {
                            y.Type = ManufactureLogType.UpdatePlanTaskItemFormat;
                            return y;
                        })
                    });
                    changes.Add(change);
                }
            }

            planChange.ParamList.Add(new ManufactureLogItem
            {
                Type = ManufactureLogType.DeletePlanTaskFormat,
                Field = task.Order.ToString()
            });
            changes.Add(planChange);

            ServerConfig.ApiDb.Execute(
                "UPDATE manufacture_plan_task SET `MarkedDelete` = @MarkedDelete, `MarkedDateTime`= NOW(), `Order` = @Order, `Relation` = @Relation WHERE `Id` = @Id;", manufacturePlanTasks);

            ServerConfig.ApiDb.Execute(
                "UPDATE manufacture_plan_task SET `MarkedDateTime`= NOW(), `TotalOrder` = `TotalOrder` - 1 WHERE `TotalOrder` > @totalOrder;",
                new { task.TotalOrder });
            changes.Add(new ManufactureLog
            {
                Time = markedDateTime,
                Account = createUserId,
                PlanId = task.PlanId,
                TaskId = task.OldId != 0 ? plan?.TaskId ?? 0 : 0,
                IsAssign = true,
                ItemId = task.Id,
                Type = ManufactureLogType.TaskDelete
            });

            if (plan != null)
            {
                ServerConfig.ApiDb.Execute(
                    "UPDATE manufacture_plan_item SET `Order` = `Order` - 1 WHERE `PlanId` = @Id AND MarkedDelete = 0 AND `State` = @State;",
                    new
                    {
                        plan.Id,
                        State = ManufacturePlanItemState.WaitAssign
                    });
            }

            ManufactureLog.AddLog(changes);
            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 启动任务
        /// </summary>
        /// <param name="tId">任务id</param>
        /// <returns></returns>
        // POST: api/ManufactureTaskWorkSpace
        [HttpGet("Start")]
        public Result StartManufactureTask([FromQuery] int tId)
        {
            var sql =
                "SELECT * FROM manufacture_plan_task WHERE Id = @tId AND MarkedDelete = 0;";
            var task = ServerConfig.ApiDb.Query<ManufacturePlanTask>(sql, new { tId }).FirstOrDefault();
            if (task == null)
            {
                return Result.GenError<Result>(Error.ManufactureTaskItemNotExist);
            }
            if (task.State != ManufacturePlanTaskState.Stop)
            {
                return Result.GenError<Result>(Error.ManufactureTaskStateError);
            }
            var plan =
                ServerConfig.ApiDb.Query<ManufacturePlan>("SELECT * FROM `manufacture_plan` WHERE `Id` = @PlanId;",
                    new { task.PlanId }).FirstOrDefault();

            //sql =
            //    "SELECT COUNT(1) FROM `manufacture_plan_task` WHERE PlanId = @PlanId AND Relation = @Order AND MarkedDelete = 0";
            //var cnt = ServerConfig.ApiDb.Query<int>(sql, new { task.PlanId, task.Order }).FirstOrDefault();
            //if (cnt > 0)
            //{
            //    return Result.GenError<Result>(Error.ManufacturePlaneTaskStopHaveRelation);
            //}

            var changes = new List<ManufactureLog>();
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            task.State = ManufacturePlanTaskState.Wait;
            if (task.IsCheck)
            {
                sql =
                    "SELECT * FROM manufacture_plan_task WHERE PlanId = @PlanId AND `Order` = @Relation";
                var preTask = ServerConfig.ApiDb.Query<ManufacturePlanTask>(sql, new { task.PlanId, task.Relation }).FirstOrDefault();
                if (preTask != null && preTask.State == ManufacturePlanTaskState.Done)
                {
                    task.State = ManufacturePlanTaskState.WaitCheck;
                }
            }
            var change = new ManufactureLog
            {
                Time = markedDateTime,
                Account = createUserId,
                PlanId = task.PlanId,
                IsAssign = true,
                TaskId = task.OldId != 0 ? plan?.TaskId ?? 0 : 0,
                ItemId = task.Id,
                Type = ManufactureLogType.StopTask,
            };
            changes.Add(change);
            ServerConfig.ApiDb.Execute("UPDATE manufacture_plan_task SET `MarkedDateTime`= NOW(), `State` = @State WHERE `Id` = @Id;", task);

            ManufactureLog.AddLog(changes);
            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 停止任务
        /// </summary>
        /// <param name="tId">任务id</param>
        /// <returns></returns>
        // POST: api/ManufactureTaskWorkSpace
        [HttpGet("Stop")]
        public Result PauseManufactureTask([FromQuery] int tId)
        {
            var sql =
                "SELECT * FROM manufacture_plan_task WHERE Id = @tId AND MarkedDelete = 0;";
            var task = ServerConfig.ApiDb.Query<ManufacturePlanTask>(sql, new { tId }).FirstOrDefault();
            if (task == null)
            {
                return Result.GenError<Result>(Error.ManufactureTaskItemNotExist);
            }
            if (task.State == ManufacturePlanTaskState.Stop)
            {
                return Result.GenError<Result>(Error.ManufactureTaskStateError);
            }
            var plan =
                ServerConfig.ApiDb.Query<ManufacturePlan>("SELECT * FROM `manufacture_plan` WHERE `Id` = @PlanId;",
                    new { task.PlanId }).FirstOrDefault();

            //sql =
            //    "SELECT COUNT(1) FROM `manufacture_plan_task` WHERE PlanId = @PlanId AND Relation = @Order AND MarkedDelete = 0";
            //var cnt = ServerConfig.ApiDb.Query<int>(sql, new { task.PlanId, task.Order }).FirstOrDefault();
            //if (cnt > 0)
            //{
            //    return Result.GenError<Result>(Error.ManufacturePlaneTaskStopHaveRelation);
            //}

            var changes = new List<ManufactureLog>();
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            task.State = ManufacturePlanTaskState.Stop;
            var change = new ManufactureLog
            {
                Time = markedDateTime,
                Account = createUserId,
                PlanId = task.PlanId,
                IsAssign = true,
                TaskId = task.OldId != 0 ? plan?.TaskId ?? 0 : 0,
                ItemId = task.Id,
                Type = ManufactureLogType.StopTask,
            };
            changes.Add(change);
            ServerConfig.ApiDb.Execute("UPDATE manufacture_plan_task SET `MarkedDateTime`= NOW(), `State` = @State WHERE `Id` = @Id;", task);

            ManufactureLog.AddLog(changes);
            return Result.GenError<Result>(Error.Success);
        }

    }
}