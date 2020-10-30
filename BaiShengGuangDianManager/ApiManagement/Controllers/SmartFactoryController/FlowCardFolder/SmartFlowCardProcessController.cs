using ApiManagement.Base.Helper;
using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using ApiManagement.Models.SmartFactoryModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.SmartFactoryController.FlowCardFolder
{
    /// <summary>
    /// 
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class SmartFlowCardProcessController : ControllerBase
    {
        // GET: api/SmartFlowCardProcess
        [HttpGet]
        public DataResult GetSmartFlowCardProcess([FromQuery]int qId, int flowCardId)
        {
            var result = new DataResult();
            var sql = $"SELECT a.*, b.Process, IFNULL(c.`Name`, '') Processor, IFNULL(d.`Code`, '') `DeviceCode` FROM `t_flow_card_process` a " +
                      $"JOIN (SELECT a.Id, b.Process FROM `t_product_process` a " +
                      $"JOIN (SELECT a.Id, b.Process FROM `t_process_code_category_process` a " +
                      $"JOIN `t_process` b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id " +
                      $"LEFT JOIN `t_user` c ON a.ProcessorId = c.Id " +
                      $"LEFT JOIN `t_device` d ON a.DeviceId = d.Id " +
                      $"WHERE a.MarkedDelete = 0 " +
                      $"{(qId == 0 ? "" : " AND a.Id = @qId ")}" +
                      $"{(flowCardId == 0 ? "" : " AND a.FlowCardId = @flowCardId ")}" +
                      "ORDER BY a.FlowCardId, a.Id";
            result.datas.AddRange(ServerConfig.ApiDb.Query<SmartFlowCardProcessDetail>(sql, new { qId, flowCardId }));
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.SmartFlowCardProcessNotExist;
                return result;
            }
            return result;
        }

        // PUT: api/SmartFlowCardProcess
        [HttpPut]
        public Result PutSmartFlowCardProcess([FromBody] IEnumerable<SmartFlowCardProcess> smartFlowCardProcesses)
        {
            if (smartFlowCardProcesses == null || !smartFlowCardProcesses.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            var smartFlowCardProcessIds = smartFlowCardProcesses.Select(x => x.Id);
            var data = SmartFlowCardProcessHelper.Instance.GetByIds<SmartFlowCardProcess>(smartFlowCardProcessIds);
            if (data.Count() != smartFlowCardProcesses.Count())
            {
                return Result.GenError<Result>(Error.SmartFlowCardProcessNotExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartFlowCardProcess in smartFlowCardProcesses)
            {
                smartFlowCardProcess.CreateUserId = createUserId;
                smartFlowCardProcess.MarkedDateTime = markedDateTime;
            }

            SmartFlowCardProcessHelper.Instance.Update(smartFlowCardProcesses);
            WorkFlowHelper.Instance.OnSmartFlowCardProcessChanged(smartFlowCardProcesses);
            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 录入
        /// </summary>
        /// <param name="smartFlowCardProcess"></param>
        /// <returns></returns>
        // HttpPost: api/SmartFlowCardProcess/Report
        [HttpPost("Report")]
        public Result ReportSmartFlowCardProcess([FromBody] SmartFlowCardProcess smartFlowCardProcess)
        {
            var process = SmartFlowCardProcessHelper.Instance.Get<SmartFlowCardProcess>(smartFlowCardProcess.Id);
            if (process == null)
            {
                return Result.GenError<Result>(Error.SmartFlowCardProcessNotExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            smartFlowCardProcess.CreateUserId = createUserId;
            smartFlowCardProcess.MarkedDateTime = markedDateTime;

            process.Count++;
            process.Qualified += smartFlowCardProcess.Qualified;
            process.Unqualified += smartFlowCardProcess.Unqualified;
            process.ProcessorId = smartFlowCardProcess.ProcessorId;
            process.DeviceId = smartFlowCardProcess.DeviceId;
            SmartFlowCardProcessHelper.Instance.Update(smartFlowCardProcess);
            SmartFlowCardProcessLogHelper.Instance.Add(new SmartFlowCardProcessLog(createUserId, markedDateTime, process, process.Qualified, process.Unqualified));
            WorkFlowHelper.Instance.OnSmartFlowCardProcessChanged(new List<SmartFlowCardProcess> { smartFlowCardProcess });
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartFlowCardProcess
        [HttpPost]
        public object PostSmartFlowCardProcess([FromBody] IEnumerable<SmartFlowCardProcess> smartFlowCardProcesses)
        {
            if (smartFlowCardProcesses == null || !smartFlowCardProcesses.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartFlowCardProcess in smartFlowCardProcesses)
            {
                smartFlowCardProcess.CreateUserId = createUserId;
                smartFlowCardProcess.MarkedDateTime = markedDateTime;
            }

            SmartFlowCardProcessHelper.Instance.Update(smartFlowCardProcesses);
            WorkFlowHelper.Instance.OnSmartFlowCardProcessChanged(smartFlowCardProcesses);
            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/SmartFlowCardProcess
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteSmartFlowCardProcess([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var smartFlowCards = SmartFlowCardProcessHelper.Instance.GetByIds<SmartFlowCardProcess>(ids);
            if (!smartFlowCards.Any() || smartFlowCards.Count() != ids.Count())
            {
                return Result.GenError<Result>(Error.SmartFlowCardProcessNotExist);
            }
            SmartFlowCardProcessHelper.Instance.Delete(ids);
            WorkFlowHelper.Instance.OnSmartFlowCardProcessChanged(smartFlowCards);
            return Result.GenError<Result>(Error.Success);
        }
    }
}