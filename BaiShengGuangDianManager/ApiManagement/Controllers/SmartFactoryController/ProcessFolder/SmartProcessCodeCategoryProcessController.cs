using ApiManagement.Base.Server;
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
    [Route("api/[controller]")]
    [ApiController]
    public class SmartProcessCodeCategoryProcessController : ControllerBase
    {
        // GET: api/SmartProcessCodeCategoryProcess
        [HttpGet]
        public DataResult GetSmartProcessCodeCategoryProcess([FromQuery]int qId, int categoryId)
        {
            var result = new DataResult();
            var sql =
                $"SELECT a.*, b.Process, b.Remark FROM `t_process_code_category_process` a JOIN `t_process` b ON a.ProcessId = b.Id WHERE a.MarkedDelete = 0 AND b.MarkedDelete = 0" +
                $"{(qId == 0 ? "" : " AND a.Id = @qId")}" +
                $"{(categoryId == 0 ? "" : " AND ProcessCodeCategoryId = @categoryId")}" +
                $" ORDER BY `Order`;";
            result.datas.AddRange(ServerConfig.ApiDb.Query<SmartProcessCodeCategoryProcessDetail>(sql, new { qId, categoryId }));
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.SmartProcessCodeCategoryProcessNotExist;
                return result;
            }
            return result;
        }

        // PUT: api/SmartProcessCodeCategoryProcess
        [HttpPut]
        public Result PutSmartProcessCodeCategoryProcess([FromBody] IEnumerable<SmartProcessCodeCategoryProcess> smartProcessCodeCategoryProcesses)
        {
            if (smartProcessCodeCategoryProcesses == null || !smartProcessCodeCategoryProcesses.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            var smartProcessCodeCategoryProcessIds = smartProcessCodeCategoryProcesses.Select(x => x.Id);
            var data = SmartProcessCodeCategoryProcessHelper.Instance.GetByIds<SmartProcessCodeCategoryProcess>(smartProcessCodeCategoryProcessIds);
            if (data.Count() != smartProcessCodeCategoryProcesses.Count())
            {
                return Result.GenError<Result>(Error.SmartProcessCodeCategoryProcessNotExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartProcessCodeCategory in smartProcessCodeCategoryProcesses)
            {
                smartProcessCodeCategory.CreateUserId = createUserId;
                smartProcessCodeCategory.MarkedDateTime = markedDateTime;
            }

            SmartProcessCodeCategoryProcessHelper.Instance.Update(smartProcessCodeCategoryProcesses);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartProcessCodeCategoryProcess
        [HttpPost]
        public Result PostSmartProcessCodeCategoryProcess([FromBody] IEnumerable<SmartProcessCodeCategoryProcess> smartProcessCodeCategories)
        {
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartProcessCodeCategory in smartProcessCodeCategories)
            {
                smartProcessCodeCategory.CreateUserId = createUserId;
                smartProcessCodeCategory.MarkedDateTime = markedDateTime;
            }

            SmartProcessCodeCategoryProcessHelper.Instance.Add(smartProcessCodeCategories);
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