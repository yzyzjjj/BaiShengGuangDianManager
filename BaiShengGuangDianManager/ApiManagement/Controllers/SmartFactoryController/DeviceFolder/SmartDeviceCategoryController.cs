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

namespace ApiManagement.Controllers.SmartFactoryController.DeviceFolder
{
    /// <summary>
    /// 
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]"), ApiController]
    public class SmartDeviceCategoryController : ControllerBase
    {
        /// <summary>
        /// GET: api/SmartDeviceCategory
        /// </summary>
        /// <param name="qId">设备类型ID</param>
        /// <param name="wId">车间ID</param>
        /// <param name="menu">是否菜单</param>
        /// <returns></returns>
        [HttpGet]
        public DataResult GetSmartDeviceCategory([FromQuery]int qId, int wId, bool menu)
        {
            var result = new DataResult();
            result.datas.AddRange(menu
                ? SmartDeviceCategoryHelper.GetMenu(qId, wId)
                : SmartDeviceCategoryHelper.GetDetail(qId, wId));
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.SmartDeviceCategoryNotExist;
                return result;
            }
            return result;
        }

        // PUT: api/SmartDeviceCategory
        [HttpPut]
        public Result PutSmartDeviceCategory([FromBody] IEnumerable<SmartDeviceCategory> deviceCategories)
        {
            if (deviceCategories == null || !deviceCategories.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (deviceCategories.Any(x => x.Category.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SmartDeviceCategoryNotEmpty);
            }
            if (deviceCategories.GroupBy(x => x.Category).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.SmartDeviceCategoryDuplicate);
            }

            var wId = deviceCategories.FirstOrDefault()?.WorkshopId ?? 0;
            var sames = deviceCategories.Select(x => x.Category);
            var ids = deviceCategories.Select(x => x.Id);
            if (SmartDeviceCategoryHelper.GetHaveSame(wId, sames, ids))
            {
                return Result.GenError<Result>(Error.SmartDeviceCategoryIsExist);
            }

            var cnt = SmartDeviceCategoryHelper.Instance.GetCountByIds(ids);
            if (cnt != deviceCategories.Count())
            {
                return Result.GenError<Result>(Error.SmartDeviceCategoryNotExist);
            }

            var markedDateTime = DateTime.Now;
            foreach (var category in deviceCategories)
            {
                category.MarkedDateTime = markedDateTime;
                category.Remark = category.Remark ?? "";
            }
            SmartDeviceCategoryHelper.Instance.Update(deviceCategories);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartDeviceCategory
        [HttpPost]
        public Result PostSmartDeviceCategory([FromBody] IEnumerable<SmartDeviceCategory> deviceCategories)
        {
            if (deviceCategories == null || !deviceCategories.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (deviceCategories.Any(x => x.Category.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SmartDeviceCategoryNotEmpty);
            }
            if (deviceCategories.GroupBy(x => x.Category).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.SmartDeviceCategoryDuplicate);
            }

            var wId = deviceCategories.FirstOrDefault()?.WorkshopId ?? 0;
            var sames = deviceCategories.Select(x => x.Category);
            if (SmartDeviceCategoryHelper.GetHaveSame(wId, sames))
            {
                return Result.GenError<Result>(Error.SmartDeviceCategoryIsExist);
            }

            var userId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var deviceCategory in deviceCategories)
            {
                deviceCategory.CreateUserId = userId;
                deviceCategory.MarkedDateTime = markedDateTime;
                deviceCategory.Remark = deviceCategory.Remark ?? "";
            }
            SmartDeviceCategoryHelper.Instance.Add(deviceCategories);
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