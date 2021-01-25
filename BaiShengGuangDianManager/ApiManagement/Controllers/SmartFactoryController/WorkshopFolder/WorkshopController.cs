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

namespace ApiManagement.Controllers.SmartFactoryController.WorkshopFolder
{
    /// <summary>
    /// 车间
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]"), ApiController]
    public class SmartWorkshopController : ControllerBase
    {
        // GET: api/SmartWorkshop
        [HttpGet]
        public DataResult GetSmartWorkshop([FromQuery]int qId, bool menu)
        {
            var result = new DataResult();
            result.datas.AddRange(menu
                ? SmartWorkshopHelper.GetSmartWorkshopSimple(qId)
                : SmartWorkshopHelper.Instance.CommonGet<SmartWorkshop>(qId));
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.SmartWorkshopNotExist;
                return result;
            }
            return result;
        }

        // PUT: api/SmartWorkshop
        [HttpPut]
        public Result PutSmartWorkshop([FromBody] IEnumerable<SmartWorkshop> smartWorkshops)
        {
            if (smartWorkshops == null || !smartWorkshops.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (smartWorkshops.Any(x => x.Workshop.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SmartWorkshopNotEmpty);
            }

            var names = smartWorkshops.Select(x => x.Workshop);
            var ids = smartWorkshops.Select(x => x.Id);
            if (SmartWorkshopHelper.Instance.HaveSame(names, ids))
            {
                return Result.GenError<Result>(Error.SmartWorkshopDuplicate);
            }

            var cnt = SmartWorkshopHelper.Instance.GetCountByIds(ids);
            if (cnt != smartWorkshops.Count())
            {
                return Result.GenError<Result>(Error.SmartWorkshopNotExist);
            }

            var markedDateTime = DateTime.Now;
            foreach (var smartWorkshop in smartWorkshops)
            {
                smartWorkshop.MarkedDateTime = markedDateTime;
                smartWorkshop.Remark = smartWorkshop.Remark ?? "";
            }
            SmartWorkshopHelper.Instance.Update(smartWorkshops);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartWorkshop
        [HttpPost]
        public Result PostSmartWorkshop([FromBody] IEnumerable<SmartWorkshop> smartWorkshops)
        {
            if (smartWorkshops == null || !smartWorkshops.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (smartWorkshops.Any(x => x.Workshop.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SmartWorkshopNotEmpty);
            }

            var names = smartWorkshops.Select(x => x.Workshop);
            if (SmartWorkshopHelper.Instance.HaveSame(names))
            {
                return Result.GenError<Result>(Error.SmartWorkshopIsExist);
            }
            var userId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartWorkshop in smartWorkshops)
            {
                smartWorkshop.CreateUserId = userId;
                smartWorkshop.MarkedDateTime = markedDateTime;
                smartWorkshop.Remark = smartWorkshop.Remark ?? "";
            }
            SmartWorkshopHelper.Instance.Add(smartWorkshops);
            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/SmartWorkshop
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteSmartWorkshop([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var count = SmartWorkshopHelper.Instance.GetCountByIds(ids);
            if (count == 0)
            {
                return Result.GenError<Result>(Error.SmartUserNotExist);
            }
            SmartWorkshopHelper.Instance.Delete(ids);
            return Result.GenError<Result>(Error.Success);
        }
    }
}