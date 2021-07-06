using ApiManagement.Base.Server;
using ApiManagement.Models.ManufactureModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using ModelBase.Models.BaseModel;

namespace ApiManagement.Controllers.ManufactureController
{
    /// <summary>
    /// 生产计划
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class ManufacturePlanController : ControllerBase
    {
        /// <summary>
        /// 获取计划状态列表
        /// </summary>
        /// <returns></returns>
        // GET: api/ManufacturePlan?qId=0&item=false
        [HttpGet("State")]
        public DataResult GetManufacturePlanState()
        {
            var result = new DataResult();
            result.datas.AddRange(EnumHelper.EnumToList<ManufacturePlanState>().Select(x => new
            {
                Id = x.EnumValue,
                State = x.Description,
            }));
            return result;
        }

        /// <summary>
        /// 获取计划
        /// </summary>
        /// <param name="qId"></param>
        /// <param name="state">状态</param>
        /// <param name="menu">下拉框</param>
        /// <param name="sTime">开始</param>
        /// <param name="eTime">结束</param>
        /// <returns></returns>
        // GET: api/ManufacturePlan?qId=0&item=false
        [HttpGet]
        public DataResult GetManufacturePlan([FromQuery] int qId, int state, DateTime sTime, DateTime eTime, bool menu)
        {
            var result = new DataResult();
            string sql;
            if (menu)
            {
                sql =
                    $"SELECT Id, State, `Plan`, `TaskId` FROM `manufacture_plan` a  WHERE {(qId == 0 ? "" : "Id = @qId AND ")}{(state == 0 ? "" : "State = @state AND ")}{((sTime == default(DateTime) || eTime == default(DateTime)) ? "" : "PlannedStartTime >= @sTime AND PlannedStartTime <= @eTime AND ")}`MarkedDelete` = 0;";
                var data = ServerConfig.ApiDb.Query<dynamic>(sql, new { qId, state, sTime, eTime });
                result.datas.AddRange(data);
            }
            else
            {
                sql = $"SELECT a.*, b.Task FROM `manufacture_plan` a LEFT JOIN `manufacture_task` b ON a.TaskId = b.Id " +
                      $"WHERE {(qId == 0 ? "" : "a.Id = @qId AND ")}{(state == 0 ? "" : "State = @state AND ")}" +
                      $"{((sTime == default(DateTime) || eTime == default(DateTime)) ? "" : "PlannedStartTime >= @sTime AND PlannedStartTime <= @eTime AND ")}a.`MarkedDelete` = 0;";
                var data = ServerConfig.ApiDb.Query<ManufacturePlan>(sql, new { qId, state, sTime, eTime });
                result.datas.AddRange(data);
            }

            if (qId != 0 && !result.datas.Any())
            {
                return Result.GenError<DataResult>(Error.ManufacturePlanNotExist);
            }
            return result;
        }

        /// <summary>
        /// 获取计划任务
        /// </summary>
        /// <param name="qId"></param>
        /// <returns></returns>
        // GET: api/ManufacturePlan?qId=0
        [HttpGet("Item")]
        public DataResult GetManufacturePlanItem([FromQuery] int qId)
        {
            var plan =
                ServerConfig.ApiDb.Query<ManufacturePlan>("SELECT * FROM `manufacture_plan` WHERE `Id` = @qId AND MarkedDelete = 0;",
                    new { qId }).FirstOrDefault();
            if (plan == null)
            {
                return Result.GenError<DataResult>(Error.ManufacturePlanNotExist);
            }

            var result = new DataResult();
            string sql;
            //已下发任务和额外添加任务
            IEnumerable<ManufacturePlanItem> manufacturePlanItems = null;
            if (plan.State != ManufacturePlanState.Wait)
            {
                sql = $"SELECT a.*, IFNULL(b.Plan, '') Plan, c.GroupId, IFNULL(c.`Group`, '') `Group`, IFNULL(c.Processor, '') Processor, IFNULL(d.Module, '') Module, IFNULL(e.`Check`, '') `Check`, @State `State` " +
                      $"FROM  `manufacture_plan_task` a " +
                      "LEFT JOIN `manufacture_plan` b ON a.PlanId = b.Id " +
                      "LEFT JOIN (SELECT a.*, b.Name Processor, c.`Group` FROM `manufacture_processor` a JOIN `accounts` b ON a.ProcessorId = b.Id JOIN `manufacture_group` c ON a.GroupId = c.Id WHERE a.MarkedDelete = 0) c ON a.Person = c.Id " +
                      "LEFT JOIN `manufacture_task_module` d ON a.ModuleId = d.Id " +
                      "LEFT JOIN `manufacture_check` e ON a.CheckId = e.Id " +
                      "WHERE a.PlanId = @qId AND a.MarkedDelete = 0 ORDER BY a.`Order`;";
                manufacturePlanItems = ServerConfig.ApiDb.Query<ManufacturePlanItem>(sql, new { qId, State = ManufacturePlanItemState.Default });
            }
            var order = 0;
            if (manufacturePlanItems != null)
            {
                order = manufacturePlanItems.Max(x => x.Order);
                result.datas.AddRange(manufacturePlanItems);
            }

            sql = $"SELECT a.*, IFNULL(b.Plan, '') Plan, c.GroupId, IFNULL(c.`Group`, '') `Group`, IFNULL(c.Processor, '') Processor, IFNULL(d.Module, '') Module, IFNULL(e.`Check`, '') `Check` " +
                  $"FROM `manufacture_plan_item` a " +
                  "LEFT JOIN `manufacture_plan` b ON a.PlanId = b.Id " +
                  "LEFT JOIN (SELECT a.*, b.Name Processor, c.`Group` FROM `manufacture_processor` a JOIN `accounts` b ON a.ProcessorId = b.Id JOIN `manufacture_group` c ON a.GroupId = c.Id WHERE a.MarkedDelete = 0) c ON a.Person = c.Id " +
                  "LEFT JOIN `manufacture_task_module` d ON a.ModuleId = d.Id " +
                  "LEFT JOIN `manufacture_check` e ON a.CheckId = e.Id " +
                  "WHERE a.PlanId = @qId AND a.MarkedDelete = 0 ORDER BY a.`Order`;";
            var data = ServerConfig.ApiDb.Query<ManufacturePlanItem>(sql, new { qId });
            var waitAssign = data.Where(x => x.State == ManufacturePlanItemState.WaitAssign);
            var oldOrders = new Dictionary<int, int>();
            foreach (var d in waitAssign)
            {
                var newOrder = ++order;
                if (d.Order != newOrder)
                {
                    oldOrders.Add(d.Order, newOrder);
                    d.Order = newOrder;
                }
            }

            //var doneIds = new List<int>();
            //foreach (var d in oldOrders)
            //{
            //    var first = data.FirstOrDefault(x => x.Order == d.Key);
            //    if (first != null)
            //    {
            //        var waitDo = waitAssign.Where(x => !doneIds.Contains(x.Id) && x.Relation == d.Key);
            //        doneIds.AddRange(waitDo.Select(y => y.Id));
            //        if (first.State == ManufacturePlanTaskState.WaitAssign)
            //        {
            //            foreach (var wd in waitDo)
            //            {
            //                wd.Relation = d.Value;
            //            }
            //        }
            //        else
            //        {
            //            var assign = manufacturePlanItems.FirstOrDefault(x => x.OldId == first.Id);
            //            foreach (var wd in waitDo)
            //            {
            //                wd.Relation = assign.Order;
            //            }
            //        }
            //    }
            //}

            result.datas.AddRange(waitAssign);
            return result;
        }

        /// <summary>
        /// 更新计划
        /// </summary>
        /// <param name="manufacturePlans"></param>
        /// <returns></returns>
        // PUT: api/ManufacturePlan
        [HttpPut]
        public DataResult PutManufacturePlan([FromBody] IEnumerable<ManufacturePlanTasks> manufacturePlans)
        {
            if (manufacturePlans == null || manufacturePlans.Any(x => x.Id == 0))
            {
                return Result.GenError<DataResult>(Error.ManufacturePlanNotExist);
            }
            var manufacturePlanOlds =
                ServerConfig.ApiDb.Query<ManufacturePlan>("SELECT a.*, b.Task FROM `manufacture_plan` a JOIN `manufacture_task` b ON a.TaskId = b.Id WHERE a.Id IN @Id AND a.MarkedDelete = 0;",
                    new { Id = manufacturePlans.Select(x => x.Id) });
            if (manufacturePlanOlds == null || !manufacturePlanOlds.Any() || manufacturePlanOlds.Count() != manufacturePlans.Count())
            {
                return Result.GenError<DataResult>(Error.ManufacturePlanNotExist);
            }

            //if (manufacturePlanOlds.Any(x => x.State != ManufacturePlanState.Wait))
            //{
            //    return Result.GenError<DataResult>(Error.ManufacturePlaneChangeState);
            //}

            var changes = new List<ManufactureLog>();
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var manufacturePlan in manufacturePlans)
            {
                var manufacturePlanOld = manufacturePlanOlds.FirstOrDefault(x => x.Id == manufacturePlan.Id);
                if (manufacturePlanOld == null)
                {
                    return Result.GenError<DataResult>(Error.ManufacturePlanNotExist);
                }
                var changeItems = new List<ManufactureLog>();
                var changePlan = false;
                if (manufacturePlan.Plan != null)
                {
                    if (manufacturePlan.Plan == string.Empty)
                    {
                        return Result.GenError<DataResult>(Error.ManufacturePlanNotEmpty);
                    }

                    if (manufacturePlan.Plan != manufacturePlanOld.Plan)
                    {
                        var cnt = ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `manufacture_plan` WHERE `Plan` = @Plan AND MarkedDelete = 0;",
                            new { manufacturePlan.Plan }).FirstOrDefault();
                        if (cnt > 0)
                        {
                            return Result.GenError<DataResult>(Error.ManufacturePlanIsExist);
                        }
                    }
                }

                if (manufacturePlan.TaskId == 0)
                {
                    return Result.GenError<DataResult>(Error.ManufactureTaskNotExist);
                }
                if (manufacturePlan.TaskId != manufacturePlanOld.TaskId)
                {
                    var task = ServerConfig.ApiDb.Query<ManufactureTask>("SELECT Id, Task FROM `manufacture_task` WHERE Id = @Id AND MarkedDelete = 0;",
                        new { Id = manufacturePlan.TaskId }).FirstOrDefault();
                    if (task == null)
                    {
                        return Result.GenError<DataResult>(Error.ManufactureTaskNotExist);
                    }

                    manufacturePlan.Task = task.Task;
                }
                manufacturePlan.Plan = !manufacturePlan.Plan.IsNullOrEmpty() ? manufacturePlan.Plan : manufacturePlanOld.Plan;
                manufacturePlan.PlannedStartTime = manufacturePlan.PlannedStartTime != default(DateTime) ? manufacturePlan.PlannedStartTime : manufacturePlanOld.PlannedStartTime;
                manufacturePlan.PlannedEndTime = manufacturePlan.PlannedEndTime != default(DateTime) ? manufacturePlan.PlannedEndTime : manufacturePlanOld.PlannedEndTime;
                if (manufacturePlanOld.HaveChange(manufacturePlan, out var planChange))
                {
                    changePlan = true;
                    manufacturePlan.MarkedDateTime = markedDateTime;
                    planChange.Time = markedDateTime;
                    planChange.Account = createUserId;
                    planChange.PlanId = manufacturePlan.Id;
                    planChange.TaskId = manufacturePlan.TaskId;
                    changes.Add(planChange);
                }

                if (manufacturePlan.Items != null)
                {
                    var changeItem = false;
                    var items = new List<ManufacturePlanTask>();
                    var add = false;
                    foreach (var item in manufacturePlan.Items)
                    {
                        if (item.State == ManufacturePlanTaskState.WaitAssign)
                        {
                            add = true;
                        }
                        if (add)
                        {
                            items.Add(item);
                        }
                    }
                    var result = new DataResult();
                    if (items != null && items.Any())
                    {
                        if (items.Any(x => x.State != ManufacturePlanTaskState.WaitAssign))
                        {
                            return Result.GenError<DataResult>(Error.ManufacturePlaneTaskAfterAssign);
                        }

                        var sql = $"SELECT a.*, IFNULL(b.Plan, '') Plan, IFNULL(c.Name, '') Processor, IFNULL(d.Module, '') Module, IFNULL(e.`Check`, '') `Check` FROM `manufacture_plan_item` a " +
                                  "LEFT JOIN `manufacture_plan` b ON a.PlanId = b.Id " +
                                  "LEFT JOIN (SELECT a.*, b.Name FROM `manufacture_processor` a JOIN `accounts` b ON a.ProcessorId = b.Id WHERE a.MarkedDelete = 0) c ON a.Person = c.Id " +
                                  "LEFT JOIN `manufacture_task_module` d ON a.ModuleId = d.Id " +
                                  "LEFT JOIN `manufacture_check` e ON a.CheckId = e.Id " +
                                  "WHERE a.PlanId = @Id AND a.`State` = @state AND a.MarkedDelete = 0 ORDER BY a.`Order`;";
                        var waitAssign =
                            ServerConfig.ApiDb.Query<ManufacturePlanTask>(sql, new { manufacturePlan.Id, state = ManufacturePlanItemState.WaitAssign });

                        //已下发的和额外添加的
                        var haveManufacturePlanItems = ServerConfig.ApiDb.Query<ManufacturePlanTask>("SELECT * FROM `manufacture_plan_task` WHERE PlanId = @PlanId AND MarkedDelete = 0 ORDER BY `Order`;",
                            new { PlanId = manufacturePlan.Id });

                        var max = haveManufacturePlanItems.Count();
                        var order = max;
                        foreach (var item in items)
                        {
                            item.Order = ++order;
                        }

                        //var oldToNew = new Dictionary<int, int>();
                        //var i = 0;
                        //foreach (var item in items)
                        //{
                        //    oldToNew.Add(item.Order, ++i);
                        //    item.Order = oldToNew[item.Order];
                        //    if (item.Relation != 0)
                        //    {
                        //        item.Relation = oldToNew[item.Relation];
                        //    }
                        //}
                        if (items.Any(x => x.Item.IsNullOrEmpty()))
                        {
                            return Result.GenError<DataResult>(Error.ManufactureTaskItemNotEmpty);
                        }

                        if (items.GroupBy(x => x.Order).Any(y => y.Count() > 1))
                        {
                            return Result.GenError<DataResult>(Error.ManufactureTaskItemOrderDuplicate);
                        }

                        var error = 0;
                        foreach (var item in items)
                        {
                            item.PlanId = manufacturePlan.Id;
                            if (error != 2 && item.Order <= item.Relation || (item.Relation != 0 && (items.All(x => x.Order != item.Relation) && item.Relation > max)))
                            {
                                error = 1;
                                result.datas.Add(item.Item);
                            }
                            else if (error != 1 && item.IsCheck && item.Relation == 0)
                            {
                                error = 2;
                                result.datas.Add(item.Item);
                            }
                            if (error != 0)
                            {
                                continue;
                            }

                            var d = waitAssign.FirstOrDefault(x => x.Id == item.Id);
                            if (d != null)
                            {
                                item.Item = item.Item ?? d.Item;
                                item.Desc = item.Desc ?? d.Desc;
                                item.CheckId = !item.IsCheck && d.IsCheck ? 0 : item.CheckId;
                                item.Check = !item.IsCheck && d.IsCheck ? "" : !item.Check.IsNullOrEmpty() ? item.Check : "";
                                if (d.HaveChange(item, out var change))
                                {
                                    changeItem = true;
                                    item.MarkedDateTime = markedDateTime;
                                    change.Time = markedDateTime;
                                    change.Account = createUserId;
                                    change.PlanId = manufacturePlan.Id;
                                    change.TaskId = manufacturePlan.TaskId;
                                    change.ItemId = item.Id;
                                    change.Order = d.Order;
                                    changeItems.Add(change);
                                }
                            }
                            else
                            {
                                item.CreateUserId = createUserId;
                                item.MarkedDateTime = markedDateTime;
                                item.Desc = item.Desc ?? "";
                            }
                        }

                        if (result.datas.Any())
                        {
                            result.errno = error == 1 ? Error.ManufactureTaskItemRelationError : Error.ManufactureCheckItemNoRelation;
                            return result;
                        }

                        var itemChange = new ManufactureLog
                        {
                            Time = markedDateTime,
                            Account = createUserId,
                            PlanId = manufacturePlan.Id,
                            TaskId = manufacturePlan.TaskId,
                            Type = ManufactureLogType.PlanUpdateItem,
                            ParsingWay = 1,
                        };
                        #region 更新
                        var updateItems = items.Where(x => x.Id != 0 && waitAssign.Any(y => y.Id == x.Id));
                        if (updateItems.Any() && changeItem)
                        {
                            ServerConfig.ApiDb.Execute("UPDATE manufacture_plan_item SET `MarkedDateTime` = @MarkedDateTime, `Order` = @Order, `Person` = @Person, `ModuleId` = @ModuleId, `IsCheck` = @IsCheck, " +
                                                       "`CheckId` = @CheckId, `Item` = @Item, `EstimatedHour` = @EstimatedHour, `EstimatedMin` = @EstimatedMin, `Score` = @Score, `Desc` = @Desc, `Relation` = @Relation WHERE `Id` = @Id;", updateItems);
                        }
                        #endregion

                        #region 删除
                        var delItems = waitAssign.Where(x => items.All(y => y.Id != x.Id));
                        if (delItems.Any())
                        {
                            foreach (var delItem in delItems.OrderBy(x => x.Order))
                            {
                                delItem.MarkedDateTime = markedDateTime;
                                delItem.MarkedDelete = true;
                                itemChange.ParamList.Add(new ManufactureLogItem
                                {
                                    Type = ManufactureLogType.DeletePlanTaskFormat,
                                    Field = delItem.Order.ToString()
                                });
                            }

                            changes.AddRange(delItems.Select(x => new ManufactureLog
                            {
                                Time = markedDateTime,
                                Account = createUserId,
                                PlanId = manufacturePlan.Id,
                                TaskId = manufacturePlan.TaskId,
                                ItemId = x.Id,
                                Type = ManufactureLogType.TaskDelete
                            }));
                            ServerConfig.ApiDb.Execute("UPDATE `manufacture_plan_item` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id` = @Id;", delItems);
                        }

                        #endregion
                        if (updateItems.Any() && changeItem)
                        {
                            itemChange.ParamList.AddRange(changeItems.Select(x => new ManufactureLogItem
                            {
                                Type = ManufactureLogType.UpdatePlanTaskFormat,
                                Field = x.Order.ToString(),
                                Items = x.ParamList.Select(y =>
                                {
                                    y.Type = ManufactureLogType.UpdatePlanTaskItemFormat;
                                    return y;
                                })
                            }));
                        }
                        #region 添加
                        var addItems = items.Where(x => x.Id == 0);
                        if (addItems.Any())
                        {
                            ServerConfig.ApiDb.Execute(
                                "INSERT INTO manufacture_plan_item (`CreateUserId`, `MarkedDateTime`, `PlanId`, `Order`, `Person`, `ModuleId`, `IsCheck`, `CheckId`, `Item`, `EstimatedHour`, `EstimatedMin`, `Score`, `Desc`, `Relation`) " +
                                "VALUES (@CreateUserId, @MarkedDateTime, @PlanId, @Order, @Person, @ModuleId, @IsCheck, @CheckId, @Item, @EstimatedHour, @EstimatedMin, @Score, @Desc, @Relation);",
                                addItems);
                            var ids = updateItems.Select(x => x.Id);
                            var newData =
                                ServerConfig.ApiDb.Query<ManufacturePlanItem>($"SELECT Id, `Order` FROM `manufacture_plan_item` WHERE PlanId = @Id{(ids.Any() ? " AND Id NOT IN @Ids" : "")} AND MarkedDelete = 0;",
                                    new { manufacturePlan.Id, Ids = ids });

                            itemChange.ParamList.AddRange(newData.Select(x => new ManufactureLogItem
                            {
                                Type = ManufactureLogType.AddPlanTaskFormat,
                                Field = x.Order.ToString()
                            }));
                            changes.AddRange(newData.Select(x => new ManufactureLog
                            {
                                Time = markedDateTime,
                                Account = createUserId,
                                PlanId = manufacturePlan.Id,
                                TaskId = manufacturePlan.TaskId,
                                ItemId = x.Id,
                                Type = ManufactureLogType.TaskCreate
                            }));
                        }
                        #endregion
                        if (itemChange.ParamList.Any())
                        {
                            changes.Add(itemChange);
                        }
                        changes.AddRange(changeItems);
                    }
                    else
                    {

                        var itemChange = new ManufactureLog
                        {
                            Time = markedDateTime,
                            Account = createUserId,
                            PlanId = manufacturePlan.Id,
                            TaskId = manufacturePlan.TaskId,
                            Type = ManufactureLogType.PlanUpdateItem,
                            ParsingWay = 1,
                        };
                        #region 删除
                        var sql = $"SELECT a.*, IFNULL(b.Plan, '') Plan, IFNULL(c.Name, '') Processor, IFNULL(d.Module, '') Module, IFNULL(e.`Check`, '') `Check` FROM `manufacture_plan_item` a " +
                                  "LEFT JOIN `manufacture_plan` b ON a.PlanId = b.Id " +
                                  "LEFT JOIN (SELECT a.*, b.Name FROM `manufacture_processor` a JOIN `accounts` b ON a.ProcessorId = b.Id WHERE a.MarkedDelete = 0) c ON a.Person = c.Id " +
                                  "LEFT JOIN `manufacture_task_module` d ON a.ModuleId = d.Id " +
                                  "LEFT JOIN `manufacture_check` e ON a.CheckId = e.Id " +
                                  "WHERE a.PlanId = @Id AND a.`State` = @state AND a.MarkedDelete = 0 ORDER BY a.`Order`;";
                        var waitAssign =
                            ServerConfig.ApiDb.Query<ManufacturePlanTask>(sql, new { manufacturePlan.Id, state = ManufacturePlanItemState.WaitAssign });
                        var delItems = waitAssign.Where(x => items.All(y => y.Id != x.Id));
                        if (delItems.Any())
                        {
                            foreach (var delItem in delItems.OrderBy(x => x.Order))
                            {
                                delItem.MarkedDateTime = markedDateTime;
                                delItem.MarkedDelete = true;
                                itemChange.ParamList.Add(new ManufactureLogItem
                                {
                                    Type = ManufactureLogType.DeletePlanTaskFormat,
                                    Field = delItem.Order.ToString()
                                });
                            }

                            changes.AddRange(delItems.Select(x => new ManufactureLog
                            {
                                Time = markedDateTime,
                                Account = createUserId,
                                PlanId = manufacturePlan.Id,
                                TaskId = manufacturePlan.TaskId,
                                ItemId = x.Id,
                                Type = ManufactureLogType.TaskDelete
                            }));
                            ServerConfig.ApiDb.Execute("UPDATE `manufacture_plan_item` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id` = @Id;", delItems);
                        }
                        #endregion
                    }

                }

                if (changePlan)
                {
                    ServerConfig.ApiDb.Execute(
                        "UPDATE manufacture_plan SET `MarkedDateTime` = @MarkedDateTime, `Plan` = @Plan, `PlannedStartTime` = @PlannedStartTime, `PlannedEndTime` = @PlannedEndTime, `EstimatedHour` = @EstimatedHour, `EstimatedMin` = @EstimatedMin, `TaskId` = @TaskId WHERE `Id` = @Id;", manufacturePlan);

                }
            }
            ManufactureLog.AddLog(changes);
            return Result.GenError<DataResult>(Error.Success);
        }

        /// <summary>
        /// 计划下发
        /// </summary>
        /// <param name="assignPlan"></param>
        /// <returns></returns>
        //POST: api/ManufacturePlan/Assign
        [HttpPost("Assign")]
        public Result AssignManufacturePlan([FromBody] ManufacturePlanAssign assignPlan)
        {
            var plan =
                ServerConfig.ApiDb.Query<ManufacturePlan>("SELECT * FROM `manufacture_plan` WHERE `Id` = @PlanId AND MarkedDelete = 0;",
                    new { assignPlan.PlanId }).FirstOrDefault();
            if (plan == null)
            {
                return Result.GenError<Result>(Error.ManufacturePlanNotExist);
            }

            var manufacturePlanItems =
                ServerConfig.ApiDb.Query<ManufacturePlanTask>("SELECT * FROM `manufacture_plan_item` WHERE PlanId = @PlanId AND MarkedDelete = 0 ORDER BY `Order`;", new { assignPlan.PlanId });
            if (!manufacturePlanItems.Any())
            {
                return Result.GenError<Result>(Error.ManufacturePlaneNoTask);
            }

            if (manufacturePlanItems.All(x => x.State != ManufacturePlanTaskState.WaitAssign))
            {
                return Result.GenError<Result>(Error.ManufacturePlaneAssignState);
            }

            if (assignPlan.TaskIds.IsNullOrEmpty())
            {
                var changes = new List<ManufactureLog>();
                var createUserId = Request.GetIdentityInformation();
                var markedDateTime = DateTime.Now;
                var assigned = plan.State == ManufacturePlanState.Assigned;
                if (!assigned)
                {
                    plan.State = ManufacturePlanState.Assigned;
                    plan.AssignedTime = markedDateTime;
                    changes.Add(new ManufactureLog
                    {
                        Time = markedDateTime,
                        Account = createUserId,
                        PlanId = plan.Id,
                        IsAssign = plan.State > ManufacturePlanState.Wait,
                        TaskId = plan.TaskId,
                        Type = ManufactureLogType.PlanAssigned
                    });
                }

                ServerConfig.ApiDb.Execute(
                    "UPDATE manufacture_plan SET `State` = @State, `AssignedTime` = @AssignedTime WHERE `Id` = @Id;", plan);

                if (manufacturePlanItems != null && manufacturePlanItems.Any())
                {
                    manufacturePlanItems = manufacturePlanItems.OrderBy(x => x.Order);
                    var i = 0;
                    foreach (var manufacturePlanItem in manufacturePlanItems)
                    {
                        manufacturePlanItem.CreateUserId = createUserId;
                        manufacturePlanItem.MarkedDateTime = markedDateTime;
                        manufacturePlanItem.OldId = manufacturePlanItem.Id;
                        manufacturePlanItem.Assignor = createUserId;
                        manufacturePlanItem.Desc = manufacturePlanItem.Desc ?? "";
                        manufacturePlanItem.Order = ++i;
                        manufacturePlanItem.State = ManufacturePlanTaskState.Wait;
                    }
                    ServerConfig.ApiDb.Execute(
                        "INSERT INTO manufacture_plan_task (`CreateUserId`, `MarkedDateTime`, `PlanId`, `Order`, `Person`, `ModuleId`, `IsCheck`, `CheckId`, `Item`, `EstimatedHour`, `EstimatedMin`, `Score`, `Desc`, `Relation`, `OldId`, `Assignor`) " +
                        "VALUES (@CreateUserId, @MarkedDateTime, @PlanId, @Order, @Person, @ModuleId, @IsCheck, @CheckId, @Item, @EstimatedHour, @EstimatedMin, @Score, @Desc, @Relation, @OldId, @Assignor);",
                        manufacturePlanItems);

                    ServerConfig.ApiDb.Execute(
                        "UPDATE manufacture_plan_item SET `MarkedDateTime` = @MarkedDateTime, `Order` = @Order, `State` = @State WHERE `Id` = @Id;",
                        manufacturePlanItems);

                    ServerConfig.ApiDb.Execute(
                        "UPDATE manufacture_plan_task SET `TotalOrder` = (SELECT a.TotalOrder FROM (SELECT MAX(TotalOrder) TotalOrder FROM `manufacture_plan_task` WHERE MarkedDelete = 0) a) + `Order` WHERE `TotalOrder` = 0 AND PlanId = @PlanId AND MarkedDelete = 0;", new { assignPlan.PlanId });

                    var newData =
                        ServerConfig.ApiDb.Query<ManufacturePlanItem>("SELECT Id FROM `manufacture_plan_task` WHERE PlanId = @PlanId AND MarkedDelete = 0;", new { assignPlan.PlanId });

                    changes.AddRange(newData.Select(x => new ManufactureLog
                    {
                        Time = markedDateTime,
                        Account = createUserId,
                        PlanId = plan.Id,
                        IsAssign = plan.State > ManufacturePlanState.Wait,
                        TaskId = plan.TaskId,
                        ItemId = x.Id,
                        Type = ManufactureLogType.TaskAssigned
                    }));
                }
                ManufactureLog.AddLog(changes);
            }
            else
            {
                var taskIds = assignPlan.TaskIds.Split(",").Select(int.Parse);
                if (!taskIds.Any())
                {
                    return Result.GenError<DataResult>(Error.ParamError);
                }

                if (taskIds.Any(x => manufacturePlanItems.All(y => y.Id != x)))
                {
                    return Result.GenError<DataResult>(Error.ManufactureTaskItemNotExist);
                }

                //待下发任务
                var assignManufacturePlanItems = manufacturePlanItems.Where(x => taskIds.Contains(x.Id));
                if (assignManufacturePlanItems.Any(x => x.State != ManufacturePlanTaskState.WaitAssign))
                {
                    return Result.GenError<DataResult>(Error.ManufacturePlaneAssignState);
                }

                //已下发的和额外添加的
                var haveManufacturePlanItems = ServerConfig.ApiDb.Query<ManufacturePlanTask>("SELECT * FROM `manufacture_plan_task` WHERE PlanId = @PlanId AND MarkedDelete = 0 ORDER BY `Order`;",
                    new { assignPlan.PlanId });

                var notAssignManufacturePlanItems = manufacturePlanItems.Where(x => x.State == ManufacturePlanTaskState.WaitAssign && !taskIds.Contains(x.Id));
                var relationIds = assignManufacturePlanItems.Where(x => x.Relation != 0).Select(y => y.Relation);
                //if (relationIds.Any())
                //{
                if (relationIds.Any(x => x > haveManufacturePlanItems.Count()) && relationIds.Any(x => assignManufacturePlanItems.All(y => y.Order != x)))
                {
                    return Result.GenError<DataResult>(Error.ManufactureTaskItemAssignAfterRelation);
                }
                ////关联任务
                //var relationManufacturePlanItems = manufacturePlanItems.Where(x => relationIds.Contains(x.Id));
                ////未下发的关联任务
                //var taskRelations = relationManufacturePlanItems.Where(x => x.State == ManufacturePlanTaskState.WaitAssign);
                //if (taskRelations.Any() && taskRelations.All(x => !taskIds.Contains(x.Id)))
                //{
                //    return Result.GenError<DataResult>(Error.ManufactureTaskItemAssignAfterRelation);
                //}
                //}

                var changes = new List<ManufactureLog>();
                var createUserId = Request.GetIdentityInformation();
                var markedDateTime = DateTime.Now;
                var assigned = plan.State == ManufacturePlanState.Assigned;
                if (!assigned)
                {
                    plan.State = ManufacturePlanState.Assigned;
                    plan.AssignedTime = markedDateTime;
                    changes.Add(new ManufactureLog
                    {
                        Time = markedDateTime,
                        Account = createUserId,
                        PlanId = plan.Id,
                        IsAssign = true,
                        TaskId = plan.TaskId,
                        Type = ManufactureLogType.PlanAssigned
                    });
                }

                ServerConfig.ApiDb.Execute(
                    "UPDATE manufacture_plan SET `State` = @State, `AssignedTime` = @AssignedTime WHERE `Id` = @Id;", plan);

                if (assignManufacturePlanItems.Any())
                {
                    var order = haveManufacturePlanItems.Count();
                    foreach (var assignManufacturePlanItem in assignManufacturePlanItems)
                    {
                        assignManufacturePlanItem.CreateUserId = createUserId;
                        assignManufacturePlanItem.MarkedDateTime = markedDateTime;
                        assignManufacturePlanItem.OldId = assignManufacturePlanItem.Id;
                        assignManufacturePlanItem.Assignor = createUserId;
                        assignManufacturePlanItem.Desc = assignManufacturePlanItem.Desc ?? "";
                        assignManufacturePlanItem.Order = ++order;
                        assignManufacturePlanItem.State = ManufacturePlanTaskState.Wait;
                    }
                    foreach (var notAssignManufacturePlanItem in notAssignManufacturePlanItems)
                    {
                        notAssignManufacturePlanItem.CreateUserId = createUserId;
                        notAssignManufacturePlanItem.MarkedDateTime = markedDateTime;
                        notAssignManufacturePlanItem.Assignor = createUserId;
                        notAssignManufacturePlanItem.Desc = notAssignManufacturePlanItem.Desc ?? "";
                        notAssignManufacturePlanItem.Order = ++order;
                    }
                    assignManufacturePlanItems = assignManufacturePlanItems.OrderBy(x => x.Order);
                    ServerConfig.ApiDb.Execute(
                        "INSERT INTO manufacture_plan_task (`CreateUserId`, `MarkedDateTime`, `PlanId`, `Order`, `Person`, `ModuleId`, `IsCheck`, `CheckId`, `Item`, `EstimatedHour`, `EstimatedMin`, `Score`, `Desc`, `Relation`, `OldId`, `Assignor`) " +
                        "VALUES (@CreateUserId, @MarkedDateTime, @PlanId, @Order, @Person, @ModuleId, @IsCheck, @CheckId, @Item, @EstimatedHour, @EstimatedMin, @Score, @Desc, @Relation, @OldId, @Assignor);",
                        assignManufacturePlanItems);

                    ServerConfig.ApiDb.Execute(
                        "UPDATE manufacture_plan_item SET `MarkedDateTime` = @MarkedDateTime, `Order` = @Order, `State` = @State WHERE `Id` = @Id;",
                        manufacturePlanItems);

                    ServerConfig.ApiDb.Execute(
                        "UPDATE manufacture_plan_task SET `TotalOrder` = (SELECT a.TotalOrder FROM (SELECT MAX(TotalOrder) TotalOrder FROM `manufacture_plan_task` WHERE MarkedDelete = 0) a) + `Order` WHERE `TotalOrder` = 0 AND PlanId = @PlanId AND MarkedDelete = 0;", new { assignPlan.PlanId });

                    var newData =
                        ServerConfig.ApiDb.Query<ManufacturePlanItem>("SELECT Id FROM `manufacture_plan_task` WHERE PlanId = @PlanId AND MarkedDelete = 0;", new { assignPlan.PlanId });

                    changes.AddRange(newData.Select(x => new ManufactureLog
                    {
                        Time = markedDateTime,
                        Account = createUserId,
                        PlanId = plan.Id,
                        IsAssign = plan.State > ManufacturePlanState.Wait,
                        TaskId = plan.TaskId,
                        ItemId = x.Id,
                        Type = ManufactureLogType.TaskAssigned
                    }));
                }
                ManufactureLog.AddLog(changes);
            }

            return Result.GenError<DataResult>(Error.Success);
        }

        /// <summary>
        /// 添加计划
        /// </summary>
        // POST: api/ManufacturePlan
        [HttpPost]
        public DataResult PostManufacturePlan([FromBody] ManufacturePlanTasks manufacturePlan)
        {
            if (manufacturePlan.Plan.IsNullOrEmpty())
            {
                return Result.GenError<DataResult>(Error.ManufacturePlanNotEmpty);
            }
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `manufacture_plan` WHERE `Plan` = @Plan AND MarkedDelete = 0;",
                    new { manufacturePlan.Plan }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<DataResult>(Error.ManufacturePlanIsExist);
            }
            cnt =
              ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `manufacture_task` WHERE Id = @Id AND MarkedDelete = 0;",
                    new { Id = manufacturePlan.TaskId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<DataResult>(Error.ManufactureTaskNotExist);
            }

            var items = manufacturePlan.Items;
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            var result = new DataResult();
            if (items != null && items.Any())
            {
                items = manufacturePlan.Items.OrderBy(x => x.Order);
                if (items.Any(x => x.Item.IsNullOrEmpty()))
                {
                    return Result.GenError<DataResult>(Error.ManufactureTaskItemNotEmpty);
                }

                if (items.GroupBy(x => x.Order).Any(y => y.Count() > 1))
                {
                    return Result.GenError<DataResult>(Error.ManufactureTaskItemOrderDuplicate);
                }

                if (items.Any(x => x.IsCheck && x.Relation == 0))
                {
                    return Result.GenError<DataResult>(Error.ManufactureCheckItemNoRelation);
                }
                var oldToNew = new Dictionary<int, int>();
                var i = 0;
                foreach (var item in items)
                {
                    oldToNew.Add(item.Order, ++i);
                    item.Order = oldToNew[item.Order];
                    if (item.Relation != 0)
                    {
                        item.Relation = oldToNew[item.Relation];
                    }
                }

                foreach (var item in items)
                {
                    if (item.Order <= item.Relation
                        || (item.Relation != 0 && items.All(x => x.Order != item.Relation)))
                    {
                        result.errno = Error.ManufactureTaskItemRelationError;
                        result.datas.Add(item.Item);
                        return result;
                    }
                    item.CreateUserId = createUserId;
                    item.MarkedDateTime = markedDateTime;
                    item.Desc = item.Desc ?? "";
                }
            }
            manufacturePlan.CreateUserId = createUserId;
            manufacturePlan.MarkedDateTime = markedDateTime;
            manufacturePlan.Id = ServerConfig.ApiDb.Query<int>("INSERT INTO manufacture_plan (`CreateUserId`, `MarkedDateTime`, `State`, `Plan`, `PlannedStartTime`, `PlannedEndTime`, `EstimatedHour`, `EstimatedMin`, `TaskId`) " +
                                                   "VALUES (@CreateUserId, @MarkedDateTime, @State, @Plan, @PlannedStartTime, @PlannedEndTime, @EstimatedHour, @EstimatedMin, @TaskId);SELECT LAST_INSERT_ID();", manufacturePlan).FirstOrDefault();
            var changes = new List<ManufactureLog>
            {
                new ManufactureLog
                {
                    Time = markedDateTime,
                    Account = createUserId,
                    PlanId = manufacturePlan.Id,
                    TaskId = manufacturePlan.TaskId,
                    Type =  ManufactureLogType.PlanCreate
                }
            };
            if (items != null && items.Any())
            {
                foreach (var item in items)
                {
                    item.PlanId = manufacturePlan.Id;
                }
                ServerConfig.ApiDb.Execute(
                    "INSERT INTO manufacture_plan_item (`CreateUserId`, `MarkedDateTime`, `PlanId`, `Order`, `Person`, `ModuleId`, `IsCheck`, `CheckId`, `Item`, `EstimatedHour`, `EstimatedMin`, `Score`, `Desc`, `Relation`) " +
                    "VALUES (@CreateUserId, @MarkedDateTime, @PlanId, @Order, @Person, @ModuleId, @IsCheck, @CheckId, @Item, @EstimatedHour, @EstimatedMin, @Score, @Desc, @Relation);",
                    items);

                var data =
                    ServerConfig.ApiDb.Query<ManufacturePlanItem>("SELECT Id FROM `manufacture_plan_item` WHERE PlanId = @Id AND MarkedDelete = 0;", new { manufacturePlan.Id });

                changes.AddRange(data.Select(x => new ManufactureLog
                {
                    Time = markedDateTime,
                    Account = createUserId,
                    PlanId = manufacturePlan.Id,
                    TaskId = manufacturePlan.TaskId,
                    ItemId = x.Id,
                    Type = ManufactureLogType.TaskCreate
                }));
            }
            ManufactureLog.AddLog(changes);
            return Result.GenError<DataResult>(Error.Success);
        }

        // DELETE: api/ManufacturePlan
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteManufacturePlan([FromBody] BatchDelete batchDelete)
        {
            var changes = new List<ManufactureLog>();
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            var ids = batchDelete.ids;
            var data =
                ServerConfig.ApiDb.Query<ManufacturePlan>("SELECT Id, TaskId, State FROM `manufacture_plan` WHERE Id IN @id AND `MarkedDelete` = 0;", new { id = ids });
            if (!data.Any())
            {
                return Result.GenError<Result>(Error.ManufacturePlanNotExist);
            }

            if (data.Any(x => x.State != ManufacturePlanState.Wait))
            {
                return Result.GenError<Result>(Error.ManufacturePlaneAssignState);
            }

            changes.AddRange(data.Select(x => new ManufactureLog
            {
                Time = markedDateTime,
                Account = createUserId,
                PlanId = x.Id,
                TaskId = x.TaskId,
                Type = ManufactureLogType.PlanDelete
            }));

            var waitPlanIds = data.Select(x => x.Id);
            var delPlanItems =
                ServerConfig.ApiDb.Query<ManufacturePlanItem>("SELECT Id FROM `manufacture_plan_item` WHERE PlanId IN @Id AND MarkedDelete = 0;", new { Id = waitPlanIds });
            changes.AddRange(delPlanItems.Select(x => new ManufactureLog
            {
                Time = markedDateTime,
                Account = createUserId,
                PlanId = x.PlanId,
                TaskId = data.FirstOrDefault(y => y.Id == x.PlanId)?.TaskId ?? 0,
                ItemId = x.Id,
                Type = ManufactureLogType.TaskDelete
            }));
            //var otherIds = data.Select(x => x.Id);
            //if (otherIds.Any())
            //{
            //    delItems =
            //        ServerConfig.ApiDb.Query<ManufacturePlanItem>("SELECT Id FROM `manufacture_plan_task` WHERE PlanId IN @Id AND MarkedDelete = 0;", new { Id = otherIds });
            //    changes.AddRange(delItems.Select(x => new ManufactureLog
            //    {
            //        Time = markedDateTime,
            //        Account = createUserId,
            //        PlanId = x.PlanId,
            //        TaskId = data.FirstOrDefault(y => y.Id == x.PlanId)?.TaskId ?? 0,
            //        ItemId = x.Id,
            //        Type = ManufactureLogType.TaskDelete
            //    }));
            //}

            ManufactureLog.AddLog(changes);
            ServerConfig.ApiDb.Execute(
                "UPDATE `manufacture_plan` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id` IN @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = ids
                });
            ServerConfig.ApiDb.Execute(
                "UPDATE `manufacture_plan_item` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `PlanId` IN @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = ids
                });

            //ServerConfig.ApiDb.Execute(
            //    "UPDATE `manufacture_plan_task` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `PlanId` IN @Id;", new
            //    {
            //        MarkedDateTime = DateTime.Now,
            //        MarkedDelete = true,
            //        Id = ids
            //    });


            return Result.GenError<Result>(Error.Success);
        }
    }
}