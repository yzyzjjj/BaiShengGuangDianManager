using ApiManagement.Base.Helper;
using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using ApiManagement.Models.SmartFactoryModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.SmartFactoryController.CapacityFolder
{
    /// <summary>
    /// 
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]"), ApiController]
    public class SmartCapacityController : ControllerBase
    {
        // GET: api/SmartCapacity
        [HttpGet]
        public DataResult GetSmartCapacity([FromQuery]int qId, int categoryId, bool menu)
        {
            var result = new DataResult();
            var sql = menu ? $"SELECT a.Id, `Capacity`, Category FROM t_capacity a JOIN t_process_code_category b ON a.CategoryId = b.Id " +
                             $"WHERE a.MarkedDelete = 0{(qId == 0 ? "" : " AND a.Id = @qId")}{(categoryId == 0 ? "" : " AND a.CategoryId = @categoryId")};"
                : $"SELECT a.*, b.Category FROM t_capacity a JOIN t_process_code_category b ON a.CategoryId = b.Id " +
                  $"WHERE a.MarkedDelete = 0{(qId == 0 ? "" : " AND a.Id = @qId")}{(categoryId == 0 ? "" : " AND a.CategoryId = @categoryId")};";
            result.datas.AddRange(menu
                ? ServerConfig.ApiDb.Query<dynamic>(sql, new { qId, categoryId })
                : ServerConfig.ApiDb.Query<SmartCapacityDetail>(sql, new { qId, categoryId }));
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.SmartCapacityNotExist;
                return result;
            }
            return result;
        }

        // PUT: api/SmartCapacity
        [HttpPut]
        public object PutSmartCapacity([FromBody] IEnumerable<SmartCapacityDetail> smartCapacities)
        {
            if (smartCapacities == null || !smartCapacities.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            if (!smartCapacities.Any(x => x.Capacity.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SmartProcessNotEmpty);
            }
            var smartCapacityIds = smartCapacities.Select(x => x.Id);
            var data = SmartDeviceHelper.Instance.GetByIds<SmartCapacity>(smartCapacityIds);
            if (data.Count() != smartCapacities.Count())
            {
                return Result.GenError<Result>(Error.SmartCapacityNotExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartCapacity in smartCapacities)
            {
                smartCapacity.CreateUserId = createUserId;
                smartCapacity.MarkedDateTime = markedDateTime;
            }
            SmartCapacityHelper.Instance.Update(smartCapacities);
            return Result.GenError<Result>(Error.Success);
        }

        // PUT: api/SmartCapacity
        [HttpPut("List")]
        public object PutSmartCapacityList([FromBody] OpSmartCapacity smartCapacity)
        {
            var capacityId = smartCapacity.Id;
            var capacity = SmartCapacityHelper.Instance.Get<SmartCapacity>(capacityId);
            if (capacity == null)
            {
                return Result.GenError<Result>(Error.SmartCapacityNotExist);
            }

            var smartCapacityLists = smartCapacity.List;
            if (smartCapacityLists == null || !smartCapacityLists.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            var processes = SmartProcessCodeCategoryProcessHelper.Instance
                .GetSmartProcessCodeCategoryProcessesByProcessCodeCategoryId(capacity.CategoryId);
            if (!processes.Any())
            {
                return Result.GenError<Result>(Error.SmartProcessCodeCategoryProcessNotExist);
            }

            if (smartCapacityLists.Count() != processes.Count())
            {
                return Result.GenError<Result>(Error.SmartCapacityListCountError);
            }

            if (smartCapacityLists.Any(x => !x.IsSet()))
            {
                return Result.GenError<Result>(Error.SmartCapacityListNotEmpty);
            }

            var smartCapacityListIds = smartCapacityLists.Select(x => x.Id);
            var data = SmartCapacityListHelper.Instance.GetByIds<SmartCapacityList>(smartCapacityListIds);
            if (data.Count() != smartCapacityLists.Count())
            {
                return Result.GenError<Result>(Error.SmartCapacityListNotExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            if (capacity.CategoryId != smartCapacity.CategoryId)
            {
                capacity.MarkedDateTime = markedDateTime;
                SmartCapacityHelper.Instance.UpdateSmartCapacity(capacity);
            }

            foreach (var smartCapacityList in smartCapacityLists)
            {
                smartCapacityList.CreateUserId = createUserId;
                smartCapacityList.MarkedDateTime = markedDateTime;
                smartCapacityList.CapacityId = capacityId;
            }
            var oldSmartCapacityLists = SmartCapacityListHelper.Instance.GetSmartCapacityLists(capacityId);
            //删除 
            var delete = oldSmartCapacityLists.Where(z => smartCapacityLists.Where(x => x.Id != 0).All(y => y.Id != z.Id));
            if (delete.Any())
            {
                SmartCapacityListHelper.Instance.Delete(delete.Select(x => x.Id));
            }
            //更新 
            var update = smartCapacityLists.Where(x => x.Id != 0);
            if (update.Any())
            {
                SmartCapacityListHelper.Instance.Update(update);
            }

            //新增
            var add = smartCapacityLists.Where(x => x.Id == 0);
            if (add.Any())
            {
                SmartCapacityListHelper.Instance.Add(add);
            }

            WorkFlowHelper.Instance.OnSmartCapacityListChanged(smartCapacityLists);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartCapacity
        [HttpPost]
        public object PostSmartCapacity([FromBody] IEnumerable<OpSmartCapacity> smartCapacities)
        {
            if (smartCapacities == null || !smartCapacities.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            var result = new DataResult();
            var capacities = smartCapacities.Select(x => x.Capacity);
            if (capacities.Any(x => x.IsNullOrEmpty()))
            {
                result.errno = Error.SmartCapacityNotEmpty;
                result.datas.AddRange(smartCapacities.Where(x => x.Capacity.IsNullOrEmpty()).Select(y => y.Capacity));
                return result;
            }

            var categoryIds = smartCapacities.Select(x => x.CategoryId);
            if (categoryIds.Any(x => x == 0))
            {
                result.errno = Error.SmartProcessCodeCategoryNotExist;
                result.datas.AddRange(smartCapacities.Where(x => categoryIds.Any(y => y == x.CategoryId)).Select(z => z.Capacity));
                return result;
            }

            if (smartCapacities.Any(x => !x.List.Any() || x.List.Any(y => !y.IsSet())))
            {
                result.errno = Error.SmartCapacityListNotEmpty;
                result.datas.AddRange(smartCapacities.Where(x => x.List.Any() || x.List.Any(y => !y.IsSet())).Select(y => y.Capacity));
                return result;
            }

            if (smartCapacities.Count() != capacities.GroupBy(x => x).Count())
            {
                return Result.GenError<Result>(Error.SmartCapacityDuplicate);
            }

            var data = SmartCapacityHelper.Instance.GetSmartCapacities(capacities);
            if (data.Any())
            {
                result.errno = Error.SmartCapacityDuplicate;
                result.datas.AddRange(data.Select(x => x.Capacity));
                return result;
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartCapacity in smartCapacities)
            {
                smartCapacity.CreateUserId = createUserId;
                smartCapacity.MarkedDateTime = markedDateTime;
            }
            SmartCapacityHelper.Instance.Add(smartCapacities);
            var capacityList = SmartCapacityHelper.Instance.GetSmartCapacities(capacities);
            foreach (var smartCapacity in smartCapacities)
            {
                var capacity = capacityList.FirstOrDefault(x => x.Capacity == smartCapacity.Capacity);
                if (capacity != null)
                {
                    foreach (var l in smartCapacity.List)
                    {
                        l.CapacityId = capacity.Id;
                        l.CreateUserId = createUserId;
                        l.MarkedDateTime = markedDateTime;
                    }
                }
            }

            var add = smartCapacities.SelectMany(x => x.List).Where(y => y.CapacityId != 0);
            SmartCapacityListHelper.Instance.Add(add);
            WorkFlowHelper.Instance.OnSmartCapacityListChanged(add);
            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/SmartCapacity
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteSmartCapacity([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var cnt = SmartCapacityHelper.Instance.GetCountByIds(ids);
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.SmartCapacityNotExist);
            }
            SmartCapacityHelper.Instance.Delete(ids);
            SmartCapacityListHelper.Instance.DeleteByCapacityId(ids);
            return Result.GenError<Result>(Error.Success);
        }
    }
}