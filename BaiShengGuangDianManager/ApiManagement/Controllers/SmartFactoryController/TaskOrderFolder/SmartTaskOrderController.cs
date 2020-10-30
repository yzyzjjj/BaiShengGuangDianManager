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

namespace ApiManagement.Controllers.SmartFactoryController.TaskOrderFolder
{
    /// <summary>
    /// 
    /// </summary>
    [Route("api/[controller]"), ApiController]
    public class SmartTaskOrderController : ControllerBase
    {
        // GET: api/SmartTaskOrder
        [HttpGet]
        public DataResult GetSmartTaskOrder([FromQuery]int qId, bool menu)
        {
            var result = new DataResult();
            var sql = menu ? $"SELECT Id, `TaskOrder` FROM `t_task_order` WHERE MarkedDelete = 0{(qId == 0 ? "" : " AND Id = @qId")} ORDER BY Id Desc;"
                : $"SELECT a.*, b.WorkOrder, c.Product FROM t_task_order a JOIN t_work_order b ON a.WorkOrderId = b.Id JOIN t_product c ON a.ProductId = c.Id WHERE a.MarkedDelete = 0{(qId == 0 ? "" : " AND a.Id = @qId")} ORDER BY Id Desc;";
            var data = ServerConfig.ApiDb.Query<SmartTaskOrderDetail>(sql, new { qId });
            if (qId != 0 && !data.Any())
            {
                result.errno = Error.SmartTaskOrderNotExist;
                return result;
            }
            if (qId != 0 && !menu)
            {
                var d = data.First();
                if (d.State != SmartTaskOrderState.未加工)
                {
                    var flowCards = SmartFlowCardHelper.Instance.GetSmartFlowCardsByTaskOrderId(d.Id);
                    var processes = SmartFlowCardProcessHelper.Instance.GetSmartFlowCardProcessesByFlowCardIds(flowCards.Select(x => x.Id));
                    if (processes.Any())
                    {
                        var st = processes.Where(x => x.StartTime != default(DateTime)).Min(y => y.StartTime);
                        var et = processes.Where(x => x.EndTime != default(DateTime)).Min(y => y.EndTime);
                        if (d.State == SmartTaskOrderState.已完成 || d.State == SmartTaskOrderState.已取消)
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
            if (menu)
            {
                result.datas.AddRange(data.Select(x => new
                {
                    x.Id,
                    x.TaskOrder
                }));
            }
            else
            {
                result.datas.AddRange(data);
            }
            return result;
        }

        // PUT: api/SmartTaskOrder
        [HttpPut]
        public Result PutSmartTaskOrder([FromBody] IEnumerable<SmartTaskOrder> smartTaskOrders)
        {
            if (smartTaskOrders == null || !smartTaskOrders.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            var smartTaskOrderIds = smartTaskOrders.Select(x => x.Id);
            var data = SmartTaskOrderHelper.Instance.GetByIds<SmartTaskOrder>(smartTaskOrderIds);
            if (data.Count() != smartTaskOrders.Count())
            {
                return Result.GenError<Result>(Error.SmartTaskOrderNotExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartTaskOrder in smartTaskOrders)
            {
                smartTaskOrder.CreateUserId = createUserId;
                smartTaskOrder.MarkedDateTime = markedDateTime;
            }

            SmartTaskOrderHelper.Instance.Update(smartTaskOrders);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartTaskOrder
        [HttpPost]
        public Result PostSmartTaskOrder([FromBody] IEnumerable<SmartTaskOrder> smartTaskOrders)
        {
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartTaskOrder in smartTaskOrders)
            {
                smartTaskOrder.CreateUserId = createUserId;
                smartTaskOrder.MarkedDateTime = markedDateTime;
            }
            SmartTaskOrderHelper.Instance.Add(smartTaskOrders);
            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/SmartTaskOrder
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteSmartTaskOrder([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var count = SmartTaskOrderHelper.Instance.GetCountByIds(ids);
            if (count != 0)
            {
                return Result.GenError<Result>(Error.SmartTaskOrderNotExist);
            }
            SmartTaskOrderHelper.Instance.Delete(ids);
            return Result.GenError<Result>(Error.Success);
        }
    }
}