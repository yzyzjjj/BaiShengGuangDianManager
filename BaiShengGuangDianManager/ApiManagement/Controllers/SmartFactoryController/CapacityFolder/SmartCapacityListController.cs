using ApiManagement.Base.Helper;
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

namespace ApiManagement.Controllers.SmartFactoryController.CapacityFolder
{
    /// <summary>
    /// 
    /// </summary>
    [Route("api/[controller]"), ApiController]
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
            var sql = "";
            if (capacityId != 0 && categoryId != 0)
            {
                sql =
                    "SELECT IFNULL(c.Id, 0) Id, a.Id ProcessId, b.Process, b.DeviceCategoryId, b.Category" +
                    ", IFNULL(c.DeviceModel, '') DeviceModel, IFNULL(c.DeviceNumber, '') DeviceNumber, IFNULL(c.OperatorLevel, '') OperatorLevel, IFNULL(c.OperatorNumber, '') OperatorNumber " +
                    "FROM `t_process_code_category_process` a " +
                    "JOIN (SELECT a.*, IFNULL(b.Category, '') Category FROM `t_process` a " +
                    "LEFT JOIN `t_device_category` b ON a.DeviceCategoryId = b.Id) b ON a.ProcessId = b.Id " +
                    "LEFT JOIN (SELECT * FROM `t_capacity_list` WHERE  MarkedDelete = 0 AND CapacityId = @capacityId) c ON a.Id = c.ProcessId " +
                    "WHERE a.MarkedDelete = 0 AND ProcessCodeCategoryId = @categoryId;";
            }
            else if (capacityId != 0 && categoryId == 0)
            {
                var capacity = SmartCapacityHelper.Instance.Get<SmartCapacity>(capacityId);
                if (capacity == null)
                {
                    return Result.GenError<DataResult>(Error.SmartCapacityNotExist);
                }

                categoryId = capacity.CategoryId;
                sql =
                    "SELECT IFNULL(c.Id, 0) Id, a.Id ProcessId, b.Process, b.DeviceCategoryId, b.Category" +
                    ", IFNULL(c.DeviceModel, '') DeviceModel, IFNULL(c.DeviceNumber, '') DeviceNumber, IFNULL(c.OperatorLevel, '') OperatorLevel, IFNULL(c.OperatorNumber, '') OperatorNumber " +
                    "FROM `t_process_code_category_process` a " +
                    "JOIN (SELECT a.*, IFNULL(b.Category, '') Category FROM `t_process` a " +
                    "LEFT JOIN `t_device_category` b ON a.DeviceCategoryId = b.Id) b ON a.ProcessId = b.Id " +
                    "LEFT JOIN (SELECT * FROM `t_capacity_list` WHERE  MarkedDelete = 0 AND CapacityId = @capacityId) c ON a.Id = c.ProcessId " +
                    "WHERE a.MarkedDelete = 0 AND ProcessCodeCategoryId = @categoryId;";
            }
            else if (capacityId == 0 && categoryId != 0)
            {
                sql =
                    "SELECT a.Id ProcessId, b.Process, b.DeviceCategoryId, b.Category FROM `t_process_code_category_process` a " +
                    "JOIN (SELECT a.*, IFNULL(b.Category, '') Category FROM `t_process` a " +
                    "LEFT JOIN `t_device_category` b ON a.DeviceCategoryId = b.Id) b ON a.ProcessId = b.Id " +
                    "WHERE a.MarkedDelete = 0 AND b.MarkedDelete = 0 AND ProcessCodeCategoryId = @categoryId;";
            }
            else
            {
                sql = $"SELECT a.*, b.Capacity, c.Process, c.DeviceCategoryId, c.Category FROM `t_capacity_list` a " +
                      $"JOIN `t_capacity` b ON a.CapacityId = b.Id " +
                      $"JOIN (SELECT a.*, b.Process, b.DeviceCategoryId, b.Category FROM `t_process_code_category_process` a " +
                      $"JOIN (SELECT a.*, b.Category FROM `t_process` a " +
                      $"JOIN `t_device_category` b ON a.DeviceCategoryId = b.Id) b ON a.ProcessId = b.Id) c ON a.ProcessId = c.Id " +
                      $"WHERE a.MarkedDelete = 0{(qId == 0 ? "" : " AND a.Id = @qId")};";
            }

            result.datas.AddRange(ServerConfig.ApiDb.Query<SmartCapacityListCol>(sql, new { qId, capacityId, categoryId }));
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
            var categoryId = 0;
            SmartCapacityList capacityList;
            if (qId != 0)
            {
                capacityList = SmartCapacityListHelper.Instance.Get<SmartCapacityList>(qId);
                if (capacityList == null)
                {
                    return Result.GenError<Result>(Error.SmartCapacityListNotExist);
                }
                var capacity = SmartCapacityHelper.Instance.Get<SmartCapacity>(capacityList.CapacityId);
                if (capacity == null)
                {
                    return Result.GenError<Result>(Error.SmartCapacityNotExist);
                }

                categoryId = capacity.CategoryId;
            }
            else if (processId != 0)
            {
                var process = ServerConfig.ApiDb.Query<dynamic>(
                    "SELECT a.Id, b.DeviceCategoryId FROM `t_process_code_category_process` a JOIN `t_process` b ON a.ProcessId = b.Id WHERE a.MarkedDelete = 0 AND a.Id = @processId;", new
                    {
                        processId
                    }).FirstOrDefault();
                if (process == null)
                {
                    return Result.GenError<Result>(Error.SmartProcessCodeCategoryProcessNotExist);
                }
                capacityList = new SmartCapacityList
                {
                    ProcessId = process.Id
                };
                categoryId = process.DeviceCategoryId;
            }
            else
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            var actDevices = new List<SmartDeviceCapacity>();
            if (categoryId != 0)
            {
                var models = SmartDeviceModelHelper.Instance.GetSmartDeviceModelDetails(categoryId);
                var devices = capacityList.DeviceList;
                if (models.Any())
                {
                    var modelCount = ServerConfig.ApiDb.Query<dynamic>(
                        "SELECT ModelId, COUNT(1) Count FROM `t_device` WHERE ModelId IN @modelId GROUP BY ModelId;", new
                        {
                            modelId = models.Select(x => x.Id)
                        });

                    foreach (var model in models)
                    {
                        var device = devices.FirstOrDefault(x => x.ModelId == model.Id) ?? new SmartDeviceCapacity();
                        device.Category = model.Category;
                        device.CategoryId = model.CategoryId;
                        device.ModelId = model.Id;
                        device.Model = model.Model;
                        device.Count = modelCount.FirstOrDefault(x => (int)x.ModelId == model.Id) != null
                            ? (int)modelCount.FirstOrDefault(x => (int)x.ModelId == model.Id).Count : 0;
                        actDevices.Add(device);
                    }
                }
            }

            var actOperators = new List<SmartOperatorCapacity>();
            var levels = SmartOperatorLevelHelper.Instance.GetAll<SmartOperatorLevel>().OrderBy(x => x.Order);
            if (levels.Any())
            {
                var operatorCount = ServerConfig.ApiDb.Query<dynamic>(
                    "SELECT LevelId, COUNT(1) Count FROM t_operator WHERE ProcessId = @ProcessId GROUP BY LevelId;", new
                    {
                        capacityList.ProcessId
                    });
                var operators = capacityList.OperatorList;
                if (levels.Any())
                {
                    foreach (var level in levels)
                    {
                        var op = operators.FirstOrDefault(x => x.LevelId == level.Id) ?? new SmartOperatorCapacity();
                        op.Level = level.Level;
                        op.LevelId = level.Id;
                        op.Count = operatorCount.FirstOrDefault(x => (int)x.LevelId == level.Id) != null
                            ? (int)operatorCount.FirstOrDefault(x => (int)x.LevelId == level.Id).Count : 0;
                        actOperators.Add(op);
                    }
                }
            }

            result.datas.Add(capacityList);
            return new
            {
                errno = 0,
                msg = "成功",
                Devices = actDevices,
                Operators = actOperators
            };
        }

        // PUT: api/SmartCapacityList
        [HttpPut]
        public Result PutSmartCapacityList([FromBody] IEnumerable<SmartCapacityList> smartCapacityLists)
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
                return Result.GenError<Result>(Error.ParamError);
            }

            var capacity = SmartCapacityHelper.Instance.Get<SmartCapacity>(capacityId);
            if (capacity == null)
            {
                return Result.GenError<Result>(Error.SmartCapacityNotExist);
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

            var smartCapacityListIds = smartCapacityLists.Select(x => x.Id);
            var data = SmartCapacityListHelper.Instance.GetByIds<SmartCapacityList>(smartCapacityListIds);
            if (data.Count() != smartCapacityLists.Count())
            {
                return Result.GenError<Result>(Error.SmartCapacityListNotExist);
            }

            if (smartCapacityLists.Count() != processes.Count())
            {
                return Result.GenError<Result>(Error.SmartCapacityListCountError);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
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

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartCapacityList in smartCapacityLists)
            {
                smartCapacityList.CreateUserId = createUserId;
                smartCapacityList.MarkedDateTime = markedDateTime;
                smartCapacityList.CapacityId = capacityId;
            }
            SmartCapacityListHelper.Instance.DeleteByCapacityId(capacityId);
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