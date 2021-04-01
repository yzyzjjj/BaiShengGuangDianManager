using ApiManagement.Base.Server;
using ApiManagement.Models.DeviceManagementModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Models.Warning;

namespace ApiManagement.Controllers.DeviceManagementController
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    //[Authorize]  //根据需要可添加角色和自定义授权
    public class DeviceClassController : ControllerBase
    {
        // GET: api/DeviceClass
        [HttpGet]
        public DataResult GetDeviceClass([FromQuery] bool menu, int qId)
        {
            var result = new DataResult();
            result.datas.AddRange(menu
                ? DeviceClassHelper.GetMenu(qId)
                : DeviceClassHelper.GetDetails(qId));
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.ScriptVersionNotExist;
                return result;
            }
            return result;
        }

        // PUT: api/DeviceClass
        [HttpPut]
        public Result PutDeviceClass([FromBody] DeviceClass deviceClass)
        {
            var data = DeviceClassHelper.Instance.Get<DeviceClass>(deviceClass.Id);
            if (data == null)
            {
                return Result.GenError<Result>(Error.DeviceClassNotExist);
            }

            if (deviceClass.Class.IsNullOrEmpty())
                return Result.GenError<Result>(Error.DeviceClassNotEmpty);

            var names = new List<string> { deviceClass.Class };
            var ids = new List<int> { deviceClass.Id };
            if (DeviceClassHelper.GetHaveSame(names, ids))
            {
                return Result.GenError<Result>(Error.DeviceClassIsExist);
            }

            deviceClass.CreateUserId = Request.GetIdentityInformation();
            deviceClass.MarkedDateTime = DateTime.Now;
            DeviceClassHelper.Instance.Update(deviceClass);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/DeviceClass
        [HttpPost]
        public Result PostDeviceClass([FromBody] DeviceClass deviceClass)
        {
            var names = new List<string> { deviceClass.Class };
            if (DeviceClassHelper.GetHaveSame(names))
            {
                return Result.GenError<Result>(Error.DeviceClassIsExist);
            }

            deviceClass.CreateUserId = Request.GetIdentityInformation();
            deviceClass.MarkedDateTime = DateTime.Now;
            DeviceClassHelper.Instance.Add(deviceClass);
            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/DeviceClass/5
        [HttpDelete("{id}")]
        public Result DeleteDeviceClass([FromRoute] int id)
        {
            var data = DeviceClassHelper.Instance.Get<DeviceClass>(id);
            if (data == null)
            {
                return Result.GenError<Result>(Error.DeviceClassNotExist);
            }

            var cnt = DeviceLibraryHelper.GetCountByClass(id);
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.DeviceModelUseDeviceClass);
            }
            DeviceClassHelper.Instance.Delete(id);
            return Result.GenError<Result>(Error.Success);
        }

    }
}