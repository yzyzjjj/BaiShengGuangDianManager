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

namespace ApiManagement.Controllers.ManufactureController
{
    /// <summary>
    /// 生产任务配置单
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class ManufactureTaskController : ControllerBase
    {
        /// <summary>
        /// 获取任务列表
        /// </summary>
        /// <param name="qId"></param>
        /// <param name="menu">下拉框</param>
        /// <param name="time">下拉框</param>
        /// <returns></returns>
        // GET: api/ManufactureTask?qId=0&item=false
        [HttpGet]
        public DataResult GetManufactureTask([FromQuery] int qId, bool menu, bool time)
        {
            var result = new DataResult();
            string sql;
            if (menu)
            {
                sql = time
                    ? $"SELECT Id, `Task`, IFNULL(b.EstimatedHour, 0) EstimatedHour, IFNULL(b.EstimatedMin, 0) EstimatedMin FROM `manufacture_task` a LEFT JOIN(SELECT TaskId, SUM(EstimatedHour) EstimatedHour, SUM(EstimatedMin) EstimatedMin FROM `manufacture_task_item` GROUP BY TaskId) b ON a.Id = b.TaskId WHERE {(qId == 0 ? "" : "Id = @qId AND ")}`MarkedDelete` = 0;"
                    : $"SELECT Id, `Task` FROM `manufacture_task` WHERE {(qId == 0 ? "" : "Id = @qId AND ")}`MarkedDelete` = 0;";
                var data = ServerConfig.ApiDb.Query<dynamic>(sql, new { qId });
                result.datas.AddRange(data);
            }
            else
            {
                sql = $"SELECT * FROM `manufacture_task` WHERE {(qId == 0 ? "" : "Id = @qId AND ")}`MarkedDelete` = 0;";
                var data = ServerConfig.ApiDb.Query<ManufactureTask>(sql, new { qId });
                result.datas.AddRange(data);

            }
            if (qId != 0 && !result.datas.Any())
            {
                return Result.GenError<DataResult>(Error.ManufactureTaskNotExist);
            }
            return result;
        }

        // PUT: api/ManufactureTask
        [HttpPut]
        public Result PutManufactureTask([FromBody] ManufactureTask manufactureTask)
        {
            if (manufactureTask.Id == 0)
            {
                return Result.GenError<Result>(Error.ManufactureTaskNotExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            if (manufactureTask.Task != null)
            {
                var manufactureTaskOld =
                    ServerConfig.ApiDb.Query<ManufactureTask>("SELECT * FROM `manufacture_task` WHERE Id = @Id AND MarkedDelete = 0;",
                        new { manufactureTask.Id }).FirstOrDefault();
                if (manufactureTaskOld == null)
                {
                    return Result.GenError<Result>(Error.ManufactureTaskNotExist);
                }

                manufactureTask.Task = manufactureTask.Task ?? manufactureTaskOld.Task;
                if (manufactureTaskOld.Task != manufactureTask.Task)
                {
                    if (manufactureTask.Task.IsNullOrEmpty())
                    {
                        return Result.GenError<Result>(Error.ManufactureTaskNotEmpty);
                    }
                    manufactureTask.MarkedDateTime = markedDateTime;
                    ServerConfig.ApiDb.Execute(
                        "UPDATE manufacture_task SET `MarkedDateTime` = @MarkedDateTime, `Task` = @Task WHERE `Id` = @Id;", manufactureTask);
                }
            }

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/ManufactureTask
        [HttpPost]
        public Result PostManufactureTask([FromBody] ManufactureTaskCopy manufactureTask)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `manufacture_task` WHERE `Task` = @Task AND MarkedDelete = 0;",
                    new { manufactureTask.Task }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.ManufactureTaskIsExist);
            }
            IEnumerable<ManufactureTaskItem> manufactureTaskItems = null;
            if (manufactureTask.CopyId != 0)
            {
                cnt =
                    ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `manufacture_task` WHERE `Id` = @CopyId AND MarkedDelete = 0;",
                        new { manufactureTask.CopyId }).FirstOrDefault();
                if (cnt == 0)
                {
                    return Result.GenError<Result>(Error.ManufactureTaskNotExist);
                }
                manufactureTaskItems =
                    ServerConfig.ApiDb.Query<ManufactureTaskItem>("SELECT * FROM `manufacture_task_item` WHERE TaskId = @CopyId AND `MarkedDelete` = 0;", new { manufactureTask.CopyId });
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            manufactureTask.CreateUserId = createUserId;
            manufactureTask.MarkedDateTime = markedDateTime;
            manufactureTask.Task = manufactureTask.Task ?? "";
            manufactureTask.Id = ServerConfig.ApiDb.Query<int>(
                "INSERT INTO manufacture_task (`CreateUserId`, `MarkedDateTime`, `Task`) VALUES (@CreateUserId, @MarkedDateTime, @Task);SELECT LAST_INSERT_ID();", manufactureTask).FirstOrDefault();

            if (manufactureTaskItems != null && manufactureTaskItems.Any())
            {
                manufactureTaskItems = manufactureTaskItems.OrderBy(x => x.Order);
                var oldToNew = new Dictionary<int, int>();
                var i = 0;
                foreach (var item in manufactureTaskItems)
                {
                    oldToNew.Add(item.Order, ++i);
                    item.Order = oldToNew[item.Order];
                    if (item.Relation != 0)
                    {
                        item.Relation = oldToNew[item.Relation];
                    }
                }

                i = 0;
                foreach (var manufactureTaskItem in manufactureTaskItems)
                {
                    manufactureTaskItem.CreateUserId = createUserId;
                    manufactureTaskItem.MarkedDateTime = markedDateTime;
                    manufactureTaskItem.TaskId = manufactureTask.Id;
                    manufactureTaskItem.Order = ++i;
                }
                ServerConfig.ApiDb.Execute(
                    "INSERT INTO manufacture_task_item (`CreateUserId`, `MarkedDateTime`, `TaskId`, `Order`, `Person`, `ModuleId`, `IsCheck`, `CheckId`, `Item`, `EstimatedHour`, `EstimatedMin`, `Score`, `Desc`, `Relation`) " +
                    "VALUES (@CreateUserId, @MarkedDateTime, @TaskId, @Order, @Person, @ModuleId, @IsCheck, @CheckId, @Item, @EstimatedHour, @EstimatedMin, @Score, @Desc, @Relation);",
                    manufactureTaskItems);
            }
            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/ManufactureTask
        /// <summary>
        /// 单个删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete("{id}")]
        public Result DeleteManufactureTask([FromRoute] int id)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `manufacture_task` WHERE Id = @Id AND `MarkedDelete` = 0;", new { Id = id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ManufactureTaskNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `manufacture_task` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id` = @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });

            ServerConfig.ApiDb.Execute(
                "UPDATE `manufacture_task_item` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `TaskId` = @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}