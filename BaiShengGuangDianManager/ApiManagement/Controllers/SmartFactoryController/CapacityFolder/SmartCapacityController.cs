using ApiManagement.Base.Helper;
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
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]"), ApiController]
    public class SmartCapacityController : ControllerBase
    {
        /// <summary>
        /// GET: api/SmartCapacity
        /// </summary>
        /// <param name="qId">设备型号ID</param>
        /// <param name="cId">设备类型ID</param>
        /// <param name="wId">车间ID</param>
        /// <param name="menu">是否菜单</param>
        /// <returns></returns>
        [HttpGet]
        public DataResult GetSmartCapacity([FromQuery]int qId, int cId, int wId, bool menu)
        {
            var result = new DataResult();
            result.datas.AddRange(menu
                ? SmartCapacityHelper.GetMenu(qId, cId, wId)
                : SmartCapacityHelper.GetDetail(qId, cId, wId));
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.SmartCapacityNotExist;
                return result;
            }
            return result;
        }

        // PUT: api/SmartCapacity
        [HttpPut]
        public object PutSmartCapacity([FromBody] IEnumerable<SmartCapacityDetail> capacities)
        {
            if (capacities == null || !capacities.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (capacities.Any(x => x.Capacity.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SmartCapacityNotEmpty);
            }
            if (capacities.GroupBy(x => x.Category).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.SmartCapacityDuplicate);
            }

            var wId = capacities.FirstOrDefault()?.WorkshopId ?? 0;
            var cId = capacities.FirstOrDefault()?.CategoryId ?? 0;
            var sames = capacities.Select(x => x.Capacity);
            var ids = capacities.Select(x => x.Id);
            if (SmartCapacityHelper.GetHaveSame(wId, cId, sames, ids))
            {
                return Result.GenError<Result>(Error.SmartCapacityIsExist);
            }

            var cnt = SmartCapacityHelper.Instance.GetCountByIds(ids);
            if (cnt != capacities.Count())
            {
                return Result.GenError<Result>(Error.SmartCapacityNotExist);
            }

            var markedDateTime = DateTime.Now;
            foreach (var capacity in capacities)
            {
                capacity.MarkedDateTime = markedDateTime;
                capacity.Remark = capacity.Remark ?? "";
            }
            SmartCapacityHelper.Instance.Update(capacities);
            return Result.GenError<Result>(Error.Success);
        }

        // PUT: api/SmartCapacity
        [HttpPut("List")]
        public object PutSmartCapacityList([FromBody] OpSmartCapacity capacity)
        {
            var capacityId = capacity.Id;
            var oldCapacity = SmartCapacityHelper.Instance.Get<SmartCapacity>(capacityId);
            if (oldCapacity == null)
            {
                return Result.GenError<Result>(Error.SmartCapacityNotExist);
            }

            var capacityLists = capacity.List;
            if (capacityLists == null)
            {
                return Result.GenError<Result>(Error.SmartCapacityListNotSet);
            }

            var processes = SmartProcessCodeCategoryProcessHelper.GetDetailByCategoryId(capacity.CategoryId);
            if (!processes.Any())
            {
                return Result.GenError<Result>(Error.SmartProcessCodeCategoryProcessNotExist);
            }

            if (capacityLists.Count() != processes.Count())
            {
                return Result.GenError<Result>(Error.SmartCapacityListCountError);
            }

            if (capacityLists.Any(x => !x.IsSet()))
            {
                return Result.GenError<Result>(Error.SmartCapacityListNotEmpty);
            }

            var smartCapacityListIds = capacityLists.Select(x => x.Id).Where(y => y != 0);
            if (smartCapacityListIds.Any())
            {
                var data = SmartCapacityListHelper.Instance.GetByIds<SmartCapacityList>(smartCapacityListIds);
                if (data.Count() != smartCapacityListIds.Count())
                {
                    return Result.GenError<Result>(Error.SmartCapacityListNotExist);
                }

            }
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            if (oldCapacity.CategoryId != capacity.CategoryId)
            {
                capacity.MarkedDateTime = markedDateTime;
                SmartCapacityHelper.UpdateCategoryId(capacity);
            }

            foreach (var smartCapacityList in capacityLists)
            {
                smartCapacityList.CreateUserId = createUserId;
                smartCapacityList.MarkedDateTime = markedDateTime;
                smartCapacityList.CapacityId = capacityId;
            }
            var oldSmartCapacityLists = SmartCapacityListHelper.GetSmartCapacityLists(capacityId);
            //删除 
            var delete = oldSmartCapacityLists.Where(z => capacityLists.Where(x => x.Id != 0).All(y => y.Id != z.Id));
            if (delete.Any())
            {
                SmartCapacityListHelper.Instance.Delete(delete.Select(x => x.Id));
            }
            //更新 
            var update = capacityLists.Where(x => x.Id != 0);
            if (update.Any())
            {
                SmartCapacityListHelper.Instance.Update(update);
            }

            //新增
            var add = capacityLists.Where(x => x.Id == 0);
            if (add.Any())
            {
                SmartCapacityListHelper.Instance.Add(add);
            }

            WorkFlowHelper.Instance.OnSmartCapacityListChanged(capacityLists);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartCapacity
        [HttpPost]
        public object PostSmartCapacity([FromBody] IEnumerable<OpSmartCapacity> capacities)
        {
            if (capacities == null || !capacities.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (capacities.Any(x => x.Capacity.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SmartCapacityNotEmpty);
            }
            if (capacities.GroupBy(x => x.Capacity).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.SmartCapacityDuplicate);
            }

            var wId = capacities.FirstOrDefault()?.WorkshopId ?? 0;
            var cId = capacities.FirstOrDefault()?.CategoryId ?? 0;
            var sames = capacities.Select(x => x.Capacity);
            if (SmartCapacityHelper.GetHaveSame(wId, cId, sames))
            {
                return Result.GenError<Result>(Error.SmartCapacityIsExist);
            }

            var categoryIds = capacities.Select(x => x.CategoryId);
            if (categoryIds.Any(x => x == 0))
            {
                return Result.GenError<Result>(Error.SmartProcessCodeCategoryNotExist);
            }

            if (capacities.Any(x => !x.List.Any() || x.List.Any(y => !y.IsSet())))
            {
                return Result.GenError<Result>(Error.SmartCapacityListNotEmpty);
            }

            var userId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var capacity in capacities)
            {
                capacity.CreateUserId = userId;
                capacity.MarkedDateTime = markedDateTime;
                capacity.Remark = capacity.Remark ?? "";
            }
            SmartCapacityHelper.Instance.Add(capacities);
            var capacityList = SmartCapacityHelper.GetSmartCapacities(capacities.Select(x => x.Capacity));
            foreach (var smartCapacity in capacities)
            {
                var capacity = capacityList.FirstOrDefault(x => x.Capacity == smartCapacity.Capacity);
                if (capacity != null)
                {
                    foreach (var l in smartCapacity.List)
                    {
                        l.CapacityId = capacity.Id;
                        l.CreateUserId = userId;
                        l.MarkedDateTime = markedDateTime;
                    }
                }
            }

            var add = capacities.SelectMany(x => x.List).Where(y => y.CapacityId != 0);
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
            SmartCapacityListHelper.Instance.DeleteFromParent(ids);
            return Result.GenError<Result>(Error.Success);
        }
    }
}