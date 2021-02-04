using ApiManagement.Base.Helper;
using ApiManagement.Models.BaseModel;
using ApiManagement.Models.SmartFactoryModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.SmartFactoryController.CapacityFolder
{
    /// <summary>
    /// 
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]"), ApiController]
    public class SmartCapacityListController : ControllerBase
    {
        // GET: api/SmartCapacityList
        /// <summary>
        /// 
        /// </summary>
        /// <param name="qId"></param>
        /// <param name="capacityId">产能类型id</param>
        /// <param name="categoryId">流程编号类型id</param>
        /// <returns></returns>
        [HttpGet]
        public DataResult GetSmartCapacityList([FromQuery]int qId, int capacityId, int categoryId)
        {
            var result = new DataResult();
            result.datas.AddRange(SmartCapacityListHelper.GetDetail(qId, capacityId, categoryId));
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.SmartCapacityListNotExist;
                return result;
            }
            return result;
        }

        // GET: api/SmartCapacityList
        /// <summary>
        /// 
        /// </summary>
        /// <param name="qId">产能清单设置id</param>
        /// <param name="processId">标准流程id</param>
        /// <returns></returns>
        [HttpGet("Info")]
        public object GetSmartCapacityListInfo([FromQuery]int qId, int processId)
        {
            var result = new DataResult();
            var deviceCategoryId = 0;
            SmartCapacityListDetail capacityList = null;
            if (qId != 0)
            {
                capacityList = SmartCapacityListHelper.GetDetail(qId);
                if (capacityList == null)
                {
                    return Result.GenError<Result>(Error.SmartCapacityListNotExist);
                }
                var capacity = SmartCapacityHelper.Instance.Get<SmartCapacity>(capacityList.CapacityId);
                if (capacity == null)
                {
                    return Result.GenError<Result>(Error.SmartCapacityNotExist);
                }

                deviceCategoryId = capacityList.CategoryId;
            }
            else if (processId != 0)
            {
                var process = SmartProcessCodeCategoryProcessHelper.GetDetailByProcessId(processId);
                if (process == null)
                {
                    return Result.GenError<Result>(Error.SmartProcessCodeCategoryProcessNotExist);
                }

                capacityList = new SmartCapacityListDetail
                {
                    ProcessId = process.Id
                };
                deviceCategoryId = process.DeviceCategoryId;
            }
            else
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            capacityList.PId = SmartProcessCodeCategoryProcessHelper.Instance.Get<SmartProcessCodeCategoryProcess>(capacityList.ProcessId)?.ProcessId ?? 0;
            var actDevices = new List<SmartDeviceCapacity>();
            if (deviceCategoryId != 0)
            {
                var models = SmartDeviceModelHelper.GetDetail(0, deviceCategoryId);
                var devices = capacityList.DeviceList;
                if (models.Any())
                {
                    var modelCount = SmartDeviceHelper.GetNormalDeviceModelCount(models.Select(x => x.Id));
                    foreach (var model in models)
                    {
                        var device = devices.FirstOrDefault(x => x.ModelId == model.Id) ?? new SmartDeviceCapacity();
                        device.Category = model.Category;
                        device.CategoryId = model.CategoryId;
                        device.ModelId = model.Id;
                        device.Model = model.Model;
                        device.Count = modelCount.FirstOrDefault(x => x.ModelId == model.Id) != null
                            ? modelCount.FirstOrDefault(x => x.ModelId == model.Id).Count : 0;
                        actDevices.Add(device);
                    }
                }
            }

            var actOperators = new List<SmartOperatorCapacity>();
            var levels = SmartOperatorLevelHelper.Instance.GetAll<SmartOperatorLevel>().OrderBy(x => x.Order);
            if (levels.Any())
            {
                var operatorCount = SmartOperatorHelper.GetNormalOperatorCount(capacityList.ProcessId);
                var operators = capacityList.OperatorList;
                if (levels.Any())
                {
                    foreach (var level in levels)
                    {
                        var op = operators.FirstOrDefault(x => x.LevelId == level.Id) ?? new SmartOperatorCapacity();
                        op.Level = level.Level;
                        op.LevelId = level.Id;
                        op.Count = operatorCount.FirstOrDefault(x => x.ProcessId == capacityList.PId && x.LevelId == op.LevelId) != null
                            ? operatorCount.FirstOrDefault(x => x.ProcessId == capacityList.PId && x.LevelId == level.Id).Count : 0;
                        actOperators.Add(op);
                    }
                }
            }

            result.datas.Add(capacityList);
            return new
            {
                errno = 0,
                errmsg = "成功",
                Devices = actDevices,
                Operators = actOperators
            };
        }

        // PUT: api/SmartCapacityList
        [HttpPut]
        public Result PutSmartCapacityList([FromBody] IEnumerable<SmartCapacityList> capacityLists)
        {
            if (capacityLists == null || !capacityLists.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            if (capacityLists.Any(x => !x.IsSet()))
            {
                return Result.GenError<Result>(Error.SmartCapacityListNotEmpty);
            }

            var capacityId = capacityLists.FirstOrDefault()?.CapacityId ?? 0;
            if (capacityId == 0)
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            var capacity = SmartCapacityHelper.Instance.Get<SmartCapacity>(capacityId);
            if (capacity == null)
            {
                return Result.GenError<Result>(Error.SmartCapacityNotExist);
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

            var capacityListIds = capacityLists.Select(x => x.Id);
            var data = SmartCapacityListHelper.Instance.GetByIds<SmartCapacityList>(capacityListIds);
            if (data.Count() != capacityLists.Count())
            {
                return Result.GenError<Result>(Error.SmartCapacityListNotExist);
            }

            if (capacityLists.Count() != processes.Count())
            {
                return Result.GenError<Result>(Error.SmartCapacityListCountError);
            }

            var userId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var capacityList in capacityLists)
            {
                capacityList.CreateUserId = userId;
                capacityList.MarkedDateTime = markedDateTime;
                capacityList.CapacityId = capacityId;
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

        // POST: api/SmartCapacityList
        [HttpPost]
        public Result PostSmartCapacityList([FromBody] IEnumerable<SmartCapacityList> smartCapacityLists)
        {
            if (smartCapacityLists == null || !smartCapacityLists.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (smartCapacityLists.Any(x => !x.IsSet()))
            {
                return Result.GenError<Result>(Error.SmartCapacityListNotEmpty);
            }

            var capacityId = smartCapacityLists.FirstOrDefault()?.CapacityId ?? 0;
            if (capacityId == 0)
            {
                return Result.GenError<Result>(Error.SmartCapacityNotExist);
            }

            var capacity = SmartCapacityHelper.Instance.Get<SmartCapacity>(capacityId);
            if (capacity == null)
            {
                return Result.GenError<Result>(Error.SmartCapacityNotExist);
            }

            var processes = SmartProcessCodeCategoryProcessHelper.GetDetailByCategoryId(capacity.CategoryId);
            if (!processes.Any())
            {
                return Result.GenError<Result>(Error.SmartProcessCodeCategoryProcessNotExist);
            }

            if (smartCapacityLists.Count() != processes.Count())
            {
                return Result.GenError<Result>(Error.SmartCapacityListCountError);
            }

            var userId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartCapacityList in smartCapacityLists)
            {
                smartCapacityList.CreateUserId = userId;
                smartCapacityList.MarkedDateTime = markedDateTime;
                smartCapacityList.CapacityId = capacityId;
            }
            SmartCapacityListHelper.Instance.DeleteFromParent(capacityId);
            SmartCapacityListHelper.Instance.Add(smartCapacityLists);
            WorkFlowHelper.Instance.OnSmartCapacityListChanged(smartCapacityLists);
            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/SmartCapacityList
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteSmartCapacityList([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var data = SmartCapacityListHelper.Instance.GetByIds<SmartCapacityList>(ids);
            if (!data.Any())
            {
                return Result.GenError<Result>(Error.SmartCapacityListNotExist);
            }
            SmartCapacityListHelper.Instance.Delete(ids);
            WorkFlowHelper.Instance.OnSmartCapacityListChanged(data);
            return Result.GenError<Result>(Error.Success);
        }
    }
}