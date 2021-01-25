using ApiManagement.Models.SmartFactoryModel;
using ModelBase.Base.Utils;
using Newtonsoft.Json;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
#if DEBUG
#else
using System.Threading;
#endif
using DateTime = System.DateTime;

namespace ApiManagement.Base.Helper
{
    /// <summary>
    /// 生产排程
    /// </summary>
    public class ScheduleHelper
    {
        public ScheduleHelper()
        {
            Init();
        }
#if DEBUG
#else
        private static Timer _scheduleTimer;
#endif
        public static readonly ScheduleHelper Instance = new ScheduleHelper();
        private const int Day = 100;
        public void Init()
        {
            //_scheduleTimer = new Timer(NeedArrange, null, 5000, 1000 * 60 * 1);
        }

        /// <summary>
        /// 安排入口
        /// </summary>
        /// <param name="tasks">所有待排程任务</param>
        /// <param name="schedule">最终安排结果</param>
        /// <param name="indexes">指数</param>
        /// <param name="isArrange">是否安排 入库</param>
        /// <param name="arrangeId">是否安排过</param>
        /// <param name="createUserId">安排人</param>
        /// <param name="markedDateTime">安排时间</param>
        /// <param name="type">排程方法 （1）最短工期（2）最早交货期（3）按照工期和交货期之间的距离（4）CR值</param>
        /// https://blog.csdn.net/console11/article/details/96288314
        public static List<SmartTaskOrderScheduleCostDays> ArrangeSchedule(
            ref IEnumerable<SmartTaskOrderConfirm> tasks,
            ref List<SmartTaskOrderScheduleDetail> schedule,
            out IEnumerable<SmartTaskOrderScheduleIndex> indexes,
            bool isArrange = false,
            string createUserId = "",
            DateTime markedDateTime = default(DateTime),
            string arrangeId = "")
        {
            //if (arrangeId == "")
            //{

            //}
            var costDays = new List<SmartTaskOrderScheduleCostDays>();
            var today = DateTime.Today;
            var taskIds = tasks.Select(x => x.Id);
            var theseTasks = SmartTaskOrderHelper.GetWillArrangedSmartTaskOrders(taskIds);
            var needs = new List<SmartTaskOrderNeedDetail>();
            if (theseTasks.Any())
            {
                needs.AddRange(SmartTaskOrderNeedHelper.GetSmartTaskOrderNeedsByTaskOrderIds(
                    theseTasks.Select(x => x.Id)));
            }

            foreach (var task in tasks)
            {
                var aTask = theseTasks.FirstOrDefault(x => x.Id == task.Id);
                if (aTask != null)
                {
                    task.DeliveryTime = aTask.DeliveryTime;
                    task.ArrangedTime = aTask.ArrangedTime;
                    //task.Init();
                    task.Arranged = aTask.Arranged;
                    task.ProductId = aTask.ProductId;
                    task.Target = aTask.Target;
                    task.LevelId = aTask.LevelId;
                    task.TaskOrder = aTask.TaskOrder;
                    var tNeeds = needs.Where(x => x.TaskOrderId == task.Id);
                    if (task.Arranged && tNeeds.Any())
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
                }
            }
            if (!tasks.Any())
            {
                indexes = null;
                return costDays;
            }
            //所有工序
            var processes = SmartProcessHelper.Instance.GetAll<SmartProcess>();
            var productIds = tasks.Select(x => x.ProductId);
            // 任务单计划号
            var products = SmartProductHelper.Instance.GetByIds<SmartProduct>(productIds);
            // 计划号产能
            var productCapacities = SmartProductCapacityHelper.GetAllSmartProductCapacities(productIds);
            var capacityIds = products.Select(x => x.CapacityId);
            // 产能设置
            var smartCapacityLists = SmartCapacityListHelper.GetAllSmartCapacityListsWithOrder(capacityIds);
            //设备型号数量
            var deviceList = SmartDeviceHelper.GetNormalSmartDevices();
            var modelCount = deviceList.GroupBy(x => new { x.CategoryId, x.ModelId }).Select(y => new SmartDeviceModelCount
            {
                CategoryId = y.Key.CategoryId,
                ModelId = y.Key.ModelId,
                Count = y.Count()
            });
            //人员等级数量
            var operatorList = SmartOperatorHelper.GetNormalSmartOperators();
            var operatorCount = operatorList.GroupBy(x => new { x.ProcessId, x.LevelId }).Select(y => new SmartOperatorCount
            {
                ProcessId = y.Key.ProcessId,
                LevelId = y.Key.LevelId,
                Count = y.Count()
            });
            //工序已安排数量
            var schedules = SmartTaskOrderScheduleHelper.GetSmartTaskOrderSchedule(taskIds);

            //设备 0  人员 1
            var way = 2;
            var total = 4;
            var cnScore = new int[way][];
            var cnWaitTasks = new List<SmartTaskOrderConfirm>[way][];
            var cnSchedules = new Dictionary<DateTime, SmartTaskOrderScheduleDay>[way][];
            var cnCostList = new List<SmartTaskOrderScheduleCostDays>[way][];
            for (var i = 0; i < way; i++)
            {
                cnScore[i] = new int[total];
                cnWaitTasks[i] = new List<SmartTaskOrderConfirm>[total];
                cnSchedules[i] = new Dictionary<DateTime, SmartTaskOrderScheduleDay>[total];
                cnCostList[i] = new List<SmartTaskOrderScheduleCostDays>[total];
                for (var j = 0; j < total; j++)
                {
                    cnScore[i][j] = 0;
                    cnWaitTasks[i][j] = new List<SmartTaskOrderConfirm>();
                    cnSchedules[i][j] = new Dictionary<DateTime, SmartTaskOrderScheduleDay>();
                    cnCostList[i][j] = new List<SmartTaskOrderScheduleCostDays>();
                }
            }

            foreach (var task in tasks)
            {
                task.SetStartTime = task.StartTime < today ? today : task.StartTime;
                var product = products.FirstOrDefault(x => x.Id == task.ProductId);
                task.Product = product.Product;
                task.CapacityId = product.CapacityId;
                task.CapacityCostDay = product.Number != 0 ? (int)Math.Ceiling((decimal)task.Left / product.Number) : 0;
                var preProcessId = 0;
                var i = 0;
                foreach (var need in task.Needs)
                {
                    if (i++ == 0)
                    {
                        //首次加工原料数量
                        need.Have = need.Put;
                    }
                    var processId = need.ProcessId;
                    need.PreProcessId = preProcessId;
                    var process = processes.FirstOrDefault(x => x.Id == need.PId);
                    if (process != null)
                    {
                        need.Process = process.Process ?? "";
                        need.Order = process.Order;
                    }
                    var pre = task.Needs.FirstOrDefault(x => x.ProcessId == preProcessId);
                    if (pre != null)
                    {
                        need.Have = pre.Stock;
                        pre.NextProcessId = processId;
                    }
                    preProcessId = processId;
                }

                var s = task.ToJSON();
                for (var j = 0; j < total; j++)
                {
                    for (var k = 0; k < way; k++)
                    {
                        cnWaitTasks[k][j].Add(JsonConvert.DeserializeObject<SmartTaskOrderConfirm>(s));
                    }
                }
            }

            for (var i = 0; i < total; i++)
            {
                //var f = true;
                for (var j = 0; j < way; j++)
                {
                    var sc = CalSchedule(ref cnWaitTasks[j][i], schedules, productCapacities,
                        smartCapacityLists, deviceList, modelCount, operatorList, operatorCount, today, j, i);
                    //var t = today;
                    //var last = sc.Values.LastOrDefault(x => x.HaveArranged());
                    //if (last != null)
                    //{
                    //    t = last.ProcessTime.AddDays(1);
                    //}
                    //cnSchedules[j][i] = sc.Where(x => x.Key < t).ToDictionary(y => y.Key, y => y.Value);
                    cnSchedules[j][i] = sc;
                }

                //if (!f)
                //{
                //    continue;
                //}
                //回复耗时
                //var d = waitTasks1_d.Where(x => tasks.Any(y => y.Id == x.Id));
                //var o = waitTasks1_o.Where(x => tasks.Any(y => y.Id == x.Id));
                var d = cnWaitTasks[0][i];
                var o = cnWaitTasks[1][i];

                foreach (var task in tasks)
                {
                    var dCost = d.FirstOrDefault(x => x.Id == task.Id);
                    var oCost = o.FirstOrDefault(x => x.Id == task.Id);
                    var dEstimatedStartTime = default(DateTime);
                    var dEstimatedEndTime = default(DateTime);
                    var oEstimatedStartTime = default(DateTime);
                    var oEstimatedEndTime = default(DateTime);
                    var dC = new SmartTaskOrderScheduleCostDays
                    {
                        Id = task.Id,
                        TaskOrder = task.TaskOrder,
                        ProductId = task.ProductId,
                        Product = task.Product,
                        StartTime = task.StartTime,
                        EndTime = task.EndTime,
                    };
                    var oC = new SmartTaskOrderScheduleCostDays
                    {
                        Id = task.Id,
                        TaskOrder = task.TaskOrder,
                        ProductId = task.ProductId,
                        Product = task.Product,
                        StartTime = task.StartTime,
                        EndTime = task.EndTime,
                    };
                    if (dCost == null || !dCost.Needs.Any() || oCost == null || !oCost.Needs.Any())
                    {
                        continue;
                    }
                    foreach (var need in task.Needs)
                    {
                        var day = new SmartTaskOrderScheduleCostDay
                        {
                            ProcessId = need.ProcessId,
                            PId = need.PId,
                            Process = need.Process,
                            Order = need.Order,
                            DeviceDay = dCost.Needs.FirstOrDefault(x => x.ProcessId == need.ProcessId)?.CostDay ?? 0,
                            OperatorDay = oCost.Needs.FirstOrDefault(x => x.ProcessId == need.ProcessId)?.CostDay ?? 0
                        };
                        var tNeed = dCost.Needs.FirstOrDefault(x => x.ProcessId == need.ProcessId);
                        //设备开始时间
                        var estimatedStartTime = tNeed?.EstimatedStartTime ?? DateTime.Today;
                        if (estimatedStartTime != default(DateTime))
                        {
                            dEstimatedStartTime = dEstimatedStartTime == default(DateTime) ? estimatedStartTime :
                                (estimatedStartTime < dEstimatedStartTime ? estimatedStartTime : dEstimatedStartTime);
                        }

                        //设备结束时间
                        var estimatedCompleteTime = tNeed?.EstimatedEndTime ?? DateTime.Today;
                        if (estimatedCompleteTime != default(DateTime))
                        {
                            dEstimatedEndTime = dEstimatedEndTime == default(DateTime) ? estimatedCompleteTime :
                                (estimatedCompleteTime > dEstimatedEndTime ? estimatedCompleteTime : dEstimatedEndTime);
                        }

                        tNeed = oCost.Needs.FirstOrDefault(x => x.ProcessId == need.ProcessId);
                        //人员开始时间
                        estimatedStartTime = tNeed?.EstimatedStartTime ?? DateTime.Today;
                        if (estimatedStartTime != default(DateTime))
                        {
                            oEstimatedStartTime = oEstimatedStartTime == default(DateTime) ? estimatedStartTime :
                                (estimatedStartTime < oEstimatedStartTime ? estimatedStartTime : oEstimatedStartTime);
                        }

                        //人员结束时间
                        estimatedCompleteTime = tNeed?.EstimatedEndTime ?? DateTime.Today;
                        if (estimatedCompleteTime != default(DateTime))
                        {
                            oEstimatedEndTime = oEstimatedEndTime == default(DateTime) ? estimatedCompleteTime :
                                (estimatedCompleteTime > oEstimatedEndTime ? estimatedCompleteTime : oEstimatedEndTime);
                        }

                        dC.CostDays.Add(day);
                        oC.CostDays.Add(day);
                    }

                    dC.EstimatedStartTime = dEstimatedStartTime;
                    dC.EstimatedEndTime = dEstimatedEndTime;
                    cnCostList[0][i].Add(dC);

                    oC.EstimatedStartTime = oEstimatedStartTime;
                    oC.EstimatedEndTime = oEstimatedEndTime;
                    cnCostList[1][i].Add(oC);
                    var dCostDay = (int)(dEstimatedEndTime - dEstimatedStartTime).TotalDays + 1;
                    var oCostDay = (int)(oEstimatedEndTime - oEstimatedStartTime).TotalDays + 1;
                    if (dCostDay < oCostDay)
                    {
                        cnScore[0][i]++;
                    }
                    else
                    {
                        cnScore[1][i]++;
                    }
                }

                cnCostList[0][i] = cnCostList[0][i].OrderBy(x => x.EstimatedStartTime)
                    .ThenBy(x => x.EstimatedEndTime).ToList();
                cnCostList[1][i] = cnCostList[1][i].OrderBy(x => x.EstimatedStartTime)
                    .ThenBy(x => x.EstimatedEndTime).ToList();
            }
            BestArrange(way, total, cnScore, cnCostList, cnSchedules, out var jj, out var ii);
            if (jj == -1)
            {
                indexes = null;
                return costDays;
            }
            costDays.AddRange(cnCostList[ii][jj]);
            //foreach (var task in tasks)
            //{
            //    var arrangeTask = cnWaitTasks[ii][jj].FirstOrDefault(x => x.Id == task.Id);
            //    if (arrangeTask != null)
            //    {
            //        task.StartTime = task.StartTime == default(DateTime) ? arrangeTask.Needs.Min(x => x.FirstArrangedTime) : task.StartTime;
            //        task.EndTime = arrangeTask.Needs.Max(x => x.EstimatedEndTime);
            //    }
            //}
            var t = costDays.Any() ? costDays.Max(x => x.EstimatedEndTime) : today;
            if (t == default(DateTime))
            {
                t = today;
            }
            var best = cnSchedules[ii][jj].Where(x => x.Key <= t).ToDictionary(y => y.Key, y => y.Value);
            schedule.AddRange(best.Values.SelectMany(y => y.Needs.SelectMany(z => z.Value).OrderBy(x => x.Order).ThenBy(x => x.ArrangeOrder)));
            indexes = best.Values.OrderBy(x => x.ProcessTime).SelectMany(y => y.CapacityIndexList.OrderBy(x => x.Order));
            //var ss =best.Values.SelectMany(y => y.Needs).SelectMany(z => z.Value);
            //schedule.AddRange(best.Values.Where(v => v.ProcessTime >= today).OrderBy(x => x.ProcessTime).SelectMany(y => y.Needs).SelectMany(z => z.Value));

            if (isArrange)
            {
                var batch = SmartTaskOrderHelper.AddSmartTaskOrderBatch(createUserId);
                var oldNeeds = SmartTaskOrderNeedHelper.GetSmartTaskOrderNeedsByTaskOrderIds(taskIds);
                var calNeeds = cnWaitTasks[ii][jj].SelectMany(x => x.Needs);
                var newNeeds = tasks.SelectMany(x => x.Needs);
                foreach (var need in newNeeds)
                {
                    var old = oldNeeds.FirstOrDefault(x => x.TaskOrderId == need.TaskOrderId && x.PId == need.PId);
                    if (old != null)
                    {
                        need.Stock = old.Stock;
                        need.DoneTarget = old.DoneTarget;
                        need.HavePut = old.HavePut;
                        need.Done = old.Done;
                        need.DoingCount = old.DoingCount;
                        need.Doing = old.Doing;
                        need.IssueCount = old.IssueCount;
                        need.Issue = old.Issue;
                    }
                    var cal = calNeeds.FirstOrDefault(x => x.TaskOrderId == need.TaskOrderId && x.PId == need.PId);
                    if (cal != null)
                    {
                        need.EstimatedStartTime = cal.EstimatedStartTime;
                        need.EstimatedEndTime = cal.EstimatedEndTime;
                        need.FirstArrangedTime = cal.FirstArrangedTime;
                    }
                    need.Batch = batch;
                    need.CreateUserId = createUserId;
                    need.MarkedDateTime = markedDateTime;
                }

                SmartTaskOrderScheduleHelper.Instance.Add(schedule.Select(x =>
                {
                    x.Batch = batch;
                    x.CreateUserId = createUserId;
                    x.MarkedDateTime = markedDateTime;
                    return x;
                }));
                SmartTaskOrderNeedHelper.Instance.Add(newNeeds);
                SmartTaskOrderScheduleIndexHelper.Instance.Add(indexes.Select(x =>
                {
                    x.Batch = batch;
                    x.CreateUserId = createUserId;
                    x.MarkedDateTime = markedDateTime;
                    return x;
                }));
                SmartTaskOrderHelper.Arrange(tasks.Select(x =>
                {
                    x.ArrangedTime = markedDateTime;
                    x.MarkedDateTime = markedDateTime;
                    return x;
                }));
                //排程是否改变
                var haveChange = false;
                var arrange = new List<int>();


                if (haveChange)
                {
                    WorkFlowHelper.Instance.OnTaskOrderArrangeChanged(arrange);
                }
            }
            return costDays;
        }

        /// <summary>
        /// 计算方法
        /// </summary>
        /// <param name="allTasks"></param>
        /// <param name="schedules"></param>
        /// <param name="productCapacities"></param>
        /// <param name="smartCapacityLists"></param>
        /// <param name="deviceList"></param>
        /// <param name="modelCounts"></param>
        /// <param name="operatorList"></param>
        /// <param name="operatorCounts"></param>
        /// <param name="time"></param>
        /// <param name="productType"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private static Dictionary<DateTime, SmartTaskOrderScheduleDay> CalSchedule(
            ref List<SmartTaskOrderConfirm> allTasks,
            IEnumerable<SmartTaskOrderScheduleDetail> schedules,
            IEnumerable<SmartProductCapacityDetail> productCapacities,
            IEnumerable<SmartCapacityListDetail> smartCapacityLists,
            IEnumerable<SmartDevice> deviceList,
            IEnumerable<SmartDeviceModelCount> modelCounts,
            IEnumerable<SmartOperatorDetail> operatorList,
            IEnumerable<SmartOperatorCount> operatorCounts,
            DateTime time,
            int productType,
            int type)
        {
            var newSchedules = new Dictionary<DateTime, SmartTaskOrderScheduleDay>();
            var minTime = time;
            var maxTime = time.AddDays(Day);
            var setEndTime = allTasks.Where(x => x.EndTime != default(DateTime));
            //最小时间
            minTime = !setEndTime.Any() ? minTime : setEndTime.Min(x => x.StartTime) < minTime ? setEndTime.Min(x => x.EndTime) : minTime;
            minTime = !schedules.Any() ? minTime : schedules.Min(x => x.ProcessTime) < minTime ? schedules.Min(x => x.ProcessTime) : minTime;
            //最大时间
            maxTime = !setEndTime.Any() ? maxTime : setEndTime.Max(x => x.EndTime) > maxTime ? setEndTime.Max(x => x.EndTime) : maxTime;
            maxTime = !schedules.Any() ? maxTime : schedules.Max(x => x.ProcessTime) > maxTime ? schedules.Max(x => x.ProcessTime) : maxTime;

            AddDay(ref newSchedules, minTime, maxTime, deviceList, operatorList);
            InitData(ref newSchedules, allTasks, schedules, productCapacities, smartCapacityLists, deviceList, operatorList);
            IEnumerable<SmartTaskOrderConfirm> superTasks;
            IEnumerable<SmartTaskOrderConfirm> normalTasks;
            List<SmartTaskOrderConfirm> superTimeLimitTasks;
            List<SmartTaskOrderConfirm> superNotTimeLimitTasks;
            List<SmartTaskOrderConfirm> normalTimeLimitTasks;
            List<SmartTaskOrderConfirm> normalNotTimeLimitTasks;
            List<SmartTaskOrderConfirm> processTasks = new List<SmartTaskOrderConfirm>();
            var f = false;
            if (f)
            {
#region 普通
                //S级任务  排产优先
                superTasks = allTasks.Where(x => x.LevelId == 1).OrderBy(x => x.EndTime).ThenBy(x => x.Id);
                //有时间要求的任务 先按生产天数从小到大排，再按截止时间从小到大排
                superTimeLimitTasks = superTasks.Where(x => x.EndTime != default(DateTime)).OrderBy(t => t.Order).ThenBy(y => y.CapacityCostDay).ThenBy(y => y.EndTime).ThenBy(y => y.StartTime).ToList();
                //没有时间要求的任务 先按生产天数从小到大排，再按目标量从小到大排
                superNotTimeLimitTasks = superTasks.Where(x => x.EndTime == default(DateTime)).OrderBy(t => t.Order).ThenBy(y => y.CapacityCostDay).ThenBy(y => y.StartTime).ThenBy(y => y.Target).ToList();

                //非S级任务  排产可修改
                normalTasks = allTasks.Where(x => x.LevelId != 1).OrderBy(y => y.Order);
                //有时间要求的任务 先按生产天数从小到大排，再按截止时间从小到大排
                normalTimeLimitTasks = normalTasks.Where(x => x.EndTime != default(DateTime)).OrderBy(t => t.Order).ThenBy(y => y.CapacityCostDay).ThenBy(y => y.EndTime).ThenBy(y => y.StartTime).ToList();
                //没有时间要求的任务 先按生产天数从小到大排，再按目标量从小到大排
                normalNotTimeLimitTasks = normalTasks.Where(x => x.EndTime == default(DateTime)).OrderBy(t => t.Order).ThenBy(y => y.CapacityCostDay).ThenBy(y => y.StartTime).ThenBy(y => y.Target).ToList();
#endregion
            }
            else
            {
#region MyRegion
                switch (type)
                {
                    case 0:
#region 最短工期
                        //S级任务  排产优先
                        superTasks = allTasks.Where(x => x.LevelId == 1);
                        //有开始时间和截止时间的任务
                        superTimeLimitTasks = superTasks.Where(x => x.SetStartTime != default(DateTime) && x.EndTime != default(DateTime)).ToList();
                        //开始时间从小到大排 截止时间从小到大排 生产天数从小到大排
                        processTasks.AddRange(superTimeLimitTasks.OrderBy(t => t.SetStartTime).ThenBy(t => t.EndTime).ThenBy(t => t.CapacityCostDay));
                        //有开始时间，没有截止时间的任务
                        superNotTimeLimitTasks = superTasks.Where(x => x.SetStartTime != default(DateTime) && x.EndTime == default(DateTime)).ToList();
                        //开始时间从小到大排 生产天数从小到大排
                        processTasks.AddRange(superNotTimeLimitTasks.OrderBy(t => t.SetStartTime).ThenBy(t => t.CapacityCostDay));
                        //有截止时间，没有开始时间的任务
                        superNotTimeLimitTasks = superTasks.Where(x => x.SetStartTime == default(DateTime) && x.EndTime != default(DateTime)).ToList();
                        //截止时间从小到大排 生产天数从小到大排
                        processTasks.AddRange(superNotTimeLimitTasks.OrderBy(t => t.EndTime).ThenBy(t => t.CapacityCostDay));
                        //没有开始时间和截止时间的任务
                        superNotTimeLimitTasks = superTasks.Where(x => x.SetStartTime == default(DateTime) && x.EndTime == default(DateTime)).ToList();
                        //生产天数从小到大排
                        processTasks.AddRange(superNotTimeLimitTasks.OrderBy(t => t.CapacityCostDay));

                        //非S级任务  排产可修改
                        normalTasks = allTasks.Where(x => x.LevelId != 1);
                        //有开始时间和截止时间的任务
                        normalTimeLimitTasks = normalTasks.Where(x => x.SetStartTime != default(DateTime) && x.EndTime != default(DateTime)).ToList();
                        //开始时间从小到大排 截止时间从小到大排 生产天数从小到大排
                        processTasks.AddRange(normalTimeLimitTasks.OrderBy(t => t.SetStartTime).ThenBy(t => t.EndTime).ThenBy(t => t.CapacityCostDay));
                        //有开始时间，没有截止时间的任务
                        normalNotTimeLimitTasks = normalTasks.Where(x => x.SetStartTime != default(DateTime) && x.EndTime == default(DateTime)).ToList();
                        //开始时间从小到大排 生产天数从小到大排
                        processTasks.AddRange(normalNotTimeLimitTasks.OrderBy(t => t.SetStartTime).ThenBy(t => t.CapacityCostDay));
                        //有截止时间，没有开始时间的任务
                        normalNotTimeLimitTasks = normalTasks.Where(x => x.SetStartTime == default(DateTime) && x.EndTime != default(DateTime)).ToList();
                        //截止时间从小到大排 生产天数从小到大排
                        processTasks.AddRange(normalNotTimeLimitTasks.OrderBy(t => t.EndTime).ThenBy(t => t.CapacityCostDay));
                        //没有开始时间和截止时间的任务
                        normalNotTimeLimitTasks = normalTasks.Where(x => x.SetStartTime == default(DateTime) && x.EndTime == default(DateTime)).ToList();
                        //生产天数从小到大排
                        processTasks.AddRange(normalNotTimeLimitTasks.OrderBy(t => t.CapacityCostDay));
#endregion
                        break;
                    case 1:
#region 最早交货期  截止日期
                        //S级任务  排产优先
                        superTasks = allTasks.Where(x => x.LevelId == 1);
                        //有开始时间和截止时间的任务
                        superTimeLimitTasks = superTasks.Where(x => x.SetStartTime != default(DateTime) && x.EndTime != default(DateTime)).ToList();
                        //开始时间从小到大排 截止时间从小到大排 生产天数从小到大排
                        processTasks.AddRange(superTimeLimitTasks.OrderBy(t => t.SetStartTime).ThenBy(t => t.EndTime).ThenBy(t => t.DeliveryTime));
                        //有开始时间，没有截止时间的任务
                        superNotTimeLimitTasks = superTasks.Where(x => x.SetStartTime != default(DateTime) && x.EndTime == default(DateTime)).ToList();
                        //开始时间从小到大排 生产天数从小到大排
                        processTasks.AddRange(superNotTimeLimitTasks.OrderBy(t => t.SetStartTime).ThenBy(t => t.DeliveryTime));
                        //有截止时间，没有开始时间的任务
                        superNotTimeLimitTasks = superTasks.Where(x => x.SetStartTime == default(DateTime) && x.EndTime != default(DateTime)).ToList();
                        //截止时间从小到大排 生产天数从小到大排
                        processTasks.AddRange(superNotTimeLimitTasks.OrderBy(t => t.EndTime).ThenBy(t => t.DeliveryTime));
                        //没有开始时间和截止时间的任务
                        superNotTimeLimitTasks = superTasks.Where(x => x.SetStartTime == default(DateTime) && x.EndTime == default(DateTime)).ToList();
                        //生产天数从小到大排
                        processTasks.AddRange(superNotTimeLimitTasks.OrderBy(t => t.DeliveryTime));

                        //非S级任务  排产可修改
                        normalTasks = allTasks.Where(x => x.LevelId != 1);
                        //有开始时间和截止时间的任务
                        normalTimeLimitTasks = normalTasks.Where(x => x.SetStartTime != default(DateTime) && x.EndTime != default(DateTime)).ToList();
                        //开始时间从小到大排 截止时间从小到大排 生产天数从小到大排
                        processTasks.AddRange(normalTimeLimitTasks.OrderBy(t => t.SetStartTime).ThenBy(t => t.EndTime).ThenBy(t => t.DeliveryTime));
                        //有开始时间，没有截止时间的任务
                        normalNotTimeLimitTasks = normalTasks.Where(x => x.SetStartTime != default(DateTime) && x.EndTime == default(DateTime)).ToList();
                        //开始时间从小到大排 生产天数从小到大排
                        processTasks.AddRange(normalNotTimeLimitTasks.OrderBy(t => t.SetStartTime).ThenBy(t => t.DeliveryTime));
                        //有截止时间，没有开始时间的任务
                        normalNotTimeLimitTasks = normalTasks.Where(x => x.SetStartTime == default(DateTime) && x.EndTime != default(DateTime)).ToList();
                        //截止时间从小到大排 生产天数从小到大排
                        processTasks.AddRange(normalNotTimeLimitTasks.OrderBy(t => t.EndTime).ThenBy(t => t.DeliveryTime));
                        //没有开始时间和截止时间的任务
                        normalNotTimeLimitTasks = normalTasks.Where(x => x.SetStartTime == default(DateTime) && x.EndTime == default(DateTime)).ToList();
                        //生产天数从小到大排
                        processTasks.AddRange(normalNotTimeLimitTasks.OrderBy(t => t.DeliveryTime));

#endregion
                        break;
                    case 2:
#region 工期和交货期之间的距离
                        //S级任务  排产优先
                        superTasks = allTasks.Where(x => x.LevelId == 1);
                        //有开始时间和截止时间的任务
                        superTimeLimitTasks = superTasks.Where(x => x.SetStartTime != default(DateTime) && x.EndTime != default(DateTime)).ToList();
                        //开始时间从小到大排 截止时间从小到大排 生产天数从小到大排
                        processTasks.AddRange(superTimeLimitTasks.OrderBy(t => t.SetStartTime).ThenBy(t => t.EndTime).ThenBy(t => t.DistanceDay));
                        //有开始时间，没有截止时间的任务
                        superNotTimeLimitTasks = superTasks.Where(x => x.SetStartTime != default(DateTime) && x.EndTime == default(DateTime)).ToList();
                        //开始时间从小到大排 生产天数从小到大排
                        processTasks.AddRange(superNotTimeLimitTasks.OrderBy(t => t.SetStartTime).ThenBy(t => t.DistanceDay));
                        //有截止时间，没有开始时间的任务
                        superNotTimeLimitTasks = superTasks.Where(x => x.SetStartTime == default(DateTime) && x.EndTime != default(DateTime)).ToList();
                        //截止时间从小到大排 生产天数从小到大排
                        processTasks.AddRange(superNotTimeLimitTasks.OrderBy(t => t.EndTime).ThenBy(t => t.DistanceDay));
                        //没有开始时间和截止时间的任务
                        superNotTimeLimitTasks = superTasks.Where(x => x.SetStartTime == default(DateTime) && x.EndTime == default(DateTime)).ToList();
                        //生产天数从小到大排
                        processTasks.AddRange(superNotTimeLimitTasks.OrderBy(t => t.DistanceDay));

                        //非S级任务  排产可修改
                        normalTasks = allTasks.Where(x => x.LevelId != 1);
                        //有开始时间和截止时间的任务
                        normalTimeLimitTasks = normalTasks.Where(x => x.SetStartTime != default(DateTime) && x.EndTime != default(DateTime)).ToList();
                        //开始时间从小到大排 截止时间从小到大排 生产天数从小到大排
                        processTasks.AddRange(normalTimeLimitTasks.OrderBy(t => t.SetStartTime).ThenBy(t => t.EndTime).ThenBy(t => t.DistanceDay));
                        //有开始时间，没有截止时间的任务
                        normalNotTimeLimitTasks = normalTasks.Where(x => x.SetStartTime != default(DateTime) && x.EndTime == default(DateTime)).ToList();
                        //开始时间从小到大排 生产天数从小到大排
                        processTasks.AddRange(normalNotTimeLimitTasks.OrderBy(t => t.SetStartTime).ThenBy(t => t.DistanceDay));
                        //有截止时间，没有开始时间的任务
                        normalNotTimeLimitTasks = normalTasks.Where(x => x.SetStartTime == default(DateTime) && x.EndTime != default(DateTime)).ToList();
                        //截止时间从小到大排 生产天数从小到大排
                        processTasks.AddRange(normalNotTimeLimitTasks.OrderBy(t => t.EndTime).ThenBy(t => t.DistanceDay));
                        //没有开始时间和截止时间的任务
                        normalNotTimeLimitTasks = normalTasks.Where(x => x.SetStartTime == default(DateTime) && x.EndTime == default(DateTime)).ToList();
                        //生产天数从小到大排
                        processTasks.AddRange(normalNotTimeLimitTasks.OrderBy(t => t.DistanceDay));
#endregion
                        break;
                    case 3:
#region CR值
                        //Critical Ratio, 可以翻译为重要比率。它的计算方法：交期减去目前日期之差额,再除以工期，数值越小表示紧急程度越高，排程优先级高。
                        //S级任务  排产优先
                        superTasks = allTasks.Where(x => x.LevelId == 1);
                        //有开始时间和截止时间的任务
                        superTimeLimitTasks = superTasks.Where(x => x.SetStartTime != default(DateTime) && x.EndTime != default(DateTime)).ToList();
                        //开始时间从小到大排 截止时间从小到大排 生产天数从小到大排
                        processTasks.AddRange(superTimeLimitTasks.OrderBy(t => t.SetStartTime).ThenBy(t => t.EndTime).ThenBy(t => t.CR));
                        //有开始时间，没有截止时间的任务
                        superNotTimeLimitTasks = superTasks.Where(x => x.SetStartTime != default(DateTime) && x.EndTime == default(DateTime)).ToList();
                        //开始时间从小到大排 生产天数从小到大排
                        processTasks.AddRange(superNotTimeLimitTasks.OrderBy(t => t.SetStartTime).ThenBy(t => t.CR));
                        //有截止时间，没有开始时间的任务
                        superNotTimeLimitTasks = superTasks.Where(x => x.SetStartTime == default(DateTime) && x.EndTime != default(DateTime)).ToList();
                        //截止时间从小到大排 生产天数从小到大排
                        processTasks.AddRange(superNotTimeLimitTasks.OrderBy(t => t.EndTime).ThenBy(t => t.CR));
                        //没有开始时间和截止时间的任务
                        superNotTimeLimitTasks = superTasks.Where(x => x.SetStartTime == default(DateTime) && x.EndTime == default(DateTime)).ToList();
                        //生产天数从小到大排
                        processTasks.AddRange(superNotTimeLimitTasks.OrderBy(t => t.CR));

                        //非S级任务  排产可修改
                        normalTasks = allTasks.Where(x => x.LevelId != 1);
                        //有开始时间和截止时间的任务
                        normalTimeLimitTasks = normalTasks.Where(x => x.SetStartTime != default(DateTime) && x.EndTime != default(DateTime)).ToList();
                        //开始时间从小到大排 截止时间从小到大排 生产天数从小到大排
                        processTasks.AddRange(normalTimeLimitTasks.OrderBy(t => t.SetStartTime).ThenBy(t => t.EndTime).ThenBy(t => t.CR));
                        //有开始时间，没有截止时间的任务
                        normalNotTimeLimitTasks = normalTasks.Where(x => x.SetStartTime != default(DateTime) && x.EndTime == default(DateTime)).ToList();
                        //开始时间从小到大排 生产天数从小到大排
                        processTasks.AddRange(normalNotTimeLimitTasks.OrderBy(t => t.SetStartTime).ThenBy(t => t.CR));
                        //有截止时间，没有开始时间的任务
                        normalNotTimeLimitTasks = normalTasks.Where(x => x.SetStartTime == default(DateTime) && x.EndTime != default(DateTime)).ToList();
                        //截止时间从小到大排 生产天数从小到大排
                        processTasks.AddRange(normalNotTimeLimitTasks.OrderBy(t => t.EndTime).ThenBy(t => t.CR));
                        //没有开始时间和截止时间的任务
                        normalNotTimeLimitTasks = normalTasks.Where(x => x.SetStartTime == default(DateTime) && x.EndTime == default(DateTime)).ToList();
                        //生产天数从小到大排
                        processTasks.AddRange(normalNotTimeLimitTasks.OrderBy(t => t.CR));
#endregion
                        break;
                    default:
                        return newSchedules;
                }
#endregion
            }


            ScheduleArrangeCal(ref processTasks, ref newSchedules, productCapacities, smartCapacityLists,
                deviceList, modelCounts, operatorList, operatorCounts, time, productType);

            //ScheduleArrangeCal(ref superTimeLimitTasks, ref newSchedules, productCapacities, smartCapacityLists,
            //    deviceList, modelCounts, operatorList, operatorCounts, time, productType);
            //ScheduleArrangeCal(ref superNotTimeLimitTasks, ref newSchedules, productCapacities, smartCapacityLists,
            //    deviceList, modelCounts, operatorList, operatorCounts, time, productType);
            //ScheduleArrangeCal(ref normalTimeLimitTasks, ref newSchedules, productCapacities, smartCapacityLists,
            //    deviceList, modelCounts, operatorList, operatorCounts, time, productType);
            //ScheduleArrangeCal(ref normalNotTimeLimitTasks, ref newSchedules, productCapacities, smartCapacityLists,
            //    deviceList, modelCounts, operatorList, operatorCounts, time, productType);
            return newSchedules;
        }

        /// <summary>
        /// 最优计算
        /// </summary>
        /// <param name="way"></param>
        /// <param name="total"></param>
        /// <param name="cnScore"></param>
        /// <param name="cnCostList"></param>
        /// <param name="cnSchedules"></param>
        /// <param name="jj"></param>
        /// <param name="ii"></param>
        private static void BestArrange(int way, int total,
            int[][] cnScore,
            List<SmartTaskOrderScheduleCostDays>[][] cnCostList,
            Dictionary<DateTime, SmartTaskOrderScheduleDay>[][] cnSchedules,
            out int jj,
            out int ii)
        {
            jj = -1;
            ii = -1;
            if (way != cnScore.Length)
            {
                return;
            }

            List<SmartTaskOrderScheduleCostDays> l = null;
            var b = 0;
            List<SmartTaskOrderScheduleCostDays> l1 = null;
            var j1 = -1;
            var b1 = 0;
            for (var i = 0; i < total; i++)
            {
                var dScore = cnScore[0];
                var oScore = cnScore[1];
                b = dScore[i] >= oScore[i] ? 0 : 1;
                l = cnCostList[b][i];
                if (!l.Any())
                {
                    if (i == 1)
                    {
                        jj = j1;
                        ii = b1;
                    }
                    else
                    {

                    }
                    break;
                }
                if (i != 0)
                {
                    if (l.Sum(x => x.OverdueDay) < l1.Sum(x => x.OverdueDay))
                    {
                        jj = i;
                        ii = b;
                    }
                    else
                    {
                        jj = j1;
                        ii = b1;
                    }
                }

                j1++;
                b1 = dScore[i] <= oScore[i] ? 0 : 1;
                l1 = cnCostList[b1][j1];
            }
        }

        /// <summary>
        /// 排程计算
        /// </summary>
        /// <param name="tasks"></param>
        /// <param name="newSchedules"></param>
        /// <param name="productCapacities"></param>
        /// <param name="smartCapacityLists"></param>
        /// <param name="deviceList"></param>
        /// <param name="modelCounts"></param>
        /// <param name="operatorList"></param>
        /// <param name="operatorCounts"></param>
        /// <param name="time"></param>
        /// <param name="productType"></param>
        /// <param name="critical">是否紧急</param>
        private static void ScheduleArrangeCal(
            ref List<SmartTaskOrderConfirm> tasks,
            ref Dictionary<DateTime, SmartTaskOrderScheduleDay> newSchedules,
            IEnumerable<SmartProductCapacityDetail> productCapacities,
            IEnumerable<SmartCapacityListDetail> smartCapacityLists,
            IEnumerable<SmartDevice> deviceList,
            IEnumerable<SmartDeviceModelCount> modelCounts,
            IEnumerable<SmartOperatorDetail> operatorList,
            IEnumerable<SmartOperatorCount> operatorCounts,
            DateTime time,
            int productType,
            bool critical = false)
        {
            var count = tasks.Count;
            var t = time.Date;
            var doneCount = 0;
            while (doneCount < count)
            {
                foreach (var task in tasks)
                {
                    var productId = task.ProductId;
                    var capacityId = task.CapacityId;
                    //产能配置
                    var capacityList = smartCapacityLists.Where(x => x.CapacityId == capacityId);
                    //计划号单日工序实际产能列表
                    var pCapacities = productCapacities.Where(x => x.ProductId == productId);
                    if (pCapacities.Any(x => x.DeviceNumber == 0 || x.OperatorNumber == 0))
                    {
                        task.CanArrange = false;
                        continue;
                    }

                    decimal waitIndex = 0;
                    for (var i = 0; i < task.Needs.Count; i++)
                    {
                        var need = task.Needs.ElementAt(i);
                        var processId = need.ProcessId;
                        //工序单日产能配置
                        var cList = capacityList.FirstOrDefault(x => x.ProcessId == processId);
                        need.PId = cList.PId;
                        need.Process = cList.Process;
                        //计划号工序单日产能
                        var pCapacity = pCapacities.FirstOrDefault(x => x.ProcessId == processId);
                        //安排设备或人员列表
                        var arrangeList = new List<ArrangeInfo>();
                        //用设备产能计算但是不支持设备加工
                        if (productType == 0 && cList.CategoryId == 0)
                        {
                            productType = 1;
                        }
                        SmartTaskOrderScheduleDetail sc;
                        if (task.SetStartTime != default(DateTime) && task.SetStartTime > t)
                        {
                            //当前没东西可加工
                            sc = new SmartTaskOrderScheduleDetail(t, task, cList, pCapacity)
                            {
                                MarkedDateTime = time,
                                Process = need.Process,
                                Stock = 0,
                                Target = 0,
                                Put = 0,
                                //Target = (int)(put * pCapacity.Rate / 100).ToRound(0),
                                CapacityIndex = 0
                            };
                            newSchedules[t].AddTaskOrderSchedule(sc, arrangeList);
                            continue;
                        }

                        if (need.Have <= 0)
                        {
                            //当前没东西可加工
                            sc = new SmartTaskOrderScheduleDetail(t, task, cList, pCapacity)
                            {
                                MarkedDateTime = time,
                                Process = need.Process,
                                Stock = 0,
                                Target = 0,
                                Put = 0,
                                //Target = (int)(put * pCapacity.Rate / 100).ToRound(0),
                                CapacityIndex = 0
                            };
                            newSchedules[t].AddTaskOrderSchedule(sc, arrangeList);
                            continue;
                        }

                        //根据productType判断用设备产能计算还是人员产能计算， 以安排的前道工序同时生产一次产能总和来排产，等待中的产能损耗额外安排
                        ArrangeDeviceOrOperator(task, t, need.Have, out var capacity, ref waitIndex, ref arrangeList, ref newSchedules,
                            cList, pCapacity, deviceList, modelCounts, operatorList, operatorCounts, productType);
                        //本次剩余产能
                        if (capacity == 0)
                        {
                            //没产能
                            sc = new SmartTaskOrderScheduleDetail(t, task, cList, pCapacity)
                            {
                                MarkedDateTime = time,
                                Process = need.Process,
                                Stock = 0,
                                Target = 0,
                                Put = 0,
                                //Target = (int)(put * pCapacity.Rate / 100).ToRound(0),
                                CapacityIndex = 0
                            };
                            newSchedules[t].AddTaskOrderSchedule(sc, arrangeList);
                            continue;
                        }
                        if (need.FirstArrangedTime == default(DateTime))
                        {
                            need.FirstArrangedTime = t;
                        }
                        if (need.EstimatedStartTime == default(DateTime))
                        {
                            need.EstimatedStartTime = t;
                        }

                        //已有可加工数量 与 产能
                        var put = need.Have < capacity ? need.Have : capacity;
                        need.HavePut += put;
                        need.Have -= put;
                        sc = new SmartTaskOrderScheduleDetail(t, task, cList, pCapacity)
                        {
                            MarkedDateTime = time,
                            ProductType = productType,
                            Process = need.Process,
                            Stock = need.Stock,
                            Target = (int)Math.Floor(put * pCapacity.Rate / 100),
                            Put = put,
                            //Target = (int)(put * pCapacity.Rate / 100).ToRound(0),
                            CapacityIndex = ((decimal)put / capacity).ToRound(4)
                        };
                        var old = newSchedules[t].AddTaskOrderSchedule(sc, arrangeList);
                        if (old != null)
                        {
                            sc = old;
                        }

                        need.ProductType = productType;
                        need.DoneTarget += sc.Target;
                        var next = task.Needs.ElementAtOrDefault(i + 1);
                        if (next != null)
                        {
                            next.Have += sc.Target;
                        }
                        else
                        {
                            task.DoneTarget += sc.Target;
                        }

                        if (task.AllDone(need))
                        {
                            need.EstimatedEndTime = t;
                        }
                    }
                }

                doneCount = tasks.Count(x => x.AllDone());
                if (!newSchedules[t].HaveArranged())
                {
                    break;
                }

                t = t.AddDays(1);
            }
        }

        /// <summary>
        /// 数据初始化
        /// </summary>
        /// <param name="schedules"></param>
        /// <param name="minTime">加工时间</param>
        /// <param name="maxTime">加工时间</param>
        /// <param name="deviceList"></param>
        /// <param name="operatorList"></param>
        /// <returns></returns>
        private static void AddDay(
            ref Dictionary<DateTime, SmartTaskOrderScheduleDay> schedules,
            DateTime minTime,
            DateTime maxTime,
            IEnumerable<SmartDevice> deviceList,
            IEnumerable<SmartOperatorDetail> operatorList)
        {
            if (maxTime >= minTime)
            {
                var totalDays = (maxTime - minTime).TotalDays + 1;
                for (var i = 0; i < totalDays; i++)
                {
                    var tt = minTime.AddDays(i);
                    if (!schedules.ContainsKey(tt))
                    {
                        var sc = new SmartTaskOrderScheduleDay(tt);
                        sc.Init(deviceList, operatorList);
                        schedules.Add(tt, sc);
                    }
                }
            }

            schedules = schedules.OrderBy(x => x.Key).ToDictionary(y => y.Key, y => y.Value);
        }

        /// <summary>
        /// 数据初始化
        /// </summary>
        /// <param name="newSchedules"></param>
        /// <param name="tasks"></param>
        /// <param name="schedules"></param>
        /// <param name="productCapacities"></param>
        /// <param name="smartCapacityLists"></param>
        /// <param name="deviceList"></param>
        /// <param name="operatorList"></param>
        private static void InitData(
            ref Dictionary<DateTime, SmartTaskOrderScheduleDay> newSchedules,
            IEnumerable<SmartTaskOrderConfirm> tasks,
            IEnumerable<SmartTaskOrderScheduleDetail> schedules,
            IEnumerable<SmartProductCapacityDetail> productCapacities,
            IEnumerable<SmartCapacityListDetail> smartCapacityLists,
            IEnumerable<SmartDevice> deviceList,
            IEnumerable<SmartOperatorDetail> operatorList)
        {
            foreach (var schedule in schedules)
            {
                var processTime = schedule.ProcessTime;
                var taskOrderId = schedule.TaskOrderId;
                var processId = schedule.ProcessId;
                var productId = schedule.ProductId;
                //计划号工序单日产能
                var pCapacity = productCapacities.FirstOrDefault(x => x.ProductId == productId && x.ProcessId == processId);
                var capacityId = pCapacity?.CapacityId ?? 0;
                var capacityList = smartCapacityLists.FirstOrDefault(x => x.CapacityId == capacityId && x.ProcessId == processId);
                var pId = schedule.PId;
                schedule.CategoryId = capacityList.CategoryId;
                schedule.Order = capacityList.Order;
                //任务已排产
                var task = tasks.FirstOrDefault(x => x.Id == schedule.TaskOrderId);
                if (task != null && task.Arranged)
                {
                    if (!newSchedules.ContainsKey(processTime))
                    {
                        var sc = new SmartTaskOrderScheduleDay(processTime);
                        sc.Init(deviceList, operatorList);
                        newSchedules.Add(processTime, sc);
                    }

                    var arrangeInfos = new List<ArrangeInfo>();
                    if (schedule.ProductType == 0)
                    {
                        foreach (var (id, count) in schedule.ActualDeviceList)
                        {
                            var device = deviceList.FirstOrDefault(x => x.Id == id);
                            if (device != null)
                            {
                                var cList = capacityList.DeviceList.FirstOrDefault(x => x.ModelId == device.ModelId);
                                if (cList == null)
                                {
                                    continue;
                                }

                                var single = cList.Single;
                                var singleCount = cList.SingleCount;

                                if (single == 0 || singleCount == 0)
                                {
                                    continue;
                                }

                                var first = arrangeInfos.FirstOrDefault(x => x.Id == device.Id);
                                if (first == null)
                                {
                                    arrangeInfos.Add(new ArrangeInfo(device.ModelId, device.Id));
                                    first = arrangeInfos.FirstOrDefault(x => x.Id == device.Id);
                                }
                                var productArrange = first.Arranges.FirstOrDefault(x => x.ProductId == productId && x.PId == pId);
                                if (productArrange != null)
                                {
                                    productArrange.Count += count;
                                }
                                else
                                {
                                    first.Arranges.Add(new ArrangeDetail(taskOrderId, productId, pId, capacityList.Order, single, count, singleCount));
                                }
                            }
                        }
                    }
                    else if (schedule.ProductType == 1)
                    {
                        foreach (var (id, count) in schedule.ActualOperatorsList)
                        {
                            var op = operatorList.FirstOrDefault(x => x.Id == id);
                            if (op != null)
                            {
                                var cList = capacityList.OperatorList.FirstOrDefault(x => x.LevelId == op.LevelId);
                                if (cList == null)
                                {
                                    continue;
                                }

                                var single = cList.Single;
                                var singleCount = cList.SingleCount;

                                if (single == 0 || singleCount == 0)
                                {
                                    continue;
                                }

                                var first = arrangeInfos.FirstOrDefault(x => x.Id == op.Id);
                                if (first == null)
                                {
                                    arrangeInfos.Add(new ArrangeInfo(op.LevelId, op.Id));
                                    first = arrangeInfos.FirstOrDefault(x => x.Id == op.Id);
                                }
                                var productArrange = first.Arranges.FirstOrDefault(x => x.ProductId == productId && x.PId == pId);
                                if (productArrange != null)
                                {
                                    productArrange.Count += count;
                                }
                                else
                                {
                                    first.Arranges.Add(new ArrangeDetail(taskOrderId, productId, pId, capacityList.Order, single, count, singleCount));
                                }
                            }
                        }
                    }
                    if (arrangeInfos.Any())
                    {
                        //var capacity = GetDeviceOrOperatorCapacity(arrangeInfos, capacityList, pCapacity, schedule.ProductType);
                        schedule.Target = schedule.DoneTarget;
                        schedule.Put = schedule.HavePut;
                        schedule.Devices = schedule.ActualDevices;
                        newSchedules[processTime].AddTaskOrderSchedule(schedule, arrangeInfos);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="task">加工任务单</param>
        /// <param name="processTime">加工日期</param>
        /// <param name="number"></param>
        /// <param name="capacity"></param>
        /// <param name="arrangeList">设备类型  设备型号 设备id  设备次数 /流程工序  人员等级 人员id  人员次数</param>
        /// <param name="newSchedules"></param>
        /// <param name="capacityList"></param>
        /// <param name="productCapacity"></param>
        /// <param name="deviceList"></param>
        /// <param name="modelCounts"></param>
        /// <param name="operatorList"></param>
        /// <param name="operatorCounts"></param>
        /// <param name="productType"></param>
        /// <param name="waitIndex">等待消耗的产能</param>
        private static void ArrangeDeviceOrOperator(
            SmartTaskOrderConfirm task,
            DateTime processTime,
            int number,
            out int capacity,
            ref decimal waitIndex,
            ref List<ArrangeInfo> arrangeList,
            ref Dictionary<DateTime, SmartTaskOrderScheduleDay> newSchedules,
            SmartCapacityListDetail capacityList,
            SmartProductCapacityDetail productCapacity,
            IEnumerable<SmartDevice> deviceList,
            IEnumerable<SmartDeviceModelCount> modelCounts,
            IEnumerable<SmartOperatorDetail> operatorList,
            IEnumerable<SmartOperatorCount> operatorCounts,
            int productType)
        {
            capacity = 0;
            if (number <= 0)
            {
                return;
            }
            var schedule = newSchedules[processTime];
            var taskOrderId = task.Id;
            var capacityId = capacityList.CapacityId;
            var categoryId = capacityList.CategoryId;
            var pId = capacityList.PId;
            var productId = productCapacity.ProductId;
            var processId = capacityList.ProcessId;
            var indexes = schedule.ProcessLeftCapacityIndex(categoryId, pId, capacityList, deviceList, operatorList, productType, waitIndex);

            if (!indexes.Any())
            {
                return;
            }
            var dy = false;
            if (dy)
            {
#region 动态规划最优安排
                //安排设备
                if (productType == 0)
                {
                    var devices = capacityList.DeviceList;
                    var maxSingle = devices.Max(x => x.Single);
                    var max = devices.Sum(device => (modelCounts.FirstOrDefault(x => x.ModelId == device.ModelId)?.Count ?? 0) * device.SingleCount) + 1;
                    var C = number + maxSingle + 1;
                    var f = new int[max][];
                    for (var i = 0; i < max; i++)
                    {
                        f[i] = new int[C];
                    }
                    //占用次数
                    var w = new int[max];
                    //加工数量
                    var v = new int[max];
                    var zt = new int[max];
                    var m = 0;
                    var k = 1;
                    var lefts = new List<int>();
                    //可生产的设备型号产量及单日次数
                    foreach (var device in devices)
                    {
                        //设备单次生产数量
                        var single = device.Single;
                        //设备单日生产次数
                        var singleCount = device.SingleCount;
                        if (singleCount > 0)
                        {
                            // 设备类型  设备型号 设备id  设备次数 /流程工序  人员等级 人员id  人员次数
                            var sameMode = indexes.Values.Where(x => x.Item1 == device.ModelId);
                            for (var i = 0; i < sameMode.Count(); i++)
                            {
                                var left = (int)(singleCount * sameMode.ElementAt(i).Item2).ToRound();
                                for (var j = 0; j < left; j++)
                                {
                                    w[k] = 1;
                                    v[k] = single;
                                    k++;
                                }
                                lefts.Add(left);
                                m += left;
                            }
                        }
                    }

                    var i1 = 0;
                    var j1 = 0;
                    var ca = -1;
                    for (var i = 1; i <= m; i++)
                    {
                        //尝试放置每次安排
                        for (var j = 1; j < C; j++)
                        {
                            if (j >= w[i])
                            {
                                f[i][j] = Math.Max(f[i - 1][j - w[i]] + v[i], f[i - 1][j]);
                            }
                            else
                            {
                                f[i][j] = f[i - 1][j];
                            }

                            var cur = f[i][j];
                            if (cur >= number)
                            {
                                if (ca == -1)
                                {
                                    i1 = i;
                                    j1 = j;
                                    ca = cur - number;
                                }
                                if (ca > cur - number)
                                {
                                    i1 = i;
                                    j1 = j;
                                    ca = cur - number;
                                }
                            }

                            //倒叙是为了保证每次安排都使用一次
                            //var cur = f[j - w[i]] + v[i];
                            //var old = f[j];
                            //var cur = f[j][];
                            //if (cur >= number)
                            //{
                            //    if (ca == -1)
                            //    {
                            //        i1 = i;
                            //        j1 = j;
                            //        ca = cur - number;
                            //    }
                            //    if (ca > cur - number)
                            //    {
                            //        i1 = i;
                            //        j1 = j;
                            //        ca = cur - number;
                            //    }
                            //}
                            //if (j >= w[i] && f[j] < number)
                            //{
                            //    zt[i] = 1;
                            //    f[j] = Math.Max(f[j - w[i]] + v[i], f[j]);
                            //}
                        }
                    }
                    var s = f[i1][j1];
                    for (var i = i1; i >= 1; i--)
                    {
                        if (f[i][s] > f[i - 1][s])
                        {
                            zt[i] = 1; //装入背包
                            s -= v[i]; //物品i装入背包之前背包的容量
                        }
                        else
                        {
                            zt[i] = 0; //没有装入背包
                        }
                    }

                    var ss = 1;
                    var d = new List<int>();
                    foreach (var left in lefts)
                    {
                        d.Add(zt.Skip(ss).Take(left).Sum());
                        ss += left;
                    }
                }
                else if (productType == 1)
                {

                }


#endregion
            }
            else
            {
                //安排设备
                if (productType == 0)
                {
                    var devices = capacityList.DeviceList;
                    //可生产的设备型号产量及单日次数
                    foreach (var deviceCapacity in devices)
                    {
                        //设备单次生产数量
                        var single = deviceCapacity.Single;
                        //设备单日生产次数
                        var singleCount = deviceCapacity.SingleCount;
                        if (single > 0 && singleCount > 0)
                        {
                            //设备型号 设备id  设备次数
                            var sames = indexes.Where(x => x.Value.Item1 == deviceCapacity.ModelId);
                            foreach (var (id, info) in sames)
                            {
                                var count = (int)(singleCount * info.Item2).ToRound();
                                var maxCount = (int)Math.Ceiling(((decimal)number - capacity) / single);
                                var actCount = count > maxCount ? maxCount : count;
                                capacity += actCount * single;
                                var device = deviceList.FirstOrDefault(x => x.Id == id);
                                if (device != null)
                                {
                                    var first = arrangeList.FirstOrDefault(x => x.Id == device.Id);
                                    if (first == null)
                                    {
                                        arrangeList.Add(new ArrangeInfo(device.ModelId, device.Id));
                                        first = arrangeList.FirstOrDefault(x => x.Id == device.Id);
                                    }
                                    var productArrange = first.Arranges.FirstOrDefault(x => x.ProductId == productId && x.PId == pId);
                                    if (productArrange != null)
                                    {
                                        productArrange.Count += actCount;
                                    }
                                    else
                                    {
                                        first.Arranges.Add(new ArrangeDetail(taskOrderId, productId, pId, capacityList.Order, single, actCount, singleCount));
                                    }
                                }
                                if (capacity >= number)
                                {
                                    break;
                                }
                            }
                            if (capacity >= number)
                            {
                                break;
                            }
                        }
                    }
                }
                else if (productType == 1)
                {
                    var operators = capacityList.OperatorList;
                    //可生产的人员等级产量及单日次数
                    foreach (var operatorCapacity in operators)
                    {
                        //设备单次生产数量
                        var single = operatorCapacity.Single;
                        //设备单日生产次数
                        var singleCount = operatorCapacity.SingleCount;
                        if (single > 0 && singleCount > 0)
                        {
                            // 人员等级 人员id  人员次数
                            var sames = indexes.Where(x => x.Value.Item1 == operatorCapacity.LevelId);
                            foreach (var (id, info) in sames)
                            {
                                var count = (int)(singleCount * info.Item2).ToRound();
                                var maxCount = (int)Math.Ceiling(((decimal)number - capacity) / single);
                                var actCount = count > maxCount ? maxCount : count;
                                capacity += actCount * single;
                                var op = operatorList.FirstOrDefault(x => x.Id == id);
                                if (op != null)
                                {
                                    var first = arrangeList.FirstOrDefault(x => x.Id == op.Id);
                                    if (first == null)
                                    {
                                        arrangeList.Add(new ArrangeInfo(op.LevelId, op.Id));
                                        first = arrangeList.FirstOrDefault(x => x.Id == op.Id);
                                    }
                                    var productArrange = first.Arranges.FirstOrDefault(x => x.ProductId == productId && x.PId == pId);
                                    if (productArrange != null)
                                    {
                                        productArrange.Count += actCount;
                                    }
                                    else
                                    {
                                        first.Arranges.Add(new ArrangeDetail(taskOrderId, productId, pId, capacityList.Order, single, actCount, singleCount));
                                    }
                                }
                                if (capacity >= number)
                                {
                                    break;
                                }
                            }
                            if (capacity >= number)
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="arrangeList">设备类型  设备型号 设备id  设备次数 /流程工序  人员等级 人员id  人员次数</param>
        /// <param name="capacityList"></param>
        /// <param name="productCapacity"></param>
        /// <param name="productType"></param>
        /// <param name="all">是否全部生产</param>
        private static int GetDeviceOrOperatorCapacity(
            List<ArrangeInfo> arrangeList,
            SmartCapacityListDetail capacityList,
            SmartProductCapacityDetail productCapacity,
            int productType,
            bool all = false)
        {
            var capacity = 0;
            //安排设备
            if (productType == 0)
            {
                var devices = capacityList.DeviceList;
                //设备型号 设备id  设备次数
                foreach (var info in arrangeList)
                {
                    var deviceCapacity = devices.FirstOrDefault(x => x.ModelId == info.Item);
                    if (deviceCapacity != null)
                    {
                        //设备单次生产数量
                        var single = deviceCapacity.Single;
                        //设备单日生产次数
                        var singleCount = deviceCapacity.SingleCount;
                        foreach (var arrange in info.Arranges)
                        {
                            var count = !all ? arrange.Count > singleCount ? singleCount : arrange.Count : singleCount;
                            capacity += count * single;
                        }
                    }
                }
            }
            else if (productType == 1)
            {
                var operators = capacityList.OperatorList;
                //人员等级 人员id  人员次数
                foreach (var info in arrangeList)
                {
                    var operatorCapacity = operators.FirstOrDefault(x => x.LevelId == info.Item);
                    if (operatorCapacity != null)
                    {
                        //人员单次生产数量
                        var single = operatorCapacity.Single;
                        //人员单日生产次数
                        var singleCount = operatorCapacity.SingleCount;
                        foreach (var arrange in info.Arranges)
                        {
                            var count = !all ? arrange.Count > singleCount ? singleCount : arrange.Count : singleCount;
                            capacity += count * single;
                        }
                    }
                }
            }
            return capacity;
        }
    }

}
