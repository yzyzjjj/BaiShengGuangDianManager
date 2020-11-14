using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using ApiManagement.Models.SmartFactoryModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.SmartFactoryController.ProcessFolder
{
    /// <summary>
    /// 
    /// </summary>
    [Route("api/[controller]"), ApiController]
    public class SmartProcessCodeCategoryController : ControllerBase
    {
        // GET: api/SmartProcessCodeCategory
        [HttpGet]
        public DataResult GetSmartProcessCodeCategory([FromQuery]int qId, bool menu)
        {
            var result = new DataResult();
            var sql = menu ? $"SELECT Id, `Category` FROM `t_process_code_category` WHERE MarkedDelete = 0{(qId == 0 ? "" : " AND Id = @qId")};"
                : $"SELECT a.*, IFNULL(b.List, '') List FROM `t_process_code_category` a LEFT JOIN (SELECT ProcessCodeCategoryId, GROUP_CONCAT(Process) List FROM `t_process_code_category_process` a JOIN `t_process` b ON a.ProcessId = b.Id WHERE a.MarkedDelete = 0 GROUP BY ProcessCodeCategoryId) b ON a.Id = b.ProcessCodeCategoryId WHERE MarkedDelete = 0{(qId == 0 ? "" : " AND a.Id = @qId")};";
            result.datas.AddRange(menu
                ? ServerConfig.ApiDb.Query<dynamic>(sql, new { qId })
                : ServerConfig.ApiDb.Query<SmartProcessCodeCategory>(sql, new { qId }));
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.SmartProcessCodeCategoryNotExist;
                return result;
            }
            return result;
        }

        // PUT: api/SmartProcessCodeCategory
        [HttpPut]
        public Result PutSmartProcessCodeCategory([FromBody] IEnumerable<SmartProcessCodeCategoryDetail> smartProcessCodeCategories)
        {
            if (smartProcessCodeCategories == null || !smartProcessCodeCategories.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            var smartProcessCodeCategoryIds = smartProcessCodeCategories.Select(x => x.Id);
            var data = SmartProcessCodeCategoryHelper.Instance.GetByIds<SmartProcessCodeCategory>(smartProcessCodeCategoryIds);
            if (data.Count() != smartProcessCodeCategories.Count())
            {
                return Result.GenError<Result>(Error.SmartProcessCodeCategoryNotExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            var add = new List<SmartProcessCodeCategoryProcess>();
            var update = new List<SmartProcessCodeCategoryProcess>();
            var exist =
                ServerConfig.ApiDb.Query<SmartProcessCodeCategoryProcess>("SELECT* FROM `t_process_code_category_process` WHERE MarkedDelete = 0 AND ProcessCodeCategoryId IN @Id;",
                    new { Id = smartProcessCodeCategories.Select(x => x.Id) });
            foreach (var smartProcessCodeCategory in smartProcessCodeCategories)
            {

                smartProcessCodeCategory.CreateUserId = createUserId;
                smartProcessCodeCategory.MarkedDateTime = markedDateTime;
                var categoryProcesses = exist.Where(x => x.ProcessCodeCategoryId == smartProcessCodeCategory.Id);
                if (smartProcessCodeCategory.Processes != null && smartProcessCodeCategory.Processes.Any())
                {
                    add.AddRange(smartProcessCodeCategory.Processes.Where(x => x.Id == 0).Select(y =>
                    {
                        y.CreateUserId = createUserId;
                        y.MarkedDateTime = markedDateTime;
                        y.ProcessCodeCategoryId = smartProcessCodeCategory.Id;
                        return y;
                    }));

                    update.AddRange(smartProcessCodeCategory.Processes.Where(x => categoryProcesses.Any(y => y.Id == x.Id)).Select(z =>
                    {
                        z.MarkedDateTime = markedDateTime;
                        return z;
                    }));

                    update.AddRange(categoryProcesses.Where(x => smartProcessCodeCategory.Processes.Any(y => y.Id == x.Id)).Select(z =>
                    {
                        z.MarkedDateTime = markedDateTime;
                        z.MarkedDelete = true;
                        return z;
                    }));

                }
                else
                {
                    update.AddRange(categoryProcesses.Select(x =>
                    {
                        x.MarkedDateTime = markedDateTime;
                        x.MarkedDelete = true;
                        return x;
                    }));
                }
            }
            if (add.Any())
            {
                ServerConfig.ApiDb.Execute(
                    "INSERT INTO `t_process_code_category_process` (`CreateUserId`, `MarkedDateTime`, `ProcessCodeCategoryId`, `Order`, `ProcessId`) " +
                    "VALUES (@CreateUserId, @MarkedDateTime, @ProcessCodeCategoryId, @Order, @ProcessId);",
                    add);
            }

            if (update.Any())
            {
                ServerConfig.ApiDb.Execute(
                    "UPDATE `t_process_code_category_process` SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ProcessCodeCategoryId` = @ProcessCodeCategoryId, `Order` = @Order, `ProcessId` = @ProcessId WHERE `Id` = @Id;", update);
            }

            SmartProcessCodeCategoryHelper.Instance.Update(smartProcessCodeCategories);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartProcessCodeCategory
        [HttpPost]
        public Result PostSmartProcessCodeCategory([FromBody] IEnumerable<SmartProcessCodeCategoryDetail> smartProcessCodeCategories)
        {
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartProcessCodeCategory in smartProcessCodeCategories)
            {
                smartProcessCodeCategory.CreateUserId = createUserId;
                smartProcessCodeCategory.MarkedDateTime = markedDateTime;
            }
            SmartProcessCodeCategoryHelper.Instance.Add(smartProcessCodeCategories);

            var newCategories = smartProcessCodeCategories.Select(x => x.Category);
            var categories = SmartProcessCodeCategoryHelper.Instance.GetSmartProcessCodeCategoriesByCategories(newCategories);

            var processes = smartProcessCodeCategories.SelectMany(x => x.Processes);
            if (processes.Any())
            {
                foreach (var smartProcessCodeCategory in smartProcessCodeCategories)
                {
                    if (smartProcessCodeCategory.Processes != null && smartProcessCodeCategory.Processes.Any())
                    {
                        var categoryId = categories.FirstOrDefault(x => x.Category == smartProcessCodeCategory.Category)?.Id ?? 0;
                        foreach (var process in smartProcessCodeCategory.Processes)
                        {
                            process.ProcessCodeCategoryId = categoryId;
                            process.CreateUserId = createUserId;
                            process.MarkedDateTime = markedDateTime;
                        }
                    }
                }
                SmartProcessCodeCategoryProcessHelper.Instance.Add(processes);
            }

            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/SmartProcessCodeCategory
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteSmartProcessCodeCategory([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var cnt = SmartProcessCodeCategoryHelper.Instance.GetCountByIds(ids);
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.SmartProcessCodeCategoryNotExist);
            }
            SmartProcessCodeCategoryHelper.Instance.Delete(ids);
            return Result.GenError<Result>(Error.Success);
        }
    }
}