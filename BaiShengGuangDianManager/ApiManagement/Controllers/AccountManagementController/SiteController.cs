using System;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Base.Server;
using ApiManagement.Models.AccountManagementModel;
using ApiManagement.Models.DeviceManagementModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.BaseModel;
using ModelBase.Models.Result;
using ServiceStack;

namespace ApiManagement.Controllers.AccountManagementController
{
    //[Authorize]
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]"), ApiController]
    public class SiteController : ControllerBase
    {
        // GET: api/Site
        [HttpGet]
        public DataResult GetSite([FromQuery]int qId, int wId, bool menu)
        {
            var result = new DataResult();
            result.datas.AddRange(menu
                ? SiteHelper.GetMenu(qId, wId)
                : SiteHelper.GetDetails(qId, wId));
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.SiteNotExist;
                return result;
            }
            return result;
        }

        // PUT: api/Site
        [HttpPut]
        public Result PutSite([FromBody] IEnumerable<Site> sites)
        {
            if (sites == null || !sites.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (sites.Any(x => x.Region.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SiteNotEmpty);
            }
            if (sites.GroupBy(x => x.Region).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.SiteDuplicate);
            }

            var wId = sites.FirstOrDefault()?.WorkshopId ?? 0;
            var sames = sites.Select(x => x.Region);
            var ids = sites.Select(x => x.Id);
            if (SiteHelper.GetHaveSame(wId, sames, ids))
            {
                return Result.GenError<Result>(Error.SiteIsExist);
            }

            var cnt = SiteHelper.Instance.GetCountByIds(ids);
            if (cnt != sites.Count())
            {
                return Result.GenError<Result>(Error.SiteNotExist);
            }

            var markedDateTime = DateTime.Now;
            foreach (var workshop in sites)
            {
                workshop.MarkedDateTime = markedDateTime;
                workshop.Remark = workshop.Remark ?? "";
            }
            SiteHelper.Instance.Update(sites);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/Site
        [HttpPost]
        public Result PostSite([FromBody] IEnumerable<Site> sites)
        {
            if (sites == null || !sites.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (sites.Any(x => x.Region.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SiteNotEmpty);
            }
            if (sites.GroupBy(x => x.Region).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.SiteDuplicate);
            }

            var wId = sites.FirstOrDefault()?.WorkshopId ?? 0;
            var sames = sites.Select(x => x.Region);
            if (SiteHelper.GetHaveSame(wId, sames))
            {
                return Result.GenError<Result>(Error.SiteIsExist);
            }

            var markedDateTime = DateTime.Now;
            foreach (var workshop in sites)
            {
                workshop.MarkedDateTime = markedDateTime;
                workshop.Remark = workshop.Remark ?? "";
            }
            SiteHelper.Instance.Add(sites);
            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/Site
        [HttpDelete]
        public Result DeleteSite([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var count = SiteHelper.Instance.GetCountByIds(ids);
            if (count == 0)
            {
                return Result.GenError<Result>(Error.SiteNotExist);
            }
            SiteHelper.Instance.Delete(ids);
            return Result.GenError<Result>(Error.Success);
        }
    }
}