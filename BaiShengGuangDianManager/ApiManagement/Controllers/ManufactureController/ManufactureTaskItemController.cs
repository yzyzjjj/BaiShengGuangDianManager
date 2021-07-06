using ApiManagement.Base.Server;
using ApiManagement.Models.ManufactureModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.BaseModel;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.ManufactureController
{
    /// <summary>
    /// 生产任务配置
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class ManufactureTaskItemController : ControllerBase
    {
        /// <summary>
        ///  获取任务单配置项
        /// </summary>
        /// <param name="taskId">检验单id</param>
        /// <returns></returns>
        // GET: api/ManufactureTaskItem?qId=0
        [HttpGet]
        public DataResult GetManufactureTaskItem([FromQuery] int taskId)
        {
            var result = new DataResult();
            var data = ServerConfig.ApiDb.Query<ManufactureTaskItemDetail>("SELECT a.*, b.GroupId, b.`Group`, b.Processor, c.Module FROM `manufacture_task_item` a " +
                                                                           "JOIN ( SELECT a.*, b.Name Processor, c.`Group` FROM `manufacture_processor` a JOIN `accounts` b ON a.ProcessorId = b.Id JOIN `manufacture_group` c ON a.GroupId = c.Id ) b ON a.Person = b.Id " +
                                                                           "JOIN `manufacture_task_module` c ON " +
                                                                           "a.ModuleId = c.Id WHERE a.TaskId = @taskId AND a.`MarkedDelete` = 0 ORDER BY a.`Order`;", new { taskId });
            result.datas.AddRange(data);
            return result;
        }
        /// <summary>
        /// 更新单个任务配置单
        /// </summary>
        /// <param name="manufactureTaskItems"></param>
        /// <returns></returns>
        // PUT: api/ManufactureTaskItem
        [HttpPut]
        public DataResult PutManufactureTaskItem([FromBody] IEnumerable<ManufactureTaskItem> manufactureTaskItems)
        {
            if (manufactureTaskItems == null)
            {
                return Result.GenError<DataResult>(Error.ManufactureTaskItemNotExist);
            }

            if (manufactureTaskItems.Any(x => x.TaskId == 0) || manufactureTaskItems.GroupBy(x => x.TaskId).Count() != 1)
            {
                return Result.GenError<DataResult>(Error.ManufactureTaskNotExist);
            }

            var taskId = manufactureTaskItems.First(x => x.TaskId != 0).TaskId;
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `manufacture_task` WHERE Id = @Id AND `MarkedDelete` = 0;", new { Id = taskId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<DataResult>(Error.ManufactureTaskNotExist);
            }

            if (manufactureTaskItems.Any(x => x.Item.IsNullOrEmpty()))
            {
                return Result.GenError<DataResult>(Error.ManufactureTaskItemNotEmpty);
            }

            var data =
                ServerConfig.ApiDb.Query<ManufactureTaskItem>("SELECT * FROM `manufacture_task_item` WHERE TaskId = @taskId AND MarkedDelete = 0;", new { taskId });

            var update = false;
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            if (manufactureTaskItems.GroupBy(x => x.Order).Any(y => y.Count() > 1))
            {
                return Result.GenError<DataResult>(Error.ManufactureTaskItemOrderDuplicate);
            }

            var result = new DataResult();
            manufactureTaskItems = manufactureTaskItems.OrderBy(x => x.Order);
            var error = 0;
            foreach (var manufactureTaskItem in manufactureTaskItems)
            {
                manufactureTaskItem.TaskId = taskId;
                if (error != 2 && manufactureTaskItem.Order <= manufactureTaskItem.Relation
                    || (manufactureTaskItem.Relation != 0 && manufactureTaskItems.All(x => x.Order != manufactureTaskItem.Relation)))
                {
                    error = 1;
                    result.datas.Add(manufactureTaskItem.Item);
                }
                else if (error != 1 && manufactureTaskItem.IsCheck && manufactureTaskItem.Relation == 0)
                {
                    error = 2;
                    result.datas.Add(manufactureTaskItem.Item);
                }
                if (error != 0)
                {
                    continue;
                }

                var item = data.FirstOrDefault(x => x.Id == manufactureTaskItem.Id);
                if (item != null)
                {
                    manufactureTaskItem.Item = manufactureTaskItem.Item ?? item.Item;
                    manufactureTaskItem.Desc = manufactureTaskItem.Desc ?? item.Desc;
                    if (manufactureTaskItem.Item != item.Item || manufactureTaskItem.Desc != item.Desc || manufactureTaskItem.Order != item.Order
                        || manufactureTaskItem.Person != item.Person || manufactureTaskItem.ModuleId != item.ModuleId || manufactureTaskItem.EstimatedHour != item.EstimatedHour
                        || manufactureTaskItem.EstimatedMin != item.EstimatedMin || manufactureTaskItem.Score != item.Score || manufactureTaskItem.Relation != item.Relation)
                    {
                        update = true;
                        manufactureTaskItem.MarkedDateTime = markedDateTime;
                    }

                    if (manufactureTaskItem.IsCheck != item.IsCheck)
                    {
                        update = true;
                        manufactureTaskItem.MarkedDateTime = markedDateTime;
                    }

                    if (manufactureTaskItem.IsCheck && manufactureTaskItem.CheckId != item.CheckId)
                    {
                        update = true;
                        manufactureTaskItem.MarkedDateTime = markedDateTime;
                    }
                }
                else
                {
                    manufactureTaskItem.CreateUserId = createUserId;
                    manufactureTaskItem.MarkedDateTime = markedDateTime;
                    manufactureTaskItem.Desc = manufactureTaskItem.Desc ?? "";
                }
            }

            if (result.datas.Any())
            {
                result.errno = error == 1 ? Error.ManufactureTaskItemRelationError : Error.ManufactureCheckItemNoRelation;
                return result;
            }

            #region 更新
            var updateItems = manufactureTaskItems.Where(x => x.Id != 0 && data.Any(y => y.Id == x.Id) && ClassExtension.HaveChange(data.First(y => y.Id == x.Id), x));
            if (updateItems.Any() && update)
            {
                ServerConfig.ApiDb.Execute("UPDATE manufacture_task_item SET `MarkedDateTime` = @MarkedDateTime, `Order` = @Order, `Person` = @Person, `ModuleId` = @ModuleId, `IsCheck` = @IsCheck, " +
                                           "`CheckId` = @CheckId, `Item` = @Item, `EstimatedHour` = @EstimatedHour, `EstimatedMin` = @EstimatedMin, `Score` = @Score, `Desc` = @Desc, `Relation` = @Relation WHERE `Id` = @Id;", updateItems);
            }
            #endregion

            #region 删除
            var delItems = data.Where(x => manufactureTaskItems.All(y => y.Id != x.Id));
            if (delItems.Any())
            {
                foreach (var delItem in delItems)
                {
                    delItem.MarkedDateTime = markedDateTime;
                    delItem.MarkedDelete = true;
                }

                ServerConfig.ApiDb.Execute("UPDATE `manufacture_task_item` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id` = @Id;", delItems);
            }

            #endregion

            #region 添加
            var addItems = manufactureTaskItems.Where(x => x.Id == 0);
            if (addItems.Any())
            {
                ServerConfig.ApiDb.Execute(
                    "INSERT INTO manufacture_task_item (`CreateUserId`, `MarkedDateTime`, `TaskId`, `Order`, `Person`, `ModuleId`, `IsCheck`, `CheckId`, `Item`, `EstimatedHour`, `EstimatedMin`, `Score`, `Desc`, `Relation`) " +
                    "VALUES (@CreateUserId, @MarkedDateTime, @TaskId, @Order, @Person, @ModuleId, @IsCheck, @CheckId, @Item, @EstimatedHour, @EstimatedMin, @Score, @Desc, @Relation);",
                    addItems);
            }
            #endregion
            return Result.GenError<DataResult>(Error.Success);
        }

        // POST: api/ManufactureTaskItem
        //[HttpPost]
        //public Result PostManufactureTaskItem([FromBody] IEnumerable<ManufactureTaskItem> manufactureTaskItems)
        //{
        //if (manufactureTaskItems == null)
        //{
        //    return Result.GenError<Result>(Error.ManufactureTaskItemNotExist);
        //}

        //if (manufactureTaskItems.Any(x => x.Item.IsNullOrEmpty()))
        //{
        //    return Result.GenError<Result>(Error.ManufactureTaskItemNotEmpty);
        //}
        //var checkIdList = manufactureTaskItems.GroupBy(x => x.CheckId).Select(y => y.Key);
        //var cnt =
        //    ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `manufacture_task` WHERE Id IN @checkIdList AND `MarkedDelete` = 0;", new { checkIdList }).FirstOrDefault();
        //if (cnt != checkIdList.Count())
        //{
        //    return Result.GenError<Result>(Error.ManufactureTaskNotExist);
        //}

        //var createUserId = Request.GetIdentityInformation();
        //var markedDateTime = DateTime.Now;
        //foreach (var manufactureTaskItem in manufactureTaskItems)
        //{
        //    manufactureTaskItem.CreateUserId = createUserId;
        //    manufactureTaskItem.MarkedDateTime = markedDateTime;
        //}
        //ServerConfig.ApiDb.Execute(
        //    "INSERT INTO manufacture_task_item (`CreateUserId`, `MarkedDateTime`, `CheckId`, `Item`, `Method`) VALUES (@CreateUserId, @MarkedDateTime, @CheckId, @Item, @Method);",
        //    manufactureTaskItems);

        //    return Result.GenError<DataResult>(Error.Success);
        //}

        // DELETE: api/ManufactureTaskItem
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteManufactureTaskItem([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var items =
                ServerConfig.ApiDb.Query<ManufactureTaskItem>("SELECT * FROM `manufacture_task_item` WHERE Id IN @id AND `MarkedDelete` = 0;", new { id = ids });
            if (items.Count() != ids.Count())
            {
                return Result.GenError<Result>(Error.ManufactureTaskItemNotExist);
            }

            var taskIds = items.Select(x => x.TaskId).Distinct();
            var tasks =
                ServerConfig.ApiDb.Query<ManufactureTaskItem>("SELECT * FROM `manufacture_task_item` WHERE TaskId IN @taskIds AND MarkedDelete = 0;", new { taskIds });
            var update = new List<ManufactureTaskItem>();
            var time = DateTime.Now;
            foreach (var taskId in taskIds)
            {
                var order = 1;
                var oldToNew = new Dictionary<int, int>();
                var ts = tasks.Where(x => x.TaskId == taskId).OrderBy(x => x.Order);
                foreach (var t in ts)
                {
                    t.MarkedDateTime = time;
                    if (ids.Contains(t.Id))
                    {
                        t.MarkedDelete = true;
                        update.Add(t);
                        order--;
                    }
                    else if (t.Order != order)
                    {
                        if (!oldToNew.ContainsKey(t.Order))
                        {
                            oldToNew.Add(t.Order, order);
                        }
                        t.Order = order;
                        update.Add(t);
                    }
                    order++;

                    if (!t.MarkedDelete && oldToNew.ContainsKey(t.Relation))
                    {
                        t.Relation = oldToNew[t.Relation];
                    }
                }
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `manufacture_task_item` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete, `Order` = @Order, `Relation` = @Relation WHERE `Id` = @Id;", update);

            return Result.GenError<Result>(Error.Success);
        }
    }
}