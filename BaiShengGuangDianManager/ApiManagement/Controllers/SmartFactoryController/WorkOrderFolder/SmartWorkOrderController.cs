using ApiManagement.Base.Server;
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

namespace ApiManagement.Controllers.SmartFactoryController.WorkOrderFolder
{
    /// <summary>
    /// 
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class SmartWorkOrderController : ControllerBase
    {
        /// <summary>
        /// GET: api/SmartWorkOrder
        /// </summary>
        /// <param name="qId">设备类型ID</param>
        /// <param name="wId">车间ID</param>
        /// <param name="menu">是否菜单</param>
        /// <returns></returns>
        [HttpGet]
        public DataResult GetSmartWorkOrder([FromQuery]int qId, int wId, bool menu)
        {
            var result = new DataResult();
            if (menu)
            {
                var data = SmartWorkOrderHelper.GetMenu(qId, wId);
                if (qId != 0 && !data.Any())
                {
                    result.errno = Error.SmartWorkOrderNotExist;
                    return result;
                }
                result.datas.AddRange(data);
            }
            else
            {
                var data = SmartWorkOrderHelper.GetDetail(qId, wId).Select(ClassExtension.ParentCopyToChild<SmartWorkOrder, SmartWorkOrderDetail>);
                if (qId != 0 && !menu)
                {
                    var d = data.First();
                    if (d.State != SmartWorkOrderState.未加工)
                    {
                        var taskOrders = SmartTaskOrderHelper.GetDetailByWorkOrderId(d.Id);
                        var flowCards = SmartFlowCardHelper.GetSmartFlowCardsByTaskOrderIds(taskOrders.Select(x => x.Id));
                        var processes = SmartFlowCardProcessHelper.GetSmartFlowCardProcessesByFlowCardIds(flowCards.Select(x => x.Id));
                        if (processes.Any())
                        {
                            var st = processes.Where(x => x.StartTime != default(DateTime)).Min(y => y.StartTime);
                            var et = processes.Where(x => x.EndTime != default(DateTime)).Min(y => y.EndTime);
                            if (d.State == SmartWorkOrderState.已完成 || d.State == SmartWorkOrderState.已取消)
                            {
                                d.Consume = (int)(et - st).TotalSeconds;
                            }
                            else
                            {
                                d.Consume = (int)(DateTime.Now - st).TotalSeconds;
                            }
                        }
                    }
                }
                result.datas.AddRange(data);
            }
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.SmartWorkOrderNotExist;
                return result;
            }
            return result;
        }

        // PUT: api/SmartWorkOrder
        [HttpPut]
        public Result PutSmartWorkOrder([FromBody] IEnumerable<SmartWorkOrder> workOrders)
        {
            if (workOrders == null || !workOrders.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (workOrders.Any(x => x.WorkOrder.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SmartWorkOrderNotEmpty);
            }
            if (workOrders.GroupBy(x => x.WorkOrder).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.SmartWorkOrderDuplicate);
            }

            var wId = workOrders.FirstOrDefault()?.WorkshopId ?? 0;
            var sames = workOrders.Select(x => x.WorkOrder);
            var ids = workOrders.Select(x => x.Id);
            if (SmartWorkOrderHelper.GetHaveSame(wId, sames, ids))
            {
                return Result.GenError<Result>(Error.SmartWorkOrderIsExist);
            }

            var cnt = SmartWorkOrderHelper.Instance.GetCountByIds(ids);
            if (cnt != workOrders.Count())
            {
                return Result.GenError<Result>(Error.SmartWorkOrderNotExist);
            }

            var markedDateTime = DateTime.Now;
            foreach (var workOrder in workOrders)
            {
                workOrder.MarkedDateTime = markedDateTime;
                workOrder.Remark = workOrder.Remark ?? "";
            }

            SmartWorkOrderHelper.Instance.Update(workOrders);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartWorkOrder
        [HttpPost]
        public Result PostSmartWorkOrder([FromBody] IEnumerable<SmartWorkOrder> workOrders)
        {
            if (workOrders == null || !workOrders.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (workOrders.Any(x => x.WorkOrder.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SmartWorkOrderNotEmpty);
            }
            if (workOrders.GroupBy(x => x.WorkOrder).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.SmartWorkOrderDuplicate);
            }

            var wId = workOrders.FirstOrDefault()?.WorkshopId ?? 0;
            var sames = workOrders.Select(x => x.WorkOrder);
            if (SmartWorkOrderHelper.GetHaveSame(wId, sames))
            {
                return Result.GenError<Result>(Error.SmartWorkOrderIsExist);
            }

            var userId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var workOrder in workOrders)
            {
                workOrder.CreateUserId = userId;
                workOrder.MarkedDateTime = markedDateTime;
                workOrder.Remark = workOrder.Remark ?? "";
            }

            SmartWorkOrderHelper.Instance.Add(workOrders);
            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/SmartWorkOrder
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteSmartWorkOrder([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var cnt = SmartWorkOrderHelper.Instance.GetCountByIds(ids);
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.SmartWorkOrderNotExist);
            }
            SmartWorkOrderHelper.Instance.Delete(ids);
            return Result.GenError<Result>(Error.Success);
        }
    }
}