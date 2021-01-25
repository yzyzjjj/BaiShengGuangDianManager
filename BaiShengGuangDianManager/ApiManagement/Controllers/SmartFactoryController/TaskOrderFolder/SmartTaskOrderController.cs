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

namespace ApiManagement.Controllers.SmartFactoryController.TaskOrderFolder
{
    /// <summary>
    /// 
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]"), ApiController]
    public class SmartTaskOrderController : ControllerBase
    {
        // GET: api/SmartTaskOrder
        [HttpGet]
        public DataResult GetSmartTaskOrder([FromQuery]int qId, bool menu)
        {
            var result = new DataResult();
            var sql = menu ? $"SELECT Id, `TaskOrder` FROM `t_task_order` WHERE MarkedDelete = 0{(qId == 0 ? "" : " AND Id = @qId")} ORDER BY Id Desc;"
                : $"SELECT c.*, b.*, a.* FROM t_task_order a JOIN t_work_order b ON a.WorkOrderId = b.Id JOIN t_product c ON a.ProductId = c.Id WHERE a.MarkedDelete = 0{(qId == 0 ? "" : " AND a.Id = @qId")} ORDER BY a.Id Desc;";
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
                    var flowCards = SmartFlowCardHelper.GetSmartFlowCardsByTaskOrderId(d.Id);
                    var processes = SmartFlowCardProcessHelper.GetSmartFlowCardProcessesByFlowCardIds(flowCards.Select(x => x.Id));
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

        // Post: api/SmartSchedule/Capacity
        [HttpPost("Capacity")]
        public object PostSmartSchedulePreview([FromBody]IEnumerable<SmartTaskOrderCapacity> taskOrders)
        {
            if (taskOrders == null)
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            var taskIds = taskOrders.GroupBy(x => x.Id).Select(y => y.Key);
            if (taskIds.Count() != taskOrders.Count())
            {
                return Result.GenError<Result>(Error.SmartTaskOrderDuplicate);
            }
            var result = new SmartTaskOrderNeedWithOrderPreviewResult();
            if (taskOrders.Any())
            {
                var tOrders = SmartTaskOrderHelper.Instance.GetByIds<SmartTaskOrderCapacity>(taskIds);
                if (taskIds.Count() != tOrders.Count())
                {
                    result.errno = Error.SmartTaskOrderNotExist;
                    result.datas.AddRange(taskOrders.Where(x => tOrders.All(y => y.Id != x.Id)).Select(x => x.TaskOrder.IsNullOrEmpty() ? x.Id.ToString() : x.TaskOrder));
                    return result;
                }

                foreach (var task in taskOrders)
                {
                    var t = tOrders.FirstOrDefault(x => x.Id == task.Id);
                    if (t == null)
                    {
                        result.errno = Error.SmartTaskOrderNotExist;
                        result.datas.Add(task.TaskOrder);
                    }
                    else
                    {
                        task.TaskOrder = t.TaskOrder;
                        task.Target = t.Target;
                        task.DeliveryTime = t.DeliveryTime;
                        task.ProductId = t.ProductId;
                        task.CapacityId = t.CapacityId;
                    }
                }
            }

            if (result.errno != Error.Success)
            {
                return result;
            }
            var productIds = taskOrders.GroupBy(x => x.ProductId).Select(y => y.Key);
            if (!productIds.Any())
            {
                return Result.GenError<Result>(Error.SmartProductNotExist);
            }

            var products = SmartProductHelper.Instance.GetByIds<SmartProduct>(productIds);
            if (products.Count() != productIds.Count())
            {
                return Result.GenError<Result>(Error.SmartProductNotExist);
            }

            var productCapacities = SmartProductCapacityHelper.GetSmartProductCapacities(productIds);
            if (!productCapacities.Any())
            {
                return Result.GenError<Result>(Error.SmartProductCapacityNotExist);
            }

            var capacityIds = products.GroupBy(x => x.CapacityId).Select(y => y.Key);
            if (!capacityIds.Any())
            {
                return Result.GenError<Result>(Error.SmartCapacityNotExist);
            }
            var capacityLists = SmartCapacityListHelper.GetSmartCapacityListsWithOrder(capacityIds);
            foreach (var productId in productIds)
            {
                var tasks = taskOrders.Where(x => x.ProductId == productId);
                var product = products.FirstOrDefault(x => x.Id == productId);
                var pCapacities = productCapacities.Where(x => x.ProductId == productId);
                var cLists = capacityLists.Where(x => x.CapacityId == product.CapacityId);
                if (cLists.Count() != pCapacities.Count())
                {
                    result.errno = Error.SmartProductCapacityNotExist;
                    result.datas.AddRange(tasks.Select(x => x.TaskOrder));
                }
            }

            if (result.errno != Error.Success)
            {
                return result;
            }
            //设备型号
            var deviceModels = SmartDeviceModelHelper.Instance.GetAll<SmartDeviceModel>();
            //设备型号数量
            var deviceList = SmartDeviceHelper.GetNormalSmartDevices();
            var modelCounts = deviceList.GroupBy(x => new { x.CategoryId, x.ModelId }).Select(y => new SmartDeviceModelCount
            {
                CategoryId = y.Key.CategoryId,
                ModelId = y.Key.ModelId,
                Count = y.Count()
            });
            //人员等级
            var operatorLevels = SmartOperatorLevelHelper.Instance.GetAll<SmartOperatorLevel>();
            //人员等级数量
            var operatorList = SmartOperatorHelper.GetNormalSmartOperators();
            var operatorCounts = operatorList.GroupBy(x => new { x.ProcessId, x.LevelId }).Select(y => new SmartOperatorCount
            {
                ProcessId = y.Key.ProcessId,
                LevelId = y.Key.LevelId,
                Count = y.Count()
            });
            var taskNeeds = SmartTaskOrderNeedHelper.GetSmartTaskOrderNeedsByTaskOrderIds(taskIds);

            foreach (var task in taskOrders)
            {
                var needs = taskNeeds.Where(need => need.TaskOrderId == task.Id);
                if (task.Needs.Any())
                {
                    task.Needs = task.Needs.Select(x =>
                    {
                        var need = needs.FirstOrDefault(y => y.TaskOrderId == x.TaskOrderId && y.PId == x.PId);
                        if (need != null)
                        {
                            x.DoneTarget = need.DoneTarget;
                            x.HavePut = need.HavePut;
                        }

                        return x;
                    }).ToList();
                }
                var oldNeeds = task.Needs.ToList();
                task.Needs.Clear();
                var productId = task.ProductId;
                var product = products.FirstOrDefault(x => x.Id == productId);
                task.Product = product.Product;
                var pCapacities = productCapacities.Where(x => x.ProductId == productId);
                var cLists = capacityLists.Where(x => x.CapacityId == product.CapacityId);
                var target = task.Target;
                foreach (var cList in cLists.Reverse())
                {
                    var need = oldNeeds.FirstOrDefault(x => x.Id == cList.Id);
                    var stock = need?.Stock ?? 0;
                    var doneTarget = need?.DoneTarget ?? 0;
                    var havePut = need?.HavePut ?? 0;
                    if (target < stock)
                    {
                        stock = target;
                        target = 0;
                    }
                    else
                    {
                        target -= stock;
                    }

                    if (!task.All)
                    {
                        target = target > doneTarget ? target - doneTarget : 0;
                    }
                    var pCapacity = pCapacities.FirstOrDefault(x => x.ProcessId == cList.ProcessId);
                    var put = pCapacity.Rate != 0 ? (int)Math.Ceiling((target) * 100 / pCapacity.Rate) : 0;
                    var newNeed = new SmartTaskOrderCapacityNeed
                    {
                        Id = cList.Id,
                        TaskOrderId = task.Id,
                        ProductId = productId,
                        ProcessId = cList.ProcessId,
                        CategoryId = cList.CategoryId,
                        PId = cList.PId,
                        Target = target,
                        DoneTarget = doneTarget,
                        Stock = stock,
                        Rate = pCapacity.Rate,
                        Put = put,
                        HavePut = havePut,
                        Process = cList.Process,
                        Order = cList.Order
                    };
                    if (need != null)
                    {
                        newNeed.Devices = need.Devices;
                        newNeed.NeedDCapacity = newNeed.DCapacity != 0 ? ((decimal)put / newNeed.DCapacity).ToRound() : 0;
                        foreach (var device in need.DeviceList)
                        {
                            var modelCount = modelCounts.FirstOrDefault(x => x.ModelId == device.Key)?.Count ?? 0;
                            newNeed.TotalDCapacity += modelCount * device.Value.Item1 * device.Value.Item2;
                        }
                        newNeed.HaveDCapacity = newNeed.DCapacity != 0 ? ((decimal)newNeed.TotalDCapacity / newNeed.DCapacity).ToRound() : 0;

                        newNeed.Operators = need.Operators;
                        newNeed.NeedOCapacity = newNeed.OCapacity != 0 ? ((decimal)put / newNeed.OCapacity).ToRound() : 0;
                        foreach (var op in need.OperatorsList)
                        {
                            var operatorCount = operatorCounts.FirstOrDefault(x => x.LevelId == op.Key)?.Count ?? 0;
                            newNeed.TotalOCapacity += operatorCount * op.Value.Item1 * op.Value.Item2;
                        }
                        newNeed.HaveOCapacity = newNeed.OCapacity != 0 ? ((decimal)newNeed.TotalOCapacity / newNeed.OCapacity).ToRound() : 0;
                    }
                    task.Needs.Insert(0, newNeed);
                    target = put;
                }
            }
            result.datas.AddRange(taskOrders);
            var orders = taskOrders.SelectMany(x => x.Needs).GroupBy(y => new { y.PId, y.Order, y.Process }).Select(z => new SmartTaskOrderNeedWithOrderPreview
            {
                Id = z.Key.PId,
                Process = z.Key.Process,
                Order = z.Key.Order
            }).ToList();
            foreach (var task in taskOrders)
            {
                foreach (var newNeed in task.Needs)
                {
                    var order = orders.FirstOrDefault(x => x.Id == newNeed.PId);
                    if (order != null)
                    {
                        order.Stock += newNeed.Stock;
                        order.Put += newNeed.Put;
                        order.Target += newNeed.Target;
                        if (newNeed.DeviceList.Any())
                        {
                            foreach (var device in newNeed.DeviceList)
                            {
                                var modelDevice = order.Devices.FirstOrDefault(x => x.Id == device.Key);
                                if (modelDevice == null)
                                {
                                    order.Devices.Add(new SmartTaskOrderNeedCapacityPreview
                                    {
                                        Id = device.Key,
                                        Name = deviceModels.FirstOrDefault(x => x.Id == device.Key)?.Model ?? "",
                                    });
                                }
                                modelDevice = order.Devices.FirstOrDefault(x => x.Id == device.Key);
                                modelDevice.NeedCapacity += newNeed.NeedDCapacity;

                                var modelCount = modelCounts.FirstOrDefault(x => x.ModelId == device.Key)?.Count ?? 0;
                                modelDevice.HaveCapacity = modelCount;
                            }
                        }

                        if (newNeed.OperatorsList.Any())
                        {
                            foreach (var op in newNeed.OperatorsList)
                            {
                                var levelOp = order.Operators.FirstOrDefault(x => x.Id == op.Key);
                                if (levelOp == null)
                                {
                                    order.Operators.Add(new SmartTaskOrderNeedCapacityPreview
                                    {
                                        Id = op.Key,
                                        Name = operatorLevels.FirstOrDefault(x => x.Id == op.Key)?.Level ?? "",
                                    });
                                }
                                levelOp = order.Operators.FirstOrDefault(x => x.Id == op.Key);
                                levelOp.NeedCapacity += newNeed.NeedOCapacity;

                                var operatorCount = operatorCounts.FirstOrDefault(x => newNeed.PId == x.ProcessId && x.LevelId == op.Key)?.Count ?? 0;
                                levelOp.HaveCapacity = operatorCount;
                            }
                        }
                    }
                }
            }
            result.Orders.AddRange(orders.OrderBy(z => z.Order).Select(x =>
                {
                    x.Devices = x.Devices.OrderBy(y => y.Id).ToList();
                    x.Operators = x.Operators.OrderBy(y => y.Id).ToList();
                    return x;
                }));
            return result;
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