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

namespace ApiManagement.Controllers.SmartFactoryController.ProcessFolder
{
    /// <summary>
    /// 
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]"), ApiController]
    public class SmartProcessController : ControllerBase
    {
        /// <summary>
        /// GET: api/SmartProcess
        /// </summary>
        /// <param name="qId">流程ID</param>
        /// <param name="wId">车间ID</param>
        /// <param name="menu">是</param>
        /// <returns></returns>
        [HttpGet]
        public DataResult GetSmartProcess([FromQuery]int qId, int wId, bool menu)
        {
            var result = new DataResult();
            result.datas.AddRange(menu
                ? SmartProcessHelper.GetMenu(qId, wId)
                : SmartProcessHelper.GetDetail(qId, wId));
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.SmartProcessNotExist;
                return result;
            }
            return result;
        }

        // PUT: api/SmartProcess
        [HttpPut]
        public Result PutSmartProcess([FromBody] IEnumerable<SmartProcess> processes)
        {
            if (processes == null || !processes.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (processes.Any(x => x.Process.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SmartProcessNotEmpty);
            }
            if (processes.GroupBy(x => x.Process).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.SmartProcessDuplicate);
            }

            var wId = processes.FirstOrDefault()?.WorkshopId ?? 0;
            var cIds = processes.Select(x => x.DeviceCategoryId);
            var sames = processes.Select(x => x.Process);
            var ids = processes.Select(x => x.Id);
            if (SmartProcessHelper.GetHaveSame(wId, cIds, sames, ids))
            {
                return Result.GenError<Result>(Error.SmartProcessIsExist);
            }

            var cnt = SmartProcessHelper.Instance.GetCountByIds(ids);
            if (cnt != processes.Count())
            {
                return Result.GenError<Result>(Error.SmartProcessNotExist);
            }

            var markedDateTime = DateTime.Now;
            foreach (var process in processes)
            {
                process.MarkedDateTime = markedDateTime;
                process.Remark = process.Remark ?? "";
            }

            SmartProcessHelper.Instance.Update(processes);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartProcess
        [HttpPost]
        public Result PostSmartProcess([FromBody] IEnumerable<SmartProcess> processes)
        {
            if (processes == null || !processes.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (processes.Any(x => x.Process.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SmartProcessNotEmpty);
            }
            if (processes.GroupBy(x => x.Process).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.SmartProcessDuplicate);
            }

            var wId = processes.FirstOrDefault()?.WorkshopId ?? 0;
            var cIds = processes.Select(x => x.DeviceCategoryId);
            var sames = processes.Select(x => x.Process);
            if (SmartProcessHelper.GetHaveSame(wId, cIds, sames))
            {
                return Result.GenError<Result>(Error.SmartProcessIsExist);
            }

            var userId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var process in processes)
            {
                process.CreateUserId = userId;
                process.MarkedDateTime = markedDateTime;
                process.Remark = process.Remark ?? "";
            }

            SmartProcessHelper.Instance.Add(processes);
            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/SmartProcess
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteSmartProcess([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var cnt = SmartProcessHelper.Instance.GetCountByIds(ids);
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.SmartProcessNotExist);
            }
            SmartProcessHelper.Instance.Delete(ids);
            return Result.GenError<Result>(Error.Success);
        }
    }
}