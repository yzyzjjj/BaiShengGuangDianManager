using ApiManagement.Models.SmartFactoryModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.BaseModel;
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
    public class SmartDeviceModelController : ControllerBase
    {
        /// <summary>
        /// GET: api/SmartDeviceModel
        /// </summary>
        /// <param name="qId">设备型号ID</param>
        /// <param name="cId">设备类型ID</param>
        /// <param name="wId">车间ID</param>
        /// <param name="menu">是否菜单</param>
        /// <returns></returns>
        [HttpGet]
        public DataResult GetSmartDeviceModel([FromQuery]int qId, int cId, int wId, bool menu)
        {
            var result = new DataResult();
            result.datas.AddRange(menu
                ? SmartDeviceModelHelper.GetMenu(qId, cId, wId)
                : SmartDeviceModelHelper.GetDetail(qId, cId, wId));
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.SmartDeviceModelNotExist;
                return result;
            }
            return result;
        }

        // PUT: api/SmartDeviceModel
        [HttpPut]
        public Result PutSmartDeviceModel([FromBody] IEnumerable<SmartDeviceModel> deviceModels)
        {
            if (deviceModels == null || !deviceModels.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (deviceModels.Any(x => x.Model.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SmartDeviceModelNotEmpty);
            }
            if (deviceModels.GroupBy(x => x.Model).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.SmartDeviceModelDuplicate);
            }
            var cId = deviceModels.FirstOrDefault()?.CategoryId ?? 0;
            var sames = deviceModels.Select(x => x.Model);
            var ids = deviceModels.Select(x => x.Id);
            if (SmartDeviceModelHelper.GetHaveSame(cId, sames, ids))
            {
                return Result.GenError<Result>(Error.SmartDeviceModelIsExist);
            }
            var oldModels = SmartDeviceModelHelper.Instance.GetByIds<SmartDeviceModel>(ids);
            if (oldModels.Count() != deviceModels.Count())
            {
                return Result.GenError<Result>(Error.SmartDeviceModelNotExist);
            }
            var updates = new List<Tuple<int, int>>();
            var markedDateTime = DateTime.Now;
            foreach (var model in deviceModels)
            {
                model.MarkedDateTime = markedDateTime;
                model.Remark = model.Remark ?? "";
                var first = oldModels.FirstOrDefault(x => x.Id == model.Id);
                if (first != null && first.CategoryId != model.CategoryId)
                {
                    updates.Add(new Tuple<int, int>(model.Id, model.CategoryId));
                }
            }
            SmartDeviceModelHelper.Instance.Update(deviceModels);
            SmartDeviceHelper.UpdateSmartDeviceCategory(updates);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartDeviceModel
        [HttpPost]
        public Result PostSmartDeviceModel([FromBody] IEnumerable<SmartDeviceModel> deviceModels)
        {
            if (deviceModels == null || !deviceModels.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (deviceModels.Any(x => x.Model.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SmartDeviceModelNotEmpty);
            }
            if (deviceModels.GroupBy(x => x.Model).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.SmartDeviceModelDuplicate);
            }
            var cId = deviceModels.FirstOrDefault()?.CategoryId ?? 0;
            var sames = deviceModels.Select(x => x.Model);
            if (SmartDeviceModelHelper.GetHaveSame(cId, sames))
            {
                return Result.GenError<Result>(Error.SmartDeviceModelIsExist);
            }
            var userId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var model in deviceModels)
            {
                model.CreateUserId = userId;
                model.MarkedDateTime = markedDateTime;
                model.Remark = model.Remark ?? "";
            }
            SmartDeviceModelHelper.Instance.Add(deviceModels);
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
            if (cnt != ids.Count())
            {
                return Result.GenError<Result>(Error.SmartDeviceModelNotExist);
            }
            SmartDeviceModelHelper.Instance.Delete(ids);
            return Result.GenError<Result>(Error.Success);
        }
    }
}