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

namespace ApiManagement.Controllers.SmartFactoryController.WorkOrderFolder
{
    /// <summary>
    /// 
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class SmartWorkOrderController : ControllerBase
    {
        // GET: api/SmartWorkOrder
        [HttpGet]
        public DataResult GetSmartWorkOrder([FromQuery]int qId, bool menu)
        {
            var result = new DataResult();
            var sql = menu ? $"SELECT Id, `WorkOrder` FROM `t_work_order` WHERE MarkedDelete = 0{(qId == 0 ? "" : " AND Id = @qId")};"
                : $"SELECT * FROM `t_work_order` WHERE MarkedDelete = 0{(qId == 0 ? "" : " AND Id = @qId")};";
            var data = ServerConfig.ApiDb.Query<SmartWorkOrderDetail>(sql, new { qId });
            if (qId != 0 && !data.Any())
            {
                result.errno = Error.SmartWorkOrderNotExist;
                return result;
            }
            if (qId != 0 && !menu)
            {
                var d = data.First();
                if (d.State != SmartWorkOrderState.未加工)
                {
                    var taskOrders = SmartTaskOrderHelper.GetSmartTaskOrdersByWorkOrderId(d.Id);
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
            if (menu)
            {
                result.datas.AddRange(data.Select(x => new
                {
                    x.Id,
                    x.WorkOrder
                }));
            }
            else
            {
                result.datas.AddRange(data);
            }
            return result;
        }

        // PUT: api/SmartWorkOrder
        [HttpPut]
        public Result PutSmartWorkOrder([FromBody] IEnumerable<SmartWorkOrder> smartWorkOrders)
        {
            if (smartWorkOrders == null || !smartWorkOrders.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            var data =
                ServerConfig.ApiDb.Query<SmartWorkOrder>("SELECT * FROM `t_work_order` WHERE Id IN @Id AND MarkedDelete = 0;", new { Id = smartWorkOrders.Select(x => x.Id) });
            if (data.Count() != smartWorkOrders.Count())
            {
                return Result.GenError<Result>(Error.SmartWorkOrderNotExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartWorkOrder in smartWorkOrders)
            {
                smartWorkOrder.CreateUserId = createUserId;
                smartWorkOrder.MarkedDateTime = markedDateTime;
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `t_work_order` SET `MarkedDateTime` = @MarkedDateTime, `WorkOrder` = @WorkOrder, `Target` = @Target, `DeliveryTime` = @DeliveryTime, `Remark` = @Remark WHERE `Id` = @Id;", smartWorkOrders);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartWorkOrder
        [HttpPost]
        public Result PostSmartWorkOrder([FromBody] IEnumerable<SmartWorkOrder> smartWorkOrders)
        {
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartWorkOrder in smartWorkOrders)
            {
                smartWorkOrder.CreateUserId = createUserId;
                smartWorkOrder.MarkedDateTime = markedDateTime;
            }
            ServerConfig.ApiDb.Execute(
                "INSERT INTO `t_work_order` (`CreateUserId`, `MarkedDateTime`, `WorkOrder`, `Target`, `DeliveryTime`, `Remark`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @WorkOrder, @Target, @DeliveryTime, @Remark);",
                smartWorkOrders);

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
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `t_work_order` WHERE Id IN @id AND `MarkedDelete` = 0;", new { id = ids }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.SmartWorkOrderNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `t_work_order` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`IN @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = ids
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}