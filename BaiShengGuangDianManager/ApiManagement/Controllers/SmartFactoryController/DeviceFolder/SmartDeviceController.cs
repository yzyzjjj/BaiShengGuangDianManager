using System;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using ApiManagement.Models.SmartFactoryModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;

namespace ApiManagement.Controllers.SmartFactoryController.DeviceFolder
{
    /// <summary>
    /// 
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class SmartDeviceController : ControllerBase
    {
        // GET: api/SmartDevice
        [HttpGet]
        public DataResult GetSmartDevice([FromQuery]int qId, bool menu)
        {
            var result = new DataResult();
            var sql = menu ? $"SELECT Id, `Code` FROM t_device WHERE MarkedDelete = 0{(qId == 0 ? "" : " AND Id = @qId")};"
                : $"SELECT a.*, b.Category, c.Model FROM t_device a JOIN t_device_category b ON a.CategoryId = b.Id JOIN t_device_model c ON a.ModelId = c.Id WHERE a.MarkedDelete = 0{(qId == 0 ? "" : " AND a.Id = @qId")};";
            result.datas.AddRange(menu
                ? ServerConfig.ApiDb.Query<dynamic>(sql, new { qId })
                : ServerConfig.ApiDb.Query<SmartDeviceDetail>(sql, new { qId }));
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.SmartDeviceNotExist;
                return result;
            }
            return result;
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="smartDevices"></param>
        /// <returns></returns>
        // PUT: api/SmartDevice/Id/5
        [HttpPut]
        public Result PutSmartDevice([FromBody] IEnumerable<SmartDevice> smartDevices)
        {
            if (smartDevices == null || !smartDevices.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            var smartDeviceIds = smartDevices.Select(x => x.Id);
            var data = SmartDeviceHelper.Instance.GetByIds<SmartDevice>(smartDeviceIds);
            if (data.Count() != smartDevices.Count())
            {
                return Result.GenError<Result>(Error.SmartDeviceNotExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var SmartDevice in smartDevices)
            {
                SmartDevice.CreateUserId = createUserId;
                SmartDevice.MarkedDateTime = markedDateTime;
            }
            SmartDeviceHelper.Instance.Update(smartDevices);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartDevice
        [HttpPost]
        public Result PostSmartDevice([FromBody] IEnumerable<SmartDevice> smartDevices)
        {
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartDevice in smartDevices)
            {
                smartDevice.CreateUserId = createUserId;
                smartDevice.MarkedDateTime = markedDateTime;
            }
            SmartDeviceHelper.Instance.Add(smartDevices);
            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/SmartDevice
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteSmartDevice([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var cnt = SmartDeviceHelper.Instance.GetCountByIds(ids);
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.SmartDeviceNotExist);
            }
            SmartDeviceHelper.Instance.Delete(ids);
            return Result.GenError<Result>(Error.Success);
        }
    }
}