using ApiManagement.Base.Server;
using ApiManagement.Models.DeviceManagementModel;
using ApiManagement.Models.ManufactureModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.ManufactureController
{
    /// <summary>
    /// 检验工作台
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class ManufactureCheckWorkSpaceController : ControllerBase
    {
        private static readonly Dictionary<int, List<ManufacturePlanItemState>> ValidStates = new Dictionary<int, List<ManufacturePlanItemState>>
        {
            {1, new List<ManufacturePlanItemState>{ManufacturePlanItemState.Pause}},
            {2, new List<ManufacturePlanItemState>{ManufacturePlanItemState.Checking}},
            {3, new List<ManufacturePlanItemState>{ManufacturePlanItemState.WaitCheck}},
        };
        /// <summary>
        /// 获取当前检验任务
        /// </summary>
        /// <returns></returns>
        /// <param name="account">员工id</param>
        // GET: api/ManufactureTaskWorkSpace
        [HttpGet]
        public DataResult GetManufactureCurrentCheck([FromQuery]  string account)
        {
            var processor =
                ServerConfig.ApiDb.Query<Processor>("SELECT a.Id, b.Account FROM `manufacture_processor` a JOIN `processor` b ON a.ProcessorId = b.Id " +
                                                    "WHERE b.Account = @account AND a.`MarkedDelete` = 0;", new { account }).FirstOrDefault();
            if (processor == null)
            {
                return Result.GenError<DataResult>(Error.ManufactureProcessorNotExist);
            }

            var pId = processor.Id;
            var result = new DataResult();
            var sql =
                "SELECT a.*, IFNULL(b.Plan, '') Plan, IFNULL(c.ProcessorName, '') Processor, IFNULL(d.Module, '') Module, IFNULL(e.`Check`, '') `Check`, IFNULL(f.ProcessorName, a.Assignor) Assignor FROM manufacture_plan_task a " +
                "LEFT JOIN `manufacture_plan` b ON a.PlanId = b.Id " +
                "LEFT JOIN (SELECT a.*, b.ProcessorName FROM `manufacture_processor` a JOIN `processor` b ON a.ProcessorId = b.Id WHERE a.MarkedDelete = 0) c ON a.Person = c.Id " +
                "LEFT JOIN `manufacture_task_module` d ON a.ModuleId = d.Id " +
                "LEFT JOIN `manufacture_check` e ON a.CheckId = e.Id " +
                "LEFT JOIN `processor` f ON a.Assignor = f.Account " +
                "WHERE a.State IN @state AND a.Person = @pId AND a.MarkedDelete = 0 AND a.IsCheck = 1 ORDER BY a.`TotalOrder` LIMIT 2;";
            var tasks = new List<ManufacturePlanTask>();
            foreach (var validState in ValidStates)
            {
                tasks.AddRange(ServerConfig.ApiDb.Query<ManufacturePlanTask>(sql, new { state = validState.Value, pId }));
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
                    "SELECT a.*, IFNULL(b.ProcessorName, a.Person) Processor FROM `manufacture_plan_task` a " +
                    "LEFT JOIN (SELECT a.*, b.ProcessorName FROM `manufacture_processor` a JOIN `processor` b ON a.ProcessorId = b.Id ) b ON a.Person = b.Id " +
                    "WHERE PlanId = @PlanId AND `Order` = @Relation;";
                var preTask = ServerConfig.ApiDb.Query<ManufacturePlanTask>(sql, new { first.PlanId, first.Relation }).FirstOrDefault();
                if (preTask != null)
                {
                    first.CheckTask = preTask.Item;
                    first.CheckProcessor = preTask.Processor;
                }

                first.Items = first.IsCheckItem ?
                    ServerConfig.ApiDb.Query<ManufacturePlanCheckItem>("SELECT * FROM `manufacture_plan_check_item` WHERE PlanId = @PlanId AND ItemId = @Id AND MarkedDelete = 0;", new { first.PlanId, first.Id })
                    : ServerConfig.ApiDb.Query<ManufacturePlanCheckItem>("SELECT * FROM `manufacture_check_item` WHERE CheckId = @CheckId AND MarkedDelete = 0;", new { first.CheckId });
                result.datas.Add(first);
                return result;
            }

            return Result.GenError<DataResult>(Error.ManufactureNoTask);
        }

        /// <summary>
        /// 获取待检验
        /// </summary>
        /// <param name="account"></param>
        /// <param name="limit">记录条数</param>
        /// <returns></returns>
        // GET: api/ManufactureTaskWorkSpace?qId=0&item=false
        [HttpGet("WaitCheck")]
        public DataResult GetManufactureWaitCheck([FromQuery] string account, int limit = 10)
        {
            var processor =
                ServerConfig.ApiDb.Query<Processor>("SELECT a.Id, b.Account FROM `manufacture_processor` a JOIN `processor` b ON a.ProcessorId = b.Id " +
                                                    "WHERE b.Account = @account AND a.`MarkedDelete` = 0;", new { account }).FirstOrDefault();
            if (processor == null)
            {
                return Result.GenError<DataResult>(Error.ManufactureProcessorNotExist);
            }

            var pId = processor.Id;
            var result = new DataResult();
            var sql =
                "SELECT a.*, IFNULL(b.Plan, '') Plan FROM manufacture_plan_task a " +
                "LEFT JOIN `manufacture_plan` b ON a.PlanId = b.Id WHERE a.State = @state AND a.Person = @pId AND a.MarkedDelete = 0 AND a.IsCheck = 1 ORDER BY a.`TotalOrder` LIMIT @limit;";
            result.datas.AddRange(ServerConfig.ApiDb.Query<ManufacturePlanTask>(sql, new { state = ManufacturePlanItemState.WaitCheck, pId, limit }));
            return result;
        }

        /// <summary>
        /// 获取已通过检验
        /// </summary>
        /// <param name="account"></param>
        /// <param name="limit">记录条数</param>
        /// <returns></returns>
        // GET: api/ManufactureTaskWorkSpace?qId=0&item=false
        [HttpGet("PassCheck")]
        public DataResult GetManufacturePassCheck([FromQuery] string account, int limit = 10)
        {
            var processor =
                ServerConfig.ApiDb.Query<Processor>("SELECT a.Id, b.Account FROM `manufacture_processor` a JOIN `processor` b ON a.ProcessorId = b.Id " +
                                                    "WHERE b.Account = @account AND a.`MarkedDelete` = 0;", new { account }).FirstOrDefault();
            if (processor == null)
            {
                return Result.GenError<DataResult>(Error.ManufactureProcessorNotExist);
            }

            var pId = processor.Id;
            var result = new DataResult();
            var sql =
                "SELECT a.*, IFNULL(b.Plan, '') Plan FROM manufacture_plan_task a " +
                "LEFT JOIN `manufacture_plan` b ON a.PlanId = b.Id WHERE a.State = @state AND a.CheckResult = @check AND a.Person = @pId AND a.MarkedDelete = 0 AND a.IsCheck = 1 ORDER BY a.`TotalOrder` DESC LIMIT @limit;";
            result.datas.AddRange(ServerConfig.ApiDb.Query<ManufacturePlanTask>(sql, new { state = ManufacturePlanItemState.Done, check = ManufacturePlanCheckState.Pass, pId, limit }));
            return result;
        }

        /// <summary>
        /// 获取返工检验
        /// </summary>
        /// <param name="account"></param>
        /// <param name="limit">记录条数</param>
        /// <returns></returns>
        // GET: api/ManufactureTaskWorkSpace?qId=0&item=false
        [HttpGet("RedoCheck")]
        public DataResult GetManufactureRedoCheck([FromQuery] string account, int limit = 10)
        {
            var processor =
                ServerConfig.ApiDb.Query<Processor>("SELECT a.Id, b.Account FROM `manufacture_processor` a JOIN `processor` b ON a.ProcessorId = b.Id " +
                                                    "WHERE b.Account = @account AND a.`MarkedDelete` = 0;", new { account }).FirstOrDefault();
            if (processor == null)
            {
                return Result.GenError<DataResult>(Error.ManufactureProcessorNotExist);
            }

            var pId = processor.Id;
            var result = new DataResult();
            var sql =
                "SELECT a.*, IFNULL(b.Plan, '') Plan FROM manufacture_plan_task a " +
                "LEFT JOIN `manufacture_plan` b ON a.PlanId = b.Id WHERE a.CheckResult = @check AND a.Person = @pId AND a.MarkedDelete = 0 AND a.IsCheck = 1 ORDER BY a.`TotalOrder` DESC LIMIT @limit;";
            result.datas.AddRange(ServerConfig.ApiDb.Query<ManufacturePlanTask>(sql, new { check = ManufacturePlanCheckState.Redo, pId, limit }));
            return result;
        }

        /// <summary>
        /// 获取阻塞检验
        /// </summary>
        /// <param name="account"></param>
        /// <param name="limit">记录条数</param>
        /// <returns></returns>
        // GET: api/ManufactureTaskWorkSpace?qId=0&item=false
        [HttpGet("BlockCheck")]
        public DataResult GetManufactureBlockCheck([FromQuery] string account, int limit = 10)
        {
            var processor =
                ServerConfig.ApiDb.Query<Processor>("SELECT a.Id, b.Account FROM `manufacture_processor` a JOIN `processor` b ON a.ProcessorId = b.Id " +
                                                    "WHERE b.Account = @account AND a.`MarkedDelete` = 0;", new { account }).FirstOrDefault();
            if (processor == null)
            {
                return Result.GenError<DataResult>(Error.ManufactureProcessorNotExist);
            }

            var pId = processor.Id;
            var result = new DataResult();
            var sql =
                "SELECT a.*, IFNULL(b.Plan, '') Plan FROM manufacture_plan_task a " +
                "LEFT JOIN `manufacture_plan` b ON a.PlanId = b.Id WHERE a.State = @state AND a.CheckResult = @check AND a.Person = @pId AND a.MarkedDelete = 0 AND a.IsCheck = 1 ORDER BY a.`TotalOrder` DESC LIMIT @limit;";
            result.datas.AddRange(ServerConfig.ApiDb.Query<ManufacturePlanTask>(sql, new { state = ManufacturePlanItemState.Done, check = ManufacturePlanCheckState.Block, pId, limit }));
            return result;
        }


        /// <summary>
        /// 开始任务
        /// </summary>
        /// <param name="tId">任务id</param>
        /// <param name="account"></param>
        /// <returns></returns>
        // POST: api/ManufactureTaskWorkSpace
        [HttpPost("Start")]
        public Result StartManufactureCheck([FromBody]ManufactureOpTask opTask)
        {
            var tId = opTask.TaskId;
            var account = opTask.Account;
            var processor =
                ServerConfig.ApiDb.Query<Processor>("SELECT a.Id, b.Account FROM `manufacture_processor` a JOIN `processor` b ON a.ProcessorId = b.Id " +
                                                    "WHERE b.Account = @account AND a.`MarkedDelete` = 0;", new { account }).FirstOrDefault();
            if (processor == null)
            {
                return Result.GenError<Result>(Error.ManufactureProcessorNotExist);
            }

            var pId = processor.Id;
            var sql =
                "SELECT b.*, a.* FROM manufacture_plan_task a " +
                "LEFT JOIN `manufacture_plan` b ON a.PlanId = b.Id " +
                "WHERE a.State IN @state AND a.Person = @pId AND a.MarkedDelete = 0 AND a.IsCheck = 1 ORDER BY a.`TotalOrder` LIMIT 1;";
            ManufacturePlanTask task = null;
            foreach (var validState in ValidStates)
            {
                task = ServerConfig.ApiDb.Query<ManufacturePlanTask>(sql, new { state = validState.Value, pId })
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
                if (task.State != ManufacturePlanItemState.WaitCheck && task.State != ManufacturePlanItemState.Pause)
                {
                    return Result.GenError<DataResult>(Error.ManufactureTaskStateError);
                }

                var changes = new List<ManufactureLog>();
                var now = DateTime.Now;
                var oldTask = (ManufacturePlanTask)task.Clone();
                task.State = ManufacturePlanItemState.Checking;
                if (!task.IsRedo)
                {
                    task.FirstStartTime = now.NoSecond();
                }
                else
                {
                    task.RedoCount++;
                }
                task.ActualStartTime = now.NoSecond();
                if (oldTask.HaveChange(task, out var change))
                {
                    change.Time = now;
                    change.Account = processor.Account;
                    change.PlanId = task.PlanId;
                    change.IsAssign = true;
                    change.TaskId = task.TaskId;
                    change.ItemId = task.Id;
                    change.Type = ManufactureLogType.StartTask;
                    changes.Add(change);
                }
                if (!oldTask.IsCheckItem)
                {
                    task.IsCheckItem = true;
                    var manufacturePlanCheckItems =
                        ServerConfig.ApiDb.Query<ManufacturePlanCheckItem>("SELECT * FROM `manufacture_check_item` WHERE CheckId = @CheckId AND MarkedDelete = 0;", new { task.CheckId });

                    var createUserId = Request.GetIdentityInformation();
                    var markedDateTime = DateTime.Now;
                    if (manufacturePlanCheckItems.Any())
                    {
                        foreach (var manufactureCheckItem in manufacturePlanCheckItems)
                        {
                            manufactureCheckItem.CreateUserId = createUserId;
                            manufactureCheckItem.PlanId = oldTask.PlanId;
                            manufactureCheckItem.ItemId = oldTask.Id;
                        }
                        ServerConfig.ApiDb.Execute(
                            "INSERT INTO manufacture_plan_check_item (`CreateUserId`, `PlanId`, `ItemId`, `Item`, `Method`) " +
                            "VALUES (@CreateUserId, @PlanId, @ItemId, @Item, @Method);",
                            manufacturePlanCheckItems);

                        changes.Add(new ManufactureLog
                        {
                            Time = markedDateTime,
                            Account = createUserId,
                            PlanId = task.PlanId,
                            IsAssign = true,
                            TaskId = task.TaskId,
                            ItemId = task.Id,
                            Type = ManufactureLogType.CheckAssigned
                        });
                    }
                }

                ServerConfig.ApiDb.Execute("UPDATE manufacture_plan_task SET `State` = @State, `IsCheckItem` = @IsCheckItem, `FirstStartTime` = @FirstStartTime, " +
                                           "`ActualStartTime` = @ActualStartTime, `RedoCount` = @RedoCount WHERE `Id` = @Id;", task);
                ManufactureLog.AddLog(changes);
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
        public Result PauseManufactureCheck([FromBody]ManufactureOpTask opTask)
        {
            var tId = opTask.TaskId;
            var account = opTask.Account;
            var processor =
                ServerConfig.ApiDb.Query<Processor>("SELECT a.Id, b.Account FROM `manufacture_processor` a JOIN `processor` b ON a.ProcessorId = b.Id " +
                                                    "WHERE b.Account = @account AND a.`MarkedDelete` = 0;", new { account }).FirstOrDefault();
            if (processor == null)
            {
                return Result.GenError<Result>(Error.ManufactureProcessorNotExist);
            }

            var pId = processor.Id;
            var sql =
                "SELECT b.*, a.* FROM manufacture_plan_task a " +
                "LEFT JOIN `manufacture_plan` b ON a.PlanId = b.Id " +
                "WHERE a.State IN @state AND a.Person = @pId AND a.MarkedDelete = 0 AND a.IsCheck = 1 ORDER BY a.`TotalOrder` LIMIT 1;";
            ManufacturePlanTask task = null;
            foreach (var validState in ValidStates)
            {
                task = ServerConfig.ApiDb.Query<ManufacturePlanTask>(sql, new { state = validState.Value, pId })
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
                if (task.State != ManufacturePlanItemState.Checking)
                {
                    return Result.GenError<DataResult>(Error.ManufactureTaskStateError);
                }

                var now = DateTime.Now;
                var oldTask = (ManufacturePlanTask)task.Clone();
                task.State = ManufacturePlanItemState.Pause;
                task.PauseTime = now.NoSecond();
                var totalSecond = (int)(task.PauseTime - task.ActualStartTime).TotalSeconds;
                task.ActualHour += totalSecond / 3600;
                task.ActualMin += (totalSecond - task.ActualHour * 3600) / 60;
                if (oldTask.HaveChange(task, out var change))
                {
                    change.Time = now;
                    change.Account = processor.Account;
                    change.PlanId = task.PlanId;
                    change.IsAssign = true;
                    change.TaskId = task.TaskId;
                    change.ItemId = task.Id;
                    change.Type = ManufactureLogType.PauseTask;
                }

                ServerConfig.ApiDb.Execute("UPDATE manufacture_plan_task SET `State` = @State, `PauseTime` = @PauseTime, " +
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
        /// <param name="checkResult">检验结果</param>
        /// <returns></returns>
        // POST: api/ManufactureTaskWorkSpace
        [HttpPost("Finish")]
        public Result FinishManufactureCheck([FromBody]ManufactureOpCheckTask opTask)
        {
            var tId = opTask.TaskId;
            var account = opTask.Account;
            var checkResult = opTask.CheckResult;
            if (!EnumHelper.TryParseInt(checkResult, out ManufacturePlanCheckState result))
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            var processor =
                ServerConfig.ApiDb.Query<Processor>("SELECT a.Id, b.Account FROM `manufacture_processor` a JOIN `processor` b ON a.ProcessorId = b.Id " +
                                                    "WHERE b.Account = @account AND a.`MarkedDelete` = 0;", new { account }).FirstOrDefault();
            if (processor == null)
            {
                return Result.GenError<Result>(Error.ManufactureProcessorNotExist);
            }

            var pId = processor.Id;
            var sql =
                "SELECT b.*, a.* FROM manufacture_plan_task a " +
                "LEFT JOIN `manufacture_plan` b ON a.PlanId = b.Id " +
                "WHERE a.State IN @state AND a.Person = @pId AND a.MarkedDelete = 0 AND a.IsCheck = 1 ORDER BY a.`TotalOrder` LIMIT 1;";
            ManufacturePlanTask task = null;
            foreach (var validState in ValidStates)
            {
                task = ServerConfig.ApiDb.Query<ManufacturePlanTask>(sql, new { state = validState.Value, pId })
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
                if (task.State != ManufacturePlanItemState.Checking)
                {
                    return Result.GenError<DataResult>(Error.ManufactureTaskStateError);
                }

                var changes = new List<ManufactureLog>();
                var now = DateTime.Now;
                var oldTask = (ManufacturePlanTask)task.Clone();
                task.State = ManufacturePlanItemState.Done;
                task.ActualEndTime = now.NoSecond();
                task.CheckResult = result;
                if (task.PauseTime != default(DateTime))
                {
                    var totalSecond = (int)(task.PauseTime - task.ActualStartTime).TotalSeconds;
                    task.ActualHour += totalSecond / 3600;
                    task.ActualMin += (totalSecond - task.ActualHour * 3600) / 60;
                    task.PauseTime = default(DateTime);
                }
                else
                {
                    var totalSecond = (int)(task.ActualEndTime - task.ActualStartTime).TotalSeconds;
                    task.ActualHour += totalSecond / 3600;
                    task.ActualMin += (totalSecond - task.ActualHour * 3600) / 60;
                }
                if (oldTask.HaveChange(task, out var change))
                {
                    change.Time = now;
                    change.Account = processor.Account;
                    change.PlanId = task.PlanId;
                    change.IsAssign = true;
                    change.TaskId = task.TaskId;
                    change.ItemId = task.Id;
                    change.Type = ManufactureLogType.UpdateCheckResult;
                    changes.Add(change);
                }

                if (result == ManufacturePlanCheckState.Redo)
                {
                    sql =
                        "SELECT * FROM `manufacture_plan_task` WHERE PlanId = @PlanId AND Order = @Relation;";
                    var preTask = ServerConfig.ApiDb.Query<ManufacturePlanTask>(sql, new { task.PlanId, task.Relation }).FirstOrDefault();
                    if (preTask != null)
                    {
                        var oldPreTask = (ManufacturePlanTask)preTask.Clone();
                        preTask.State = ManufacturePlanItemState.WaitRedo;
                        if (oldPreTask.HaveChange(preTask, out var changePre))
                        {
                            changePre.Time = now;
                            changePre.Account = processor.Account;
                            changePre.PlanId = oldPreTask.PlanId;
                            changePre.IsAssign = true;
                            changePre.TaskId = oldPreTask.TaskId;
                            changePre.ItemId = oldPreTask.Id;
                            changes.Add(changePre);
                        }
                    }

                    ServerConfig.ApiDb.Execute("UPDATE manufacture_plan_task SET `State` = @State WHERE `Id` = @Id;", preTask);
                }

                ServerConfig.ApiDb.Execute("UPDATE manufacture_plan_task SET `State` = @State, `ActualEndTime` = @ActualEndTime, `PauseTime` = @PauseTime, " +
                                           "`ActualHour` = @ActualHour, `ActualMin` = @ActualMin, `CheckResult` = @CheckResult WHERE `Id` = @Id;", task);

                ManufactureLog.AddLog(changes);
                return Result.GenError<DataResult>(Error.Success);
            }

            return Result.GenError<DataResult>(Error.ManufactureNoTask);
        }

        /// <summary>
        /// 检验任务
        /// </summary>
        /// <param name="item">检验任务</param>
        /// <returns></returns>
        // POST: api/ManufactureTaskWorkSpace
        [HttpPut("Check")]
        public Result ManufactureCheck([FromBody] ManufacturePlanCheckItem item)
        {
            if (item.Id == 0)
            {
                return Result.GenError<DataResult>(Error.ManufactureCheckItemNotExist);
            }
            var account = Request.GetIdentityInformation();
            var now = DateTime.Now;
            var processor =
                ServerConfig.ApiDb.Query<Processor>("SELECT a.Id, b.Account FROM `manufacture_processor` a JOIN `processor` b ON a.ProcessorId = b.Id " +
                                                    "WHERE b.Account = @account AND a.`MarkedDelete` = 0;", new { account }).FirstOrDefault();
            if (processor == null)
            {
                return Result.GenError<Result>(Error.ManufactureProcessorNotExist);
            }

            var pId = processor.Id;
            var sql =
                "SELECT * FROM manufacture_plan_task WHERE Id = @ItemId AND Person = @pId AND MarkedDelete = 0 AND IsCheck = 1 ORDER BY `TotalOrder`;";
            var task = ServerConfig.ApiDb.Query<ManufacturePlanTask>(sql, new { item.ItemId, pId })
                .FirstOrDefault();
            if (task != null)
            {
                if (task.State != ManufacturePlanItemState.Checking)
                {
                    return Result.GenError<DataResult>(Error.ManufactureTaskStateError);
                }

                var changes = new List<ManufactureLog>();
                var oldItem =
                    ServerConfig.ApiDb.Query<ManufacturePlanCheckItem>("SELECT * FROM `manufacture_plan_check_item` WHERE Id = @Id AND MarkedDelete = 0;",
                        new { item.Id }).FirstOrDefault();

                if (oldItem == null)
                {
                    return Result.GenError<DataResult>(Error.ManufactureCheckItemNotExist);
                }

                item.CheckTime = item.CheckTime == default(DateTime) ? oldItem.CheckTime : item.CheckTime;
                item.Desc = item.Desc ?? oldItem.Desc;
                item.Result = item.Result == ManufacturePlanCheckItemState.Wait && oldItem.Result != ManufacturePlanCheckItemState.Wait ? oldItem.Result : item.Result;
                item.Images = item.Images ?? oldItem.Images;
                //oldItem.CheckTime = now;
                if (oldItem.HaveChange(item, out var change))
                {
                    change.Time = now;
                    change.Account = processor.Account;
                    change.PlanId = task.PlanId;
                    change.IsAssign = true;
                    change.TaskId = task.TaskId;
                    change.ItemId = task.Id;
                    change.Type = ManufactureLogType.UpdateCheckItem;
                    changes.Add(change);
                }

                if (changes.Any())
                {
                    ServerConfig.ApiDb.Execute(
                        oldItem.Images == item.Images
                            ? "UPDATE manufacture_plan_check_item SET `CheckTime` = @CheckTime, `Result` = @Result, `Desc` = @Desc WHERE `Id` = @Id;"
                            : "UPDATE manufacture_plan_check_item SET `Images` = @Images WHERE `Id` = @Id;",
                        item);
                    ManufactureLog.AddLog(changes);
                }
                return Result.GenError<DataResult>(Error.Success);
            }

            return Result.GenError<DataResult>(Error.ManufactureNoTask);
        }

        /// <summary>
        /// 检验任务详情
        /// </summary>
        /// <param name="qId">检验任务Id</param>
        /// <returns></returns>
        // Get: api/ManufactureTaskWorkSpace
        [HttpGet("Detail")]
        public DataResult ManufactureCheckDetail([FromQuery] int qId)
        {
            if (qId == 0)
            {
                return Result.GenError<DataResult>(Error.ManufactureTaskItemNotExist);
            }
            var account = Request.GetIdentityInformation();
            var now = DateTime.Now;
            var processor =
                ServerConfig.ApiDb.Query<Processor>("SELECT a.Id, b.Account FROM `manufacture_processor` a JOIN `processor` b ON a.ProcessorId = b.Id " +
                                                    "WHERE b.Account = @account AND a.`MarkedDelete` = 0;", new { account }).FirstOrDefault();
            if (processor == null)
            {
                return Result.GenError<DataResult>(Error.ManufactureProcessorNotExist);
            }

            var pId = processor.Id;
            var sql =
                "SELECT * FROM manufacture_plan_task WHERE Id = @qId AND Person = @pId AND MarkedDelete = 0 AND IsCheck = 1 ORDER BY `TotalOrder`;";
            var task = ServerConfig.ApiDb.Query<ManufacturePlanTask>(sql, new { qId, pId })
                .FirstOrDefault();
            var result = new DataResult();
            if (task != null)
            {
                IEnumerable<ManufacturePlanCheckItem> checkItems;
                if (task.IsCheckItem)
                {
                    checkItems = ServerConfig.ApiDb.Query<ManufacturePlanCheckItem>("SELECT * FROM `manufacture_plan_check_item` WHERE ItemId = @ItemId AND MarkedDelete = 0;",
                        new { ItemId = qId });
                }
                else
                {
                    checkItems =
                       ServerConfig.ApiDb.Query<ManufacturePlanCheckItem>("SELECT * FROM `manufacture_check_item` WHERE CheckId = @CheckId AND MarkedDelete = 0;",
                           new { task.CheckId });
                }
                result.datas.AddRange(checkItems);
            }
            return result;
        }
    }
}