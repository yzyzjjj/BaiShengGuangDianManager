using ApiManagement.Base.Helper;
using ApiManagement.Models.SmartFactoryModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using ModelBase.Models.BaseModel;

namespace ApiManagement.Controllers.SmartFactoryController.FlowCardFolder
{
    /// <summary>
    /// 
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]"), ApiController]
    public class SmartFlowCardController : ControllerBase
    {
        // GET: api/SmartFlowCard
        [HttpGet]
        public DataResult GetSmartFlowCard([FromQuery]int wId, int qId, int productId, int taskOrderId, bool menu, DateTime startTime, DateTime endTime)
        {
            var result = new DataResult();
            if (menu)
            {
                result.datas.AddRange(SmartFlowCardHelper.GetMenu(qId, wId));
            }
            else
            {
                var data = SmartFlowCardHelper.GetDetail(startTime, endTime, qId, wId, taskOrderId, productId);
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
                result.datas.AddRange(data);
            }

            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.SmartFlowCardNotExist;
            }
            return result;
        }

        // PUT: api/SmartFlowCard
        [HttpPut]
        public Result PutSmartFlowCard([FromBody] IEnumerable<SmartFlowCard> flowCards)
        {
            if (flowCards == null || !flowCards.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (flowCards.Any(x => x.FlowCard.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SmartFlowCardNotEmpty);
            }
            if (flowCards.GroupBy(x => x.FlowCard).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.SmartFlowCardDuplicate);
            }

            var wId = flowCards.FirstOrDefault()?.WorkshopId ?? 0;
            var sames = flowCards.Select(x => x.FlowCard);
            var ids = flowCards.Select(x => x.Id);
            if (SmartFlowCardHelper.GetHaveSame(wId, sames, ids))
            {
                return Result.GenError<Result>(Error.SmartFlowCardIsExist);
            }

            var cnt = SmartFlowCardHelper.Instance.GetCountByIds(ids);
            if (cnt != flowCards.Count())
            {
                return Result.GenError<Result>(Error.SmartFlowCardNotExist);
            }

            var markedDateTime = DateTime.Now;
            foreach (var operatorLevel in flowCards)
            {
                operatorLevel.MarkedDateTime = markedDateTime;
                operatorLevel.Remark = operatorLevel.Remark ?? "";
            }
            SmartFlowCardHelper.Instance.Update(flowCards);
            WorkFlowHelper.Instance.OnSmartFlowCardChanged(flowCards);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartFlowCard
        [HttpPost]
        public object PostSmartFlowCard([FromBody] IEnumerable<SmartFlowCard> flowCards)
        {
            if (flowCards == null || !flowCards.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            if (flowCards.Count(x => x.Number <= 0) > 1)
            {
                return Result.GenError<Result>(Error.SmartFlowCardNumberError);
            }

            var wId = flowCards.First().WorkshopId;
            var taskOrderId = flowCards.First().TaskOrderId;
            if (flowCards.GroupBy(x => x.ProcessCodeId).Count() > 1)
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            var processCodeId = flowCards.First().ProcessCodeId;

            var thisTotal = flowCards.Sum(x => x.Number);

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

            var count = flowCards.Count();
            var userId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            var fcs = CodeHelper.GenCodes(CodeType.流程卡, count, markedDateTime).ToArray();
            var i = 0;
            foreach (var flowCard in flowCards)
            {
                flowCard.CreateUserId = userId;
                flowCard.MarkedDateTime = markedDateTime;
                flowCard.CreateTime = markedDateTime;
                flowCard.FlowCard = fcs[i];
                flowCard.Batch = batch;
                flowCard.Remark = flowCard.Remark ?? "";
                i++;
            }
            SmartFlowCardHelper.Instance.Add(flowCards);
            var addFlowCards = SmartFlowCardHelper.GetSmartFlowCardsByBatch(taskOrderId, batch);

            var smartFlowCardProcesses = new List<SmartFlowCardProcess>();
            i = 0;
            foreach (var smartFlowCard in addFlowCards)
            {
                var newSmartFlowCard = flowCards.ElementAt(i);
                smartFlowCard.ProcessorId = newSmartFlowCard.ProcessorId;
                var t = true;
                foreach (var process in processes)
                {
                    var flowCardProcess = new SmartFlowCardProcess
                    {
                        CreateUserId = userId,
                        MarkedDateTime = markedDateTime,
                        WorkshopId = wId,
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
            var flowCards = SmartFlowCardHelper.Instance.GetByIds<SmartFlowCard>(ids);
            if (!flowCards.Any() || flowCards.Count() != ids.Count())
            {
                return Result.GenError<Result>(Error.SmartFlowCardProcessNotExist);
            }
            SmartFlowCardHelper.Instance.Delete(ids);
            SmartFlowCardProcessHelper.DeleteByFlowCardIs(ids);
            WorkFlowHelper.Instance.OnSmartFlowCardChanged(flowCards);
            return Result.GenError<Result>(Error.Success);
        }
    }
}