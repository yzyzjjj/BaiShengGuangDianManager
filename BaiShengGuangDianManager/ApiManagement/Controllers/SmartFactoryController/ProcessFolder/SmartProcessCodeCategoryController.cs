using ApiManagement.Models.SmartFactoryModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using ModelBase.Models.BaseModel;

namespace ApiManagement.Controllers.SmartFactoryController.ProcessFolder
{
    /// <summary>
    /// 
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]"), ApiController]
    public class SmartProcessCodeCategoryController : ControllerBase
    {
        /// <summary>
        /// GET: api/SmartProcessCodeCategory
        /// </summary>
        /// <param name="qId">流程编号类型ID</param>
        /// <param name="wId">车间ID</param>
        /// <param name="menu">是否菜单</param>
        /// <returns></returns>
        [HttpGet]
        public DataResult GetSmartProcessCodeCategory([FromQuery]int qId, int wId, bool menu)
        {
            var result = new DataResult();
            result.datas.AddRange(menu
                ? SmartProcessCodeCategoryHelper.GetMenu(qId, wId)
                : SmartProcessCodeCategoryHelper.GetDetail(qId, wId));
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.SmartProcessCodeCategoryNotExist;
                return result;
            }
            return result;
        }

        // PUT: api/SmartProcessCodeCategory
        [HttpPut]
        public Result PutSmartProcessCodeCategory([FromBody] IEnumerable<SmartProcessCodeCategoryDetail> processCodeCategories)
        {
            if (processCodeCategories == null || !processCodeCategories.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (processCodeCategories.Any(x => x.Category.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SmartProcessCodeCategoryNotEmpty);
            }
            if (processCodeCategories.GroupBy(x => x.Category).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.SmartProcessCodeCategoryDuplicate);
            }

            var wId = processCodeCategories.FirstOrDefault()?.WorkshopId ?? 0;
            var sames = processCodeCategories.Select(x => x.Category);
            var ids = processCodeCategories.Select(x => x.Id);
            if (SmartProcessCodeCategoryHelper.GetHaveSame(wId, sames, ids))
            {
                return Result.GenError<Result>(Error.SmartProcessCodeCategoryIsExist);
            }

            var cnt = SmartProcessCodeCategoryHelper.Instance.GetCountByIds(ids);
            if (cnt != processCodeCategories.Count())
            {
                return Result.GenError<Result>(Error.SmartProcessCodeCategoryNotExist);
            }
            var processCodeCategoryIds = processCodeCategories.Select(x => x.Id);
            var data = SmartProcessCodeCategoryHelper.Instance.GetByIds<SmartProcessCodeCategory>(processCodeCategoryIds);
            if (data.Count() != processCodeCategories.Count())
            {
                return Result.GenError<Result>(Error.SmartProcessCodeCategoryNotExist);
            }

            var userId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            var add = new List<SmartProcessCodeCategoryProcess>();
            var update = new List<SmartProcessCodeCategoryProcess>();
            var exist = SmartProcessCodeCategoryProcessHelper.GetDetailByCategoryId(processCodeCategories.Select(x => x.Id));
            foreach (var processCodeCategory in processCodeCategories)
            {
                processCodeCategory.CreateUserId = userId;
                processCodeCategory.MarkedDateTime = markedDateTime;
                processCodeCategory.Remark = processCodeCategory.Remark ?? "";
                processCodeCategory.Processes = processCodeCategory.Processes.Select(x =>
                {
                    x.ProcessCodeCategoryId = processCodeCategory.Id;
                    return x;
                });
                var categoryProcesses = exist.Where(x => x.ProcessCodeCategoryId == processCodeCategory.Id);
                if (processCodeCategory.Processes != null && processCodeCategory.Processes.Any())
                {
                    add.AddRange(processCodeCategory.Processes.Where(x => x.Id == 0 && categoryProcesses.FirstOrDefault(a => a.Order == x.Order && a.ProcessId == x.ProcessId) == null)
                        .Select(y =>
                          {
                              y.CreateUserId = userId;
                              y.MarkedDateTime = markedDateTime;
                              y.ProcessCodeCategoryId = processCodeCategory.Id;
                              return y;
                          }));

                    update.AddRange(processCodeCategory.Processes
                        .Where(x => categoryProcesses.Any(y => y.Id == x.Id)
                                    && (ClassExtension.HaveChange(categoryProcesses.First(y => y.Id == x.Id), x))).Select(z =>
                                    //|| (x.Id == 0 && categoryProcesses.FirstOrDefault(a => a.Order == x.Order && a.ProcessId == x.ProcessId) != null)).Select(z =>
                                    {
                                        var first = categoryProcesses.First(a => a.Id == z.Id);
                                        z.Id = first.Id;
                                        z.MarkedDateTime = markedDateTime;
                                        return z;
                                    }));

                    update.AddRange(categoryProcesses.Where(x => processCodeCategory.Processes.All(y => y.Id != x.Id)).Select(z =>
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
                SmartProcessCodeCategoryProcessHelper.Instance.Add<SmartProcessCodeCategoryProcess>(add);
            }

            if (update.Any())
            {
                SmartProcessCodeCategoryProcessHelper.Instance.Update<SmartProcessCodeCategoryProcess>(update);
            }

            SmartProcessCodeCategoryHelper.Instance.Update(processCodeCategories);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartProcessCodeCategory
        [HttpPost]
        public Result PostSmartProcessCodeCategory([FromBody] IEnumerable<SmartProcessCodeCategoryDetail> processCodeCategories)
        {
            if (processCodeCategories == null || !processCodeCategories.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (processCodeCategories.Any(x => x.Category.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SmartProcessCodeCategoryNotEmpty);
            }

            var wId = processCodeCategories.FirstOrDefault()?.WorkshopId ?? 0;
            var sames = processCodeCategories.Select(x => x.Category);
            if (SmartProcessCodeCategoryHelper.GetHaveSame(wId, sames))
            {
                return Result.GenError<Result>(Error.SmartProcessCodeCategoryDuplicate);
            }

            var userId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var processCodeCategory in processCodeCategories)
            {
                processCodeCategory.CreateUserId = userId;
                processCodeCategory.MarkedDateTime = markedDateTime;
                processCodeCategory.Remark = processCodeCategory.Remark ?? "";
            }
            SmartProcessCodeCategoryHelper.Instance.Add(processCodeCategories);

            var newCategories = processCodeCategories.Select(x => x.Category);
            var categories = SmartProcessCodeCategoryHelper.GetDetail(newCategories);

            var processes = processCodeCategories.SelectMany(x => x.Processes);
            if (processes.Any())
            {
                foreach (var processCodeCategory in processCodeCategories)
                {
                    if (processCodeCategory.Processes != null && processCodeCategory.Processes.Any())
                    {
                        var categoryId = categories.FirstOrDefault(x => x.Category == processCodeCategory.Category)?.Id ?? 0;
                        foreach (var process in processCodeCategory.Processes)
                        {
                            process.CreateUserId = userId;
                            process.MarkedDateTime = markedDateTime;
                            process.ProcessCodeCategoryId = categoryId;
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