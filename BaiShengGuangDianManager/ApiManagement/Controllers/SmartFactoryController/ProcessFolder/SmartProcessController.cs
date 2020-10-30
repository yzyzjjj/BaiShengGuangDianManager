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
    public class SmartProcessController : ControllerBase
    {
        // GET: api/SmartProcess
        [HttpGet]
        public DataResult GetSmartProcess([FromQuery]int qId, bool menu)
        {
            var result = new DataResult();
            var sql = menu ? $"SELECT Id, `Process` FROM `t_process` WHERE MarkedDelete = 0{(qId == 0 ? "" : " AND Id = @qId")};"
                : $"SELECT a.*, IFNULL(b.Category, '') DeviceCategory FROM `t_process` a LEFT JOIN t_device_category b ON a.DeviceCategoryId = b.Id WHERE a.MarkedDelete = 0{(qId == 0 ? "" : " AND a.Id = @qId")};";
            result.datas.AddRange(menu
                ? ServerConfig.ApiDb.Query<dynamic>(sql, new { qId })
                : ServerConfig.ApiDb.Query<SmartProcessDetail>(sql, new { qId }));
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.SmartProcessNotExist;
                return result;
            }
            return result;
        }

        // PUT: api/SmartProcess
        [HttpPut]
        public Result PutSmartProcess([FromBody] IEnumerable<SmartProcess> smartProcesses)
        {
            if (smartProcesses == null || !smartProcesses.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            var smartProcessIds = smartProcesses.Select(x => x.Id);
            var data = SmartProcessHelper.Instance.GetByIds<SmartProcess>(smartProcessIds);
            if (data.Count() != smartProcesses.Count())
            {
                return Result.GenError<Result>(Error.SmartProcessNotExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartProcess in smartProcesses)
            {
                smartProcess.CreateUserId = createUserId;
                smartProcess.MarkedDateTime = markedDateTime;
            }

            SmartProcessHelper.Instance.Update(smartProcesses);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartProcess
        [HttpPost]
        public Result PostSmartProcess([FromBody] IEnumerable<SmartProcess> smartProcesses)
        {
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartProcess in smartProcesses)
            {
                smartProcess.CreateUserId = createUserId;
                smartProcess.MarkedDateTime = markedDateTime;
            }
            SmartProcessHelper.Instance.Add(smartProcesses);
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