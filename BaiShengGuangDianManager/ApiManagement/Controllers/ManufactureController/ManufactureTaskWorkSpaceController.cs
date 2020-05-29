using ApiManagement.Base.Server;
using ApiManagement.Models.DeviceManagementModel;
using ApiManagement.Models.ManufactureModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.ManufactureController
{
    /// <summary>
    /// 任务工作台
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class ManufactureTaskWorkSpaceController : ControllerBase
    {
        private static readonly Dictionary<int, List<ManufacturePlanTaskState>> ValidStates = new Dictionary<int, List<ManufacturePlanTaskState>>
        {
            {1, new List<ManufacturePlanTaskState>{ManufacturePlanTaskState.Pause}} ,
            {2, new List<ManufacturePlanTaskState>{ManufacturePlanTaskState.Doing}} ,
            {3, new List<ManufacturePlanTaskState>{ManufacturePlanTaskState.Redo}} ,
            {4, new List<ManufacturePlanTaskState>{ManufacturePlanTaskState.WaitRedo, ManufacturePlanTaskState.Wait}},
        };
        /// <summary>
        /// 获取当前任务
        /// </summary>
        /// <returns></returns>
        /// <param name="account">员工账号</param>
        /// <param name="gId">分组id</param>
        // GET: api/ManufactureTaskWorkSpace
        [HttpGet]
        public DataResult GetManufactureCurrentTask([FromQuery] string account, int gId)
        {
            var processors =
                ServerConfig.ApiDb.Query<Processor>("SELECT a.Id, b.Account FROM `manufacture_processor` a JOIN `processor` b ON a.ProcessorId = b.Id " +
                                                    $"WHERE b.Account = @account AND a.`MarkedDelete` = 0 {(gId == 0 ? "" : " AND GroupId = @gId")};", new { account, gId });
            if (!processors.Any())
            {
                return Result.GenError<DataResult>(Error.ManufactureProcessorNotExist);
            }

            var result = new DataResult();
            var sql =
                "SELECT a.*, IFNULL(b.Plan, '') Plan, IFNULL(c.ProcessorName, '') Processor, IFNULL(d.Module, '') Module, IFNULL(e.`Check`, '') `Check`, IFNULL(f.ProcessorName, a.Assignor) Assignor FROM manufacture_plan_task a " +
                "LEFT JOIN `manufacture_plan` b ON a.PlanId = b.Id " +
                "LEFT JOIN (SELECT a.*, b.ProcessorName FROM `manufacture_processor` a JOIN `processor` b ON a.ProcessorId = b.Id WHERE a.MarkedDelete = 0) c ON a.Person = c.Id " +
                "LEFT JOIN `manufacture_task_module` d ON a.ModuleId = d.Id " +
                "LEFT JOIN `manufacture_check` e ON a.CheckId = e.Id " +
                "LEFT JOIN `processor` f ON a.Assignor = f.Account " +
                "WHERE a.State IN @state AND a.Person IN @pId AND a.MarkedDelete = 0 AND a.IsCheck = 0 ORDER BY a.`TotalOrder` LIMIT 2;";
            var tasks = new List<ManufacturePlanTask>();
            foreach (var validState in ValidStates)
            {
                tasks.AddRange(ServerConfig.ApiDb.Query<ManufacturePlanTask>(sql, new { state = validState.Value, pId = processors.Select(x => x.Id) }));
                if (tasks.Count >= 2)
                {
                    break;
                }
            }

            if (tasks.Count > 0)
            {
                var first = tasks[0];
                if (tasks.Count > 1)
                {
                    var next = tasks[1];
                    first.NextTask = next.Item;
                }

                sql =
                    "SELECT IFNULL(b.ProcessorName, a.Person) Surveyor FROM `manufacture_plan_task` a " +
                    "LEFT JOIN (SELECT a.*, b.ProcessorName FROM `manufacture_processor` a JOIN `processor` b ON a.ProcessorId = b.Id ) b ON a.Person = b.Id " +
                    "WHERE PlanId = @PlanId AND Relation = @Order AND IsCheck = 1;";
                first.Surveyor = ServerConfig.ApiDb.Query<string>(sql, new { first.PlanId, first.Order }).FirstOrDefault();
                result.datas.Add(first);
                return result;
            }

            return Result.GenError<DataResult>(Error.ManufactureNoTask);
        }

        /// <summary>
        /// 获取绩效排行榜
        /// </summary>
        /// <param name="gId">分组id</param>
        /// <returns></returns>
        // GET: api/ManufactureTaskWorkSpace?qId=0
        [HttpGet("Score")]
        public object GetManufactureTaskWorkSpaceItem([FromQuery] int gId)
        {
            var result = new DataResult();
            var manufactureGroup =
                ServerConfig.ApiDb.Query<ManufactureGroup>("SELECT * FROM `manufacture_group` WHERE Id = @gId AND MarkedDelete = 0;",
                    new { gId }).FirstOrDefault();
            if (manufactureGroup == null)
            {
                return result;
            }

            DateTime sTime, eTime;
            switch (manufactureGroup.Interval)
            {
                case 0:
                    sTime = DateTime.Today.DayBeginTime();
                    eTime = DateTime.Today.DayEndTime();
                    break;
                case 1:
                    manufactureGroup.ScoreTime = (manufactureGroup.ScoreTime - 1) % 7 + 1;
                    sTime = DateTime.Today.WeekBeginTime();
                    eTime = DateTime.Today.WeekEndTime();
                    break;
                case 2:
                    manufactureGroup.ScoreTime = (manufactureGroup.ScoreTime - 1) % 28 + 1;
                    if (manufactureGroup.ScoreTime <= DateTime.Now.Day)
                    {
                        sTime = DateTime.Today.StartOfMonth().AddDays(manufactureGroup.ScoreTime - 1);
                        eTime = sTime.AddMonths(1).AddSeconds(-1);
                    }
                    else
                    {
                        sTime = DateTime.Today.StartOfLastMonth().AddDays(manufactureGroup.ScoreTime - 1);
                        eTime = sTime.AddMonths(1).AddSeconds(-1);
                    }
                    break;
                default: return Result.GenError<DataResult>(Error.ParamError);
            }

            var sql = "SELECT a.Id, a.GroupId, IFNULL(a.ProcessorName, b.Person) Processor, IFNULL(b.Score, 0) Score " +
                      "FROM (SELECT a.*, b.ProcessorName FROM `manufacture_processor` a JOIN `processor` b ON a.ProcessorId = b.Id WHERE a.MarkedDelete = 0 AND a.GroupId = @gId) a " +
                      "LEFT JOIN ( SELECT Person, SUM(ActualScore) Score FROM manufacture_plan_task WHERE MarkedDelete = 0 AND State = @state AND ActualEndTime >= @sTime AND ActualEndTime <= @eTime GROUP BY Person) b ON a.Id = b.Person ORDER BY b.Score DESC, b.Person;";
            var data = ServerConfig.ApiDb.Query<dynamic>(sql, new { state = ManufacturePlanTaskState.Done, gId, sTime, eTime });
            return new
            {
                sTime,
                eTime,
                interval = manufactureGroup.Interval,
                errno = Error.Success,
                errmsg = "成功",
                datas = data
            };
        }

        /// <summary>
        /// 获取未完成任务
        /// </summary>
        /// <param name="account"></param>
        /// <param name="gId">分组id</param>
        /// <param name="limit">记录条数</param>
        /// <returns></returns>
        // GET: api/ManufactureTaskWorkSpace?qId=0&item=false
        [HttpGet("UnfinishedTask")]
        public DataResult GetManufactureUnfinishedTask([FromQuery] string account, int gId, int limit = 10)
        {
            var processors =
                ServerConfig.ApiDb.Query<Processor>("SELECT a.Id, b.Account FROM `manufacture_processor` a JOIN `processor` b ON a.ProcessorId = b.Id " +
                                                    $"WHERE b.Account = @account AND a.`MarkedDelete` = 0 {(gId == 0 ? "" : " AND GroupId = @gId")};", new { account, gId });
            if (!processors.Any())
            {
                return Result.GenError<DataResult>(Error.ManufactureProcessorNotExist);
            }

            var result = new DataResult();
            var sql =
                "SELECT a.*, IFNULL(b.Plan, '') Plan FROM manufacture_plan_task a " +
                "LEFT JOIN `manufacture_plan` b ON a.PlanId = b.Id WHERE a.State != @state AND a.Person IN @pId AND a.MarkedDelete = 0 AND IsCheck = 0 ORDER BY a.`TotalOrder` LIMIT @limit;";
            result.datas.AddRange(ServerConfig.ApiDb.Query<ManufacturePlanTask>(sql, new { state = ManufacturePlanTaskState.Done, pId = processors.Select(x => x.Id), limit }));
            return result;
        }

        /// <summary>
        /// 获取已完成任务
        /// </summary>
        /// <param name="account"></param>
        /// <param name="gId">分组id</param>
        /// <param name="limit"></param>
        /// <returns></returns>
        // GET: api/ManufactureTaskWorkSpace?qId=0&item=false
        [HttpGet("FinishedTask")]
        public DataResult GetManufactureFinishedTask([FromQuery] string account, int gId, int limit = 10)
        {
            var processors =
                ServerConfig.ApiDb.Query<Processor>("SELECT a.Id, b.Account FROM `manufacture_processor` a JOIN `processor` b ON a.ProcessorId = b.Id " +
                                                    $"WHERE b.Account = @account AND a.`MarkedDelete` = 0 {(gId == 0 ? "" : " AND GroupId = @gId")};", new { account, gId });
            if (!processors.Any())
            {
                return Result.GenError<DataResult>(Error.ManufactureProcessorNotExist);
            }

            var result = new DataResult();
            var sql =
                "SELECT a.*, IFNULL(b.Plan, '') Plan FROM manufacture_plan_task a " +
                "LEFT JOIN `manufacture_plan` b ON a.PlanId = b.Id WHERE a.State = @state AND a.Person IN @pId AND a.MarkedDelete = 0 ORDER BY a.`TotalOrder` DESC LIMIT @limit;";
            result.datas.AddRange(ServerConfig.ApiDb.Query<ManufacturePlanTask>(sql, new { state = ManufacturePlanTaskState.Done, pId = processors.Select(x => x.Id), limit }));
            return result;
        }

        /// <summary>
        /// 开始任务
        /// </summary>
        /// <param name="tId">任务id</param>
        /// <param name="account">加工人</param>
        /// <param name="opTask">加工人</param>
        /// <returns></returns>
        // POST: api/ManufactureTaskWorkSpace
        [HttpPost("Start")]
        public Result StartManufactureTask([FromBody] ManufactureOpTask opTask)
        {
            var tId = opTask.TaskId;
            var account = opTask.Account;
            var gId = opTask.GId;
            var processors =
                ServerConfig.ApiDb.Query<Processor>("SELECT a.Id, b.Account FROM `manufacture_processor` a JOIN `processor` b ON a.ProcessorId = b.Id " +
                                                    $"WHERE b.Account = @account AND a.`MarkedDelete` = 0 {(gId == 0 ? "" : " AND GroupId = @gId")};", new { account, gId });
            if (!processors.Any())
            {
                return Result.GenError<DataResult>(Error.ManufactureProcessorNotExist);
            }

            var sql =
                "SELECT b.*, a.* FROM manufacture_plan_task a " +
                "LEFT JOIN `manufacture_plan` b ON a.PlanId = b.Id " +
                "WHERE a.State IN @state AND a.Person IN @pId AND a.MarkedDelete = 0 AND a.IsCheck = 0 ORDER BY a.`TotalOrder` LIMIT 1;";
            ManufacturePlanTask task = null;
            foreach (var validState in ValidStates)
            {
                task = ServerConfig.ApiDb.Query<ManufacturePlanTask>(sql, new { state = validState.Value, pId = processors.Select(x => x.Id) })
                   .FirstOrDefault();
                if (task != null)
                {
                    break;
                }
            }

            if (task != null)
            {
                if (task.Id != tId)
                {
                    return Result.GenError<DataResult>(Error.ManufactureTaskStateError);
                }
                if (task.State != ManufacturePlanTaskState.Wait && task.State != ManufacturePlanTaskState.WaitRedo && task.State != ManufacturePlanTaskState.Pause)
                {
                    return Result.GenError<DataResult>(Error.ManufactureTaskStateError);
                }

                var now = DateTime.Now;
                var oldTask = (ManufacturePlanTask)task.Clone();
                task.State = ManufacturePlanTaskState.Doing;

                if (task.FirstStartTime == default(DateTime))
                {
                    task.FirstStartTime = now;
                }

                if (task.IsRedo)
                {
                    task.RedoCount++;
                }

                task.ActualStartTime = now;
                if (oldTask.HaveChange(task, out var change))
                {
                    change.Time = now;
                    change.Account = account;
                    change.PlanId = task.PlanId;
                    change.IsAssign = true;
                    change.TaskId = task.TaskId;
                    change.ItemId = task.Id;
                    change.Type = ManufactureLogType.StartTask;
                }
                ServerConfig.ApiDb.Execute("UPDATE manufacture_plan_task SET `State` = @State, `FirstStartTime` = @FirstStartTime, `ActualStartTime` = @ActualStartTime, " +
                                           "`RedoCount` = @RedoCount WHERE `Id` = @Id;", task);
                ManufactureLog.AddLog(new List<ManufactureLog> { change });
                return Result.GenError<DataResult>(Error.Success);
            }

            return Result.GenError<DataResult>(Error.ManufactureNoTask);
        }

        /// <summary>
        /// 暂停任务
        /// </summary>
        /// <param name="tId">任务id</param>
        /// <param name="account">加工人</param>
        /// <returns></returns>
        // POST: api/ManufactureTaskWorkSpace
        [HttpPost("Pause")]
        public Result PauseManufactureTask([FromBody]ManufactureOpTask opTask)
        {
            var tId = opTask.TaskId;
            var account = opTask.Account;
            var gId = opTask.GId;
            var processors =
                ServerConfig.ApiDb.Query<Processor>("SELECT a.Id, b.Account FROM `manufacture_processor` a JOIN `processor` b ON a.ProcessorId = b.Id " +
                                                    $"WHERE b.Account = @account AND a.`MarkedDelete` = 0 {(gId == 0 ? "" : " AND GroupId = @gId")};", new { account, gId });
            if (!processors.Any())
            {
                return Result.GenError<DataResult>(Error.ManufactureProcessorNotExist);
            }
            var sql =
                "SELECT b.*, a.* FROM manufacture_plan_task a " +
                "LEFT JOIN `manufacture_plan` b ON a.PlanId = b.Id " +
                "WHERE a.State IN @state AND a.Person IN @pId AND a.MarkedDelete = 0 AND a.IsCheck = 0 ORDER BY a.`TotalOrder` LIMIT 1;";
            ManufacturePlanTask task = null;
            foreach (var validState in ValidStates)
            {
                task = ServerConfig.ApiDb.Query<ManufacturePlanTask>(sql, new { state = validState.Value, pId = processors.Select(x => x.Id) })
                    .FirstOrDefault();
                if (task != null)
                {
                    break;
                }
            }

            if (task != null)
            {
                if (task.Id != tId)
                {
                    return Result.GenError<DataResult>(Error.ManufactureTaskStateError);
                }
                if (task.State != ManufacturePlanTaskState.Doing)
                {
                    return Result.GenError<DataResult>(Error.ManufactureTaskStateError);
                }

                var now = DateTime.Now;
                var oldTask = (ManufacturePlanTask)task.Clone();
                task.State = ManufacturePlanTaskState.Pause;
                task.PauseTime = now;
                var totalSecond = (int)(task.PauseTime - task.ActualStartTime).TotalSeconds;
                var hour = totalSecond / 3600;
                task.ActualHour += hour;
                task.ActualMin += (totalSecond - hour * 3600) / 60;
                if (oldTask.HaveChange(task, out var change))
                {
                    change.Time = now;
                    change.Account = account;
                    change.PlanId = task.PlanId;
                    change.IsAssign = true;
                    change.TaskId = task.TaskId;
                    change.ItemId = task.Id;
                    change.Type = ManufactureLogType.PauseTask;
                }

                ServerConfig.ApiDb.Execute("UPDATE manufacture_plan_task SET `MarkedDateTime` = @MarkedDateTime, `State` = @State, `PauseTime` = @PauseTime, " +
                                           "`ActualHour` = @ActualHour, `ActualMin` = @ActualMin WHERE `Id` = @Id;", task);

                ManufactureLog.AddLog(new List<ManufactureLog> { change });
                return Result.GenError<DataResult>(Error.Success);
            }

            return Result.GenError<DataResult>(Error.ManufactureNoTask);
        }

        /// <summary>
        /// 完成任务
        /// </summary>
        /// <param name="tId">任务id</param>
        /// <param name="account">加工人</param>
        /// <returns></returns>
        // POST: api/ManufactureTaskWorkSpace
        [HttpPost("Finish")]
        public Result FinishManufactureTask([FromBody]ManufactureOpTask opTask)
        {
            var tId = opTask.TaskId;
            var account = opTask.Account;
            var gId = opTask.GId;
            var processors =
                ServerConfig.ApiDb.Query<Processor>("SELECT a.Id, b.Account FROM `manufacture_processor` a JOIN `processor` b ON a.ProcessorId = b.Id " +
                                                    $"WHERE b.Account = @account AND a.`MarkedDelete` = 0 {(gId == 0 ? "" : " AND GroupId = @gId")};", new { account, gId });
            if (!processors.Any())
            {
                return Result.GenError<DataResult>(Error.ManufactureProcessorNotExist);
            }

            var sql =
                "SELECT b.*, a.* FROM manufacture_plan_task a " +
                "LEFT JOIN `manufacture_plan` b ON a.PlanId = b.Id " +
                "WHERE a.State IN @state AND a.Person IN @pId AND a.MarkedDelete = 0 AND a.IsCheck = 0 ORDER BY a.`TotalOrder` LIMIT 1;";
            ManufacturePlanTask task = null;
            foreach (var validState in ValidStates)
            {
                task = ServerConfig.ApiDb.Query<ManufacturePlanTask>(sql, new { state = validState.Value, pId = processors.Select(x => x.Id) })
                    .FirstOrDefault();
                if (task != null)
                {
                    break;
                }
            }

            if (task != null)
            {
                if (task.Id != tId)
                {
                    return Result.GenError<DataResult>(Error.ManufactureTaskStateError);
                }
                if (task.State != ManufacturePlanTaskState.Doing)
                {
                    return Result.GenError<DataResult>(Error.ManufactureTaskStateError);
                }

                var now = DateTime.Now;
                var changes = new List<ManufactureLog>();
                var oldTask = (ManufacturePlanTask)task.Clone();
                task.State = ManufacturePlanTaskState.Done;
                task.ActualEndTime = now;

                var totalSecond = (int)(task.ActualEndTime - task.ActualStartTime).TotalSeconds;
                var totalHour = totalSecond / 3600;
                task.ActualHour += totalHour;
                task.ActualMin += (totalSecond - totalHour * 3600) / 60;

                //if (task.PauseTime != default(DateTime))
                //{
                //    var totalSecond = (int)(task.PauseTime - task.ActualStartTime).TotalSeconds;
                //    var totalHour = totalSecond / 3600;
                //    task.ActualHour += totalHour;
                //    task.ActualMin += (totalSecond - totalHour * 3600) / 60;
                //    task.PauseTime = default(DateTime);
                //}
                //else
                //{
                //    var totalSecond = (int)(task.ActualEndTime - task.ActualStartTime).TotalSeconds;
                //    task.ActualHour += totalSecond / 3600;
                //    task.ActualMin += (totalSecond - task.ActualHour * 3600) / 60;
                //}
                if (oldTask.HaveChange(task, out var change))
                {
                    change.Time = now;
                    change.Account = account;
                    change.PlanId = task.PlanId;
                    change.IsAssign = true;
                    change.TaskId = task.TaskId;
                    change.ItemId = task.Id;
                    change.Type = ManufactureLogType.FinishTask;
                    changes.Add(change);
                }
                var tasks = new List<ManufacturePlanTask> { task };
                sql =
                    "SELECT * FROM `manufacture_plan_task` WHERE PlanId = @PlanId AND Relation = @Order AND IsCheck = 1 AND `State` = @state;";
                var checkTasks = ServerConfig.ApiDb.Query<ManufacturePlanTask>(sql, new { task.PlanId, task.Order, state = ManufacturePlanTaskState.Wait });

                if (checkTasks != null && checkTasks.Any())
                {
                    foreach (var checkTask in checkTasks)
                    {
                        var oldCheckTask = (ManufacturePlanTask)checkTask.Clone();
                        checkTask.State = ManufacturePlanTaskState.WaitCheck;
                        if (oldCheckTask.HaveChange(checkTask, out var changeCheck))
                        {
                            changeCheck.Time = now;
                            changeCheck.Account = account;
                            changeCheck.PlanId = checkTask.PlanId;
                            changeCheck.IsAssign = true;
                            changeCheck.TaskId = checkTask.TaskId;
                            changeCheck.ItemId = checkTask.Id;
                            changeCheck.Type = ManufactureLogType.TaskUpdate;
                            changes.Add(changeCheck);
                        }
                        tasks.Add(checkTask);
                    }
                }

                ServerConfig.ApiDb.Execute("UPDATE manufacture_plan_task SET `MarkedDateTime` = @MarkedDateTime, `State` = @State, `ActualEndTime` = @ActualEndTime, " +
                                           "`ActualHour` = @ActualHour, `ActualMin` = @ActualMin WHERE `Id` = @Id;", tasks);

                ManufactureLog.AddLog(changes);
                return Result.GenError<DataResult>(Error.Success);
            }

            return Result.GenError<DataResult>(Error.ManufactureNoTask);
        }

    }
}