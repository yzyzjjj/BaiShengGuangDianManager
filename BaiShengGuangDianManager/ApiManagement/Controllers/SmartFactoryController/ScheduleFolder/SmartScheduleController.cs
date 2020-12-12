using ApiManagement.Base.Helper;
using ApiManagement.Base.Server;
using ApiManagement.Models.SmartFactoryModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.SmartFactoryController.ScheduleFolder
{
    /// <summary>
    /// 
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]"), ApiController]
    public class SmartScheduleController : ControllerBase
    {
        // Post: api/SmartSchedule/Put
        [HttpGet("Put")]
        public object GetArrangedTaskOrderPut([FromQuery]DateTime startTime, DateTime endTime, int pId)
        {
            var result = new DataResult();
            if (startTime == default(DateTime) || endTime == default(DateTime) || endTime < startTime)
            {
                return result;
            }

            var r = ServerConfig.ApiDb.Query<SmartTaskOrderScheduleInfoResult1>(
                $"SELECT a.ProcessTime, a.ProductId, b.Product, a.PId, c.Process, c.`Order`, SUM(a.Put) Put, SUM(a.HavePut) HavePut " +
                $"FROM `t_task_order_schedule` a " +
                $"JOIN `t_product` b ON a.ProductId = b.Id " +
                $"JOIN `t_process` c ON a.PId = c.Id " +
                $"JOIN (SELECT * FROM (SELECT ProcessTime, Batch FROM `t_task_order_schedule` ORDER BY Batch DESC, ProcessTime DESC) a GROUP BY a.ProcessTime) d ON a.ProcessTime = d.ProcessTime AND a.Batch = d.Batch " +
                $"WHERE a.ProcessTime >= @startTime AND a.ProcessTime <= @endTime {(pId == 0 ? "" : " AND a.PId = @pId")} " +
                $"GROUP BY a.ProcessTime, a.ProductId, a.PId ORDER BY a.ProcessTime, a.ProductId, c.`Order`;", new
                {
                    startTime,
                    endTime,
                    pId
                });
            if (r.Any())
            {
                //按工序排
                var processes = r.GroupBy(x => new { x.PId, x.Process, x.Order }).Select(y => y.Key);
                result.datas.AddRange(processes.Select(process =>
                {
                    var value = r.Where(y => y.PId == process.PId);
                    var products = value.GroupBy(y => new { y.ProductId, y.Product }).Select(z => z.Key);
                    var datas = new List<object>();
                    if (products.Any())
                    {
                        var data = products.Select(product =>
                        {
                            var dates = new List<object>();
                            var pro = value.Where(z => z.ProductId == product.ProductId);
                            for (var i = 0; i < (endTime - startTime).TotalDays + 1; i++)
                            {
                                var t = startTime.AddDays(i);
                                var pr = pro.Where(p => p.ProcessTime == t);
                                dates.Add(new
                                {
                                    ProcessTime = t,
                                    Put = pr.Sum(x => x.Put),
                                    HavePut = pr.Sum(x => x.HavePut),
                                });
                            }

                            var b = new
                            {
                                ProductId = product.ProductId,
                                Product = product.Product,
                                Data = dates
                            };
                            return b;
                        });
                        datas.AddRange(data);
                    }
                    var a = new
                    {
                        PId = process.PId,
                        Process = process.Process,
                        Data = datas
                    };
                    return a;
                }));
            }
            return result;
        }

        // Post: api/SmartSchedule/Put/Detail
        [HttpGet("Put/Detail")]
        public object GetArrangedTaskOrderPutDetail([FromQuery]DateTime time, int productId, int pId)
        {
            var result = new DataResult();
            if (time == default(DateTime) || productId == 0 || pId == 0)
            {
                return result;
            }

            var data = ServerConfig.ApiDb.Query<SmartTaskOrderScheduleInfoResult11>(
                $"SELECT a.ProcessTime, a.TaskOrderId, b.TaskOrder, a.Put, a.HavePut, a.IsDevice, a.Devices, a.Operators " +
                $"FROM `t_task_order_schedule` a " +
                $"JOIN `t_task_order` b ON a.TaskOrderId = b.Id " +
                $"JOIN (SELECT * FROM (SELECT ProcessTime, Batch FROM `t_task_order_schedule` ORDER BY Batch DESC, ProcessTime DESC) a GROUP BY a.ProcessTime) d ON a.ProcessTime = d.ProcessTime AND a.Batch = d.Batch " +
                $"WHERE a.ProductId = @productId AND a.PId = @pId AND a.ProcessTime = @time " +
                $"ORDER BY a.TaskOrderId;", new
                {
                    time,
                    productId,
                    pId
                });
            if (data.Any())
            {
                //设备型号数量
                var deviceList = SmartDeviceHelper.Instance.GetAll<SmartDevice>();
                //人员等级数量
                var operatorList = SmartOperatorHelper.Instance.GetAllSmartOperators();
                foreach (var x in data)
                {
                    x.Arranges = x.Device
                        ? x.DeviceList.ToDictionary(y => y.Key,
                            y => new Tuple<string, int>(deviceList.FirstOrDefault(z => z.Id == y.Key)?.Code ?? "",
                                y.Value))
                        : x.OperatorsList.ToDictionary(y => y.Key,
                            y => new Tuple<string, int>(operatorList.FirstOrDefault(z => z.Id == y.Key)?.Name ?? "",
                                y.Value));
                }
                result.datas.AddRange(data);
            }
            return result;
        }

        // Post: api/SmartSchedule/Warehouse
        [HttpGet("Warehouse")]
        public object GetArrangedTaskOrderWarehouse([FromQuery]DateTime startTime, DateTime endTime)
        {
            var result = new DataResult();
            if (startTime == default(DateTime) || endTime == default(DateTime))
            {
                return result;
            }

            var r = ServerConfig.ApiDb.Query<SmartTaskOrderScheduleInfoResult2>(
                $"SELECT a.ProcessTime, a.ProductId, b.Product, a.PId, c.Process, c.`Order`, SUM(a.Target) Target, SUM(a.DoneTarget) DoneTarget " +
                $"FROM `t_task_order_schedule` a " +
                $"JOIN `t_product` b ON a.ProductId = b.Id JOIN `t_process` c ON a.PId = c.Id " +
                $"JOIN (SELECT * FROM (SELECT ProcessTime, Batch FROM `t_task_order_schedule` ORDER BY Batch DESC, ProcessTime DESC) a GROUP BY a.ProcessTime) d ON a.ProcessTime = d.ProcessTime AND a.Batch = d.Batch " +
                $"WHERE a.ProcessTime >= @startTime AND a.ProcessTime <= @endTime " +
                $"GROUP BY a.ProcessTime, a.ProductId, a.PId ORDER BY a.ProcessTime, a.ProductId, c.`Order`;", new
                {
                    startTime,
                    endTime
                });
            if (r.Any())
            {
                //按工序排
                var processes = r.GroupBy(x => new { x.PId, x.Process, x.Order }).Select(y => y.Key);
                result.datas.AddRange(processes.Select(process =>
                {
                    var value = r.Where(y => y.PId == process.PId);
                    var products = value.GroupBy(y => new { y.ProductId, y.Product }).Select(z => z.Key);
                    var datas = new List<object>();
                    if (products.Any())
                    {
                        var data = products.Select(product =>
                        {
                            var dates = new List<object>();
                            var pro = value.Where(z => z.ProductId == product.ProductId);
                            for (var i = 0; i < (endTime - startTime).TotalDays + 1; i++)
                            {
                                var t = startTime.AddDays(i);
                                var pr = pro.Where(p => p.ProcessTime == t);
                                dates.Add(new
                                {
                                    ProcessTime = t,
                                    Target = pr.Sum(x => x.Target),
                                    DoneTarget = pr.Sum(x => x.DoneTarget),
                                });
                            }

                            var b = new
                            {
                                ProductId = product.ProductId,
                                Product = product.Product,
                                Data = dates
                            };
                            return b;
                        });
                        datas.AddRange(data);
                    }
                    var a = new
                    {
                        PId = process.PId,
                        Process = process.Process,
                        Data = datas
                    };
                    return a;
                }));
            }
            return result;
        }

        // Post: api/SmartSchedule/Warehouse/Detail
        [HttpGet("Warehouse/Detail")]
        public object GetArrangedTaskOrderWarehouseDetail([FromQuery]DateTime time, int productId, int pId)
        {
            var result = new DataResult();
            if (time == default(DateTime) || productId == 0 || pId == 0)
            {
                return result;
            }

            result.datas.AddRange(ServerConfig.ApiDb.Query<dynamic>(
                $"SELECT a.ProcessTime, a.TaskOrderId, b.TaskOrder, a.Target, a.DoneTarget " +
                $"FROM `t_task_order_schedule` a " +
                $"JOIN `t_task_order` b ON a.TaskOrderId = b.Id " +
                $"JOIN (SELECT * FROM (SELECT ProcessTime, Batch FROM `t_task_order_schedule` ORDER BY Batch DESC, ProcessTime DESC) a GROUP BY a.ProcessTime) d ON a.ProcessTime = d.ProcessTime AND a.Batch = d.Batch " +
                $"WHERE a.ProductId = @productId AND a.PId = @pId AND a.ProcessTime = @time " +
                $"ORDER BY a.TaskOrderId;", new
                {
                    time,
                    productId,
                    pId
                }));
            return result;
        }

        // Post: api/SmartSchedule/ArrangedTaskOrder
        [HttpGet("ArrangedTaskOrder")]
        public object GetArrangedTaskOrder([FromQuery] int page, int limit = 30)
        {
            page = page < 0 ? 0 : page;
            limit = limit < 0 ? 30 : limit;
            var result = new SmartResult();
            var state = new[]
            {
                SmartTaskOrderState.加工中,
                SmartTaskOrderState.等待中,
                SmartTaskOrderState.未加工,
                SmartTaskOrderState.暂停中,
                SmartTaskOrderState.已取消,
                SmartTaskOrderState.已完成,
            }.Select(x => (int)x).Join();
            result.datas.AddRange(ServerConfig.ApiDb.Query<SmartTaskOrderDetailLevel>($"SELECT a.*, b.Product, c.Level FROM `t_task_order` a " +
                                                                                        $"JOIN `t_product` b ON a.ProductId = b.Id " +
                                                                                        $"JOIN `t_task_order_level` c ON a.LevelId = c.Id " +
                                                                                        $"WHERE Arranged = 1 AND a.MarkedDelete = 0 ORDER BY FIELD(State,  {state}) LIMIT @page, @limit;", new
                                                                                        {
                                                                                            page,
                                                                                            limit
                                                                                        }));

            result.Count = ServerConfig.ApiDb.Query<int>($"SELECT COUNT(1) FROM `t_task_order` a " +
                                                                                      $"JOIN `t_product` b ON a.ProductId = b.Id " +
                                                                                      $"JOIN `t_task_order_level` c ON a.LevelId = c.Id " +
                                                                                      $"WHERE Arranged = 1 AND a.MarkedDelete = 0 ORDER BY FIELD(State,  {state});").FirstOrDefault();
            return result;
        }

        // Post: api/SmartSchedule/NotArrangedTaskOrder
        [HttpGet("NotArrangedTaskOrder")]
        public object GetSmartScheduleNotArranged([FromQuery] int page, int limit = 30)
        {
            var result = new SmartResult();
            result.datas.AddRange(ServerConfig.ApiDb.Query<SmartTaskOrderDetailProduct>($"SELECT a.*, b.Product FROM `t_task_order` a " +
                                                                                        $"JOIN `t_product` b ON a.ProductId = b.Id " +
                                                                                        $"WHERE Arranged = 0 AND a.MarkedDelete = 0 LIMIT @page, @limit;", new
                                                                                        {
                                                                                            page,
                                                                                            limit
                                                                                        }));
            result.Count = ServerConfig.ApiDb.Query<int>($"SELECT COUNT(1) FROM `t_task_order` a " +
                                                         $"JOIN `t_product` b ON a.ProductId = b.Id " +
                                                         $"WHERE Arranged = 0 AND a.MarkedDelete = 0;").FirstOrDefault();

            return result;
        }

        // Post: api/SmartSchedule/Preview
        [HttpPost("Preview")]
        public object PostSmartSchedulePreview([FromBody]IEnumerable<SmartTaskOrderPreview> taskOrders)
        {
            if (taskOrders == null || !taskOrders.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            var taskIds = taskOrders.GroupBy(x => x.Id).Select(y => y.Key);
            if (taskIds.Count() != taskOrders.Count())
            {
                return Result.GenError<Result>(Error.SmartTaskOrderDuplicate);
            }

            var result = new DataResult();
            var tOrders = SmartTaskOrderHelper.Instance.GetByIds<SmartTaskOrderPreview>(taskIds);
            if (taskIds.Count() != tOrders.Count())
            {
                result.errno = Error.SmartTaskOrderNotExist;
                result.datas.AddRange(taskOrders.Where(x => tOrders.All(y => y.Id != x.Id)).Select(x => x.TaskOrder.IsNullOrEmpty() ? x.Id.ToString() : x.TaskOrder));
                return result;
            }

            var arranged = tOrders.Where(x => x.Arranged);
            if (arranged.Any())
            {
                result.errno = Error.SmartTaskOrderArranged;
                result.datas.AddRange(arranged.Select(x => x.TaskOrder));
                return result;
            }

            foreach (var task in tOrders)
            {
                var t = taskOrders.FirstOrDefault(x => x.Id == task.Id);
                if (t == null)
                {
                    result.errno = Error.SmartTaskOrderNotExist;
                    result.datas.Add(task.TaskOrder);
                }

                task.StartTime = t.StartTime;
                task.EndTime = t.EndTime;
                task.OldNeeds = t.Needs;
            }

            if (result.errno != Error.Success)
            {
                return result;
            }
            var productIds = tOrders.GroupBy(x => x.ProductId).Select(y => y.Key);
            if (!productIds.Any())
            {
                return Result.GenError<Result>(Error.SmartProductNotExist);
            }

            var products = SmartProductHelper.Instance.GetByIds<SmartProduct>(productIds);
            if (products.Count() != productIds.Count())
            {
                return Result.GenError<Result>(Error.SmartProductNotExist);
            }

            var productCapacities = SmartProductCapacityHelper.Instance.GetSmartProductCapacities(productIds);
            if (!productCapacities.Any())
            {
                return Result.GenError<Result>(Error.SmartProductCapacityNotExist);
            }

            var capacityIds = products.GroupBy(x => x.CapacityId).Select(y => y.Key);
            if (!capacityIds.Any())
            {
                return Result.GenError<Result>(Error.SmartCapacityNotExist);
            }
            var capacityLists = SmartCapacityListHelper.Instance.GetSmartCapacityListsWithOrder(capacityIds);
            foreach (var productId in productIds)
            {
                var tasks = tOrders.Where(x => x.ProductId == productId);
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

            var data = tOrders.ToDictionary(x => x.Id, x => new SmartTaskOrderPreview
            {
                Id = x.Id,
                TaskOrder = x.TaskOrder,
                ProductId = x.ProductId,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                Product = products.FirstOrDefault(y => y.Id == x.ProductId)?.Product ?? ""
            });
            foreach (var task in tOrders)
            {
                var productId = task.ProductId;
                var product = products.FirstOrDefault(x => x.Id == productId);
                var pCapacities = productCapacities.Where(x => x.ProductId == productId);
                var cLists = capacityLists.Where(x => x.CapacityId == product.CapacityId);
                var target = task.Target;
                foreach (var list in cLists.Reverse())
                {
                    var stock = 0;
                    if (task.OldNeeds.Any())
                    {
                        stock = task.OldNeeds.FirstOrDefault(x => x.Order == list.Order && x.ProcessId == list.ProcessId)?.Stock ?? 0;
                    }

                    if (target < stock)
                    {
                        stock = target;
                        target = 0;
                    }
                    else
                    {
                        target -= stock;
                    }

                    var pCapacity = pCapacities.FirstOrDefault(x => x.ProcessId == list.ProcessId);
                    var put = pCapacity.Rate != 0 ? (int)Math.Ceiling(target * 100 / pCapacity.Rate) : 0;
                    data[task.Id].Needs.Insert(0, new SmartTaskOrderNeedDetail
                    {
                        TaskOrderId = task.Id,
                        ProductId = productId,
                        ProcessId = list.ProcessId,
                        PId = list.PId,
                        Target = target,
                        Stock = stock,
                        Rate = pCapacity.Rate,
                        Put = put,
                        Process = list.Process,
                        Order = list.Order
                    });
                    target = put;
                }
            }
            var r = new SmartTaskOrderNeedDetailResult();
            r.datas.AddRange(data.Values);
            var orders = data.Values.SelectMany(x => x.Needs).GroupBy(y => new { y.PId, y.Order, y.Process }).Select(z => new SmartTaskOrderNeedOrder
            {
                Id = z.Key.PId,
                Process = z.Key.Process,
                Order = z.Key.Order
            });
            r.Orders.AddRange(orders.OrderBy(z => z.Order));
            return r;
        }

        // Post: api/SmartSchedule/Preview
        [HttpPost("TaskOrderLevel")]
        public object PostSmartTaskOrderLevel([FromBody]IEnumerable<SmartTaskOrder> taskOrders)
        {
            if (taskOrders == null || !taskOrders.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            var taskIds = taskOrders.GroupBy(x => x.Id).Select(y => y.Key);
            if (taskIds.Count() != taskOrders.Count())
            {
                return Result.GenError<Result>(Error.SmartTaskOrderDuplicate);
            }

            var result = new DataResult();
            var tOrders = SmartTaskOrderHelper.Instance.GetByIds<SmartTaskOrder>(taskIds);
            if (taskIds.Count() != tOrders.Count())
            {
                result.errno = Error.SmartTaskOrderNotExist;
                result.datas.AddRange(taskOrders.Where(x => tOrders.All(y => y.Id != x.Id)).Select(x => x.TaskOrder));
                return result;
            }

            var notArranged = tOrders.Where(x => !x.Arranged);
            if (notArranged.Any())
            {
                result.errno = Error.SmartTaskOrderNotArranged;
                result.datas.AddRange(notArranged.Select(x => x.TaskOrder));
                return result;
            }

            ServerConfig.ApiDb.Execute("UPDATE `t_task_order` SET `MarkedDateTime` = @MarkedDateTime, `LevelId` = @LevelId, `StartTime` = IF(@StartTime = '0001-01-01 00:00:00', `StartTime`, @StartTime), `EndTime` = IF(@EndTime = '0001-01-01 00:00:00', `EndTime`, @EndTime) WHERE `Id` = @Id;", taskOrders);
            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 获取任务单排产天数
        /// </summary>
        /// <param name="taskOrders"></param>
        /// <returns></returns>
        // Post: api/SmartSchedule/CostDay
        [HttpPost("CostDay")]
        public object PostSmartScheduleCostDay([FromBody]IEnumerable<SmartTaskOrderConfirm> taskOrders)
        {
            if (taskOrders == null)
            {
                taskOrders = new List<SmartTaskOrderConfirm>();
            }

            if (taskOrders.Any(x => !x.Needs.Any()))
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            if (taskOrders.Any(x => !x.Needs.Any()))
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            var today = DateTime.Today;
            var schedules = new List<SmartTaskOrderScheduleDetail>();
            var costDays = ScheduleHelper.ArrangeSchedule(taskOrders, ref schedules);
            var startTime = costDays.Any() ? costDays.Min(x => x.EstimatedStartTime) : today;
            if (startTime == default(DateTime))
            {
                startTime = today;
            }
            var completeTime = costDays.Any() ? costDays.Max(x => x.EstimatedCompleteTime) : today;
            if (completeTime == default(DateTime))
            {
                completeTime = today;
            }
            var put = new List<object>();
            var orders = new List<SmartTaskOrderNeedOrder>();
            if (schedules.Any())
            {
                //设备型号数量
                var deviceList = SmartDeviceHelper.Instance.GetAll<SmartDevice>();
                //人员等级数量
                var operatorList = SmartOperatorHelper.Instance.GetAllSmartOperators();
                //按工序排
                var processes = schedules.GroupBy(x => new { x.PId, x.Process, x.Order }).Select(y => y.Key);
                put.AddRange(processes.Select(process =>
                {
                    var value = schedules.Where(y => y.PId == process.PId);
                    var products = value.GroupBy(y => new { y.ProductId, y.Product }).Select(z => z.Key);
                    var datas = new List<object>();
                    if (products.Any())
                    {
                        var data = products.Select(product =>
                        {
                            var pro = value.Where(z => z.ProductId == product.ProductId);
                            var dates = new List<dynamic>();
                            for (var i = 0; i < (completeTime - startTime).TotalDays + 1; i++)
                            {
                                var t = startTime.AddDays(i);
                                var pr = pro.Where(p => p.ProcessTime == t);
                                var d = new List<SmartTaskOrderScheduleInfoResult11>();
                                if (pr.Any())
                                {
                                    d = pr.Select(x => new SmartTaskOrderScheduleInfoResult11
                                    {
                                        ProcessTime = t,
                                        TaskOrderId = x.TaskOrderId,
                                        TaskOrder = x.TaskOrder,
                                        Put = x.Put,
                                        HavePut = x.HavePut,
                                        Arranges = x.Device
                                            ? x.DeviceList.ToDictionary(y => y.Key, y => new Tuple<string, int>(deviceList.FirstOrDefault(z => z.Id == y.Key)?.Code ?? "", y.Value))
                                            : x.OperatorsList.ToDictionary(y => y.Key, y => new Tuple<string, int>(operatorList.FirstOrDefault(z => z.Id == y.Key)?.Name ?? "", y.Value))
                                    }).ToList();
                                    //}).OrderBy(x => x.TaskOrderId).ToList();
                                }

                                dates.Add(new
                                {
                                    ProcessTime = t,
                                    Put = pr.Sum(x => x.Put),
                                    HavePut = pr.Sum(x => x.HavePut),
                                    Data = d
                                });
                            }

                            var b = new
                            {
                                ProductId = product.ProductId,
                                Product = product.Product,
                                //Data = dates.OrderBy(x => x.ProcessTime)
                                Data = dates
                            };
                            return b;
                        }).OrderBy(x => x.ProductId);
                        datas.AddRange(data);
                    }
                    var a = new
                    {
                        Order = process.Order,
                        PId = process.PId,
                        Process = process.Process,
                        Data = datas
                    };
                    return a;
                }).OrderBy(x => x.Order));

                orders.AddRange(costDays.SelectMany(x => x.CostDays).GroupBy(y => new { y.PId, y.Process, y.Order }).Select(z => new SmartTaskOrderNeedOrder
                {
                    Id = z.Key.PId,
                    Process = z.Key.Process,
                    Order = z.Key.Order
                }).OrderBy(z => z.Order));
            }

            return new
            {
                errno = 0,
                errmsg = "成功",
                StartTime = startTime,
                CompleteTime = completeTime,
                Orders = orders,
                Cost = costDays,
                //Schedule = schedules,
                Schedule = schedules.Select(x =>
                    new
                    {
                        x.ProcessTime,
                        x.PId,
                        x.TaskOrder,
                        x.TaskOrderId,
                        x.ProcessId,
                        x.ProductId,
                        x.Target,
                        x.Put,
                        x.Have,
                        x.HavePut,
                        x.LeftPut,
                        x.Rate,
                        x.Stock,
                        x.LeftTarget,
                        x.DoneTarget,
                        x.Done
                    }),
                Put = put,
            };
        }

        /// <summary>
        /// 任务单确定排产
        /// </summary>
        /// <param name="taskOrders"></param>
        /// <returns></returns>
        // Post: api/SmartSchedule/Confirm
        [HttpPost("Confirm")]
        public object PostSmartScheduleConfirm([FromBody]SmartTaskOrderArrange arrange)
        {
            if (arrange == null)
            {
                arrange = new SmartTaskOrderArrange();
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            var schedules = new List<SmartTaskOrderScheduleDetail>();
            var costDays = ScheduleHelper.ArrangeSchedule(arrange.TaskOrders, ref schedules, true, createUserId, markedDateTime);
            WorkFlowHelper.Instance.OnTaskOrderArrangeChanged();
            return Result.GenError<Result>(Error.Success);
        }
    }
}