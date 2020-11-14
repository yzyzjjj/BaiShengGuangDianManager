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

namespace ApiManagement.Controllers.SmartFactoryController.DeviceFolder
{
    /// <summary>
    /// 
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class SmartDeviceModelController : ControllerBase
    {
        // GET: api/SmartDeviceModel
        [HttpGet]
        public DataResult GetSmartDeviceModel([FromQuery]int qId, int categoryId, bool menu)
        {
            var result = new DataResult();
            var sql = menu ? $"SELECT Id, `Model` FROM `t_device_model` WHERE MarkedDelete = 0{(qId == 0 ? "" : " AND Id = @qId")}{(categoryId == 0 ? "" : " AND CategoryId = @categoryId")};"
                : $"SELECT a.*, b.`Category` FROM `t_device_model` a JOIN `t_device_category` b ON a.CategoryId = b.Id " +
                  $"WHERE a.MarkedDelete = 0{(qId == 0 ? "" : " AND a.Id = @qId")}{(categoryId == 0 ? "" : " AND CategoryId = @categoryId")};";
            result.datas.AddRange(menu
                ? ServerConfig.ApiDb.Query<dynamic>(sql, new { qId, categoryId })
                : ServerConfig.ApiDb.Query<SmartDeviceModelDetail>(sql, new { qId, categoryId }));
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.SmartDeviceModelNotExist;
                return result;
            }
            return result;
        }

        // PUT: api/SmartDeviceModel
        [HttpPut]
        public Result PutSmartDeviceModel([FromBody] IEnumerable<SmartDeviceModel> smartDeviceModels)
        {
            if (smartDeviceModels == null || !smartDeviceModels.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            var smartDeviceModelIds = smartDeviceModels.Select(x => x.Id);
            var data = SmartDeviceModelHelper.Instance.GetByIds<SmartDeviceModel>(smartDeviceModelIds);
            if (data.Count() != smartDeviceModels.Count())
            {
                return Result.GenError<Result>(Error.SmartDeviceModelNotExist);
            }
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var SmartDeviceModel in smartDeviceModels)
            {
                SmartDeviceModel.CreateUserId = createUserId;
                SmartDeviceModel.MarkedDateTime = markedDateTime;
            }
            SmartDeviceModelHelper.Instance.Update(smartDeviceModels);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartDeviceModel
        [HttpPost]
        public Result PostSmartDeviceModel([FromBody] IEnumerable<SmartDeviceModel> smartDeviceModels)
        {
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartDeviceModel in smartDeviceModels)
            {
                smartDeviceModel.CreateUserId = createUserId;
                smartDeviceModel.MarkedDateTime = markedDateTime;
            }
            SmartDeviceModelHelper.Instance.Add(smartDeviceModels);
            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/SmartDeviceModel
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteSmartDeviceModel([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var cnt = SmartDeviceModelHelper.Instance.GetCountByIds(ids);
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.SmartDeviceModelNotExist);
            }
            SmartDeviceModelHelper.Instance.Delete(ids);
            return Result.GenError<Result>(Error.Success);
        }
    }
}