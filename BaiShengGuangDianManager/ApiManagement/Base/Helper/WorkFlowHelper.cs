﻿using ApiManagement.Base.Server;
using ApiManagement.Models.AccountManagementModel;
using ApiManagement.Models.FlowCardManagementModel;
using ApiManagement.Models.MaterialManagementModel;
using ApiManagement.Models.PlanManagementModel;
using ApiManagement.Models.SmartFactoryModel;
using ModelBase.Base.Logger;
using ModelBase.Base.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Base.Helper
{
    /// <summary>
    /// 
    /// </summary>
    public class WorkFlowHelper
    {
        public static WorkFlowHelper Instance;
        public WorkFlowHelper()
        {
            Register();
        }

        #region 创建
        /// <summary>
        /// 用户创建
        /// </summary>
        public event EventHandler<AccountInfo> SmartAccountCreated;
        public void OnSmartAccountCreated(AccountInfo user)
        {
            //SmartAccountCreated?.BeginInvoke(this, user, null, null);
            SmartAccountCreated?.Invoke(this, user);
        }

        /// <summary>
        /// 流程卡创建
        /// </summary>
        public event EventHandler<IEnumerable<SmartFlowCard>> SmartFlowCardCreated;
        public void OnSmartFlowCardCreated(IEnumerable<SmartFlowCard> flowCards)
        {
            SmartFlowCardCreated?.Invoke(this, flowCards);
        }

        /// <summary>
        /// 流程卡工序 创建
        /// </summary>
        public event EventHandler<Tuple<IEnumerable<SmartFlowCard>, IEnumerable<SmartFlowCardProcess>>> SmartFlowCardProcessCreated;
        public void OnSmartFlowCardProcessCreated(IEnumerable<SmartFlowCard> flowCards, IEnumerable<SmartFlowCardProcess> smartFlowCardProcesses)
        {
            SmartFlowCardProcessCreated?.Invoke(this, new Tuple<IEnumerable<SmartFlowCard>, IEnumerable<SmartFlowCardProcess>>(flowCards, smartFlowCardProcesses));
        }

        #endregion

        #region 改变
        /// <summary>
        /// 排程改变
        /// </summary>
        public event EventHandler<IEnumerable<int>> TaskOrderArrangeChanged;
        public void OnTaskOrderArrangeChanged(IEnumerable<int> tasks = null)
        {
            TaskOrderArrangeChanged?.Invoke(this, tasks);
        }
        /// <summary>
        /// 用户改变
        /// </summary>
        public event EventHandler<IEnumerable<AccountInfo>> SmartAccountChanged;
        public void OnSmartAccountChanged(IEnumerable<AccountInfo> user)
        {
            SmartAccountChanged?.Invoke(this, user);
        }
        /// <summary>
        /// 流程卡工序 更新  开始加工 结束加工 录报表
        /// </summary>
        public event EventHandler<IEnumerable<SmartFlowCardProcess>> SmartFlowCardProcessUpdated;
        public void OnSmartFlowCardProcessUpdated(IEnumerable<SmartFlowCardProcess> smartFlowCardProcesses)
        {
            SmartFlowCardProcessUpdated?.Invoke(this, smartFlowCardProcesses);
        }

        /// <summary>
        /// 流程卡改变 增删改
        /// </summary>
        public event EventHandler<IEnumerable<SmartFlowCard>> SmartFlowCardChanged;
        public void OnSmartFlowCardChanged(IEnumerable<SmartFlowCard> flowCards)
        {
            SmartFlowCardChanged?.Invoke(this, flowCards);
        }

        /// <summary>
        /// 流程卡工序 改变  开始加工 结束加工 录报表
        /// </summary>
        public event EventHandler<IEnumerable<SmartFlowCardProcess>> SmartFlowCardProcessChanged;
        public void OnSmartFlowCardProcessChanged(IEnumerable<SmartFlowCardProcess> smartFlowCardProcesses)
        {
            SmartFlowCardProcessChanged?.Invoke(this, smartFlowCardProcesses);
        }

        /// <summary>
        /// 任务单改变 增删改
        /// </summary>
        public event EventHandler<IEnumerable<SmartTaskOrder>> SmartTaskOrderChanged;
        public void OnSmartTaskOrderChanged(IEnumerable<SmartTaskOrder> taskOrders)
        {
            SmartTaskOrderChanged?.Invoke(this, taskOrders);
        }

        /// <summary>
        /// 任务单改变 增删改
        /// </summary>
        public event EventHandler<IEnumerable<SmartProcessFault>> SmartProcessFaultChanged;
        public void OnSmartTaskOrderChanged(IEnumerable<SmartProcessFault> processFaults)
        {
            SmartProcessFaultChanged?.Invoke(this, processFaults);
        }

        /// <summary>
        /// 操作工等级改变 增删改
        /// </summary>
        public event EventHandler<IEnumerable<SmartOperatorLevel>> SmartOperatorLevelChanged;
        public void OnSmartOperatorLevelChanged(IEnumerable<SmartOperatorLevel> operatorLevels)
        {
            SmartOperatorLevelChanged?.Invoke(this, operatorLevels);
        }
        /// <summary>
        /// 产能类型设置改变 增删改
        /// </summary>
        public event EventHandler<IEnumerable<SmartCapacityList>> SmartCapacityListChanged;
        public void OnSmartCapacityListChanged(IEnumerable<SmartCapacityList> capacityLists)
        {
            SmartCapacityListChanged?.Invoke(this, capacityLists);
        }
        /// <summary>
        /// 操作工改变
        /// </summary>
        public event EventHandler<IEnumerable<SmartOperator>> SmartOperatorChanged;
        public void OnSmartOperatorChanged(IEnumerable<SmartOperator> operators)
        {
            SmartOperatorChanged?.Invoke(this, operators);
        }


        /// <summary>
        /// 流程卡上报 更新
        /// </summary>
        public event EventHandler<IEnumerable<FlowCard>> FlowCardChanged;
        public void OnFlowCardChanged(IEnumerable<FlowCard> flowCards)
        {
            FlowCardChanged?.BeginInvoke(this, flowCards, o =>
            {

                FlowCardChanged.EndInvoke(o);
            }, null);
        }

        #endregion

        #region 需要更新
        /// <summary>
        /// 任务单生产线需更新
        /// </summary>
        public event EventHandler<IEnumerable<TaskOrderIdProcessCodeId>> SmartLineTaskOrderNeedUpdate;
        public void OnSmartLineTaskOrderNeedUpdate(IEnumerable<TaskOrderIdProcessCodeId> taskOrderIdProcessCodeIds)
        {
            SmartLineTaskOrderNeedUpdate?.Invoke(this, taskOrderIdProcessCodeIds);
        }
        /// <summary>
        /// 工单生产线需更新
        /// </summary>
        public event EventHandler<IEnumerable<WorkOrderIdProcessCodeId>> SmartLineWorkOrderNeedUpdate;
        public void OnSmartLineWorkOrderNeedUpdate(IEnumerable<WorkOrderIdProcessCodeId> workOrderIdProcessCodeIds)
        {
            SmartLineWorkOrderNeedUpdate?.Invoke(this, workOrderIdProcessCodeIds);
        }
        /// <summary>
        /// 产能类型需更新
        /// </summary>
        public event EventHandler<IEnumerable<SmartCapacity>> SmartCapacityNeedUpdate;
        public void OnSmartCapacityNeedUpdate(IEnumerable<SmartCapacity> smartCapacities)
        {
            SmartCapacityNeedUpdate?.Invoke(this, smartCapacities);
        }
        /// <summary>
        /// 计划号产能需更新
        /// </summary>
        public event EventHandler<IEnumerable<SmartProduct>> SmartProductCapacityNeedUpdate;
        public void OnSmartProductCapacityNeedUpdate(IEnumerable<SmartProduct> smartProducts)
        {
            SmartProductCapacityNeedUpdate?.Invoke(this, smartProducts);
        }
        /// <summary>
        /// 检查物料 需更新
        /// </summary>
        public event EventHandler BillNeedUpdate;
        public void OnBillNeedUpdate()
        {
            BillNeedUpdate?.Invoke(this, null);
        }
        #endregion

        #region 删除触发
        /// <summary>
        /// 账号删除
        /// </summary>
        public event EventHandler<IEnumerable<AccountInfo>> AccountInfoDeleted;
        public void OnAccountInfoDeleted(IEnumerable<AccountInfo> accountInfos)
        {
            AccountInfoDeleted?.Invoke(this, accountInfos);
        }
        #endregion
        public static void Init()
        {
            Instance = Instance ?? new WorkFlowHelper();
        }

        public void Register()
        {
            AccountInfoDeleted += (o, args) =>
            {
                try
                {
                    var ids = args.Select(x => x.Id);
                    OrganizationUnitHelper.DeleteMemberByAccountIds(ids);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            };
            BillNeedUpdate += (o, args) =>
            {
                try
                {
                    var duplicateCodes = ServerConfig.ApiDb.Query<dynamic>(
                        "SELECT Id, `Code`, GROUP_CONCAT(`Id`) Ids, COUNT(1) c FROM (SELECT * FROM `material_bill` WHERE MarkedDelete = 0) a WHERE MarkedDelete = 0 GROUP BY SiteId, SpecificationId, Price HAVING c > 1;");
                    if (duplicateCodes.Any())
                    {
                        var updateBill = new List<MaterialBill>();
                        var updateLog = new List<BillChange>();
                        foreach (var duplicateCode in duplicateCodes)
                        {
                            try
                            {
                                var id = (int)duplicateCode.Id;
                                var ids = ((string)duplicateCode.Ids).Split(",").Select(int.Parse);
                                updateBill.AddRange(ids.Where(x => x != id).Select(y => new MaterialBill
                                {
                                    Id = y,
                                    MarkedDelete = true
                                }));
                                updateLog.AddRange(ids.Where(x => x != id).Select(y => new BillChange
                                {
                                    BillId = y,
                                    NewBillId = id,
                                    NewCode = (string)duplicateCode.Code,
                                }));
                            }
                            catch (Exception e)
                            {
                                Log.Error($"{duplicateCode},{e}");
                            }
                        }
                        Console.WriteLine($"物料编码去重: updateBill: {updateBill.Count()},  log: {updateLog.Count()}");
                        Log.Debug($"物料编码去重: updateBill: {updateBill.Count()},  log: {updateLog.Count()}");
                        if (updateBill.Any())
                        {
                            ServerConfig.ApiDb.Execute(
                                "UPDATE `material_bill` SET MarkedDelete = @MarkedDelete WHERE Id = @Id AND MarkedDelete = 0;",
                                updateBill);
                        }
                        if (updateLog.Any())
                        {
                            ServerConfig.ApiDb.Execute(
                                "UPDATE `material_log` SET BillId = @NewBillId, `Code` = @NewCode WHERE BillId = @BillId;",
                                updateLog);
                            ServerConfig.ApiDb.Execute(
                                "UPDATE `material_purchase_item` SET BillId = @NewBillId, `ThisCode` = @NewCode WHERE BillId = @BillId AND MarkedDelete = 0;",
                                updateLog);

                            var allBillId = updateLog.Select(x => x.BillId).Concat(updateLog.Select(x => x.NewBillId));
                            var planBills = ServerConfig.ApiDb.Query<ProductPlanBill>(
                                "SELECT * FROM `production_plan_bill` WHERE BillId IN @allBillId AND MarkedDelete = 0",
                                new { allBillId }).OrderBy(x => x.PlanId);
                            var newPlanBill = planBills.Where(x => updateLog.Any(y => y.NewBillId == x.BillId));
                            foreach (var planBill in newPlanBill)
                            {
                                var newBill = updateLog.Where(x => x.NewBillId == planBill.BillId);
                                if (newBill.Any())
                                {
                                    var delBills = planBills.Where(x =>
                                        x.PlanId == planBill.PlanId && newBill.Any(z => z.BillId == x.BillId));
                                    planBill.ActualConsumption += delBills.Sum(y => y.ActualConsumption);
                                    foreach (var del in delBills)
                                    {
                                        del.MarkedDelete = true;
                                        del.ModifyId = planBill.BillId;
                                        //del.ActualConsumption = 0;
                                    }
                                }
                            }
                            ServerConfig.ApiDb.Execute(
                                "UPDATE `production_plan_bill` SET MarkedDelete = @MarkedDelete, ModifyId = @ModifyId, ActualConsumption = @ActualConsumption WHERE BillId = @BillId AND MarkedDelete = 0;",
                                planBills);

                            ServerConfig.ApiDb.Execute(
                                "UPDATE production_plan_bill a JOIN (SELECT * FROM (SELECT a.PlanId, a.BillId, b.Time FROM `production_plan_bill` a " +
                                "JOIN `material_log` b ON a.PlanId = b.PlanId AND a.BillId = b.BillId WHERE b.PlanId != 0 ORDER BY b.Time DESC) a GROUP BY a.PlanId, a.BillId) " +
                                "b ON a.PlanId = b.PlanId AND a.BillId = b.BillId SET a.MarkedDateTime = b.Time WHERE a.MarkedDateTime != b.Time;");
                        }
                    }

                    var duplicateCodeSites = ServerConfig.ApiDb.Query<dynamic>(
                        "SELECT Id, `Code`, GROUP_CONCAT(`Id`) Ids, GROUP_CONCAT(`SiteId`) SiteIds, COUNT(1) c FROM (SELECT * FROM `material_bill` WHERE MarkedDelete = 0) a " +
                        "WHERE MarkedDelete = 0 GROUP BY SpecificationId, Price HAVING c > 1 AND SiteIds LIKE '%74%';");
                    if (duplicateCodeSites.Any())
                    {
                        var updateBill = new List<MaterialBill>();
                        var updateLog = new List<BillChange>();
                        foreach (var duplicateCode in duplicateCodeSites)
                        {
                            try
                            {
                                var id = (int)duplicateCode.Id;
                                var ids = ((string)duplicateCode.Ids).Split(",").Select(int.Parse);
                                updateBill.AddRange(ids.Where(x => x != id).Select(y => new MaterialBill
                                {
                                    Id = y,
                                    MarkedDelete = true
                                }));
                                updateLog.AddRange(ids.Where(x => x != id).Select(y => new BillChange
                                {
                                    BillId = y,
                                    NewBillId = id,
                                    NewCode = (string)duplicateCode.Code,
                                }));
                            }
                            catch (Exception e)
                            {
                                Log.Error($"{duplicateCode},{e}");
                            }
                        }
                        Console.WriteLine($"物料编码位置纠正: updateBill: {updateBill.Count()},  log: {updateLog.Count()}");
                        Log.Debug($"物料编码位置纠正: updateBill: {updateBill.Count()},  log: {updateLog.Count()}");
                        if (updateBill.Any())
                        {
                            ServerConfig.ApiDb.Execute(
                                "UPDATE `material_bill` SET MarkedDelete = @MarkedDelete WHERE Id = @Id AND MarkedDelete = 0;",
                                updateBill);
                        }
                        if (updateLog.Any())
                        {
                            ServerConfig.ApiDb.Execute(
                                "UPDATE `material_log` SET BillId = @NewBillId, `Code` = @NewCode WHERE BillId = @BillId;",
                                updateLog);
                            ServerConfig.ApiDb.Execute(
                                "UPDATE `material_purchase_item` SET BillId = @NewBillId, `ThisCode` = @NewCode WHERE BillId = @BillId AND MarkedDelete = 0;",
                                updateLog);

                            var allBillId = updateLog.Select(x => x.BillId).Concat(updateLog.Select(x => x.NewBillId));
                            var planBills = ServerConfig.ApiDb.Query<ProductPlanBill>(
                                "SELECT * FROM `production_plan_bill` WHERE BillId IN @allBillId AND MarkedDelete = 0",
                                new { allBillId }).OrderBy(x => x.PlanId);
                            var newPlanBill = planBills.Where(x => updateLog.Any(y => y.NewBillId == x.BillId));
                            foreach (var planBill in newPlanBill)
                            {
                                var newBill = updateLog.Where(x => x.NewBillId == planBill.BillId);
                                if (newBill.Any())
                                {
                                    var delBills = planBills.Where(x =>
                                        x.PlanId == planBill.PlanId && newBill.Any(z => z.BillId == x.BillId));
                                    planBill.ActualConsumption += delBills.Sum(y => y.ActualConsumption);
                                    foreach (var del in delBills)
                                    {
                                        del.MarkedDelete = true;
                                        del.ModifyId = planBill.BillId;
                                        //del.ActualConsumption = 0;
                                    }
                                }
                            }
                            ServerConfig.ApiDb.Execute(
                                "UPDATE `production_plan_bill` SET MarkedDelete = @MarkedDelete, ModifyId = @ModifyId, ActualConsumption = @ActualConsumption WHERE BillId = @BillId AND MarkedDelete = 0;",
                                planBills);

                            ServerConfig.ApiDb.Execute(
                                "UPDATE production_plan_bill a JOIN (SELECT * FROM (SELECT a.PlanId, a.BillId, b.Time FROM `production_plan_bill` a " +
                                "JOIN `material_log` b ON a.PlanId = b.PlanId AND a.BillId = b.BillId WHERE b.PlanId != 0 ORDER BY b.Time DESC) a GROUP BY a.PlanId, a.BillId) " +
                                "b ON a.PlanId = b.PlanId AND a.BillId = b.BillId SET a.MarkedDateTime = b.Time WHERE a.MarkedDateTime != b.Time;");
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            };

            SmartFlowCardProcessChanged += (o, smartFlowCardProcesses) =>
            {
                if (smartFlowCardProcesses == null || !smartFlowCardProcesses.Any())
                {
                    return;
                }

                var markedDateTime = smartFlowCardProcesses.First().MarkedDateTime;
                var flowCardIds = smartFlowCardProcesses.GroupBy(x => x.FlowCardId).Select(y => y.Key);
                ServerConfig.ApiDb.Execute("CALL UpdateFlowCardState(@flowCardId, @markedDateTime);", flowCardIds.Select(x => new
                {
                    flowCardId = x,
                    markedDateTime
                }));

                var flowCards = SmartFlowCardHelper.Instance.GetByIds<SmartFlowCard>(flowCardIds).Select(x =>
                {
                    x.MarkedDateTime = markedDateTime;
                    return x;
                });
                OnSmartFlowCardChanged(flowCards);
                var flowCardProcessIds = smartFlowCardProcesses.GroupBy(x => x.Id).Select(y => y.Key);
                var taskOrderIdProcessCodeIds = ServerConfig.ApiDb.Query<TaskOrderIdProcessCodeId>(
                    "SELECT b.TaskOrderId, c.ProcessCodeCategoryId, a.ProcessId, c.ProcessId StandardId FROM `t_flow_card_process` a " +
                    "JOIN `t_flow_card` b ON a.FlowCardId = b.Id " +
                    "JOIN (SELECT a.Id, b.ProcessCodeCategoryId, b.Id ProcessId FROM `t_product_process` a JOIN `t_process_code_category_process` b ON a.ProcessId = b.Id) c ON a.ProcessId = c.Id " +
                    "WHERE a.MarkedDelete = 0 AND a.Id IN @flowCardProcessIds GROUP BY b.TaskOrderId, c.ProcessCodeCategoryId, a.ProcessId",
                    new { flowCardProcessIds });
                OnSmartLineTaskOrderNeedUpdate(taskOrderIdProcessCodeIds);

                var workOrderIdProcessCodeIds = ServerConfig.ApiDb.Query<WorkOrderIdProcessCodeId>(
                    "SELECT b.WorkOrderId, c.ProcessCodeCategoryId, a.ProcessId, c.ProcessId StandardId FROM `t_flow_card_process` a " +
                    "JOIN (SELECT a.*, b.WorkOrderId FROM `t_flow_card` a JOIN `t_task_order` b ON a.TaskOrderId = b.Id) b ON a.FlowCardId = b.Id " +
                    "JOIN (SELECT a.Id, b.ProcessCodeCategoryId, b.Id ProcessId FROM `t_product_process` a JOIN `t_process_code_category_process` b ON a.ProcessId = b.Id) c ON a.ProcessId = c.Id " +
                    "WHERE a.MarkedDelete = 0 AND a.Id IN @flowCardProcessIds GROUP BY b.WorkOrderId, c.ProcessCodeCategoryId, a.ProcessId",
                    new { flowCardProcessIds });
                OnSmartLineWorkOrderNeedUpdate(workOrderIdProcessCodeIds);
            };

            SmartFlowCardChanged += (o, flowCards) =>
            {
                if (flowCards == null || !flowCards.Any())
                {
                    return;
                }

                var markedDateTime = flowCards.First().MarkedDateTime;
                var taskOrderIds = flowCards.GroupBy(x => x.TaskOrderId).Select(y => y.Key);
                ServerConfig.ApiDb.Execute("CALL UpdateTaskOrder(@taskOrderId, @markedDateTime);CALL UpdateTaskOrderState(@taskOrderId, @markedDateTime);", taskOrderIds.Select(x => new
                {
                    taskOrderId = x,
                    markedDateTime
                }));

                var taskOrders = SmartTaskOrderHelper.Instance.GetByIds<SmartTaskOrder>(taskOrderIds).Select(x =>
                {
                    x.MarkedDateTime = markedDateTime;
                    return x;
                });
                OnSmartTaskOrderChanged(taskOrders);
            };

            SmartTaskOrderChanged += (o, taskOrders) =>
            {
                if (taskOrders == null || !taskOrders.Any())
                {
                    return;
                }

                var markedDateTime = taskOrders.First().MarkedDateTime;
                var workOrderIds = taskOrders.GroupBy(x => x.WorkOrderId).Select(y => y.Key);
                ServerConfig.ApiDb.Execute("CALL UpdateWorkOrder(@workOrderId, @markedDateTime);CALL UpdateWorkOrderState(@workOrderId, @markedDateTime);", workOrderIds.Select(x => new
                {
                    workOrderId = x,
                    markedDateTime
                }));
            };

            SmartFlowCardCreated += (o, flowCards) =>
            {

            };

            SmartFlowCardProcessCreated += (o, param) =>
            {
                var flowCards = param.Item1;
                if (flowCards == null || !flowCards.Any())
                {
                    return;
                }

                var smartFlowCardProcesses = param.Item2;
                if (smartFlowCardProcesses == null || !smartFlowCardProcesses.Any())
                {
                    return;
                }

                var createUserId = smartFlowCardProcesses.First().CreateUserId;
                var markedDateTime = smartFlowCardProcesses.First().MarkedDateTime;
                var taskOrderIds = flowCards.GroupBy(x => x.TaskOrderId).Select(y => y.Key);
                var processCodeCategoryIds = flowCards.GroupBy(x => x.ProcessCodeId).Select(y => y.Key);
                var processes =
                    SmartProcessCodeCategoryProcessHelper.GetDetailByCategoryId(processCodeCategoryIds);
                var existTaskOrderLines = ServerConfig.ApiDb
                    .Query<SmartLineTaskOrder>(
                        "SELECT TaskOrderId, ProcessCodeCategoryId FROM `t_line_task_order` " +
                        "WHERE MarkedDelete = 0 AND TaskOrderId IN @taskOrderIds AND ProcessCodeCategoryId IN @processCodeCategoryIds GROUP BY TaskOrderId, ProcessCodeCategoryId;",
                        new { taskOrderIds, processCodeCategoryIds });

                var taskOrderLines = new List<SmartLineTaskOrder>();
                foreach (var taskOrderId in taskOrderIds)
                {
                    var ids = flowCards.Where(x => x.TaskOrderId == taskOrderId).GroupBy(y => y.ProcessCodeId).Select(z => z.Key);
                    foreach (var id in ids)
                    {
                        if (existTaskOrderLines.Any(x => x.TaskOrderId == taskOrderId && x.ProcessCodeCategoryId == id))
                        {
                            continue;
                        }
                        var ps = processes.Where(x => x.ProcessCodeCategoryId == id);
                        if (!ps.Any())
                        {
                            continue;
                        }
                        taskOrderLines.AddRange(ps.Select(x => new SmartLineTaskOrder
                        {
                            CreateUserId = createUserId,
                            MarkedDateTime = markedDateTime,
                            TaskOrderId = taskOrderId,
                            ProcessCodeCategoryId = id,
                            ProcessId = x.Id,
                        }));
                    }
                }


                var taskOrders = SmartTaskOrderHelper.Instance.GetByIds<SmartTaskOrder>(taskOrderIds);
                var workOrderIds = taskOrders.GroupBy(x => x.WorkOrderId).Select(y => y.Key);
                var existWorkOrderLines = ServerConfig.ApiDb
                    .Query<SmartLineWorkOrder>(
                        "SELECT WorkOrderId, ProcessCodeCategoryId FROM `t_line_work_order` " +
                        "WHERE MarkedDelete = 0 AND WorkOrderId IN @workOrderIds AND ProcessCodeCategoryId IN @processCodeCategoryIds GROUP BY WorkOrderId, ProcessCodeCategoryId;",
                        new { workOrderIds, processCodeCategoryIds });
                var workOrderLines = new List<SmartLineWorkOrder>();

                foreach (var workOrderId in workOrderIds)
                {
                    var tasks = taskOrders.Where(x => x.WorkOrderId == workOrderId);
                    var ids = flowCards.Where(x => tasks.Any(a => a.Id == x.TaskOrderId)).GroupBy(y => y.ProcessCodeId).Select(z => z.Key);
                    foreach (var id in ids)
                    {
                        if (existWorkOrderLines.Any(x => x.WorkOrderId == workOrderId && x.ProcessCodeCategoryId == id))
                        {
                            continue;
                        }
                        var ps = processes.Where(x => x.ProcessCodeCategoryId == id);
                        if (!ps.Any())
                        {
                            continue;
                        }

                        workOrderLines.AddRange(ps.Select(x => new SmartLineWorkOrder
                        {
                            CreateUserId = createUserId,
                            MarkedDateTime = markedDateTime,
                            WorkOrderId = workOrderId,
                            ProcessCodeCategoryId = id,
                            ProcessId = x.Id,
                        }));
                    }
                }

                SmartLineTaskOrderHelper.Instance.Add<SmartLineTaskOrder>(taskOrderLines);
                SmartLineWorkOrderHelper.Instance.Add<SmartLineWorkOrder>(workOrderLines);
            };

            SmartProcessFaultChanged += (o, processFaults) =>
            {
                if (processFaults == null || !processFaults.Any())
                {
                    return;
                }

                foreach (var fault in processFaults)
                {
                    SmartFlowCardProcessHelper.UpdateSmartFlowCardProcessFault(fault.ProcessId);
                }
            };

            SmartOperatorLevelChanged += (o, operatorLevels) =>
            {
                //if (operatorLevels == null || !operatorLevels.Any())
                //{
                //    return;
                //}

                //var levels =
                //    SmartOperatorLevelHelper.Instance.GetAll<SmartOperatorLevel>().OrderBy(x => x.Order).ThenBy(y => y.Id);

                //for (var i = 0; i < levels.Count(); i++)
                //{
                //    levels.ElementAt(i).Order = i;
                //}

                //ServerConfig.ApiDb.Execute("UPDATE `t_operator_level` SET `Order` = @Order WHERE `Id` = @Id;", levels);
            };

            SmartCapacityListChanged += (o, capacityLists) =>
            {
                if (capacityLists == null || !capacityLists.Any())
                {
                    return;
                }

                var capacityIds = capacityLists.GroupBy(x => x.CapacityId).Select(y => y.Key);
                //产能类型没有日产能
                //var list = ServerConfig.ApiDb.Query<SmartCapacityList>("SELECT * FROM (SELECT a.* FROM `t_capacity_list` a " +
                //                                                       "JOIN `t_process_code_category_process` b ON a.ProcessId = b.Id " +
                //                                                       "WHERE a.MarkedDelete = 0 AND b.MarkedDelete = 0 AND a.CapacityId IN @capacityIds ORDER BY b.`Order` DESC) a GROUP BY CapacityId;", new
                //                                                       {
                //                                                           capacityIds
                //                                                       });

                //var modelIds = capacityLists.SelectMany(x => x.DeviceList).Select(y => y.ModelId).Distinct();
                //var modelCount = ServerConfig.ApiDb.Query<dynamic>(
                //    "SELECT ModelId, COUNT(1) Count FROM `t_device` WHERE ModelId IN @modelIds AND MarkedDelete = 0 GROUP BY ModelId;",
                //    new
                //    {
                //        modelIds
                //    });

                //var processIds = capacityLists.Select(x => x.ProcessId).Distinct();
                //var operatorCount = ServerConfig.ApiDb.Query<dynamic>(
                //    "SELECT LevelId, COUNT(1) Count FROM `t_operator` a " +
                //    "JOIN `t_process_code_category_process` b ON a.ProcessId = b.ProcessId WHERE b.Id IN @processIds AND a.MarkedDelete = 0 GROUP BY LevelId", new
                //    {
                //        processIds
                //    });
                //var capacities = new List<SmartCapacity>();
                //foreach (var l in list)
                //{
                //    var capacity = new SmartCapacity
                //    {
                //        Id = l.CapacityId,
                //        Last = l.Id
                //    };
                //    var devices = l.DeviceList;
                //    foreach (var device in devices)
                //    {
                //        device.Count = modelCount.FirstOrDefault(x => (int)x.ModelId == device.ModelId) != null
                //            ? (int)modelCount.FirstOrDefault(x => (int)x.ModelId == device.ModelId).Count : 0;
                //    }
                //    var deviceCapacity = devices.Sum(x => x.Total);
                //    var operators = l.OperatorList;
                //    foreach (var op in operators)
                //    {
                //        op.Count = operatorCount.FirstOrDefault(x => (int)x.LevelId == op.LevelId) != null
                //            ? (int)operatorCount.FirstOrDefault(x => (int)x.LevelId == op.LevelId).Count : 0;
                //    }
                //    var operatorCapacity = operators.Sum(x => x.Total);
                //    capacity.DeviceNumber = deviceCapacity;
                //    capacity.OperatorNumber = operatorCapacity;
                //    capacity.Number = Math.Max(capacity.DeviceNumber, capacity.OperatorNumber);
                //    capacities.Add(capacity);
                //}

                //ServerConfig.ApiDb.Execute("UPDATE `t_capacity` SET `Number` = @Number, `DeviceNumber` = @DeviceNumber, `OperatorNumber` = @OperatorNumber, `Last` = @Last WHERE `Id` = @Id;", capacities);
                var smartProducts = SmartProductHelper.GetSmartProductsByCapacityIds(capacityIds);
                OnSmartProductCapacityNeedUpdate(smartProducts);
            };


            SmartOperatorChanged += (o, operators) =>
            {
                var smartProducts = SmartProductHelper.Instance.GetAll<SmartProduct>();
                OnSmartProductCapacityNeedUpdate(smartProducts);
            };

            TaskOrderArrangeChanged += (o, taskOrderArrange) =>
            {
                if (taskOrderArrange == null)
                {
                    return;
                }




            };




            SmartLineTaskOrderNeedUpdate += (o, taskOrderIdProcessCodeIds) =>
            {
                if (taskOrderIdProcessCodeIds == null || !taskOrderIdProcessCodeIds.Any())
                {
                    return;
                }

                var taskOrderIds = taskOrderIdProcessCodeIds.Select(x => x.TaskOrderId);
                var processCodeCategoryIds = taskOrderIdProcessCodeIds.Select(x => x.ProcessCodeCategoryId);
                var processes =
                    SmartFlowCardProcessHelper.GetSmartFlowCardProcesses1(taskOrderIds, processCodeCategoryIds);

                var smartLines = new Dictionary<Tuple<int, int, int>, SmartLineTaskOrder>();
                foreach (var process in processes)
                {
                    var key = new Tuple<int, int, int>(process.TaskOrderId, process.ProcessCodeCategoryId, process.StandardId);
                    if (!smartLines.ContainsKey(key))
                    {
                        smartLines.Add(key, new SmartLineTaskOrder { ProcessId = process.StandardId });
                    }

                    if (smartLines[key].StartTime == default(DateTime))
                    {
                        smartLines[key].StartTime = process.StartTime;
                    }

                    if (smartLines[key].StartTime != default(DateTime)
                        && process.StartTime != default(DateTime)
                        && smartLines[key].StartTime < process.StartTime)
                    {
                        smartLines[key].StartTime = process.StartTime;
                    }
                    if (smartLines[key].EndTime == default(DateTime))
                    {
                        smartLines[key].EndTime = process.EndTime;
                    }

                    if (smartLines[key].EndTime != default(DateTime)
                        && process.EndTime != default(DateTime)
                        && smartLines[key].EndTime < process.EndTime)
                    {
                        smartLines[key].EndTime = process.EndTime;
                    }

                    smartLines[key].Before += process.Before;
                    smartLines[key].Doing += process.Doing;
                    smartLines[key].Qualified += process.Qualified;
                    smartLines[key].Unqualified += process.Unqualified;
                    switch (process.State)
                    {
                        case SmartFlowCardProcessState.加工中:
                            smartLines[key].State = SmartLineState.加工中; break;
                        case SmartFlowCardProcessState.等待中:
                        case SmartFlowCardProcessState.已取消:
                        case SmartFlowCardProcessState.暂停中:
                        case SmartFlowCardProcessState.已完成:
                            if (smartLines[key].State == SmartLineState.未加工)
                            {
                                var v = process.State.ToString();
                                if (EnumHelper.TryParseStr(v, out SmartLineState state))
                                {
                                    smartLines[key].State = state;
                                }
                            }
                            break;
                    }
                }

                ServerConfig.ApiDb.Execute(
                    "UPDATE `t_line_task_order` SET `State` = @State, `StartTime` = IF(@StartTime = '0001-01-01 00:00:00', `StartTime`, @StartTime), " +
                    "`EndTime` = IF(@EndTime = '0001-01-01 00:00:00', `EndTime`, @EndTime), `Before` = @Before, `Doing` = @Doing, `Qualified` = @Qualified, `Unqualified` = @Unqualified " +
                    "WHERE TaskOrderId = @TaskOrderId AND ProcessCodeCategoryId = @ProcessCodeCategoryId AND ProcessId = @ProcessId;",
                    smartLines.Values);
            };

            SmartLineWorkOrderNeedUpdate += (o, workOrderIdProcessCodeIds) =>
            {
                if (workOrderIdProcessCodeIds == null || !workOrderIdProcessCodeIds.Any())
                {
                    return;
                }

                var workOrderIds = workOrderIdProcessCodeIds.Select(x => x.WorkOrderId);
                var processCodeCategoryIds = workOrderIdProcessCodeIds.Select(x => x.ProcessCodeCategoryId);
                var processes =
                    SmartFlowCardProcessHelper.GetSmartFlowCardProcesses2(workOrderIds, processCodeCategoryIds);

                var smartLines = new Dictionary<Tuple<int, int, int>, SmartLineWorkOrder>();
                foreach (var process in processes)
                {
                    var key = new Tuple<int, int, int>(process.WorkOrderId, process.ProcessCodeCategoryId,
                        process.StandardId);
                    if (!smartLines.ContainsKey(key))
                    {
                        smartLines.Add(key, new SmartLineWorkOrder { ProcessId = process.StandardId });
                    }

                    if (smartLines[key].StartTime == default(DateTime))
                    {
                        smartLines[key].StartTime = process.StartTime;
                    }

                    if (smartLines[key].StartTime != default(DateTime)
                        && process.StartTime != default(DateTime)
                        && smartLines[key].StartTime < process.StartTime)
                    {
                        smartLines[key].StartTime = process.StartTime;
                    }
                    if (smartLines[key].EndTime == default(DateTime))
                    {
                        smartLines[key].EndTime = process.EndTime;
                    }

                    if (smartLines[key].EndTime != default(DateTime)
                        && process.EndTime != default(DateTime)
                        && smartLines[key].EndTime < process.EndTime)
                    {
                        smartLines[key].EndTime = process.EndTime;
                    }

                    smartLines[key].Before += process.Before;
                    smartLines[key].Doing += process.Doing;
                    smartLines[key].Qualified += process.Qualified;
                    smartLines[key].Unqualified += process.Unqualified;
                    switch (process.State)
                    {
                        case SmartFlowCardProcessState.加工中:
                            smartLines[key].State = SmartLineState.加工中; break;
                        case SmartFlowCardProcessState.等待中:
                        case SmartFlowCardProcessState.已取消:
                        case SmartFlowCardProcessState.暂停中:
                        case SmartFlowCardProcessState.已完成:
                            if (smartLines[key].State == SmartLineState.未加工)
                            {
                                var v = process.State.ToString();
                                if (EnumHelper.TryParseStr(v, out SmartLineState state))
                                {
                                    smartLines[key].State = state;
                                }
                            }
                            break;
                    }
                }

                ServerConfig.ApiDb.Execute(
                    "UPDATE `t_line_work_order` SET `State` = @State, `StartTime` = IF(@StartTime = '0001-01-01 00:00:00', `StartTime`, @StartTime), " +
                    "`EndTime` = IF(@EndTime = '0001-01-01 00:00:00', `EndTime`, @EndTime), `Before` = @Before, `Doing` = @Doing, `Qualified` = @Qualified, `Unqualified` = @Unqualified " +
                    "WHERE WorkOrderId = @WorkOrderId AND ProcessCodeCategoryId = @ProcessCodeCategoryId AND ProcessId = @ProcessId;",
                    smartLines.Values);
            };

            SmartProductCapacityNeedUpdate += (o, products) =>
            {
                if (products == null || !products.Any())
                {
                    return;
                }

                var productsIds = products.Select(x => x.Id);
                products = SmartProductHelper.Instance.GetByIds<SmartProduct>(productsIds);
                var capacityIds = products.Select(x => x.CapacityId);
                var capacities = SmartCapacityListHelper.GetSmartCapacityListsWithOrder(capacityIds);
                var productCapacities = SmartProductCapacityHelper.GetSmartProductCapacities(productsIds).Select(
                    x =>
                    {
                        x.DeviceCapacity = 0;
                        x.OperatorCapacity = 0;
                        //x.Number = 0;
                        return x;
                    });

                var modelIds = capacities.SelectMany(x => x.DeviceList).Select(y => y.ModelId).Distinct();
                var modelCount = SmartDeviceHelper.GetNormalDeviceModelCount(modelIds);

                var processIds = capacities.Select(x => x.ProcessId).Distinct();
                var operatorCount = SmartOperatorHelper.GetNormalOperatorCount(processIds);

                var smartProductCapacities = new List<SmartProductCapacity>();
                foreach (var product in products)
                {
                    product.Number = 0;
                    product.DeviceCapacity = 0;
                    product.OperatorCapacity = 0;
                    var productId = product.Id;
                    var capacityId = product.CapacityId;
                    var capacityList = capacities.Where(x => x.CapacityId == capacityId);
                    var pCapacities = productCapacities.Where(x => x.ProductId == productId);
                    for (var i = 0; i < capacityList.Count(); i++)
                    {
                        var l = capacityList.ElementAt(i);
                        var pc = pCapacities.FirstOrDefault(x => x.ProcessId == l.ProcessId);
                        if (pc == null)
                        {
                            continue;
                        }

                        var deviceCapacity = 0;
                        if (l.CategoryId != 0)
                        {
                            var devices = l.DeviceList;
                            foreach (var device in devices)
                            {
                                device.Count = modelCount.FirstOrDefault(x => (int)x.ModelId == device.ModelId) != null
                                    ? (int)modelCount.FirstOrDefault(x => (int)x.ModelId == device.ModelId).Count : 0;
                            }
                            deviceCapacity = devices.Sum(x => x.Total);
                        }
                        var operators = l.OperatorList;
                        foreach (var op in operators)
                        {
                            op.Count = operatorCount.FirstOrDefault(x => (int)x.ProcessId == l.PId && (int)x.LevelId == op.LevelId) != null
                                ? (int)operatorCount.FirstOrDefault(x => (int)x.ProcessId == l.PId && (int)x.LevelId == op.LevelId).Count : 0;
                        }
                        var rate = 0m;
                        if (l.DeviceList.Any())
                        {
                            rate = l.DeviceList.First().Rate;
                        }
                        else if (l.OperatorList.Any())
                        {
                            rate = l.OperatorList.First().Rate;
                        }
                        var operatorCapacity = operators.Sum(x => x.Total);
                        if (i == 0)
                        {
                            deviceCapacity = l.CategoryId == 0 ? operatorCapacity : deviceCapacity;
                            product.DeviceCapacity = (int)Math.Floor(deviceCapacity * rate / 100);
                            product.OperatorCapacity = (int)Math.Floor(operatorCapacity * rate / 100);
                            product.Number = Math.Max(product.DeviceCapacity, product.OperatorCapacity);
                        }
                        else
                        {
                            //smartProduct.DeviceNumber = l.CategoryId != 0 ? Math.Min(deviceCapacity, smartProduct.DeviceNumber) : 0;
                            //smartProduct.OperatorNumber = Math.Min(operatorCapacity, smartProduct.OperatorNumber);
                            //smartProduct.Number = Math.Max(smartProduct.DeviceNumber, smartProduct.OperatorNumber);
                            //检验设备无设备产能取人员产能
                            //var deviceNumber = l.CategoryId != 0 ? Math.Min(deviceCapacity, smartProduct.DeviceNumber) : operatorCapacity;
                            //var operatorNumber = Math.Min(operatorCapacity, smartProduct.OperatorNumber);
                            deviceCapacity = l.CategoryId == 0 ? operatorCapacity : deviceCapacity;
                            //上道工序合格品数量 > 当前工序最大产能
                            var tDeviceCapacity = product.DeviceCapacity > deviceCapacity
                                 ? (int)Math.Floor(deviceCapacity * rate / 100)
                                 : (int)Math.Floor(product.DeviceCapacity * rate / 100);
                            //上道工序合格品数量 > 当前工序最大产能
                            var tOperatorCapacity = product.OperatorCapacity > operatorCapacity
                                 ? (int)Math.Floor(operatorCapacity * rate / 100)
                                 : (int)Math.Floor(product.OperatorCapacity * rate / 100);

                            //pc.DeviceNumber = tDeviceCapacity;
                            //pc.OperatorNumber = tOperatorCapacity;
                            //pc.Number = Math.Max(smartProduct.DeviceNumber, smartProduct.OperatorNumber);

                            product.DeviceCapacity = tDeviceCapacity;
                            product.OperatorCapacity = tOperatorCapacity;
                            product.Number = Math.Max(product.DeviceCapacity, product.OperatorCapacity);
                        }
                        pc.DeviceCapacity = product.DeviceCapacity;
                        pc.OperatorCapacity = product.OperatorCapacity;
                        //pc.Number = smartProduct.Number;
                        var f = false;
                        if (deviceCapacity == 0 && operatorCapacity == 0)
                        {
                            f = true;
                            pc.Error = SmartProductCapacityError.产能未设置;
                        }

                        if (rate == 0)
                        {
                            f = true;
                            pc.Error = SmartProductCapacityError.合格率未设置;
                        }

                        smartProductCapacities.Add((SmartProductCapacity)pc.Clone());

                        if (f)
                        {
                            break;
                        }
                    }
                }
                smartProductCapacities.AddRange(productCapacities.Where(x => smartProductCapacities.All(y => y.Id != x.Id)));

                var args = new List<string>
                {
                    "Number", "DeviceCapacity", "OperatorCapacity"
                };
                var cons = new List<string>
                {
                    "Id"
                };
                SmartProductHelper.Instance.CommonUpdate(args, cons, products);
                args.Add("Error");
                SmartProductCapacityHelper.Instance.CommonUpdate(args, cons, smartProductCapacities);

                OnTaskOrderArrangeChanged();
            };
        }
    }
    public class BillChange
    {
        public int BillId { get; set; }
        public int NewBillId { get; set; }
        public string NewCode { get; set; }
    }
    public class TaskOrderIdProcessCodeId
    {
        public int TaskOrderId { get; set; }
        /// <summary>
        /// 流程编号类型id
        /// </summary>
        public int ProcessCodeCategoryId { get; set; }
        /// <summary>
        /// 计划号流程id
        /// </summary>
        public int ProcessId { get; set; }
        /// <summary>
        /// 标准流程id
        /// </summary>
        public int StandardId { get; set; }
    }
    public class WorkOrderIdProcessCodeId
    {
        public int WorkOrderId { get; set; }
        /// <summary>
        /// 流程编号类型id
        /// </summary>
        public int ProcessCodeCategoryId { get; set; }
        /// <summary>
        /// 计划号流程id
        /// </summary>
        public int ProcessId { get; set; }
        /// <summary>
        /// 标准流程id
        /// </summary>
        public int StandardId { get; set; }
    }
}
