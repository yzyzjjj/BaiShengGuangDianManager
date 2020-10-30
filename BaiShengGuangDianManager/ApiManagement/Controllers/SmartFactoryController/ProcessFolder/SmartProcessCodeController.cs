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
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class SmartProcessCodeController : ControllerBase
    {
        // GET: api/SmartProcessCode
        [HttpGet]
        public DataResult GetSmartProcessCode([FromQuery]int qId, bool menu)
        {
            var result = new DataResult();
            var sql = $"SELECT a.*, IFNULL(b.Category, '') Category FROM t_process_code a JOIN t_process_code_category b ON a.CategoryId = b.Id WHERE a.MarkedDelete = 0{(qId == 0 ? "" : " AND a.Id = @qId")} ORDER BY a.Id;";
            var data = ServerConfig.ApiDb.Query<SmartProcessCodeDetail>(sql, new { qId });
            if (menu)
            {
                result.datas.AddRange(data.Select(x => new { x.Id, x.Code }));
            }
            else
            {
                var processIds = data.SelectMany(x => x.ProcessIdList).Distinct();
                var processList = ServerConfig.ApiDb.Query<SmartProcess>(
                    "SELECT a.Id, b.Process FROM `t_process_code_category_process` a JOIN `t_process` b ON a.ProcessId = b.Id WHERE a.MarkedDelete = 0 AND b.MarkedDelete = 0 AND a.Id IN @processIds", new
                    {
                        processIds
                    });
                if (processList.Any())
                {
                    foreach (var d in data)
                    {
                        foreach (var processId in d.ProcessIdList)
                        {
                            var process = processList.FirstOrDefault(x => x.Id == processId);
                            if (process != null)
                            {
                                d.ProcessList.Add(process.Process);
                            }
                        }
                    }
                }
                result.datas.AddRange(data);
            }

            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.SmartProcessCodeNotExist;
                return result;
            }
            return result;
        }

        // PUT: api/SmartProcessCode
        [HttpPut]
        public Result PutSmartProcessCode([FromBody] IEnumerable<SmartProcessCode> smartProcessCodes)
        {
            if (smartProcessCodes == null || !smartProcessCodes.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            var smartProcessCodeIds = smartProcessCodes.Select(x => x.Id);
            var data = SmartProcessCodeHelper.Instance.GetByIds<SmartProcessCode>(smartProcessCodeIds);
            if (data.Count() != smartProcessCodes.Count())
            {
                return Result.GenError<Result>(Error.SmartProcessCodeNotExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartProcessCode in smartProcessCodes)
            {
                smartProcessCode.CreateUserId = createUserId;
                smartProcessCode.MarkedDateTime = markedDateTime;
            }

            SmartProcessCodeHelper.Instance.Update(smartProcessCodes);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartProcessCode
        [HttpPost]
        public Result PostSmartProcessCode([FromBody] IEnumerable<SmartProcessCode> smartProcessCodes)
        {
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartProduct in smartProcessCodes)
            {
                smartProduct.CreateUserId = createUserId;
                smartProduct.MarkedDateTime = markedDateTime;
            }
            SmartProcessCodeHelper.Instance.Add(smartProcessCodes);
            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/SmartProcessCode
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteSmartProcessCode([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var cnt = SmartProcessCodeHelper.Instance.GetCountByIds(ids);
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.SmartProcessCodeNotExist);
            }
            SmartProcessCodeHelper.Instance.Delete(ids);
            return Result.GenError<Result>(Error.Success);
        }
    }
}