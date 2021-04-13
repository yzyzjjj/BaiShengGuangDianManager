using ApiManagement.Base.Helper;
using ApiManagement.Models.SmartFactoryModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using System;
using System.Collections.Generic;
using System.Linq;
using ModelBase.Models.BaseModel;

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
        public DataResult GetSmartFlowCardProcess([FromQuery]int qId, int wId, int flowCardId)
        {
            var result = new DataResult();
            result.datas.AddRange(SmartFlowCardProcessHelper.GetDetail(qId, wId, flowCardId));
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.SmartFlowCardProcessNotExist;
                return result;
            }
            return result;
        }

        // PUT: api/SmartFlowCardProcess
        [HttpPut]
        public Result PutSmartFlowCardProcess([FromBody] IEnumerable<SmartFlowCardProcess> flowCardProcesses)
        {
            if (flowCardProcesses == null || !flowCardProcesses.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            var flowCardProcessIds = flowCardProcesses.Select(x => x.Id);
            var data = SmartFlowCardProcessHelper.Instance.GetByIds<SmartFlowCardProcess>(flowCardProcessIds);
            if (data.Count() != flowCardProcesses.Count())
            {
                return Result.GenError<Result>(Error.SmartFlowCardProcessNotExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var flowCardProcess in flowCardProcesses)
            {
                flowCardProcess.CreateUserId = createUserId;
                flowCardProcess.MarkedDateTime = markedDateTime;
            }

            SmartFlowCardProcessHelper.Instance.Update(flowCardProcesses);
            WorkFlowHelper.Instance.OnSmartFlowCardProcessChanged(flowCardProcesses);
            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 录入
        /// </summary>
        /// <param name="flowCardProcess"></param>
        /// <returns></returns>
        // HttpPost: api/SmartFlowCardProcess/Report
        [HttpPost("Report")]
        public Result ReportSmartFlowCardProcess([FromBody] SmartFlowCardProcess flowCardProcess)
        {
            var process = SmartFlowCardProcessHelper.Instance.Get<SmartFlowCardProcess>(flowCardProcess.Id);
            if (process == null)
            {
                return Result.GenError<Result>(Error.SmartFlowCardProcessNotExist);
            }

            var userId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            var wId = flowCardProcess.WorkshopId;
            flowCardProcess.CreateUserId = userId;
            flowCardProcess.MarkedDateTime = markedDateTime;

            process.Count++;
            process.Qualified += flowCardProcess.Qualified;
            process.Unqualified += flowCardProcess.Unqualified;
            process.ProcessorId = flowCardProcess.ProcessorId;
            process.DeviceId = flowCardProcess.DeviceId;
            SmartFlowCardProcessHelper.Instance.Update(flowCardProcess);
            SmartFlowCardProcessLogHelper.Instance.Add(new SmartFlowCardProcessLog(wId, userId, markedDateTime, process, process.Qualified, process.Unqualified));
            WorkFlowHelper.Instance.OnSmartFlowCardProcessChanged(new List<SmartFlowCardProcess> { flowCardProcess });
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartFlowCardProcess
        [HttpPost]
        public object PostSmartFlowCardProcess([FromBody] IEnumerable<SmartFlowCardProcess> flowCardProcesses)
        {
            if (flowCardProcesses == null || !flowCardProcesses.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var flowCardProcess in flowCardProcesses)
            {
                flowCardProcess.CreateUserId = createUserId;
                flowCardProcess.MarkedDateTime = markedDateTime;
            }

            SmartFlowCardProcessHelper.Instance.Update(flowCardProcesses);
            WorkFlowHelper.Instance.OnSmartFlowCardProcessChanged(flowCardProcesses);
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