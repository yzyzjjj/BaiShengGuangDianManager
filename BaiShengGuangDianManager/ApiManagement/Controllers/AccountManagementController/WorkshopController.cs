using ApiManagement.Models.AccountManagementModel;
using ApiManagement.Models.DeviceManagementModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.BaseModel;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.AccountManagementController
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]"), ApiController]
    public class WorkshopController : ControllerBase
    {
        // GET: api/Workshop
        /// <summary>
        /// 
        /// </summary>
        /// <param name="qId">车间ID</param>
        /// <param name="menu">是否菜单</param>
        /// <returns></returns>
        [HttpGet]
        public DataResult GetWorkshop([FromQuery]int qId, bool menu)
        {
            var result = new DataResult();
            result.datas.AddRange(menu
                ? WorkshopHelper.GetMenu(qId)
                : WorkshopHelper.GetDetails(qId));
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.WorkshopNotExist;
                return result;
            }
            return result;
        }

        // GET: api/Workshop/Device
        [HttpGet("Device")]
        public DataResult GetWorkshopDevice([FromQuery]int qId)
        {
            var result = new DataResult();
            if (qId != 0)
            {
                var workshop = WorkshopHelper.GetDetail(qId);
                if (workshop == null)
                {
                    result.errno = Error.WorkshopNotExist;
                    return result;
                }
                result.datas.AddRange(DeviceLibraryHelper.GetDetails(qId));
            }
            else
            {
                result.datas.AddRange(DeviceLibraryHelper.Instance.GetAll<DeviceLibrary>());
            }
            return result;
        }

        // PUT: api/Workshop
        [HttpPut]
        public Result PutWorkshop([FromBody] IEnumerable<Workshop> workshops)
        {
            if (workshops == null || !workshops.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (workshops.Any(x => x.Name.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.WorkshopNotEmpty);
            }
            if (workshops.GroupBy(x => x.Name).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.WorkshopDuplicate);
            }

            if (workshops.Any(x => !x.ValidShifts()))
            {
                return Result.GenError<Result>(Error.WorkshopShiftsError);
            }
            var sames = workshops.Select(x => x.Name);
            var ids = workshops.Select(x => x.Id);
            if (WorkshopHelper.Instance.HaveSame(sames, ids))
            {
                return Result.GenError<Result>(Error.WorkshopIsExist);
            }

            var cnt = WorkshopHelper.Instance.GetCountByIds(ids);
            if (cnt != workshops.Count())
            {
                return Result.GenError<Result>(Error.WorkshopNotExist);
            }

            var markedDateTime = DateTime.Now;
            foreach (var workshop in workshops)
            {
                workshop.MarkedDateTime = markedDateTime;
                workshop.Abbrev = workshop.Abbrev ?? "";
                workshop.Remark = workshop.Remark ?? "";
            }
            WorkshopHelper.Instance.Update(workshops);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/Workshop
        [HttpPost]
        public Result PostWorkshop([FromBody] IEnumerable<Workshop> workshops)
        {
            if (workshops == null || !workshops.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (workshops.Any(x => x.Name.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.WorkshopNotEmpty);
            }
            if (workshops.GroupBy(x => x.Name).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.WorkshopDuplicate);
            }
            if (workshops.Any(x => !x.ValidShifts()))
            {
                return Result.GenError<Result>(Error.WorkshopShiftsError);
            }
            var sames = workshops.Select(x => x.Name);
            if (WorkshopHelper.Instance.HaveSame(sames))
            {
                return Result.GenError<Result>(Error.WorkshopIsExist);
            }
            var userId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var workshop in workshops)
            {
                workshop.CreateUserId = userId;
                workshop.MarkedDateTime = markedDateTime;
                workshop.Abbrev = workshop.Abbrev ?? "";
                workshop.Remark = workshop.Remark ?? "";
            }
            WorkshopHelper.Instance.Add(workshops);
            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/Workshop
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteWorkshop([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var count = WorkshopHelper.Instance.GetCountByIds(ids);
            if (count == 0)
            {
                return Result.GenError<Result>(Error.WorkshopNotExist);
            }
            WorkshopHelper.Instance.Delete(ids);
            return Result.GenError<Result>(Error.Success);
        }
    }
}