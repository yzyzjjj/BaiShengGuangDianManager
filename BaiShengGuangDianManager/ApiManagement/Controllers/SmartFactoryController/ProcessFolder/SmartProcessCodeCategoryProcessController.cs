using ApiManagement.Models.BaseModel;
using ApiManagement.Models.SmartFactoryModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.SmartFactoryController.ProcessFolder
{
    /// <summary>
    /// 
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]"), ApiController]
    public class SmartProcessCodeCategoryProcessController : ControllerBase
    {
        // GET: api/SmartProcessCodeCategoryProcess
        [HttpGet]
        public DataResult GetSmartProcessCodeCategoryProcess([FromQuery]int qId, int cId)
        {
            var result = new DataResult();
            result.datas.AddRange(SmartProcessCodeCategoryProcessHelper.GetDetail(qId, cId));
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.SmartProcessCodeCategoryProcessNotExist;
                return result;
            }
            return result;
        }

        // PUT: api/SmartProcessCodeCategoryProcess
        [HttpPut]
        public Result PutSmartProcessCodeCategoryProcess([FromBody] IEnumerable<SmartProcessCodeCategoryProcess> processCodeCategoryProcesses)
        {
            if (processCodeCategoryProcesses == null || !processCodeCategoryProcesses.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            var smartProcessCodeCategoryProcessIds = processCodeCategoryProcesses.Select(x => x.Id);
            var data = SmartProcessCodeCategoryProcessHelper.Instance.GetByIds<SmartProcessCodeCategoryProcess>(smartProcessCodeCategoryProcessIds);
            if (data.Count() != processCodeCategoryProcesses.Count())
            {
                return Result.GenError<Result>(Error.SmartProcessCodeCategoryProcessNotExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var processCodeCategory in processCodeCategoryProcesses)
            {
                processCodeCategory.CreateUserId = createUserId;
                processCodeCategory.MarkedDateTime = markedDateTime;
            }

            SmartProcessCodeCategoryProcessHelper.Instance.Update(processCodeCategoryProcesses);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartProcessCodeCategoryProcess
        [HttpPost]
        public Result PostSmartProcessCodeCategoryProcess([FromBody] IEnumerable<SmartProcessCodeCategoryProcess> processCodeCategoryProcesses)
        {
            var userId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var processCodeCategory in processCodeCategoryProcesses)
            {
                processCodeCategory.CreateUserId = userId;
                processCodeCategory.MarkedDateTime = markedDateTime;
            }

            SmartProcessCodeCategoryProcessHelper.Instance.Add(processCodeCategoryProcesses);
            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/SmartProcessCodeCategoryProcess
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteSmartProcessCodeCategoryProcess([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var cnt = SmartProcessCodeCategoryProcessHelper.Instance.GetCountByIds(ids);
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.SmartProcessCodeCategoryProcessNotExist);
            }
            SmartProcessCodeCategoryProcessHelper.Instance.Delete(ids);
            return Result.GenError<Result>(Error.Success);
        }
    }
}