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
    public class SmartDeviceController : ControllerBase
    {
        /// <summary>
        /// GET: api/SmartDevice
        /// </summary>
        /// <param name="qId">设备ID</param>
        /// <param name="wId">车间ID</param>
        /// <param name="menu">是否菜单</param>
        /// <returns></returns>
        [HttpGet]
        public DataResult GetSmartDevice([FromQuery]int qId, int wId, bool menu)
        {
            var result = new DataResult();
            result.datas.AddRange(menu
                ? SmartDeviceHelper.GetMenu(qId, wId)
                : SmartDeviceHelper.GetDetails(qId, wId));
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
        /// <param name="devices"></param>
        /// <returns></returns>
        // PUT: api/SmartDevice/Id/5
        [HttpPut]
        public Result PutSmartDevice([FromBody] IEnumerable<SmartDevice> devices)
        {
            if (devices == null || !devices.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (devices.Any(x => x.Code.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SmartDeviceNotEmpty);
            }
            if (devices.GroupBy(x => x.Code).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.SmartDeviceDuplicate);
            }

            var wId = devices.FirstOrDefault()?.WorkshopId ?? 0;
            var sames = devices.Select(x => x.Code);
            var ids = devices.Select(x => x.Id);
            if (SmartDeviceHelper.GetHaveSame(wId, sames, ids))
            {
                return Result.GenError<Result>(Error.SmartDeviceIsExist);
            }

            var cnt = SmartWorkshopHelper.Instance.GetCountByIds(ids);
            if (cnt != devices.Count())
            {
                return Result.GenError<Result>(Error.SmartDeviceNotExist);
            }

            var markedDateTime = DateTime.Now;
            foreach (var device in devices)
            {
                device.MarkedDateTime = markedDateTime;
                device.Remark = device.Remark ?? "";
            }
            SmartDeviceHelper.Instance.Update(devices);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartDevice
        [HttpPost]
        public Result PostSmartDevice([FromBody] IEnumerable<SmartDevice> devices)
        {
            if (devices == null || !devices.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (devices.Any(x => x.Code.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SmartDeviceNotEmpty);
            }
            if (devices.GroupBy(x => x.Code).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.SmartDeviceDuplicate);
            }

            var wId = devices.FirstOrDefault()?.WorkshopId ?? 0;
            var sames = devices.Select(x => x.Code);
            if (SmartDeviceHelper.GetHaveSame(wId, sames))
            {
                return Result.GenError<Result>(Error.SmartDeviceIsExist);
            }
            
            var userId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartDevice in devices)
            {
                smartDevice.CreateUserId = userId;
                smartDevice.MarkedDateTime = markedDateTime;
                smartDevice.Remark = smartDevice.Remark ?? "";
            }
            SmartDeviceHelper.Instance.Add(devices);
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