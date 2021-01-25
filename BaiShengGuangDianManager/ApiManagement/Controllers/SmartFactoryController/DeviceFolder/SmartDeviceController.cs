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
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class SmartDeviceController : ControllerBase
    {
        // GET: api/SmartDevice
        [HttpGet]
        public DataResult GetSmartDevice([FromQuery]int qId, int wId, bool menu)
        {
            var result = new DataResult();
            result.datas.AddRange(menu
                ? SmartDeviceHelper.GetSmartDeviceMenu(qId, wId)
                : SmartDeviceHelper.GetSmartDevice(qId, wId));
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

            if (smartDevices.Any(x => x.Code.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SmartDeviceNotEmpty);
            }

            var codes = smartDevices.Select(x => x.Code);
            var ids = smartDevices.Select(x => x.Id);
            if (SmartWorkshopHelper.Instance.HaveSame(codes, ids))
            {
                return Result.GenError<Result>(Error.SmartDeviceDuplicate);
            }

            var cnt = SmartWorkshopHelper.Instance.GetCountByIds(ids);
            if (cnt != smartDevices.Count())
            {
                return Result.GenError<Result>(Error.SmartDeviceNotExist);
            }

            var markedDateTime = DateTime.Now;
            foreach (var SmartDevice in smartDevices)
            {
                SmartDevice.MarkedDateTime = markedDateTime;
                SmartDevice.Remark = SmartDevice.Remark ?? "";
            }
            SmartDeviceHelper.Instance.Update(smartDevices);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartDevice
        [HttpPost]
        public Result PostSmartDevice([FromBody] IEnumerable<SmartDevice> smartDevices)
        {
            if (smartDevices == null || !smartDevices.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            if (smartDevices.Any(x => x.Code.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SmartDeviceNotEmpty);
            }
            var codes = smartDevices.Select(x => x.Code);
            if (SmartWorkshopHelper.Instance.HaveSame(codes))
            {
                return Result.GenError<Result>(Error.SmartDeviceDuplicate);
            }
            var userId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartDevice in smartDevices)
            {
                smartDevice.CreateUserId = userId;
                smartDevice.MarkedDateTime = markedDateTime;
                smartDevice.Remark = smartDevice.Remark ?? "";
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