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

namespace ApiManagement.Controllers.SmartFactoryController.FaultFolder
{
    /// <summary>
    /// 
    /// </summary>
    [Route("api/[controller]"), ApiController]
    public class SmartProcessFaultController : ControllerBase
    {
        // GET: api/SmartProcessFault
        [HttpGet]
        public DataResult GetSmartProcessFault([FromQuery]int qId)
        {
            var result = new DataResult();
            var sql = $"SELECT a.*, IFNULL(b.`Code`, '') `Code`, c.FlowCard, d.Process FROM `t_process_fault` a " +
                      "LEFT JOIN `t_device` b ON a.DeviceId = b.Id JOIN `t_flow_card` c ON a.FlowCardId = c.Id " +
                      "JOIN (SELECT a.Id, b.Process FROM `t_flow_card_process` a " +
                      "JOIN (SELECT a.Id, b.Process FROM `t_product_process` a " +
                      "JOIN (SELECT a.Id, b.Process FROM `t_process_code_category_process` a " +
                      "JOIN `t_process` b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) d ON a.ProcessId = d.Id " +
                      $"WHERE{(qId == 0 ? "" : " a.Id = @qId AND")} a.MarkedDelete = 0 ORDER BY a.Id Desc;";
            result.datas.AddRange(ServerConfig.ApiDb.Query<SmartProcessFaultDetail>(sql, new { qId }));
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.SmartProcessFaultNotExist;
                return result;
            }
            return result;
        }

        // PUT: api/SmartProcessFault
        [HttpPut]
        public object PutSmartProcessFault([FromBody] IEnumerable<SmartProcessFault> smartProcessFaults)
        {
            if (smartProcessFaults == null || !smartProcessFaults.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (smartProcessFaults.Any(x => x.Id == 0))
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartProcessFault in smartProcessFaults)
            {
                smartProcessFault.CreateUserId = createUserId;
                smartProcessFault.MarkedDateTime = markedDateTime;
            }

            SmartProcessFaultHelper.Instance.Update(smartProcessFaults);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartProcessFault
        [HttpPost]
        public object PostSmartProcessFault([FromBody] IEnumerable<SmartProcessFaultDetail> smartProcessFaults)
        {
            if (smartProcessFaults == null || !smartProcessFaults.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartProduct in smartProcessFaults)
            {
                smartProduct.CreateUserId = createUserId;
                smartProduct.MarkedDateTime = markedDateTime;
            }
            SmartProcessFaultHelper.Instance.Add(smartProcessFaults);
            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/SmartProcessFault
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteSmartProcessFault([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var cnt = SmartProcessFaultHelper.Instance.GetCountByIds(ids);
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.SmartProcessFaultNotExist);
            }
            SmartProcessFaultHelper.Instance.Delete(ids);
            return Result.GenError<Result>(Error.Success);
        }
    }
}