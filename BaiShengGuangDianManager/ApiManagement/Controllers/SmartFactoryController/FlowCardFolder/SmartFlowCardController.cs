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
    [Route("api/[controller]"), ApiController]
    public class SmartFlowCardController : ControllerBase
    {
        // GET: api/SmartFlowCard
        [HttpGet]
        public DataResult GetSmartFlowCard([FromQuery]int qId, int productId, int taskOrderId, bool menu, DateTime startTime, DateTime endTime)
        {
            var result = new DataResult();
            var sql = menu ? $"SELECT Id, `FlowCard` FROM `t_flow_card` WHERE MarkedDelete = 0{(qId == 0 ? "" : " AND Id = @qId")};"
                : $"SELECT a.*, b.TaskOrder, b.ProductId, b.Product, c.`Code` ProcessCode FROM `t_flow_card` a " +
                  $"LEFT JOIN (SELECT a.*, b.Product FROM `t_task_order` a JOIN `t_product` b ON a.ProductId = b.Id WHERE 1 = 1{(productId == 0 ? "" : " AND a.ProductId = @productId")}) b ON a.TaskOrderId = b.Id " +
                  $"JOIN `t_process_code` c ON a.ProcessCodeId = c.Id  " +
                  $"WHERE a.MarkedDelete = 0" +
                  $"{(qId == 0 ? "" : " AND a.Id = @qId")}" +
                  $"{(taskOrderId == 0 ? "" : " AND a.TaskOrderId = @taskOrderId")}" +
                  $"{(startTime == default(DateTime) ? "" : " AND a.CreateTime >= @startTime")}" +
                  $"{(endTime == default(DateTime) ? "" : " AND a.CreateTime <= @endTime")};";
            var data = ServerConfig.ApiDb.Query<SmartFlowCardDetail>(sql, new { qId, productId, taskOrderId, startTime, endTime });

            if (qId != 0 && !data.Any())
            {
                result.errno = Error.SmartFlowCardNotExist;
                return result;
            }
            if (qId != 0 && !menu)
            {
                var d = data.First();
                if (d.State != SmartFlowCardState.未加工)
                {
                    var processes = SmartFlowCardProcessHelper.GetSmartFlowCardProcessesByFlowCardId(d.Id);
                    if (processes.Any())
                    {
                        var st = processes.First().StartTime;
                        var etMax = processes.Where(x => x.EndTime != default(DateTime)).Min(y => y.EndTime);
                        if (d.State == SmartFlowCardState.已完成)
                        {
                            var et = processes.Last().EndTime;
                            d.Consume = (int)(et - st).TotalSeconds;
                        }
                        else if (d.State == SmartFlowCardState.已取消)
                        {
                            d.Consume = (int)(etMax - st).TotalSeconds;
                        }
                        else
                        {
                            d.Consume = (int)(DateTime.Now - st).TotalSeconds;
                        }

                        var last = processes.LastOrDefault(x => x.Before != 0);
                        if (last != null)
                        {

                            d.Done = last.Qualified + last.Unqualified;
                            d.Left = last.Before - d.Done;
                        }
                    }
                }
            }
            if (menu)
            {
                result.datas.AddRange(data.Select(x => new
                {
                    x.Id,
                    x.FlowCard
                }));
            }
            else
            {
                result.datas.AddRange(data);
            }
            return result;
        }

        // PUT: api/SmartFlowCard
        [HttpPut]
        public Result PutSmartFlowCard([FromBody] IEnumerable<SmartFlowCard> smartFlowCards)
        {
            if (smartFlowCards == null || !smartFlowCards.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            var smartFlowCardIds = smartFlowCards.Select(x => x.Id);
            var data = SmartFlowCardHelper.Instance.GetByIds<SmartFlowCard>(smartFlowCardIds);
            if (data.Count() != smartFlowCards.Count())
            {
                return Result.GenError<Result>(Error.SmartFlowCardProcessNotExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartProcess in smartFlowCards)
            {
                smartProcess.CreateUserId = createUserId;
                smartProcess.MarkedDateTime = markedDateTime;
            }

            SmartFlowCardHelper.Instance.Update(smartFlowCards);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartFlowCard
        [HttpPost]
        public object PostSmartFlowCard([FromBody] IEnumerable<SmartFlowCard> smartFlowCards)
        {
            if (smartFlowCards == null || !smartFlowCards.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (smartFlowCards.Count(x => x.Number <= 0) > 1)
            {
                return Result.GenError<Result>(Error.SmartFlowCardNumberError);
            }
            if (smartFlowCards.GroupBy(x => x.TaskOrderId).Count() > 1)
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            var taskOrderId = smartFlowCards.First().TaskOrderId;
            if (smartFlowCards.GroupBy(x => x.ProcessCodeId).Count() > 1)
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            var processCodeId = smartFlowCards.First().ProcessCodeId;

            var thisTotal = smartFlowCards.Sum(x => x.Number);

            var taskOrder = SmartTaskOrderHelper.Instance.Get<SmartTaskOrder>(taskOrderId);
            if (taskOrder == null)
            {
                return Result.GenError<Result>(Error.SmartTaskOrderNotExist);
            }

            if (taskOrder.Left <= 0)
            {
                return Result.GenError<Result>(Error.SmartFlowCardNumberLimit);
            }

            var processes = SmartProductProcessHelper.GetSmartProductProcesses(taskOrderId, processCodeId);
            if (!processes.Any())
            {
                return Result.GenError<Result>(Error.SmartProcessNotEmpty);
            }

            var batch = SmartFlowCardHelper.GetSmartFlowCardBatch(taskOrderId);
            batch++;

            var count = smartFlowCards.Count();
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            var flowCards = CodeHelper.GenCodes(CodeType.流程卡, count, markedDateTime).ToArray();
            var i = 0;
            foreach (var smartFlowCard in smartFlowCards)
            {
                smartFlowCard.CreateUserId = createUserId;
                smartFlowCard.MarkedDateTime = markedDateTime;
                smartFlowCard.CreateTime = markedDateTime;
                smartFlowCard.FlowCard = flowCards[i];
                smartFlowCard.Batch = batch;
                i++;
            }
            SmartFlowCardHelper.Instance.Add(smartFlowCards);
            var addFlowCards = SmartFlowCardHelper.GetSmartFlowCardsByBatch(taskOrderId, batch);

            var smartFlowCardProcesses = new List<SmartFlowCardProcess>();
            i = 0;
            foreach (var smartFlowCard in addFlowCards)
            {
                var newSmartFlowCard = smartFlowCards.ElementAt(i);
                smartFlowCard.ProcessorId = newSmartFlowCard.ProcessorId;
                var t = true;
                foreach (var process in processes)
                {
                    var flowCardProcess = new SmartFlowCardProcess
                    {
                        CreateUserId = createUserId,
                        MarkedDateTime = markedDateTime,
                        FlowCardId = smartFlowCard.Id,
                        ProcessId = process.Id,
                    };

                    if (t)
                    {
                        flowCardProcess.ProcessorId = smartFlowCard.ProcessorId;
                        flowCardProcess.Before = smartFlowCard.Number;
                        t = false;
                    }
                    smartFlowCardProcesses.Add(flowCardProcess);
                }
                i++;
            }

            SmartFlowCardProcessHelper.Instance.Add<SmartFlowCardProcess>(smartFlowCardProcesses);
            taskOrder.Doing += thisTotal;
            taskOrder.Issue += count;
            taskOrder.MarkedDateTime = markedDateTime;

            WorkFlowHelper.Instance.OnSmartFlowCardChanged(addFlowCards);
            WorkFlowHelper.Instance.OnSmartFlowCardProcessCreated(addFlowCards, smartFlowCardProcesses);
            return new
            {
                errno = 0,
                errmsg = "成功",
                taskOrder.Target,
                taskOrder.Left,
                taskOrder.Doing,
                taskOrder.Issue,
                FlowCards = addFlowCards
            };
        }

        // DELETE: api/SmartFlowCard
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteSmartFlowCard([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var smartFlowCards = SmartFlowCardHelper.Instance.GetByIds<SmartFlowCard>(ids);
            if (!smartFlowCards.Any() || smartFlowCards.Count() != ids.Count())
            {
                return Result.GenError<Result>(Error.SmartFlowCardProcessNotExist);
            }
            SmartFlowCardHelper.Instance.Delete(ids);
            SmartFlowCardProcessHelper.DeleteByFlowCardIs(ids);
            WorkFlowHelper.Instance.OnSmartFlowCardChanged(smartFlowCards);
            return Result.GenError<Result>(Error.Success);
        }
    }
}