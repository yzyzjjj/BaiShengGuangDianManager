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
    public class SmartDeviceCategoryController : ControllerBase
    {
        // GET: api/SmartDeviceCategory
        [HttpGet]
        public DataResult GetSmartDeviceCategory([FromQuery]int qId, int wId, bool menu)
        {
            var result = new DataResult();
            result.datas.AddRange(menu
                ? SmartDeviceCategoryHelper.GetSmartDeviceCategoryMenu(qId, wId)
                : SmartDeviceCategoryHelper.GetSmartDeviceCategory(qId, wId));
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.SmartDeviceCategoryNotExist;
                return result;
            }
            return result;
        }

        // PUT: api/SmartDeviceCategory
        [HttpPut]
        public Result PutSmartDeviceCategory([FromBody] IEnumerable<SmartDeviceCategory> smartDeviceCategories)
        {
            if (smartDeviceCategories == null || !smartDeviceCategories.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            var smartDeviceCategoryIds = smartDeviceCategories.Select(x => x.Id);
            var data = SmartDeviceCategoryHelper.Instance.GetByIds<SmartDeviceCategory>(smartDeviceCategoryIds);
            if (data.Count() != smartDeviceCategories.Count())
            {
                return Result.GenError<Result>(Error.SmartDeviceCategoryNotExist);
            }
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var SmartDeviceCategory in smartDeviceCategories)
            {
                SmartDeviceCategory.CreateUserId = createUserId;
                SmartDeviceCategory.MarkedDateTime = markedDateTime;
            }
            SmartDeviceCategoryHelper.Instance.Update(smartDeviceCategories);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartDeviceCategory
        [HttpPost]
        public Result PostSmartDeviceCategory([FromBody] IEnumerable<SmartDeviceCategory> smartDeviceCategories)
        {
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartDeviceCategory in smartDeviceCategories)
            {
                smartDeviceCategory.CreateUserId = createUserId;
                smartDeviceCategory.MarkedDateTime = markedDateTime;
            }
            SmartDeviceCategoryHelper.Instance.Add(smartDeviceCategories);
            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/SmartDeviceCategory
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteSmartDeviceCategory([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var cnt = SmartDeviceCategoryHelper.Instance.GetCountByIds(ids);
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.SmartDeviceCategoryNotExist);
            }
            SmartDeviceCategoryHelper.Instance.Delete(ids);
            return Result.GenError<Result>(Error.Success);
        }
    }
}