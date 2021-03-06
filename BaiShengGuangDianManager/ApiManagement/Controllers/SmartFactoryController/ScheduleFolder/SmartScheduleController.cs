﻿using ApiManagement.Base.Helper;
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
using ModelBase.Models.BaseModel;

namespace ApiManagement.Controllers.SmartFactoryController.ScheduleFolder
{
    /// <summary>
    /// 
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]"), ApiController]
    public class SmartScheduleController : ControllerBase
    {
        // Post: api/SmartSchedule/KanBan
        [HttpGet("KanBan")]
        public object GetKanBan([FromQuery]int wId, int page)
        {
            var result = new SmartTaskOrderNeedWithOrderResult();
            var workshop = SmartWorkshopHelper.Instance.Get<SmartWorkshop>(wId);
            if (workshop == null)
            {
                return result;
            }
            page = page < 0 ? 0 : page;
            var limit = workshop.Length < 0 ? 20 : workshop.Length;
            var allData = SmartTaskOrderKanBanHelper.GetDetail(wId);
            var data = allData.Skip(page * limit).Take(limit);
            var orders = data.SelectMany(x => x.Needs).GroupBy(y => new { y.PId, y.Order, y.Process }).Select(z => new SmartTaskOrderNeedWithOrder
            {
                Id = z.Key.PId,
                Process = z.Key.Process,
                Order = z.Key.Order
            });
            result.Orders.AddRange(orders.OrderBy(z => z.Order));
            result.datas.AddRange(data);
            result.Count = allData.Count();
            return result;
        }

        // Post: api/SmartSchedule/PutAndWarehouse
        [HttpGet("PutAndWarehouse")]
        public object GetArrangedTaskOrderPutAndWarehouse([FromQuery]int wId, DateTime startTime, DateTime endTime, DateTime deliveryTime, bool all)
        {
            var result = new SmartTaskOrderNeedOrderTimeResult();
            if (startTime == default(DateTime) || endTime == default(DateTime))
            {
                return result;
            }
            var schedules = SmartTaskOrderScheduleHelper.GetSmartTaskOrderSchedule(wId, startTime, endTime);
            var tasks = SmartTaskOrderHelper.GetAllArrangedButNotDoneSmartTaskOrderDetails(wId, deliveryTime, all);
            var taskIds = tasks.Select(x => x.Id).Concat(schedules.Select(y => y.TaskOrderId)).Distinct();
            if (!taskIds.Any())
            {
                return result;
            }

            tasks = SmartTaskOrderHelper.GetAllArrangedButNotDoneSmartTaskOrderDetails(wId, taskIds);
            var taskNeeds = SmartTaskOrderNeedHelper.GetSmartTaskOrderNeedsByTaskOrderIds(wId, taskIds, true);
            var orders = taskNeeds.GroupBy(y => new { y.PId, y.Order, y.Process, y.CategoryId }).Select(z => new SmartTaskOrderNeedWithOrder
            {
                Id = z.Key.PId,
                Process = z.Key.Process,
                CategoryId = z.Key.CategoryId,
                Order = z.Key.Order
            });
            result.StartTime = startTime;
            result.EndTime = endTime;
            result.Orders.AddRange(orders.OrderBy(z => z.Order));

            //设备型号数量
            var deviceList = SmartDeviceHelper.GetNormalSmartDevices(wId);
            //人员等级数量
            var operatorList = SmartOperatorHelper.GetNormalSmartOperators(wId);
            var modelCount = deviceList.GroupBy(x => new { x.CategoryId }).Select(y => new SmartDeviceModelCount
            {
                CategoryId = y.Key.CategoryId,
                Count = y.Count()
            });
            var operatorCount = operatorList.GroupBy(x => new { x.ProcessId }).Select(y => new SmartOperatorCount
            {
                ProcessId = y.Key.ProcessId,
                Count = y.Count()
            });
            //if (schedules.Any())
            //{
            //    var productIds = schedules.Select(x => x.ProductId);
            //    // 任务单计划号
            //    var products = SmartProductHelper.Instance.GetByIds<SmartProduct>(productIds);
            //    var capacityIds = products.Select(x => x.CapacityId);
            //    // 产能设置
            //    var smartCapacityLists = SmartCapacityListHelper.Instance.GetAllSmartCapacityListsWithOrder(capacityIds);
            //    //if ()
            //    //{

            //    //}
            //    ////产能配置
            //    //var capacityList = smartCapacityLists.Where(x => x.CapacityId == task.CapacityId);
            //    ////工序单日产能配置
            //    //var cList = capacityList.FirstOrDefault(x => x.ProcessId == y.ProcessId);
            //    //sc.Index
            //}
            var ts = taskNeeds.Where(n => tasks.Any(task => task.Id == n.TaskOrderId)).Select(x =>
            {
                var task = tasks.First(t => t.Id == x.TaskOrderId);
                var need = new SmartTaskOrderScheduleSumInfoResult
                {
                    Id = x.Id,
                    TaskOrderId = x.TaskOrderId,
                    TaskOrder = x.TaskOrder,
                    ProductId = x.ProductId,
                    Product = x.Product,
                    DeliveryTime = task.DeliveryTime,
                    ArrangedTime = task.ArrangedTime,
                    PId = x.PId,
                    ProcessId = x.ProcessId,
                    Process = x.Process,
                    Order = x.Order,
                    Put = x.Put,
                    HavePut = x.HavePut,
                    Target = x.Target,
                    DoneTarget = x.DoneTarget,
                };
                var schedule = schedules.Where(sc => sc.TaskOrderId == need.TaskOrderId && sc.ProcessId == need.ProcessId && sc.PId == need.PId).ToList();
                for (var i = 0; i < (result.EndTime - result.StartTime).TotalDays + 1; i++)
                {
                    var t = result.StartTime.AddDays(i);
                    if (schedule.All(p => p.ProcessTime != t))
                    {
                        schedule.Add(new SmartTaskOrderScheduleDetail()
                        {
                            ProcessTime = t
                        });
                    }
                }

                schedule = schedule.OrderBy(sc => sc.ProcessTime).ToList();
                need.Schedules.AddRange(schedule.Select(y => new SmartTaskOrderScheduleInfoResult
                {
                    Id = y.Id,
                    ProductType = y.ProductType,
                    ProcessTime = y.ProcessTime,
                    Put = y.Put,
                    HavePut = y.HavePut,
                    Target = y.Target,
                    DoneTarget = y.DoneTarget,
                }));
                return need;
            });
            result.datas.AddRange(ts.OrderBy(x => x.DeliveryTime).ThenBy(x => x.ArrangedTime));

            var indexes = SmartTaskOrderScheduleIndexHelper.GetSmartTaskOrderScheduleIndex(wId, startTime, endTime).ToList();
            var arrangeIndexes = new List<SmartTaskOrderScheduleIndex>();
            foreach (var order in result.Orders)
            {
                for (var i = 0; i < (result.EndTime - result.StartTime).TotalDays + 1; i++)
                {
                    var t = result.StartTime.AddDays(i);
                    if (!indexes.Any(p => p.ProcessTime == t && p.PId == order.Id))
                    {
                        arrangeIndexes.Add(new SmartTaskOrderScheduleIndex
                        {
                            ProcessTime = t,
                            PId = order.Id,
                        });
                    }
                    else
                    {
                        var ins = indexes.Where(p => p.ProcessTime == t && p.PId == order.Id);
                        decimal index = 0;
                        if (ins.Any())
                        {
                            var cnt = ins.First().ProductType == 0
                                ? (modelCount.FirstOrDefault(x => x.CategoryId == order.CategoryId)?.Count ?? 0)
                                : (operatorCount.FirstOrDefault(x => x.ProcessId == order.Id)?.Count ?? 0);
                            index = cnt == 0 ? 0 : (ins.Sum(x => x.Index) / cnt).ToRound(2);
                        }
                        arrangeIndexes.Add(new SmartTaskOrderScheduleIndex
                        {
                            ProcessTime = t,
                            PId = order.Id,
                            Index = index
                        });
                    }
                }
            }
            result.Indexes.AddRange(arrangeIndexes.OrderBy(x => result.Orders.FirstOrDefault(y => y.Id == x.PId)?.Order ?? 0).ThenBy(z => z.ProcessTime));
            return result;
        }

        // Post: api/SmartSchedule/Put/Detail
        [HttpGet("Put/Detail")]
        public object GetArrangedTaskOrderPutDetail([FromQuery]int wId, int id, int taskOrderId, int pId)
        {
            var result = new DataResult();
            IEnumerable<SmartTaskOrderSchedulePutInfoResult> data = null;
            if (id != 0)
            {
                var sql = $"SELECT a.ProcessTime, a.TaskOrderId, b.TaskOrder, a.ProductId, a.ProductType, a.Put, a.HavePut, a.Devices, a.Operators " +
                             $"FROM `t_task_order_schedule` a " +
                             $"JOIN `t_task_order` b ON a.TaskOrderId = b.Id " +
                             $"WHERE a.Id = @id AND a.WorkshopId = @wId;";
                data = ServerConfig.ApiDb.Query<SmartTaskOrderSchedulePutInfoResult>(sql, new
                {
                    wId,
                    id,
                    taskOrderId
                });
            }
            else if (taskOrderId != 0 && pId != 0)
            {
                data = SmartTaskOrderScheduleHelper.GetSmartTaskOrderSchedule(wId, taskOrderId, pId).Select(x => new SmartTaskOrderSchedulePutInfoResult
                {
                    ProcessTime = x.ProcessTime,
                    ProductType = x.ProductType,
                    TaskOrderId = x.TaskOrderId,
                    TaskOrder = x.TaskOrder,
                    ProductId = x.ProductId,
                    Put = x.Target,
                    HavePut = x.DoneTarget
                });
            }

            if (data == null)
            {
                return result;
            }
            data = data.Where(x => x.Put > 0);
            if (!data.Any())
            {
                return result;
            }
            //设备型号数量
            var deviceList = SmartDeviceHelper.Instance.GetAll<SmartDevice>();
            //人员等级数量
            var operatorList = SmartOperatorHelper.GetAllSmartOperators();
            //// 任务单计划号
            //var productIds = data.Select(x => x.ProductId);
            //// 任务单计划号
            //var products = SmartProductHelper.Instance.GetByIds<SmartProduct>(productIds);
            //// 计划号产能
            //var productCapacities = SmartProductCapacityHelper.Instance.GetAllSmartProductCapacities(productIds);
            //var capacityIds = products.Select(x => x.CapacityId);
            //// 产能设置
            //var smartCapacityLists = SmartCapacityListHelper.Instance.GetAllSmartCapacityListsWithOrder(capacityIds);
            foreach (var d in data)
            {
                if (d.ProductType == 0)
                {
                    d.Arranges = d.DeviceList.ToDictionary(de => de.Key,
                        de =>
                        {
                            var ope = deviceList.FirstOrDefault(dl => dl.Id == de.Key);
                            //var processId = d.ProcessId;
                            //var productId = d.ProductId;
                            ////计划号工序单日产能
                            //var pCapacity = productCapacities.FirstOrDefault(x => x.ProductId == productId && x.ProcessId == processId);
                            //var capacityId = pCapacity?.CapacityId ?? 0;
                            //var capacityList = smartCapacityLists.FirstOrDefault(x => x.CapacityId == capacityId && x.ProcessId == processId);
                            //var single = capacityList != null ? capacityList.OperatorList.FirstOrDefault(x => x.LevelId == (ope?.ModelId ?? 0))?.Single ?? 0 : 0;
                            //return new Tuple<string, int, int>(ope?.Code ?? "", de.Value, de.Value * single);
                            //return new Tuple<string, int, int>(ope?.Code ?? "", de.Value.Item1, de.Value.Item2);
                            return new Tuple<string, int>(ope?.Code ?? "", de.Value);
                        });
                }
                else if (d.ProductType == 1)
                {
                    d.Arranges = d.OperatorsList.ToDictionary(op => op.Key,
                        op =>
                        {
                            var ope = operatorList.FirstOrDefault(dl => dl.Id == op.Key);
                            //var processId = d.ProcessId;
                            //var productId = d.ProductId;
                            ////计划号工序单日产能
                            //var pCapacity = productCapacities.FirstOrDefault(x => x.ProductId == productId && x.ProcessId == processId);
                            //var capacityId = pCapacity?.CapacityId ?? 0;
                            //var capacityList = smartCapacityLists.FirstOrDefault(x => x.CapacityId == capacityId && x.ProcessId == processId);
                            //var single = capacityList != null ? capacityList.OperatorList.FirstOrDefault(x => x.LevelId == (ope?.LevelId ?? 0))?.Single ?? 0 : 0;
                            //return new Tuple<string, int, int>(ope?.Name ?? "", op.Value, op.Value * single);
                            return new Tuple<string, int>(ope?.Name ?? "", op.Value);
                        });
                };
            }
            result.datas.AddRange(data);
            return result;
        }

        // Post: api/SmartSchedule/Warehouse/Detail
        [HttpGet("Warehouse/Detail")]
        public object GetArrangedTaskOrderWarehouseDetail([FromQuery]int wId, int id, int taskOrderId, int pId)
        {
            var result = new DataResult();
            var sql = string.Empty;
            IEnumerable<SmartTaskOrderScheduleWarehouseInfoResult> data = null;
            if (id != 0)
            {
                sql =
                    $"SELECT a.ProcessTime, a.TaskOrderId, b.TaskOrder, a.ProductType, a.Target, a.DoneTarget " +
                    $"FROM `t_task_order_schedule` a " +
                    $"JOIN `t_task_order` b ON a.TaskOrderId = b.Id " +
                    $"WHERE a.Id = @id AND a.WorkshopId = @wId;";
                data = ServerConfig.ApiDb.Query<SmartTaskOrderScheduleWarehouseInfoResult>(sql, new
                {
                    wId,
                    id,
                    taskOrderId
                });
            }
            else if (taskOrderId != 0 && pId != 0)
            {
                data = SmartTaskOrderScheduleHelper.GetSmartTaskOrderSchedule(wId, taskOrderId, pId).Select(x => new SmartTaskOrderScheduleWarehouseInfoResult
                {
                    ProcessTime = x.ProcessTime,
                    TaskOrderId = x.TaskOrderId,
                    TaskOrder = x.TaskOrder,
                    ProductType = x.ProductType,
                    Target = x.Target,
                    DoneTarget = x.DoneTarget
                });
            }
            if (data == null)
            {
                return result;
            }
            data = data.Where(x => x.Target > 0);
            result.datas.AddRange(data);
            return result;
        }

        // Post: api/SmartSchedule/Put/Detail
        [HttpGet("Put/Index")]
        public object GetArrangedTaskOrderPutIndex([FromQuery] int wId, DateTime time, int pId)
        {
            var result = new DataResult();
            var data =
                SmartTaskOrderScheduleIndexHelper.GetSmartTaskOrderScheduleIndex(wId, time, default(DateTime), pId)
                    .Select(ClassExtension.ParentCopyToChild<SmartTaskOrderScheduleIndex, SmartTaskOrderScheduleIndexDetail>).Where(x => x.Index > 0).ToList();
            if (!data.Any())
            {
                return result;
            }
            //设备型号数量
            var deviceList = SmartDeviceHelper.GetNormalSmartDevices(wId);
            //人员等级数量
            var operatorList = SmartOperatorHelper.GetNormalSmartOperators(wId);
            var modelCount = deviceList.GroupBy(x => new { x.CategoryId, x.ModelId }).Select(y => new SmartDeviceModelCount
            {
                CategoryId = y.Key.CategoryId,
                ModelId = y.Key.ModelId,
                Count = y.Count()
            });
            var operatorCount = operatorList.GroupBy(x => new { x.ProcessId, x.LevelId }).Select(y => new SmartOperatorCount
            {
                ProcessId = y.Key.ProcessId,
                LevelId = y.Key.LevelId,
                Count = y.Count()
            });
            var order = SmartProcessHelper.Instance.Get<SmartProcess>(pId);
            var productType = data.First().ProductType;
            if (productType == 0)
            {
                var devices = deviceList.Where(x => x.CategoryId == order.DeviceCategoryId);
                foreach (var device in devices)
                {
                    if (data.All(p => p.DealId != device.Id))
                    {
                        data.Add(new SmartTaskOrderScheduleIndexDetail
                        {
                            WorkshopId = wId,
                            ProductType = productType,
                            ProcessTime = time,
                            PId = pId,
                            DealId = device.Id
                        });
                    }
                }
            }
            else if (productType == 1)
            {
                var operators = operatorList.Where(x => x.ProcessId == order.Id);
                foreach (var op in operators)
                {
                    if (data.All(p => p.DealId != op.Id))
                    {
                        data.Add(new SmartTaskOrderScheduleIndexDetail
                        {
                            WorkshopId = wId,
                            ProductType = productType,
                            ProcessTime = time,
                            PId = pId,
                            DealId = op.Id
                        });
                    }
                }
            }

            foreach (var x in data)
            {
                if (x.ProductType == 0)
                {
                    x.Code = deviceList.FirstOrDefault(z => z.Id == x.DealId)?.Code ?? "";
                }
                else if (x.ProductType == 1)
                {
                    x.Name = operatorList.FirstOrDefault(z => z.Id == x.DealId)?.Name ?? "";
                }
            }
            result.datas.AddRange(data);
            return result;
        }

        // Post: api/SmartSchedule/ArrangedTaskOrder
        [HttpGet("ArrangedTaskOrder")]
        public object GetArrangedTaskOrder([FromQuery]int wId, int page, int limit = 30)
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
                                                                                        $"WHERE Arranged = 1 AND a.MarkedDelete = 0 AND a.WorkshopId = @wId ORDER BY FIELD(State,  {state}), StartTime, EndTime, DeliveryTime LIMIT @page, @limit;", new
                                                                                        {
                                                                                            wId,
                                                                                            page,
                                                                                            limit
                                                                                        }));

            result.Count = ServerConfig.ApiDb.Query<int>($"SELECT COUNT(1) FROM `t_task_order` a " +
                                                                                      $"JOIN `t_product` b ON a.ProductId = b.Id " +
                                                                                      $"JOIN `t_task_order_level` c ON a.LevelId = c.Id " +
                                                                                      $"WHERE Arranged = 1 AND a.WorkshopId = @wId AND a.MarkedDelete = 0;", new { wId }).FirstOrDefault();
            return result;
        }

        // Post: api/SmartSchedule/NotArrangedTaskOrder
        [HttpGet("NotArrangedTaskOrder")]
        public object GetSmartScheduleNotArranged([FromQuery]int wId, int page, int limit = 30)
        {
            var result = new SmartResult();
            result.datas.AddRange(ServerConfig.ApiDb.Query<SmartTaskOrderDetailProduct>($"SELECT a.*, b.Product FROM `t_task_order` a " +
                                                                                        $"JOIN `t_product` b ON a.ProductId = b.Id " +
                                                                                        //$"WHERE Arranged = 0 AND a.MarkedDelete = 0 Order By StartTime, EndTime, DeliveryTime LIMIT @page, @limit;", new
                                                                                        $"WHERE Arranged = 0 AND a.WorkshopId = @wId AND a.MarkedDelete = 0 Order By Id Desc LIMIT @page, @limit;", new
                                                                                        {
                                                                                            wId,
                                                                                            page,
                                                                                            limit
                                                                                        }));
            result.Count = ServerConfig.ApiDb.Query<int>($"SELECT COUNT(1) FROM `t_task_order` a " +
                                                         $"JOIN `t_product` b ON a.ProductId = b.Id " +
                                                         $"WHERE Arranged = 0 AND a.WorkshopId = @wId AND a.MarkedDelete = 0;", new { wId }).FirstOrDefault();

            return result;
        }

        // Post: api/SmartSchedule/Preview
        [HttpPost("Preview")]
        public object PostSmartSchedulePreview([FromBody]SmartTaskOrderArrangeParam arrange)
        {
            var taskOrders = arrange.Previews;
            var wId = arrange.WorkshopId;
            if (taskOrders == null)
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            var taskIds = taskOrders.GroupBy(x => x.Id).Select(y => y.Key);
            if (taskIds.Count() != taskOrders.Count())
            {
                return Result.GenError<Result>(Error.SmartTaskOrderDuplicate);
            }
            var result = new DataResult();
            var allTasks = new List<SmartTaskOrderPreview>();
            if (taskOrders.Any())
            {
                var tOrders = SmartTaskOrderHelper.Instance.GetByIds<SmartTaskOrderPreview>(taskIds);
                if (taskIds.Count() != tOrders.Count())
                {
                    result.errno = Error.SmartTaskOrderNotExist;
                    result.datas.AddRange(taskOrders.Where(x => tOrders.All(y => y.Id != x.Id)).Select(x => x.TaskOrder.IsNullOrEmpty() ? x.Id.ToString() : x.TaskOrder));
                    return result;
                }

                //var arranged = tOrders.Where(x => x.Arranged);
                //if (arranged.Any())
                //{
                //    result.errno = Error.SmartTaskOrderArranged;
                //    result.datas.AddRange(arranged.Select(x => x.TaskOrder));
                //    return result;
                //}

                foreach (var task in tOrders)
                {
                    var t = taskOrders.FirstOrDefault(x => x.Id == task.Id);
                    if (t == null)
                    {
                        result.errno = Error.SmartTaskOrderNotExist;
                        result.datas.Add(task.TaskOrder);
                    }
                    else
                    {
                        //页面选择顺序
                        task.Order = !t.Arranged && t.Order == 0 ? int.MaxValue : t.Order;
                        task.StartTime = t.StartTime;
                        task.EndTime = t.EndTime;
                        task.Needs = t.Needs;
                    }
                }
                allTasks.AddRange(tOrders);
            }
            if (result.errno != Error.Success)
            {
                return result;
            }
            var otherTasks = SmartTaskOrderHelper.GetArrangedButNotDoneSmartTaskOrders(wId);
            if (otherTasks.Any())
            {
                taskIds = otherTasks.Select(x => x.Id);
                var taskNeeds = SmartTaskOrderNeedHelper.GetSmartTaskOrderNeedsByTaskOrderIds(wId, taskIds);
                foreach (var otherTask in otherTasks)
                {
                    var aTask = allTasks.FirstOrDefault(x => x.Id == otherTask.Id);
                    var needs = taskNeeds.Where(need => need.TaskOrderId == otherTask.Id);
                    if (aTask != null)
                    {
                        aTask.Arranged = true;
                        if (!aTask.Needs.Any())
                        {
                            aTask.Needs.AddRange(needs);
                        }
                        else
                        {
                            aTask.Needs = aTask.Needs.Select(x =>
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
                    }
                    else
                    {
                        var t = ClassExtension.ParentCopyToChild<SmartTaskOrder, SmartTaskOrderPreview>(otherTask);
                        t.Arranged = true;
                        t.Needs.AddRange(needs);
                        allTasks.Add(t);
                    }
                }
            }

            if (result.errno != Error.Success)
            {
                return result;
            }
            var productIds = allTasks.GroupBy(x => x.ProductId).Select(y => y.Key);
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
                var tasks = allTasks.Where(x => x.ProductId == productId);
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

            var data = allTasks.ToDictionary(x => x.Id, x =>
            {
                var t = ClassExtension.ParentCopyToChild<SmartTaskOrder, SmartTaskOrderPreview>(x);
                t.Product = products.FirstOrDefault(y => y.Id == x.ProductId)?.Product ?? "";
                return t;
            });
            foreach (var task in allTasks)
            {
                var productId = task.ProductId;
                var product = products.FirstOrDefault(x => x.Id == productId);
                var pCapacities = productCapacities.Where(x => x.ProductId == productId);
                var cLists = capacityLists.Where(x => x.CapacityId == product.CapacityId);
                var target = task.Target;
                foreach (var list in cLists.Reverse())
                {
                    var need = task.Needs.FirstOrDefault(x => x.Order == list.Order && x.ProcessId == list.ProcessId);
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

                    var pCapacity = pCapacities.FirstOrDefault(x => x.ProcessId == list.ProcessId);
                    var rate = 0m;
                    var y = pCapacity;
                    if (y.DeviceList.Any())
                    {
                        rate = y.DeviceList.First().Rate;
                    }
                    else if (y.OperatorList.Any())
                    {
                        rate = y.OperatorList.First().Rate;
                    }

                    var put = rate != 0 ? (int)Math.Ceiling((target) * 100 / rate) : 0;
                    data[task.Id].Needs.Insert(0, new SmartTaskOrderNeedDetail
                    {
                        TaskOrderId = task.Id,
                        ProductId = productId,
                        ProcessId = list.ProcessId,
                        PId = list.PId,
                        Target = target,
                        DoneTarget = doneTarget,
                        Stock = stock,
                        Rate = rate,
                        Put = put,
                        HavePut = havePut,
                        Process = list.Process,
                        Order = list.Order
                    });
                    target = put;
                }
            }
            var r = new SmartTaskOrderNeedWithOrderResult();
            r.datas.AddRange(data.Values.OrderBy(x => x.Arranged).ThenBy(x => x.ModifyId));
            var orders = data.Values.SelectMany(x => x.Needs).GroupBy(y => new { y.PId, y.Order, y.Process }).Select(z => new SmartTaskOrderNeedWithOrder
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

            var wId = taskOrders.FirstOrDefault()?.WorkshopId ?? 0;
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

            SmartTaskOrderHelper.ArrangedUpdate(taskOrders);
            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 获取任务单排产天数
        /// </summary>
        /// <param name="taskOrders"></param>
        /// <returns></returns>
        // Post: api/SmartSchedule/CostDay
        [HttpPost("CostDay")]
        public object PostSmartScheduleCostDay([FromBody]SmartTaskOrderArrangeParam arrange)
        {
            var taskOrders = arrange.Confirms;
            var wId = arrange.WorkshopId;
            if (taskOrders == null || !taskOrders.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (taskOrders.Any(x => !x.Needs.Any()))
            {
                return Result.GenError<Result>(Error.SmartScheduleNeedLost);
            }

            var today = DateTime.Today;
            var schedules = new List<SmartTaskOrderScheduleDetail>();
            var costDays = HScheduleHelper.ArrangeSchedule(wId, ref taskOrders, ref schedules, out var indexes);
            var eStartTime = costDays.Any() ? costDays.Min(x => x.EstimatedStartTime) : today;
            if (eStartTime == default(DateTime))
            {
                eStartTime = today;
            }
            var eEndTime = costDays.Any() ? costDays.Max(x => x.EstimatedEndTime) : today;
            if (eEndTime == default(DateTime))
            {
                eEndTime = today;
            }
            var put = new List<object>();
            var orders = new List<SmartTaskOrderNeedWithOrder>();
            //设备型号数量
            var deviceList = SmartDeviceHelper.Instance.GetAll<SmartDevice>();
            //人员等级数量
            var operatorList = SmartOperatorHelper.GetAllSmartOperators();
            //按工序排
            var processes = schedules.GroupBy(x => new { x.PId, x.Process, x.Order }).Select(y => y.Key);
            var ts = costDays.SelectMany(costDay =>
            {
                var taskNeeds = taskOrders.First(x => x.Id == costDay.Id).Needs;
                return taskNeeds.Where(n => taskOrders.Any(task => task.Id == n.TaskOrderId)).Select(x =>
                {
                    var task = taskOrders.First(t => t.Id == x.TaskOrderId);
                    var schedule = schedules.Where(sc =>
                        sc.TaskOrderId == x.TaskOrderId && sc.ProcessId == x.ProcessId && sc.PId == x.PId).ToList();
                    var totalDays = (eEndTime - eStartTime).TotalDays + 1;
                    if (schedule.Count < totalDays)
                    {
                        for (var i = 0; i < (eEndTime - eStartTime).TotalDays + 1; i++)
                        {
                            var t = eStartTime.AddDays(i);
                            if (schedule.All(p => p.ProcessTime != t))
                            {
                                schedule.Add(new SmartTaskOrderScheduleDetail()
                                {
                                    ProcessTime = t
                                });
                            }
                        }

                        schedule = schedule.OrderBy(sc => sc.ProcessTime).ToList();
                    }

                    var scs = schedule.Select(y =>
                    {
                        var d = new SmartTaskOrderSchedulePutAndWarehouseInfoResult
                        {
                            ProcessTime = y.ProcessTime,
                            TaskOrderId = y.TaskOrderId,
                            TaskOrder = y.TaskOrder,
                            Target = y.Target,
                            DoneTarget = y.DoneTarget,
                            Put = y.Put,
                            HavePut = y.HavePut,
                        };

                        if (y.ProductType == 0)
                        {
                            d.Arranges = y.DeviceList.ToDictionary(de => de.Key,
                                de =>
                                {
                                    var ope = deviceList.FirstOrDefault(dl => dl.Id == de.Key);
                                    return new Tuple<string, int>(ope?.Code ?? "", de.Value);
                                });
                        }
                        else if (d.ProductType == 1)
                        {
                            d.Arranges = y.OperatorsList.ToDictionary(op => op.Key,
                                op =>
                                {
                                    var ope = operatorList.FirstOrDefault(dl => dl.Id == op.Key);
                                    return new Tuple<string, int>(ope?.Name ?? "", op.Value);
                                });
                        };
                        var s = new
                        {
                            Id = y.Id,
                            ProductType = y.ProductType,
                            ProcessTime = y.ProcessTime,
                            Put = y.Put,
                            HavePut = y.HavePut,
                            Target = y.Target,
                            DoneTarget = y.DoneTarget,
                            Data = d
                        };
                        return s;
                    });
                    var need = new
                    {
                        Id = x.Id,
                        TaskOrderId = x.TaskOrderId,
                        TaskOrder = task.TaskOrder,
                        ProductId = x.ProductId,
                        Product = task.Product,
                        DeliveryTime = task.DeliveryTime,
                        ArrangedTime = task.ArrangedTime,
                        PId = x.PId,
                        ProcessId = x.ProcessId,
                        Process = x.Process,
                        Order = x.Order,
                        Put = x.Put,
                        HavePut = x.HavePut,
                        Target = x.Target,
                        DoneTarget = x.DoneTarget,
                        Schedules = scs
                    };
                    return need;
                });
            });
            put.AddRange(ts);

            orders.AddRange(costDays.SelectMany(x => x.CostDays).GroupBy(y => new { y.PId, y.Process, y.Order }).Select(z => new SmartTaskOrderNeedWithOrder
            {
                Id = z.Key.PId,
                Process = z.Key.Process,
                Order = z.Key.Order
            }).OrderBy(z => z.Order));

            var arrangeIndexes = new List<SmartTaskOrderScheduleIndex>();
            foreach (var order in orders)
            {
                for (var i = 0; i < (eEndTime - eStartTime).TotalDays + 1; i++)
                {
                    var t = eStartTime.AddDays(i);
                    if (!indexes.Any(p => p.ProcessTime == t && p.PId == order.Id))
                    {
                        arrangeIndexes.Add(new SmartTaskOrderScheduleIndex
                        {
                            ProcessTime = t,
                            PId = order.Id,
                        });
                    }
                    else
                    {
                        var ins = indexes.Where(p => p.ProcessTime == t && p.PId == order.Id);
                        arrangeIndexes.Add(new SmartTaskOrderScheduleIndex
                        {
                            ProcessTime = t,
                            PId = order.Id,
                            Index = !ins.Any() ? 0 : (ins.Sum(index => index.Index) / ins.Count()).ToRound(2)
                        });
                    }
                }
            }

            var indexs = arrangeIndexes.OrderBy(x => orders.FirstOrDefault(y => y.Id == x.PId)?.Order ?? 0)
                .ThenBy(z => z.ProcessTime);
            return new
            {
                errno = 0,
                errmsg = "成功",
                StartTime = eStartTime,
                EndTime = eEndTime,
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
                Indexes = indexs
            };
        }

        /// <summary>
        /// 任务单确定排产
        /// </summary>
        /// <param name="arrange"></param>
        /// <returns></returns>
        // Post: api/SmartSchedule/Confirm
        [HttpPost("Confirm")]
        public object PostSmartScheduleConfirm([FromBody]SmartTaskOrderArrangeParam arrange)
        {
            var taskOrders = arrange.Confirms;
            if (taskOrders == null || !taskOrders.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (taskOrders.Any(x => !x.Needs.Any()))
            {
                return Result.GenError<Result>(Error.SmartScheduleNeedLost);
            }

            var userId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            var schedules = new List<SmartTaskOrderScheduleDetail>();
            var costDays = HScheduleHelper.ArrangeSchedule(arrange.WorkshopId, ref taskOrders, ref schedules, out _, true, userId, markedDateTime);
            WorkFlowHelper.Instance.OnTaskOrderArrangeChanged();
            return Result.GenError<Result>(Error.Success);
        }
    }
}