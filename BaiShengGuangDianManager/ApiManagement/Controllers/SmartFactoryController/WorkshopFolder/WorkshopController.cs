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
using ModelBase.Models.ControllerBase;

namespace ApiManagement.Controllers.SmartFactoryController.WorkshopFolder
{
    /// <summary>
    /// 车间
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]"), ApiController]
    public class SmartWorkshopController : ControllerBase
    {
        // GET: api/SmartWorkshop
        /// <summary>
        /// 
        /// </summary>
        /// <param name="qId">车间ID</param>
        /// <param name="menu">是否菜单</param>
        /// <returns></returns>
        [HttpGet]
        public DataResult GetSmartWorkshop([FromQuery]int qId, bool menu)
        {
            var result = new DataResult();
            result.datas.AddRange(menu
                ? SmartWorkshopHelper.GetMenu(qId)
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
        public Result PutSmartWorkshop([FromBody] IEnumerable<SmartWorkshop> workshops)
        {
            if (workshops == null || !workshops.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (workshops.Any(x => x.Workshop.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SmartWorkshopNotEmpty);
            }
            if (workshops.GroupBy(x => x.Workshop).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.SmartWorkshopDuplicate);
            }

            var sames = workshops.Select(x => x.Workshop);
            var ids = workshops.Select(x => x.Id);
            if (SmartWorkshopHelper.Instance.HaveSame(sames, ids))
            {
                return Result.GenError<Result>(Error.SmartWorkshopIsExist);
            }

            var cnt = SmartWorkshopHelper.Instance.GetCountByIds(ids);
            if (cnt != workshops.Count())
            {
                return Result.GenError<Result>(Error.SmartWorkshopNotExist);
            }

            var markedDateTime = DateTime.Now;
            foreach (var workshop in workshops)
            {
                workshop.MarkedDateTime = markedDateTime;
                workshop.Remark = workshop.Remark ?? "";
            }
            SmartWorkshopHelper.Instance.Update(workshops);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartWorkshop
        [HttpPost]
        public Result PostSmartWorkshop([FromBody] IEnumerable<SmartWorkshop> workshops)
        {
            if (workshops == null || !workshops.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (workshops.Any(x => x.Workshop.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SmartWorkshopNotEmpty);
            }
            if (workshops.GroupBy(x => x.Workshop).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.SmartWorkshopDuplicate);
            }

            var sames = workshops.Select(x => x.Workshop);
            if (SmartWorkshopHelper.Instance.HaveSame(sames))
            {
                return Result.GenError<Result>(Error.SmartWorkshopIsExist);
            }
            var userId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var workshop in workshops)
            {
                workshop.CreateUserId = userId;
                workshop.MarkedDateTime = markedDateTime;
                workshop.Remark = workshop.Remark ?? "";
            }
            SmartWorkshopHelper.Instance.Add(workshops);
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