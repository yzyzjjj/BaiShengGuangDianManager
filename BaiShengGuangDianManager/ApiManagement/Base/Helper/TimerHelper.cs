using ApiManagement.Base.Server;
using ApiManagement.Models._6sModel;
using ApiManagement.Models.AccountManagementModel;
using ApiManagement.Models.DeviceSpotCheckModel;
using ApiManagement.Models.ManufactureModel;
using ApiManagement.Models.MaterialManagementModel;
using ApiManagement.Models.OtherModel;
using ApiManagement.Models.RepairManagementModel;
using ApiManagement.Models.StatisticManagementModel;
using Microsoft.EntityFrameworkCore.Internal;
using ModelBase.Base.HttpServer;
using ModelBase.Base.Logger;
using ModelBase.Base.Utils;
using Newtonsoft.Json;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace ApiManagement.Base.Helper
{
    public class TimerHelper
    {
        private static readonly string Debug = "Debug";
        private static string _url = ServerConfig.ErpUrl;
        private static string _urlFile = "http://192.168.1.100/lc/uploads/";
        private static string _createUserId = "ErpSystem";

#if DEBUG
#else
        //private static Timer _totalTimer;
#endif
        private static CancellationTokenSource cts = new CancellationTokenSource();
        public static void Init()
        {
            if (!RedisHelper.Exists(Debug))
            {
                RedisHelper.SetForever(Debug, 0);
            }

            Task.Run(async () =>
            {
                while (true)
                {
                    if (cts.IsCancellationRequested)
                    {
                        break;
                    }
                    try
                    {
#if DEBUG
                        Console.WriteLine($"{DateTime.Now.ToStr()}:TimerHelper 调试模式已开启");
                        await Task.Delay(1000 * 5, cts.Token);
                        DoSthTest(cts);
                        //需要.net4.5的支持
                        await Task.Delay(1000 * 10, cts.Token);
#else
                        Console.WriteLine($"{DateTime.Now.ToStr()}:TimerHelper 发布模式已开启");
                        await Task.Delay(1000 * 5, cts.Token);
                        DoSth(cts);
                        await Task.Delay(1000 * 60, cts.Token);
#endif
                    }
                    catch (TaskCanceledException ex)
                    {
                        Log.Error(ex);
                    }
                }
            });
            //#if DEBUG
            //            _totalTimer = new Timer(DoSthTest, null, 5000, 1000 * 10 * 1);
            //            Console.WriteLine("TimerHelper 调试模式已开启");
            //#else
            //            _totalTimer = new Timer(DoSth, null, 5000, 1000 * 60 * 1);
            //            Console.WriteLine("TimerHelper 发布模式已开启");
            //#endif
        }

        private static void DoSth(object state)
        {

#if !DEBUG
            if (RedisHelper.Get<int>("Debug") != 0)
            {
                return;
            }
#endif
            Console.WriteLine("StatisticProcess 发布模式已开启");
            StatisticProcess();
            Console.WriteLine("StatisticProcessAfterUpdate 发布模式已开启");
            StatisticProcessAfterUpdate();
            WorkFlowHelper.Instance.OnBillNeedUpdate();
            Console.WriteLine("GetErpDepartment 发布模式已开启");
            GetErpDepartment();
            Console.WriteLine("GetErpPurchase 发布模式已开启");
            GetErpPurchase();
            Console.WriteLine("GetErpValuer 发布模式已开启");
            GetErpValuer();
            Console.WriteLine("CheckSpotCheckDevice 发布模式已开启");
            CheckSpotCheckDevice();
            Console.WriteLine("CheckManufacturePlan 发布模式已开启");
            CheckManufacturePlan();
            Console.WriteLine("Check_6sItem 发布模式已开启");
            Check_6sItem();
            Console.WriteLine("MaterialRecovery 发布模式已开启");
            MaterialRecovery();
            Console.WriteLine("DayBalanceRecovery 发布模式已开启");
            DayBalanceRecovery();
            Console.WriteLine("GetDayBalance 发布模式已开启");
            GetDayBalance();
            CheckAccount();
            MaintainerSchedule();
            _first = false;
        }

        private static bool _first = true;

        private static void DoSthTest(object state)
        {

#if !DEBUG
            if (RedisHelper.Get<int>("Debug") != 0)
            {
                return;
            }
#endif
            Console.WriteLine("StatisticProcess 调试模式已开启");
            StatisticProcess();
            Console.WriteLine("StatisticProcessAfterUpdate 发布模式已开启");
            StatisticProcessAfterUpdate();
            WorkFlowHelper.Instance.OnBillNeedUpdate();
            Console.WriteLine("GetErpDepartment 调试模式已开启");
            GetErpDepartment();
            Console.WriteLine("GetErpPurchase 调试模式已开启");
            GetErpPurchase();
            Console.WriteLine("GetErpValuer 调试模式已开启");
            GetErpValuer();
            Console.WriteLine("CheckSpotCheckDevice 调试模式已开启");
            CheckSpotCheckDevice();
            Console.WriteLine("CheckManufacturePlan 调试模式已开启");
            CheckManufacturePlan();
            Console.WriteLine("Check_6sItem 调试模式已开启");
            Check_6sItem();
            Console.WriteLine("MaterialRecovery 调试模式已开启");
            MaterialRecovery();
            Console.WriteLine("DayBalanceRecovery 调试模式已开启");
            DayBalanceRecovery();
            Console.WriteLine("GetDayBalance 调试模式已开启");
            GetDayBalance();
            CheckAccount();
            MaintainerSchedule();
            _first = false;
        }

        /// <summary>
        /// 检查点检设备的点检安排
        /// </summary>
        private static void CheckSpotCheckDevice()
        {
            var checkPlanPre = "CheckPlan";
            var checkPlanLock = $"{checkPlanPre}:Lock";
            if (RedisHelper.SetIfNotExist(checkPlanLock, "lock"))
            {
                try
                {
                    RedisHelper.SetExpireAt(checkPlanLock, DateTime.Now.AddMinutes(5));
                    var sql = "SELECT c.*, a.*, b.Plan FROM `spot_check_device` a " +
                              "JOIN `spot_check_plan` b ON a.PlanId = b.Id " +
                              "JOIN `spot_check_item` c ON a.ItemId = c.Id " +
                              "WHERE a.MarkedDelete = 0 " +
                              "AND b.MarkedDelete = 0 " +
                              "AND c.MarkedDelete = 0 " +
                              "AND c.`Interval` != 0 " +
                              //"AND a.LogId = 0 " +
                              "AND ADDDATE(DATE(a.PlannedTime), 1) <= NOW();";
                    var details = ServerConfig.ApiDb.Query<SpotCheckLog>(sql);

                    if (details.Any())
                    {
                        var now = DateTime.Now;
                        var logs = new List<SpotCheckLog>();
                        foreach (var detail in details)
                        {
                            if (detail.LogId == 0)
                            {
                                detail.CreateUserId = "System";
                                detail.MarkedDateTime = now;
                                detail.Check = false;
                                logs.Add(detail);
                            }
                        }

                        if (logs.Any())
                        {
                            ServerConfig.ApiDb.Execute(
                                "INSERT INTO spot_check_log (`CreateUserId`, `MarkedDateTime`, `PlannedTime`, `DeviceId`, `ItemId`, `Item`, `PlanId`, `Plan`, `Enable`, `Remind`, `Min`, `Max`, `Unit`, `Reference`, `Remarks`, `Interval`, `Day`, `Month`, `NormalHour`, `Week`, `WeekHour`, `SurveyorId`, `Check`) " +
                                "VALUES (@CreateUserId, @MarkedDateTime, @PlannedTime, @DeviceId, @ItemId, @Item, @PlanId, @Plan, @Enable, @Remind, @Min, @Max, @Unit, @Reference, @Remarks, @Interval, @Day, @Month, @NormalHour, @Week, @WeekHour, @SurveyorId, @Check);",
                                logs);
                        }

                        //next
                        foreach (var detail in details)
                        {
                            var time = default(DateTime);
                            switch (detail.Interval)
                            {
                                case IntervalEnum.Day:
                                    time = detail.PlannedTime.Date.AddDays(detail.Day).AddMonths(detail.Month)
                                        .AddHours(detail.NormalHour);
                                    break;
                                case IntervalEnum.Week:
                                    time = detail.PlannedTime.Date.AddDays(detail.Week * 7).AddHours(detail.WeekHour);
                                    break;
                            }

                            detail.PlannedTime = time;
                            detail.LogId = 0;
                        }

                        ServerConfig.ApiDb.Execute(
                            "UPDATE `spot_check_device` SET `MarkedDateTime`= @MarkedDateTime, `PlannedTime` = @PlannedTime, `LogId` = @LogId WHERE `Id` = @Id;",
                            details);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }

                RedisHelper.Remove(checkPlanLock);
            }
        }

        /// <summary>
        /// 检查计划管理的计划状态
        /// </summary>
        private static void CheckManufacturePlan()
        {
            var manufacturePlanPre = "CheckManufacturePlan";
            var manufacturePlanLock = $"{manufacturePlanPre}:Lock";
            if (RedisHelper.SetIfNotExist(manufacturePlanLock, "lock"))
            {
                RedisHelper.SetExpireAt(manufacturePlanLock, DateTime.Now.AddMinutes(5));
                try
                {
                    var sql = "SELECT a.*, IFNULL(b.Sum, 0) Sum FROM `manufacture_plan` a " +
                              "LEFT JOIN (SELECT PlanId, SUM(1) Sum FROM `manufacture_plan_task` WHERE MarkedDelete = 0 AND State NOT IN @state GROUP BY PlanId) b ON a.Id = b.PlanId WHERE MarkedDelete = 0;";
                    var plans = ServerConfig.ApiDb.Query<ManufacturePlanCondition>(sql,
                        new { state = new[] { ManufacturePlanTaskState.Done, ManufacturePlanTaskState.Stop } });
                    if (plans.Any())
                    {
                        var change = false;
                        foreach (var plan in plans.Where(x => x.State > ManufacturePlanState.Wait))
                        {
                            var planState = plan.Sum <= 0 ? ManufacturePlanState.Done : ManufacturePlanState.Doing;
                            if (planState != plan.State)
                            {
                                change = true;
                                plan.State = planState;
                            }
                        }

                        if (change)
                        {
                            ServerConfig.ApiDb.Execute(
                                "UPDATE `manufacture_plan` SET `State`= @State WHERE `Id` = @Id;", plans);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }

                RedisHelper.Remove(manufacturePlanLock);
            }
        }

        private static void Check_6sItem()
        {
            var _6sPre = "6s";
            var _6sLock = $"{_6sPre}:Lock";
            if (RedisHelper.SetIfNotExist(_6sLock, "lock"))
            {
                try
                {
                    RedisHelper.SetExpireAt(_6sLock, DateTime.Now.AddMinutes(5));
                    Init_6sItem();
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }

                RedisHelper.Remove(_6sLock);
            }
        }

        private static void Init_6sItem(int groupId = 0)
        {
            var sql =
                "SELECT a.*, b.`Group`, b.SurveyorId SurveyorIdSet FROM `6s_item` a JOIN `6s_group` b ON a.GroupId = b.Id " +
                $"WHERE a.MarkedDelete = 0 AND b.MarkedDelete = 0 AND a.Enable = 1{(groupId == 0 ? "" : " AND a.GroupId= @groupId")}";
            var _6sItems = ServerConfig.ApiDb.Query<_6sItemPeriod>(sql, new { groupId });

            if (_6sItems.Any())
            {
                var today = DateTime.Today;
                var now = DateTime.Now;
                var logs = new List<_6sLog>();
                var old_6sItems =
                    _6sItems.Where(x => x.LastCreateTime != default(DateTime) && x.LastCreateTime < today);
                if (old_6sItems.Any())
                {
                    foreach (var _item in old_6sItems)
                    {
                        var _log = new _6sLog { CreateUserId = "System", MarkedDateTime = now };
                        var time = default(DateTime);
                        switch ((IntervalEnum)_item.Interval)
                        {
                            case IntervalEnum.Day:
                                time = _item.LastCreateTime.Date.AddDays(_item.Day);
                                break;
                            case IntervalEnum.Week:
                                var weekLastDay = _item.LastCreateTime.Date.WeekEndTime();
                                time = weekLastDay.Date.AddDays(_item.Week * 7);
                                break;
                        }

                        _log.PlannedTime = time;
                        _log.Order = _item.Order;
                        _log.Item = _item.Item;
                        _log.GroupId = _item.GroupId;
                        _log.Enable = _item.Enable;
                        _log.Standard = _item.Standard;
                        _log.Reference = _item.Reference;
                        _log.Interval = _item.Interval;
                        _log.Day = _item.Day;
                        _log.Week = _item.Week;
                        _log.Person = _item.Person;
                        _log.SurveyorIdSet = _item.SurveyorIdSet;
                        _log.Check = false;
                        logs.Add(_log);

                        _item.LastCreateTime = time;
                    }
                }

                var new_6sItems = _6sItems.Where(x => x.LastCreateTime == default(DateTime));
                if (new_6sItems.Any())
                {
                    foreach (var _item in new_6sItems)
                    {
                        var _log = new _6sLog { CreateUserId = "System", MarkedDateTime = now };
                        var time = default(DateTime);
                        switch ((IntervalEnum)_item.Interval)
                        {
                            case IntervalEnum.Day:
                                time = today;
                                break;
                            case IntervalEnum.Week:
                                time = today.WeekEndTime().Date;
                                break;
                        }

                        _log.PlannedTime = time;
                        _log.Order = _item.Order;
                        _log.Item = _item.Item;
                        _log.GroupId = _item.GroupId;
                        _log.Enable = _item.Enable;
                        _log.Standard = _item.Standard;
                        _log.Reference = _item.Reference;
                        _log.Interval = _item.Interval;
                        _log.Day = _item.Day;
                        _log.Week = _item.Week;
                        _log.Person = _item.Person;
                        _log.SurveyorIdSet = _item.SurveyorIdSet;
                        _log.Check = false;
                        logs.Add(_log);

                        _item.LastCreateTime = time;
                    }
                }

                if (logs.Any())
                {
                    ServerConfig.ApiDb.Execute(
                        "INSERT INTO 6s_log (`CreateUserId`, `MarkedDateTime`, `PlannedTime`, `Order`, `Item`, `GroupId`, `Standard`, `Reference`, `Interval`, `Day`, `Week`, `Person`, `SurveyorIdSet`, `Check`) " +
                        "VALUES (@CreateUserId, @MarkedDateTime, @PlannedTime, @Order, @Item, @GroupId, @Standard, @Reference, @Interval, @Day, @Week, @Person, @SurveyorIdSet, @Check);",
                        logs);
                }

                ServerConfig.ApiDb.Execute("UPDATE 6s_item SET `LastCreateTime` = @LastCreateTime WHERE `Id` = @Id;;",
                    _6sItems);
            }
        }

        /// <summary>
        /// 获取请购单和请购列表
        /// </summary>
        private static void GetErpDepartment()
        {
            var _pre = "GetErpDepartment";
            var redisLock = $"{_pre}:Lock";
            if (RedisHelper.SetIfNotExist(redisLock, DateTime.Now.ToStr()))
            {
                try
                {
                    RedisHelper.SetExpireAt(redisLock, DateTime.Now.AddMinutes(5));
                    var f = HttpServer.Get(_url, new Dictionary<string, string>
                    {
                        {"type", "getDepartment"},
                    });
                    if (f == "fail")
                    {
                        Log.ErrorFormat("GetErpDepartment 请求erp部门数据失败,url:{0}", _url);
                    }
                    else
                    {
                        var now = DateTime.Now;
                        var rr = HttpUtility.UrlDecode(f);
                        var res = JsonConvert.DeserializeObject<IEnumerable<ErpDepartment>>(rr);
                        //if (res.result != "ok")
                        //{
                        //    Log.ErrorFormat("InsertData 请求erp部门数据返回错误,原因:{0}", res.result);
                        //    return;
                        //}

                        var haveDepartments =
                            ServerConfig.ApiDb.Query<MaterialDepartment>(
                                "SELECT * FROM `material_department` WHERE MarkedDelete = 0;");

                        var notExistDepartments = res.Where(x => haveDepartments.All(y => y.Department != x.name));

                        ServerConfig.ApiDb.Execute(
                            "INSERT INTO  `material_department` (`CreateUserId`, `MarkedDateTime`, `Department`, `IsErp`) " +
                            "VALUES (@CreateUserId, @MarkedDateTime, @Department, @IsErp);", notExistDepartments.Select(
                                x => new MaterialDepartment
                                {
                                    CreateUserId = _createUserId,
                                    MarkedDateTime = now,
                                    Department = x.name,
                                    IsErp = true
                                }));

                        haveDepartments =
                            ServerConfig.ApiDb.Query<MaterialDepartment>(
                                "SELECT * FROM `material_department` WHERE MarkedDelete = 0;");

                        var haveDepartmentMembers =
                            ServerConfig.ApiDb.Query<MaterialDepartmentMember>(
                                "SELECT * FROM `material_department_member` WHERE MarkedDelete = 0;");

                        var r = new List<MaterialDepartmentMember>();
                        foreach (var erpDepartment in res)
                        {
                            var dep = haveDepartments.FirstOrDefault(y => y.Department == erpDepartment.name);
                            if (dep == null)
                            {
                                continue;
                            }

                            var mem = haveDepartmentMembers.Where(x => x.DepartmentId == dep.Id);
                            var newMem = erpDepartment.member.Where(x => mem.All(y => y.Member != x));
                            r.AddRange(newMem.Select(x => new MaterialDepartmentMember
                            {
                                CreateUserId = _createUserId,
                                MarkedDateTime = now,
                                DepartmentId = dep.Id,
                                Member = x,
                                IsErp = true
                            }));
                        }

                        ServerConfig.ApiDb.Execute(
                            "INSERT INTO `material_department_member` (`CreateUserId`, `MarkedDateTime`, `DepartmentId`, `Member`, `IsErp`) " +
                            "VALUES (@CreateUserId, @MarkedDateTime, @DepartmentId, @Member, @IsErp);", r);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }

                RedisHelper.Remove(redisLock);
            }
        }

        private class ErpDepartment
        {
            public string name { get; set; }
            public List<string> member { get; set; }
        }

        /// <summary>
        /// 获取报价人
        /// </summary>
        private static void GetErpValuer()
        {
            var _pre = "GetErpValuer";
            var redisLock = $"{_pre}:Lock";
            if (RedisHelper.SetIfNotExist(redisLock, DateTime.Now.ToStr()))
            {
                try
                {
                    RedisHelper.SetExpireAt(redisLock, DateTime.Now.AddMinutes(5));
                    var f = HttpServer.Get(_url, new Dictionary<string, string>
                    {
                        {"type", "getHjry"},
                    });
                    if (f == "fail")
                    {
                        Log.ErrorFormat("GetErpValuer 请求erp核价人数据失败,url:{0}", _url);
                    }
                    else
                    {
                        var now = DateTime.Now;
                        var rr = HttpUtility.UrlDecode(f);
                        var res = JsonConvert.DeserializeObject<IEnumerable<string>>(rr);
                        var haveValuers =
                            ServerConfig.ApiDb.Query<MaterialValuer>(
                                "SELECT * FROM `material_valuer` WHERE MarkedDelete = 0;");

                        var notExistValuers = res.Where(x => haveValuers.All(y => y.Valuer != x))
                            .Where(z => !z.IsNullOrEmpty());

                        ServerConfig.ApiDb.Execute(
                            "INSERT INTO `material_valuer` (`CreateUserId`, `MarkedDateTime`, `Valuer`, `IsErp`) " +
                            "VALUES (@CreateUserId, @MarkedDateTime, @Valuer, @IsErp);", notExistValuers.Select(
                                x => new MaterialValuer
                                {
                                    CreateUserId = _createUserId,
                                    MarkedDateTime = now,
                                    Valuer = x,
                                    IsErp = true
                                }));
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }

                RedisHelper.Remove(redisLock);
            }
        }

        private static void GetErpPurchase()
        {
            var _pre = "GetErpPurchase";
            var redisLock = $"{_pre}:Lock";
            if (RedisHelper.SetIfNotExist(redisLock, DateTime.Now.ToStr()))
            {
                RedisHelper.SetExpireAt(redisLock, DateTime.Now.AddMinutes(30));
                ErpPurchaseFunc();
                //RedisHelper.Remove(redisLock);
                RedisHelper.SetExpireAt(redisLock, DateTime.Now.AddMinutes(1));
            }
        }

        public static void ErpPurchaseFunc(IEnumerable<int> ids = null)
        {
            try
            {
                var validState = new List<MaterialPurchaseStateEnum>
                {
                    MaterialPurchaseStateEnum.正常,
                    //MaterialPurchaseStateEnum.中止,
                    MaterialPurchaseStateEnum.审核完成,
                    MaterialPurchaseStateEnum.开始采购,
                    MaterialPurchaseStateEnum.仓库到货,
                    //MaterialPurchaseStateEnum.订单完成,
                    MaterialPurchaseStateEnum.已入库,
                };
                var departments = ServerConfig.ApiDb.Query<MaterialDepartment>(
                    "SELECT a.Id, a.Department, IFNULL(MIN(b.ErpId), 0) ErpId FROM `material_department` a " +
                    "LEFT JOIN (SELECT * FROM `material_purchase` WHERE State IN @validState AND MarkedDelete = 0) b ON a.Id = b.DepartmentId WHERE a.Get = 1 AND a.MarkedDelete = 0 GROUP BY a.Id",
                    new { validState });
                var updatePurchaseItems = new List<MaterialPurchaseItem>();
                var addPurchaseItems = new List<MaterialPurchaseItem>();
                var updateMaterialBill = new List<MaterialBill>();
                var onBillNeedUpdate = false;
                if (ids != null)
                {
                    var havePurchases = ServerConfig.ApiDb.Query<MaterialPurchase>(
                        "SELECT a.*, b.Department FROM `material_purchase` a JOIN `material_department` b ON a.DepartmentId = b.Id WHERE a.Id IN @Id AND a.MarkedDelete = 0;", new { Id = ids });
                    foreach (var purchase in havePurchases)
                    {
                        var f = HttpServer.Get(_url, new Dictionary<string, string>
                        {
                            {"type", "getPurchase"},
                            {"department", purchase.Department},
                            {"fs", "1"},
                            {"id", purchase.ErpId.ToString()},
                        });
                        if (f == "fail")
                        {
                            Log.ErrorFormat("GetErpPurchase1 请求erp部门指定请购单数据失败,url:{0}", _url);
                        }
                        else
                        {
                            var dep = departments.FirstOrDefault(x => x.Id == purchase.DepartmentId);
                            ErpPurchaseItemFunc(true, f, dep, havePurchases, ref updatePurchaseItems, ref addPurchaseItems, ref updateMaterialBill, ref onBillNeedUpdate);
                        }
                    }
                }
                else
                {
                    foreach (var department in departments)
                    {
                        var f = HttpServer.Get(_url, new Dictionary<string, string>
                        {
                            {"type", "getPurchase"},
                            {"department", department.Department},
                            //{"id", (926 - 1).ToString()},
                            {"id", (department.ErpId - 1).ToString()},
                        });
                        if (f == "fail")
                        {
                            Log.ErrorFormat("GetErpPurchase2 请求erp部门请购单数据失败,url:{0}", _url);
                        }
                        else
                        {
                            var havePurchases =
                                ServerConfig.ApiDb.Query<MaterialPurchase>(
                                    "SELECT * FROM `material_purchase` WHERE DepartmentId = @Id AND ErpId >= @ErpId AND MarkedDelete = 0;",
                                    new { department.Id, department.ErpId });
                            ErpPurchaseItemFunc(false, f, department, havePurchases, ref updatePurchaseItems, ref addPurchaseItems, ref updateMaterialBill, ref onBillNeedUpdate);
                        }
                    }
                }
                Console.WriteLine($"采购管理: updateBill: {updateMaterialBill.Count()},  updateItem: {updatePurchaseItems.Count()},  add: {addPurchaseItems.Count()}, {onBillNeedUpdate}");

                if (updateMaterialBill.Any())
                {
                    ServerConfig.ApiDb.Execute(
                        "UPDATE `material_bill` SET `MarkedDateTime` = @MarkedDateTime, `Price` = @Price WHERE `Id` = @Id;",
                        updateMaterialBill);
                }

                if (updatePurchaseItems.Any())
                {
                    ServerConfig.ApiDb.Execute(
                        "UPDATE `material_purchase_item` SET  `ModifyId` = @ModifyId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `Class` = @Class, `Category` = @Category, `Name` = @Name, `Supplier` = @Supplier, `SupplierFull` = @SupplierFull, " +
                        "`Specification` = @Specification, `Number` = @Number, `Unit` = @Unit, `Remark` = @Remark, `Purchaser` = @Purchaser, `PurchasingCompany` = @PurchasingCompany, `Order` = @Order, " +
                        "`EstimatedTime` = @EstimatedTime, `ArrivalTime` = @ArrivalTime, `File` = @File, `FileUrl` = @FileUrl, `IsInspection` = @IsInspection, " +
                        "`Currency` = @Currency, `Payment` = @Payment, `Transaction` = @Transaction, `Invoice` = @Invoice, `TaxPrice` = @TaxPrice, `TaxAmount` = @TaxAmount, " +
                        "`Price` = @Price, `Stock` = IF(@BillId > 0, @Stock, `Stock`), `BillId` = IF(@BillId > 0, @BillId, `BillId`) WHERE `Id` = @Id;",
                        updatePurchaseItems);
                }

                if (addPurchaseItems.Any())
                {
                    ServerConfig.ApiDb.Execute(
                        "INSERT INTO `material_purchase_item` (`CreateUserId`, `MarkedDateTime`, `Time`, `IsErp`, `ErpId`, `PurchaseId`, `Code`, `Class`, `Category`, `Name`, `Supplier`, `SupplierFull`, `Specification`, `Number`, `Unit`, `Remark`, `Purchaser`, `PurchasingCompany`, `Order`, `EstimatedTime`, `ArrivalTime`, `File`, `FileUrl`, `IsInspection`, `Currency`, `Payment`, `Transaction`, `Invoice`, `TaxPrice`, `TaxAmount`, `Price`, `Stock`, `BillId`) " +
                        "VALUES (@CreateUserId, @MarkedDateTime, @Time, @IsErp, @ErpId, @PurchaseId, @Code, @Class, @Category, @Name, @Supplier, @SupplierFull, @Specification, @Number, @Unit, @Remark, @Purchaser, @PurchasingCompany, @Order, @EstimatedTime, @ArrivalTime, @File, @FileUrl, @IsInspection, @Currency, @Payment, @Transaction, @Invoice, @TaxPrice, @TaxAmount, @Price, @Stock, @BillId);",
                        addPurchaseItems);
                }
                if (onBillNeedUpdate)
                {
                    WorkFlowHelper.Instance.OnBillNeedUpdate();
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private static void ErpPurchaseItemFunc(bool changeState, string f, MaterialDepartment dep,
            IEnumerable<MaterialPurchase> havePurchases,
            ref List<MaterialPurchaseItem> updatePurchaseItems,
            ref List<MaterialPurchaseItem> addPurchaseItems,
            ref List<MaterialBill> updateMaterialBill,
            ref bool onBillNeedUpdate)
        {
            onBillNeedUpdate = false;
            try
            {
                if (havePurchases == null)
                {
                    havePurchases = new List<MaterialPurchase>();
                }
                if (updatePurchaseItems == null)
                {
                    updatePurchaseItems = new List<MaterialPurchaseItem>();
                }
                if (addPurchaseItems == null)
                {
                    addPurchaseItems = new List<MaterialPurchaseItem>();
                }
                if (updateMaterialBill == null)
                {
                    updateMaterialBill = new List<MaterialBill>();
                }
                var now = DateTime.Now;
                //var rr = HttpUtility.UrlDecode(f);
                var res = JsonConvert.DeserializeObject<IEnumerable<ErpPurchase>>(f).OrderBy(x => x.f_id);
                //var bz = res.GroupBy(x => x.f_bz).Select(y => y.Key).Join();
                //var zt = res.GroupBy(x => x.f_zt).Select(y => y.Key).Join();
                var existPurchases = res.Where(x => havePurchases.Any(y => y.ErpId == x.f_id)).ToList();
                if (existPurchases.Any())
                {
                    var havePurchaseItems = ServerConfig.ApiDb.Query<MaterialPurchaseItem>(
                        "SELECT * FROM `material_purchase_item` WHERE MarkedDelete = 0 AND PurchaseId IN @PurchaseId;",
                        new
                        {
                            PurchaseId = havePurchases
                                .Where(x => existPurchases.Any(y => y.f_id == x.ErpId)).Select(z => z.Id)
                        });
                    var purchases = new List<MaterialPurchase>();
                    foreach (var p in existPurchases)
                    {
                        if (dep == null)
                        {
                            continue;
                        }

                        if (!EnumHelper.TryParseStr(p.f_zt, out MaterialPurchaseStateEnum state, true))
                        {
                            Log.Info($"f_id:{p.f_id}, f_zt:{p.f_zt}");
                            continue;
                        }

                        if (!EnumHelper.TryParseStr(p.f_dj, out MaterialPurchasePriorityEnum priority,
                            true))
                        {
                            continue;
                        }

                        var purchase = new MaterialPurchase(dep.Id, p, _createUserId, now, state, priority);
                        var existPurchase = havePurchases.FirstOrDefault(x => x.ErpId == p.f_id);
                        if (existPurchase != null)
                        {
                            purchase.Id = existPurchase.Id;
                            if (existPurchase.State != MaterialPurchaseStateEnum.订单完成
                                && ClassExtension.HaveChange(purchase, existPurchase))
                            {
                                purchases.Add(purchase);
                            }

                            if (p.goods == null)
                            {
                                continue;
                            }

                            p.goods = p.goods.Where(x => !x.f_wlpc.IsNullOrEmpty()).ToArray();
                            var existPurchaseItems =
                                havePurchaseItems.Where(x => x.PurchaseId == purchase.Id);
                            if (existPurchaseItems.Any(x => x.Name.IsNullOrEmpty()))
                            {
                                //删除
                                updatePurchaseItems.AddRange(existPurchaseItems.Where(x => x.Name.IsNullOrEmpty()).Select(z =>
                                {
                                    z.ModifyId = 611;
                                    z.MarkedDateTime = now;
                                    z.MarkedDelete = true;
                                    return z;
                                }));
                                existPurchaseItems = existPurchaseItems.Where(x => !x.Name.IsNullOrEmpty());
                            }
                            if (existPurchaseItems.GroupBy(x => new { x.Name, x.Specification, x.Supplier, x.TaxPrice, x.Order }).Any(z => z.Count() > 1))
                            {
                                var validItems = existPurchaseItems
                                    .GroupBy(x => new { x.Name, x.Specification, x.Supplier, x.TaxPrice, x.Order })
                                    .Select(y =>
                                        y.Count() > 1
                                            ? (y.All(z => z.Code.IsNullOrEmpty())
                                                ? y.First()
                                                : y.First(x => !x.Code.IsNullOrEmpty()))
                                            : y.First());
                                //删除
                                updatePurchaseItems.AddRange(existPurchaseItems.Where(x => validItems.All(y => y.Id != x.Id)).Select(z =>
                                {
                                    z.ModifyId = 624;
                                    z.MarkedDateTime = now;
                                    z.MarkedDelete = true;
                                    return z;
                                }));
                                existPurchaseItems = validItems;
                            }

                            var existPurchaseItemsStock = existPurchaseItems.Where(y => y.Stock == 0);
                            if (purchase.State == MaterialPurchaseStateEnum.撤销 &&
                                existPurchase.State != MaterialPurchaseStateEnum.撤销)
                            {
                                //删除
                                updatePurchaseItems.AddRange(existPurchaseItemsStock.Select(z =>
                                //updatePurchaseItems.AddRange(existPurchaseItems.Select(z =>
                                {
                                    z.ModifyId = 627;
                                    z.MarkedDateTime = now;
                                    z.MarkedDelete = true;
                                    return z;
                                }));
                            }
                            else
                            {
                                IEnumerable<MaterialPurchaseItem> l;
                                var erpPurchaseItems = p.goods.Select(good =>
                                    new MaterialPurchaseItem(purchase.Id, good, _createUserId, now,
                                        _urlFile));

                                if (p.goods.Any(x => x.f_id == 0))
                                {
                                    if (erpPurchaseItems.Any())
                                    {
                                        var wlCode = erpPurchaseItems.GroupBy(x => x.Code)
                                            .Select(y => y.Key).Where(z => !z.IsNullOrEmpty());

                                        //删除不存在的code
                                        l = existPurchaseItems
                                            .Where(x => wlCode.All(y => y != x.Code) && x.Stock != 0)
                                            .Select(z =>
                                            {
                                                z.ModifyId = 652;
                                                z.MarkedDateTime = now;
                                                z.MarkedDelete = true;
                                                return z;
                                            });
                                        if (l.Any())
                                        {
                                            updatePurchaseItems.AddRange(l.Select(x =>
                                            {
                                                x.ModifyId = 661;
                                                return x;
                                            }));
                                        }
                                        foreach (var code in wlCode)
                                        {
                                            var erpCodes = erpPurchaseItems.Where(x => x.Code == code);
                                            var myCodes = existPurchaseItems.Where(x => x.Code == code);
                                            var erpCodeCnt = erpCodes.Count();
                                            var myCodeCnt = myCodes.Count();
                                            var erpDic = new Dictionary<int, bool>();
                                            for (var i = 0; i < erpCodeCnt; i++)
                                            {
                                                erpDic.Add(i, false);
                                            }

                                            var myDic = new Dictionary<int, bool>();
                                            for (var i = 0; i < myCodeCnt; i++)
                                            {
                                                myDic.Add(i, false);
                                            }

                                            for (var i = 0; i < erpCodeCnt; i++)
                                            {
                                                var erpCode = erpCodes.ElementAt(i);
                                                var myCode = myCodes.FirstOrDefault(x =>
                                                    x.ErpId != 0 && x.ErpId == erpCode.ErpId);
                                                if (myCode == null)
                                                {
                                                    myCode = myCodes.FirstOrDefault(x =>
                                                        x.ErpId == 0 && x.Number == erpCode.Number);
                                                    if (myCode != null)
                                                    {
                                                        erpDic[i] = true;
                                                        var findI = myCodes.IndexOf(myCode);
                                                        myDic[findI] = true;
                                                        erpCode.Id = myCode.Id;
                                                        //try
                                                        //{
                                                        if (ClassExtension.HaveChange(erpCode, myCode))
                                                        {
                                                            erpCode.ModifyId = 702;
                                                            updatePurchaseItems.Add(erpCode);
                                                        }

                                                        //}
                                                        //catch (Exception e)
                                                        //{
                                                        //    Console.WriteLine(e);
                                                        //    //throw;
                                                        //}
                                                    }
                                                    else
                                                    {
                                                        erpCode.ModifyId = 715;
                                                        addPurchaseItems.Add(erpCode);
                                                    }
                                                }
                                                else
                                                {
                                                    erpDic[i] = true;
                                                    var findI = myCodes.IndexOf(myCode);
                                                    myDic[findI] = true;
                                                    erpCode.Id = myCode.Id;
                                                    if (ClassExtension.HaveChange(erpCode, myCode))
                                                    {
                                                        erpCode.ModifyId = 728;
                                                        updatePurchaseItems.Add(erpCode);
                                                    }
                                                }
                                            }

                                            //删除多余的的ErpId
                                            l = myCodes
                                                .Where(x => myDic.Where(md => !md.Value)
                                                    .Any(y => myCodes.IndexOf(x) == y.Key)).Select(z =>
                                                    {
                                                        z.ModifyId = 682;
                                                        z.MarkedDateTime = now;
                                                        z.MarkedDelete = true;
                                                        return z;
                                                    });
                                            if (l.Any())
                                            {
                                                updatePurchaseItems.AddRange(l.Select(x =>
                                                {
                                                    x.ModifyId = 747;
                                                    return x;
                                                }));
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    //删除
                                    l = existPurchaseItemsStock
                                        .Where(x => p.goods.All(y => y.f_id != x.ErpId)).Select(z =>
                                        {
                                            z.ModifyId = 760;
                                            z.MarkedDateTime = now;
                                            z.MarkedDelete = true;
                                            return z;
                                        });
                                    if (l.Any())
                                    {
                                        updatePurchaseItems.AddRange(l.Select(x =>
                                        {
                                            x.ModifyId = 769;
                                            return x;
                                        }));
                                    }

                                    //删除存在的code但是请购id变了
                                    var idChange = existPurchaseItems.Where(x =>
                                    {
                                        var erpPurchaseItem =
                                            erpPurchaseItems.FirstOrDefault(y =>
                                                y.IsSame(x) && y.ErpId == x.ErpId);
                                        if (erpPurchaseItem != null)
                                        {
                                            return false;
                                        }

                                        erpPurchaseItem = erpPurchaseItems.FirstOrDefault(y => y.IsSame(x));
                                        return erpPurchaseItem != null && erpPurchaseItem.ErpId != x.ErpId;
                                    });
                                    if (idChange.Any())
                                    {
                                        var s = idChange.Select(z =>
                                        {
                                            z.ModifyId = 792;
                                            z.MarkedDateTime = now;
                                            z.MarkedDelete = true;
                                            return z;
                                        });
                                        updatePurchaseItems.AddRange(s.Select(x =>
                                        {
                                            x.ModifyId = 799;
                                            return x;
                                        }));
                                    }

                                    //更新
                                    var existGoods = p.goods.Where(x =>
                                        existPurchaseItems.Any(y => y.ErpId == x.f_id));
                                    foreach (var good in existGoods)
                                    {
                                        var g = new MaterialPurchaseItem(purchase.Id, good, _createUserId,
                                            now, _urlFile);
                                        g.ModifyId = 811;
                                        var existGood =
                                            existPurchaseItems.FirstOrDefault(x => x.ErpId == good.f_id);
                                        var change = idChange.FirstOrDefault(x => x.Code == good.f_wlbm);
                                        if (existGood != null)
                                        {
                                            g.Id = existGood.Id;
                                            g.PurchaseId = existGood.PurchaseId;
                                            if (ClassExtension.HaveChange(g, existGood))
                                            {
                                                g.ModifyId = 820;
                                                updatePurchaseItems.Add(g);
                                            }

                                            if (change != null && change.BillId > 0)
                                            {
                                                g.Stock = change.Stock;
                                                g.Batch = change.Batch;
                                                g.IncreaseTime = change.IncreaseTime;
                                                g.BillId = change.BillId;
                                                if (g.Price != change.Price)
                                                {
                                                    updateMaterialBill.Add(new MaterialBill
                                                    {
                                                        Id = g.BillId,
                                                        MarkedDateTime = now,
                                                        Price = g.Price
                                                    });
                                                    onBillNeedUpdate = true;
                                                }

                                                g.ModifyId = 839;
                                                updatePurchaseItems.Add(g);
                                            }
                                        }
                                    }

                                    //新增
                                    l = p.goods.Where(x => existPurchaseItems.All(y => y.ErpId != x.f_id))
                                        .Select(z =>
                                        {
                                            var g = new MaterialPurchaseItem(purchase.Id, z, _createUserId,
                                                now, _urlFile)
                                            { ModifyId = 850 };
                                            return g;
                                        });
                                    if (l.Any())
                                    {
                                        if (idChange.Any())
                                        {
                                            foreach (var ll in l)
                                            {
                                                var change = idChange.FirstOrDefault(x => x.IsSame(ll));
                                                if (change != null && change.BillId > 0)
                                                {
                                                    ll.Stock = change.Stock;
                                                    ll.Batch = change.Batch;
                                                    ll.IncreaseTime = change.IncreaseTime;
                                                    ll.BillId = change.BillId;
                                                    if (ll.Price != change.Price)
                                                    {
                                                        updateMaterialBill.Add(new MaterialBill
                                                        {
                                                            Id = ll.BillId,
                                                            MarkedDateTime = now,
                                                            Price = ll.Price
                                                        });
                                                        onBillNeedUpdate = true;
                                                    }
                                                }

                                                ll.ModifyId = 871;
                                                addPurchaseItems.Add(ll);
                                            }
                                        }
                                        else
                                        {
                                            addPurchaseItems.AddRange(l.Select(x =>
                                            {
                                                x.ModifyId = 879;
                                                return x;
                                            }));
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (purchases.Any())
                    {
                        ServerConfig.ApiDb.Execute(
                            "UPDATE `material_purchase` SET `MarkedDateTime` = @MarkedDateTime, `Purchase` = @Purchase, `Number` = @Number, `Name` = @Name, `Valuer` = @Valuer, `Step` = @Step, " +
                            "`State` = @State, `IsDesign` = @IsDesign, `Priority` = @Priority WHERE `Id` = @Id;",
                            purchases);
                    }
                }

                var notExistPurchases = res.Where(x => havePurchases.All(y => y.ErpId != x.f_id)).ToList();
                if (notExistPurchases.Any())
                {
                    var purchases = new List<MaterialPurchase>();
                    foreach (var p in notExistPurchases)
                    {
                        if (dep == null)
                        {
                            continue;
                        }

                        if (!EnumHelper.TryParseStr(p.f_zt, out MaterialPurchaseStateEnum state, true))
                        {
                            continue;
                        }

                        if (!EnumHelper.TryParseStr(p.f_dj, out MaterialPurchasePriorityEnum priority,
                            true))
                        {
                            continue;
                        }

                        p.DepartmentId = dep.Id;
                        p.valid = true;
                        purchases.Add(new MaterialPurchase(dep.Id, p, _createUserId, now, state, priority));
                    }

                    if (purchases.Any())
                    {
                        ServerConfig.ApiDb.Execute(
                            "INSERT INTO `material_purchase` (`CreateUserId`, `MarkedDateTime`, `Time`, `IsErp`, `ErpId`, `DepartmentId`, `Purchase`, `Number`, `Name`, `Valuer`, `Step`, `State`, `IsDesign`, `Priority`) " +
                            "VALUES (@CreateUserId, @MarkedDateTime, @Time, @IsErp, @ErpId, @DepartmentId, @Purchase, @Number, @Name, @Valuer, @Step, @State, @IsDesign, @Priority);",
                            purchases);

                        havePurchases = ServerConfig.ApiDb.Query<MaterialPurchase>(changeState
                            ? "SELECT * FROM `material_purchase` WHERE DepartmentId = @Id AND ErpId = @ErpId AND MarkedDelete = 0;"
                            : "SELECT * FROM `material_purchase` WHERE DepartmentId = @Id AND ErpId >= @ErpId AND MarkedDelete = 0;", new { dep.Id, dep.ErpId });

                        var validNotExistPurchases = res.Where(x => purchases.Any(y => y.ErpId == x.f_id));
                        foreach (var p in validNotExistPurchases)
                        {
                            var purchase = havePurchases.FirstOrDefault(x => x.ErpId == p.f_id);

                            //新增
                            var l = p.goods.Select(good =>
                            {
                                var x = new MaterialPurchaseItem(purchase.Id, good, _createUserId, now,
                                    _urlFile)
                                { ModifyId = 948 };
                                return x;
                            });
                            if (l.Any())
                            {
                                addPurchaseItems.AddRange(l.Select(x =>
                                {
                                    x.ModifyId = 955;
                                    return x;
                                }));
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }

        }

        public class ErpPurchase
        {
            /// <summary>
            /// 
            /// </summary>
            public bool valid { get; set; }

            /// <summary>
            /// 
            /// </summary>
            public int f_id { get; set; }

            /// <summary>
            /// 
            /// </summary>
            public string f_date { get; set; }

            /// <summary>
            /// 部门
            /// </summary>
            public string f_bm { get; set; }

            public int DepartmentId { get; set; }

            /// <summary>
            /// 发起人  员工编号
            /// </summary>
            public string f_name { get; set; }

            /// <summary>
            /// 发起人  员工姓名
            /// </summary>
            public string f_ygxm { get; set; }

            /// <summary>
            /// 核价人  员工姓名
            /// </summary>
            public string f_hjry { get; set; }

            /// <summary>
            /// 标题
            /// </summary>
            public string f_title { get; set; }

            /// <summary>
            /// 是否设计图纸
            /// </summary>
            public string f_istz { get; set; }

            /// <summary>
            /// 步骤
            /// </summary>
            public string f_bz { get; set; }

            /// <summary>
            /// 状态
            /// </summary>
            public string f_zt { get; set; }

            /// <summary>
            /// 等级
            /// </summary>
            public string f_dj { get; set; }

            /// <summary>
            /// 
            /// </summary>
            public ErpPurchaseItem[] goods { get; set; }
        }

        public class ErpPurchaseItem
        {
            public string spid { get; set; }
            public int f_id => int.TryParse(spid, out var fid) ? fid : 0;

            /// <summary>
            /// 物料编码
            /// </summary>
            public string f_wlbm { get; set; }

            /// <summary>
            /// 时间
            /// </summary>
            public DateTime f_time { get; set; }

            /// <summary>
            /// 品类
            /// </summary>
            public string f_lx { get; set; }

            /// <summary>
            /// 小分类
            /// </summary>
            public string f_xflmc { get; set; }

            /// <summary>
            /// 物料名称
            /// </summary>
            public string f_wlpc { get; set; }

            /// <summary>
            /// 供应商
            /// </summary>
            public string f_gys { get; set; }

            /// <summary>
            /// 供应商全称
            /// </summary>
            public string f_nickname { get; set; }

            /// <summary>
            /// 规格
            /// </summary>
            public string f_gg { get; set; }

            /// <summary>
            /// 数量
            /// </summary>
            public string f_num { get; set; }

            /// <summary>
            /// 单位
            /// </summary>
            public string f_dw { get; set; }

            /// <summary>
            /// 备注
            /// </summary>
            public string f_node { get; set; }

            /// <summary>
            /// 采购人
            /// </summary>
            public string f_cgname { get; set; }

            /// <summary>
            /// 采购数量
            /// </summary>
            public string f_cgddnum { get; set; }

            /// <summary>
            /// 采购公司
            /// </summary>
            public string f_gsmc { get; set; }

            /// <summary>
            /// 采购订单号
            /// </summary>
            public string f_cgddh { get; set; }

            /// <summary>
            /// 到位日期
            /// </summary>
            public string f_dwdate { get; set; }

            /// <summary>
            /// 时间
            /// </summary>
            public string f_dhdate { get; set; }

            /// <summary>
            /// 附件
            /// </summary>
            public string f_file { get; set; }

            /// <summary>
            /// 是否来料检验
            /// </summary>
            public string f_llj { get; set; }

            /// <summary>
            /// 货币币种
            /// </summary>
            public string f_hbbz { get; set; }

            /// <summary>
            /// 付款方式
            /// </summary>
            public string f_fkfs { get; set; }

            /// <summary>
            /// 交易方式
            /// </summary>
            public string f_jyfs { get; set; }

            /// <summary>
            /// 发票
            /// </summary>
            public string f_fpiao { get; set; }

            /// <summary>
            /// 含税单价
            /// </summary>
            public string f_hsdj { get; set; }

            /// <summary>
            /// 含税金额
            /// </summary>
            public string f_hsje { get; set; }

            /// <summary>
            /// 未税单价
            /// </summary>
            public string f_wsdj { get; set; }
        }

        private static bool _isGetDayBalance = false;
        private static bool _getDayBalanceRuning = false;
        /// <summary>
        /// 获取每日库存结存
        /// </summary>
        private static void GetDayBalance()
        {
            var _pre = "GetDayBalance";
            var redisLock = $"{_pre}:Lock";
            var timeKey = $"{_pre}:Time";
            if (_first)
            {
                RedisHelper.Remove(redisLock);
            }

            if (_getDayBalanceRuning)
            {
                //RedisHelper.SetExpireAt(redisLock, DateTime.Now.AddMinutes(10));
                return;
            }

            if (RedisHelper.SetIfNotExist(redisLock, DateTime.Now.ToStr()))
            {
                try
                {
                    _getDayBalanceRuning = true;
                    RedisHelper.SetExpireAt(redisLock, DateTime.Now.AddMinutes(5 * 12));
                    var now = DateTime.Now;
                    if (!RedisHelper.Exists(timeKey))
                    {
                        RedisHelper.SetForever(timeKey, now.ToStr());
                    }

                    var calTime = RedisHelper.Get<DateTime>(timeKey);
                    calTime = calTime == default(DateTime) ? now.Date : calTime.Date;

                    var tomorrow = calTime.AddDays(1);
                    var materialStatistics = ServerConfig.ApiDb.Query<MaterialStatistic>(
                            "SELECT a.`Id` BillId, a.`Code`, b.`CategoryId`, b.`Category`, b.`NameId`, b.`Name`, b.`SupplierId`, b.`Supplier`, a.`SpecificationId`, b.`Specification`, a.`SiteId`, d.Site, " +
                            "a.`Unit`, a.`Stock`, IFNULL(c.Number, 0) TodayNumber, a.`Price` TodayPrice, IFNULL(c.Number, 0) * a.`Price` TodayAmount FROM `material_bill` a JOIN (SELECT a.*, b.CategoryId, " +
                            "b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a JOIN (SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM `material_supplier` a JOIN (SELECT a.*, " +
                            "b.Category FROM `material_name` a JOIN `material_category` b ON a.CategoryId = b.Id) b ON a.NameId = b.Id) b ON a.SupplierId = b.Id) b ON a.SpecificationId = b.Id LEFT JOIN " +
                            "`material_management` c ON a.Id = c.BillId JOIN `material_site` d ON a.SiteId = d.Id WHERE IFNULL(c.Number, 0) > 0 ORDER BY a.Id;")
                        .ToList();
                    var bills = ServerConfig.ApiDb.Query<MaterialBill>("SELECT * FROM material_bill WHERE MarkedDelete = 0;");
                    IEnumerable<int> notExist;

                    //今日入库领用
                    var todayMaterialStatistics = ServerConfig.ApiDb.Query<MaterialStatistic>(
                        "SELECT a.BillId, SUM(IF(a.Type = 1, a.Number, 0)) Increase,  SUM(IF(a.Type = 2, a.Number, 0)) Consume, SUM(IF(a.Type = 3 AND `Mode` = 0, a.Number, 0)) CorrectIn, " +
                        "SUM(IF(a.Type = 3 AND `Mode` = 1, a.Number, 0)) CorrectCon, SUM(IF(a.Type = 3, a.Number, 0)) Correct FROM material_log a JOIN material_bill b ON a.BillId = b.Id " +
                        "WHERE Time >= @Time1 AND Time < @Time2 GROUP BY a.BillId;",
                        new { Time1 = calTime, Time2 = tomorrow });

                    if (todayMaterialStatistics.Any())
                    {
                        notExist = todayMaterialStatistics.Where(x => materialStatistics.All(y => y.BillId != x.BillId))
                            .Select(z => z.BillId);
                        if (notExist.Any())
                        {
                            materialStatistics.AddRange(ServerConfig.ApiDb.Query<MaterialStatistic>(
                                //"SELECT a.`Id` BillId, a.`Code`, b.`CategoryId`, b.`Category`, b.`NameId`, b.`Name`, b.`SupplierId`, b.`Supplier`, a.`SpecificationId`, b.`Specification`, a.`SiteId`, d.Site, a.`Unit`, a.`Stock`, a.`Price` TodayPrice FROM `material_bill` a JOIN (SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a JOIN (SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM `material_supplier` a JOIN (SELECT a.*, b.Category FROM `material_name` a JOIN `material_category` b ON a.CategoryId = b.Id) b ON a.NameId = b.Id) b ON a.SupplierId = b.Id) b ON a.SpecificationId = b.Id JOIN `material_site` d ON a.SiteId = d.Id WHERE a.Id IN @Id ORDER BY a.Id;",
                                "SELECT a.`Id` BillId, a.`Code`, b.`CategoryId`, b.`Category`, b.`NameId`, b.`Name`, b.`SupplierId`, b.`Supplier`, a.`SpecificationId`, b.`Specification`, a.`SiteId`, d.Site, a.`Unit`, a.`Stock`, a.`Price` TodayPrice, IFNULL(c.Number, 0) * a.`Price` TodayAmount FROM `material_bill` a JOIN (SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a JOIN (SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM `material_supplier` a JOIN (SELECT a.*, b.Category FROM `material_name` a JOIN `material_category` b ON a.CategoryId = b.Id) b ON a.NameId = b.Id) b ON a.SupplierId = b.Id) b ON a.SpecificationId = b.Id LEFT JOIN `material_management` c ON a.Id = c.BillId JOIN `material_site` d ON a.SiteId = d.Id WHERE a.Id IN @Id ORDER BY a.Id;",
                                new { Id = notExist }));
                        }

                        foreach (var ne in todayMaterialStatistics)
                        {
                            var bill = materialStatistics.FirstOrDefault(x => x.BillId == ne.BillId);
                            if (bill != null)
                            {
                                bill.Increase = ne.Increase;
                                bill.IncreaseAmount = ne.Increase * bill.TodayPrice;
                                bill.Consume = ne.Consume;
                                bill.ConsumeAmount = ne.Consume * bill.TodayPrice;
                                bill.CorrectIn = ne.CorrectIn;
                                bill.CorrectInAmount = ne.CorrectIn * bill.TodayPrice;
                                bill.CorrectCon = ne.CorrectCon;
                                bill.CorrectConAmount = ne.CorrectCon * bill.TodayPrice;
                                bill.Correct = ne.Correct;
                                bill.CorrectAmount = ne.Correct * bill.TodayPrice;
                            }
                        }
                    }

                    //今日入库领用
                    var todayLogs = ServerConfig.ApiDb.Query<MaterialLog>(
                        "SELECT * FROM `material_log` WHERE Time >= @Time1 AND Time <= @Time2 Order By Time DESC;",
                        new
                        {
                            Time1 = calTime.DayBeginTime(),
                            Time2 = calTime.DayEndTime(),
                            //Time2 = tomorrow.InSameDay(calTime) && tomorrow.DayEndTime() > calTime ? calTime : tomorrow.DayEndTime(),
                        });

                    notExist = todayLogs.Where(x => materialStatistics.All(y => y.BillId != x.BillId))
                        .Select(z => z.BillId);

                    if (notExist.Any())
                    {
                        materialStatistics.AddRange(ServerConfig.ApiDb.Query<MaterialStatistic>(
                            "SELECT a.`Id` BillId, a.`Code`, b.`CategoryId`, b.`Category`, b.`NameId`, b.`Name`, b.`SupplierId`, b.`Supplier`, a.`SpecificationId`, " +
                            "b.`Specification`, a.`SiteId`, d.Site, a.`Unit`, a.`Stock`, IFNULL(c.Number, 0) TodayNumber, a.`Price` TodayPrice, " +
                            "IFNULL(c.Number, 0) * a.`Price` TodayAmount FROM `material_bill` a JOIN (SELECT a.*, b.CategoryId, b.Category, b.NameId, " +
                            "b.`Name`, b.Supplier FROM `material_specification` a JOIN (SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM " +
                            "`material_supplier` a JOIN (SELECT a.*, b.Category FROM `material_name` a JOIN `material_category` b ON a.CategoryId = " +
                            "b.Id) b ON a.NameId = b.Id) b ON a.SupplierId = b.Id) b ON a.SpecificationId = b.Id LEFT JOIN `material_management` c" +
                            " ON a.Id = c.BillId JOIN `material_site` d ON a.SiteId = d.Id WHERE a.Id IN @Id ORDER BY a.Id;",
                            new { Id = notExist }).Select(x =>
                            {
                                x.Time = calTime;
                                return x;
                            }));
                    }

                    var yesterdayMaterialStatistics = ServerConfig.ApiDb.Query<MaterialStatistic>(
                        "SELECT * FROM (SELECT BillId, TodayNumber, TodayPrice, TodayAmount FROM `material_balance` WHERE Time < @Time ORDER BY Time DESC) a GROUP BY a.BillId;",
                        new { Time = calTime }, 60);

                    foreach (var statistic in materialStatistics)
                    {
                        statistic.Time = calTime;
                        statistic.LastNumber = statistic.TodayNumber;
                        statistic.LastPrice =
                            yesterdayMaterialStatistics.FirstOrDefault(x => x.BillId == statistic.BillId)
                                ?.TodayPrice ?? statistic.TodayPrice;
                    }

                    foreach (var log in todayLogs)
                    {
                        if (materialStatistics.All(x => x.BillId != log.BillId))
                        {
                            materialStatistics.Add(new MaterialStatistic
                            {
                                Time = calTime,
                                BillId = log.BillId
                            });
                        }

                        var material = materialStatistics.First(x => x.BillId == log.BillId);
                        // 1 入库; 2 出库;3 冲正;
                        switch (log.Type)
                        {
                            case 1:
                                material.LastNumber -= log.Number;
                                break;
                            case 2:
                                material.LastNumber += log.Number;
                                break;
                            case 3:
                                material.LastNumber = log.OldNumber;
                                break;
                        }
                    }

                    var valid = materialStatistics.Where(x => x.Valid() && bills.Any(y => y.Id == x.BillId)).Select(z =>
                       {
                           z.Time = calTime;
                           return z;
                       }).ToList();
                    var exist = ServerConfig.ApiDb.Query<MaterialStatistic>(
                        "SELECT * FROM material_balance WHERE Time = @Time;", new { Time = calTime });
                    var delete = exist.Where(x => valid.All(y => y.BillId != x.BillId)).ToList();
                    var add = valid.Where(x => exist.All(y => y.BillId != x.BillId)).ToList();
                    var update = valid.Where(x => exist.Any(y => y.BillId == x.BillId && ClassExtension.HaveChange(x, y))).ToList();
                    Console.WriteLine($"更新每日物料报表:  all: {valid.Count},  delete: {delete.Count},  add: {add.Count},  update: {update.Count}");
                    if (delete.Any())
                    {
                        ServerConfig.ApiDb.Execute("DELETE FROM material_balance WHERE Time = @Time AND BillId = @BillId;", delete);
                    }
                    if (add.Any())
                    {
                        ServerConfig.ApiDb.Execute(
                            "INSERT INTO `material_balance` (`Time`, `Code`, `BillId`, `CategoryId`, `Category`, `NameId`, `Name`, `SupplierId`, `Supplier`, `SpecificationId`, " +
                            "`Specification`, `SiteId`, `Site`, `Unit`, `Stock`, `LastNumber`, `LastPrice`, `LastAmount`, `TodayNumber`, `TodayPrice`, " +
                            "`TodayAmount`, `Increase`, `IncreaseAmount`, `Consume`, `ConsumeAmount`, `CorrectIn`, `CorrectInAmount`, `CorrectCon`, `CorrectConAmount`, " +
                            "`Correct`, `CorrectAmount`) " +
                            "VALUES (@Time, @Code, @BillId, @CategoryId, @Category, @NameId, @Name, @SupplierId, @Supplier, @SpecificationId, @Specification, " +
                            "@SiteId, @Site, @Unit, @Stock, @LastNumber, @LastPrice, @LastAmount, @TodayNumber, @TodayPrice, " +
                            "@TodayAmount, @Increase, @IncreaseAmount, @Consume, @ConsumeAmount, @CorrectIn, @CorrectInAmount, @CorrectCon, @CorrectConAmount, " +
                            "@Correct, @CorrectAmount);", add);
                    }

                    if (update.Any())
                    {
                        ServerConfig.ApiDb.Execute(
                            "UPDATE `material_balance` SET `Code` = @Code, `CategoryId` = @CategoryId, `Category` = @Category, `NameId` = @NameId, `Name` = @Name, " +
                            "`SupplierId` = @SupplierId, `Supplier` = @Supplier, `SpecificationId` = @SpecificationId, `Specification` = @Specification, `SiteId` = @SiteId, " +
                            "`Site` = @Site, `Unit` = @Unit, `Stock` = @Stock, `LastNumber` = @LastNumber, `LastPrice` = @LastPrice, `LastAmount` = @LastAmount, " +
                            "`TodayNumber` = @TodayNumber, `TodayPrice` = @TodayPrice, `TodayAmount` = @TodayAmount, `Increase` = @Increase, `IncreaseAmount` = @IncreaseAmount, " +
                            "`Consume` = @Consume, `ConsumeAmount` = @ConsumeAmount, `CorrectIn` = @CorrectIn, `CorrectInAmount` = @CorrectInAmount, `CorrectCon` = @CorrectCon, " +
                            "`CorrectConAmount` = @CorrectConAmount, `Correct` = @Correct, `CorrectAmount` = @CorrectAmount WHERE `Time` = @Time AND `BillId` = @BillId;;", update);
                    }

                    if (!now.InSameDay(calTime))
                    {
                        calTime = now.Date;
                    }

                    RedisHelper.SetForever(timeKey, calTime.ToStr());
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }

                RedisHelper.Remove(redisLock);
                _isGetDayBalance = true;
                _getDayBalanceRuning = false;
            }
        }

        ///// <summary>
        ///// 获取每日库存结存
        ///// </summary>
        //public static void DayBalance(IEnumerable<DateTime> calTimes)
        //{
        //    if (calTimes != null && calTimes.Any())
        //    {
        //        //ServerConfig.ApiDb.Execute("DELETE FROM `material_balance` WHERE Time IN @Time;", new { Time = calTimes.Select(x => x.Date) });

        //    }
        //}

        /// <summary>
        /// 获取每日库存结存
        /// </summary>
        public static void DayBalance(IEnumerable<MaterialLog> logs)
        {
            if (logs != null && logs.Any())
            {
                //ServerConfig.ApiDb.Execute("DELETE FROM `material_balance` WHERE Time IN @Time;", new { Time = calTimes.Select(x => x.Date) });

            }
        }

        private static bool _dayBalanceRecoveryRuning = false;

        /// <summary>
        /// 日库存结存老数据恢复
        /// </summary>
        private static void DayBalanceRecovery(IEnumerable<MaterialLog> logs = null)
        {
            var _pre = "DayBalanceRecovery";
            var redisLock = $"{_pre}:Lock";
            var recoveryTime = $"{_pre}:rTime";

            var _pre1 = "GetDayBalance";
            var timeKey = $"{_pre1}:Time";
            //if (first)
            //{
            //    RedisHelper.Remove(redisLock);
            //}

            if (!_isGetDayBalance)
            {
                return;
            }
            if (_dayBalanceRecoveryRuning)
            {
                //RedisHelper.SetExpireAt(redisLock, DateTime.Now.AddMinutes(10));
                return;
            }

            if (RedisHelper.SetIfNotExist(redisLock, DateTime.Now.ToStr()))
            {
                try
                {
                    _dayBalanceRecoveryRuning = true;
                    //RedisHelper.SetExpireAt(redisLock, DateTime.Now.AddMinutes(5 * 12));

                    var minLogDay = ServerConfig.ApiDb.Query<DateTime>("SELECT MIN(Time) FROM `material_log`;")
                        .FirstOrDefault();
                    if (minLogDay == default(DateTime))
                    {
                        RedisHelper.Remove(redisLock);
                        _dayBalanceRecoveryRuning = false;
                        return;
                    }

                    minLogDay = minLogDay.Date;
                    var balanceDays = ServerConfig.ApiDb.Query<DateTime>("SELECT Time FROM `material_balance` GROUP BY Time ORDER BY Time;");
                    if (!balanceDays.Any())
                    {
                        RedisHelper.Remove(redisLock);
                        _dayBalanceRecoveryRuning = false;
                        return;
                    }
                    var maxBalanceDay = balanceDays.LastOrDefault().Date;
                    var minBalanceDay = balanceDays.FirstOrDefault().Date;

                    var calTime = RedisHelper.Get<DateTime>(timeKey);
                    calTime = calTime == default(DateTime) ? DateTime.Now : calTime;

                    var timeDic = new Dictionary<DateTime, int>();
                    timeDic.AddRange(balanceDays.ToDictionary(x => x, x => 1));
                    var lastDay = maxBalanceDay;
                    while (lastDay >= minLogDay)
                    {
                        if (!timeDic.ContainsKey(lastDay))
                        {
                            timeDic.Add(lastDay, 0);
                        }

                        lastDay = lastDay.AddDays(-1);
                    }

                    if (minBalanceDay != default(DateTime) && !minBalanceDay.InSameDay(DateTime.Now) && timeDic.Any(x => x.Value == 0))
                    {
                        timeDic[minBalanceDay] = 0;
                    }
                    timeDic = timeDic.OrderByDescending(x => x.Key).ToDictionary(y => y.Key, y => y.Value);
                    if (timeDic.All(x => x.Value != 0))
                    {
                        var reTime = RedisHelper.Get<DateTime>(recoveryTime);
                        var rTime = reTime != default(DateTime) ? (reTime.Date > calTime.Date ? calTime.Date : reTime.Date) : calTime.Date;
                        if (reTime != default(DateTime))
                        {
                            RedisHelper.SetForever(recoveryTime, rTime.ToDateStr());
                        }
                        timeDic = timeDic.Where(x => x.Key <= rTime).ToDictionary(y => y.Key, y => y.Key == rTime ? 1 : 0);
                    }
                    var bills = ServerConfig.ApiDb.Query<MaterialBill>("SELECT * FROM material_bill WHERE MarkedDelete = 0;");
                    var materialStatistics = new List<MaterialStatistic>();
                    for (var i = 0; i < timeDic.Count; i++)
                    {
                        var time = timeDic.ElementAt(i);
                        if (time.Value == 0)
                        {
                            var t = time.Key;
                            var tomorrow = t.AddDays(1);
                            if (!timeDic.ContainsKey(tomorrow) || timeDic[tomorrow] != 1)
                            {
                                continue;
                            }
                            materialStatistics.Clear();

                            var tomorrowStatistics = ServerConfig.ApiDb.Query<MaterialStatistic>(
                                "SELECT * FROM (SELECT * FROM `material_balance` WHERE Time > @Time ORDER BY Time) a GROUP BY a.BillId;",
                                new { Time = t });
                            IEnumerable<int> notExist;
                            if (tomorrowStatistics.Any())
                            {
                                materialStatistics.AddRange(tomorrowStatistics.Select(x =>
                                {
                                    x.Time = t;
                                    x.TodayNumber = x.LastNumber;
                                    x.Init();
                                    return x;
                                }));
                            }

                            //今日入库领用
                            var todayMaterialStatistics = ServerConfig.ApiDb.Query<MaterialStatistic>(
                               "SELECT a.BillId, SUM(IF(a.Type = 1, a.Number, 0)) Increase,  SUM(IF(a.Type = 2, a.Number, 0)) Consume, " +
                               "SUM(IF(a.Type = 3 AND `Mode` = 0, a.Number, 0)) CorrectIn, SUM(IF(a.Type = 3 AND `Mode` = 1, a.Number, 0)) " +
                               "CorrectCon, SUM(IF(a.Type = 3, a.Number, 0)) Correct FROM material_log a JOIN material_bill b ON a.BillId = b.Id " +
                               "WHERE Time >= @Time1 AND Time <= @Time2 GROUP BY a.BillId;", new
                               {
                                   Time1 = t.DayBeginTime(),
                                   Time2 = t.DayEndTime(),
                               });
                            notExist = todayMaterialStatistics.Where(x => materialStatistics.All(y => y.BillId != x.BillId)).Select(z => z.BillId);
                            if (notExist.Any())
                            {
                                materialStatistics.AddRange(ServerConfig.ApiDb.Query<MaterialStatistic>(
                                    "SELECT a.`Id` BillId, a.`Code`, b.`CategoryId`, b.`Category`, b.`NameId`, b.`Name`, b.`SupplierId`, b.`Supplier`, a.`SpecificationId`, b.`Specification`, a.`SiteId`, d.Site, a.`Unit`, a.`Stock`, a.`Price` TodayPrice FROM `material_bill` a JOIN (SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a JOIN (SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM `material_supplier` a JOIN (SELECT a.*, b.Category FROM `material_name` a JOIN `material_category` b ON a.CategoryId = b.Id) b ON a.NameId = b.Id) b ON a.SupplierId = b.Id) b ON a.SpecificationId = b.Id JOIN `material_site` d ON a.SiteId = d.Id WHERE a.Id IN @Id ORDER BY a.Id;",
                                    new { Id = notExist }).Select(x =>
                                    {
                                        x.Time = t;
                                        return x;
                                    }));
                            }

                            if (todayMaterialStatistics.Any())
                            {
                                foreach (var ne in todayMaterialStatistics)
                                {
                                    var bill = materialStatistics.FirstOrDefault(x => x.BillId == ne.BillId);
                                    if (bill != null)
                                    {
                                        bill.Increase = ne.Increase;
                                        bill.IncreaseAmount = ne.Increase * bill.TodayPrice;
                                        bill.Consume = ne.Consume;
                                        bill.ConsumeAmount = ne.Consume * bill.TodayPrice;
                                        bill.CorrectIn = ne.CorrectIn;
                                        bill.CorrectInAmount = ne.CorrectIn * bill.TodayPrice;
                                        bill.CorrectCon = ne.CorrectCon;
                                        bill.CorrectConAmount = ne.CorrectCon * bill.TodayPrice;
                                        bill.Correct = ne.Correct;
                                        bill.CorrectAmount = ne.Correct * bill.TodayPrice;
                                    }
                                }
                            }

                            foreach (var statistic in materialStatistics)
                            {
                                statistic.LastNumber = statistic.TodayNumber;
                                statistic.LastPrice = statistic.TodayPrice;
                            }
                            //今日入库领用
                            var todayLogs = ServerConfig.ApiDb.Query<MaterialLog>("SELECT * FROM `material_log` WHERE Time >= @Time1 AND Time <= @Time2 Order By Time DESC;", new
                            {
                                Time1 = t.DayBeginTime(),
                                Time2 = t.DayEndTime(),
                            });

                            notExist = todayLogs.Where(x => materialStatistics.All(y => y.BillId != x.BillId)).Select(z => z.BillId);

                            if (notExist.Any())
                            {
                                materialStatistics.AddRange(ServerConfig.ApiDb.Query<MaterialStatistic>(
                                    "SELECT a.`Id` BillId, a.`Code`, b.`CategoryId`, b.`Category`, b.`NameId`, b.`Name`, b.`SupplierId`, b.`Supplier`, a.`SpecificationId`, " +
                                    "b.`Specification`, a.`SiteId`, d.Site, a.`Unit`, a.`Stock`, a.`Price` TodayPrice " +
                                    " FROM `material_bill` a JOIN (SELECT a.*, b.CategoryId, b.Category, b.NameId, " +
                                    "b.`Name`, b.Supplier FROM `material_specification` a JOIN (SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM " +
                                    "`material_supplier` a JOIN (SELECT a.*, b.Category FROM `material_name` a JOIN `material_category` b ON a.CategoryId = " +
                                    "b.Id) b ON a.NameId = b.Id) b ON a.SupplierId = b.Id) b ON a.SpecificationId = b.Id" +
                                    " JOIN `material_site` d ON a.SiteId = d.Id WHERE a.Id IN @Id ORDER BY a.Id;",
                                    new { Id = notExist }).Select(x =>
                                {
                                    x.Time = t;
                                    return x;
                                }));
                            }

                            foreach (var log in todayLogs)
                            {
                                if (materialStatistics.All(x => x.BillId != log.BillId))
                                {
                                    materialStatistics.Add(new MaterialStatistic
                                    {
                                        Time = t,
                                        BillId = log.BillId
                                    });
                                }

                                var material = materialStatistics.First(x => x.BillId == log.BillId);
                                // 1 入库; 2 出库;3 冲正;
                                switch (log.Type)
                                {
                                    case 1:
                                        material.LastNumber -= log.Number;
                                        break;
                                    case 2:
                                        material.LastNumber += log.Number;
                                        break;
                                    case 3:
                                        material.LastNumber = log.OldNumber;
                                        break;
                                }
                            }

                            var valid = materialStatistics.Where(x => x.Valid() && bills.Any(y => y.Id == x.BillId)).Select(z =>
                            {
                                z.Time = t;
                                return z;
                            }).ToList();
                            var exist = ServerConfig.ApiDb.Query<MaterialStatistic>(
                                "SELECT * FROM material_balance WHERE Time = @Time;", new { Time = t });
                            var delete = exist.Where(x => valid.All(y => y.BillId != x.BillId));
                            var add = valid.Where(x => exist.All(y => y.BillId != x.BillId));
                            var update = valid.Where(x => exist.Any(y => y.BillId == x.BillId && ClassExtension.HaveChange(x, y)));
                            Console.WriteLine($"日库存结存老数据恢复: time: {t.ToDateStr()},  all: {valid.Count()},  delete: {delete.Count()},  add: {add.Count()},  update: {update.Count()}");

                            if (delete.Any())
                            {
                                ServerConfig.ApiDb.Execute("DELETE FROM material_balance WHERE Time = @Time AND BillId = @BillId;", delete);
                            }
                            if (add.Any())
                            {
                                ServerConfig.ApiDb.Execute(
                                    "INSERT INTO `material_balance` (`Time`, `Code`, `BillId`, `CategoryId`, `Category`, `NameId`, `Name`, `SupplierId`, `Supplier`, `SpecificationId`, " +
                                    "`Specification`, `SiteId`, `Site`, `Unit`, `Stock`, `LastNumber`, `LastPrice`, `LastAmount`, `TodayNumber`, `TodayPrice`, " +
                                    "`TodayAmount`, `Increase`, `IncreaseAmount`, `Consume`, `ConsumeAmount`, `CorrectIn`, `CorrectInAmount`, `CorrectCon`, `CorrectConAmount`, " +
                                    "`Correct`, `CorrectAmount`) " +
                                    "VALUES (@Time, @Code, @BillId, @CategoryId, @Category, @NameId, @Name, @SupplierId, @Supplier, @SpecificationId, @Specification, " +
                                    "@SiteId, @Site, @Unit, @Stock, @LastNumber, @LastPrice, @LastAmount, @TodayNumber, @TodayPrice, " +
                                    "@TodayAmount, @Increase, @IncreaseAmount, @Consume, @ConsumeAmount, @CorrectIn, @CorrectInAmount, @CorrectCon, @CorrectConAmount, " +
                                    "@Correct, @CorrectAmount);", add);
                            }

                            if (update.Any())
                            {
                                ServerConfig.ApiDb.Execute(
                                    "UPDATE `material_balance` SET `Code` = @Code, `CategoryId` = @CategoryId, `Category` = @Category, `NameId` = @NameId, `Name` = @Name, " +
                                    "`SupplierId` = @SupplierId, `Supplier` = @Supplier, `SpecificationId` = @SpecificationId, `Specification` = @Specification, `SiteId` = @SiteId, " +
                                    "`Site` = @Site, `Unit` = @Unit, `Stock` = @Stock, `LastNumber` = @LastNumber, `LastPrice` = @LastPrice, `LastAmount` = @LastAmount, " +
                                    "`TodayNumber` = @TodayNumber, `TodayPrice` = @TodayPrice, `TodayAmount` = @TodayAmount, `Increase` = @Increase, `IncreaseAmount` = @IncreaseAmount, " +
                                    "`Consume` = @Consume, `ConsumeAmount` = @ConsumeAmount, `CorrectIn` = @CorrectIn, `CorrectInAmount` = @CorrectInAmount, `CorrectCon` = @CorrectCon, " +
                                    "`CorrectConAmount` = @CorrectConAmount, `Correct` = @Correct, `CorrectAmount` = @CorrectAmount WHERE `Time` = @Time AND `BillId` = @BillId;;", update);
                            }
                            timeDic[t] = 1;
                        }
                        RedisHelper.SetForever(recoveryTime, time.Key.ToDateStr());
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                //RedisHelper.Remove(redisLock);
                RedisHelper.SetExpireAt(recoveryTime, DateTime.Now.AddMinutes(5));
                RedisHelper.SetForever(redisLock, $"{ DateTime.Now.ToStr()} Done");
                _dayBalanceRecoveryRuning = false;
            }
        }

        /// <summary>
        /// 库存、日志数据修复
        /// </summary>
        public static void MaterialRecovery(bool re = false)
        {
            var _pre = "MaterialRecovery";
            var redisLock = $"{_pre}:Lock";
            if (_first || re)
            {
                RedisHelper.Remove(redisLock);
            }
            if (RedisHelper.SetIfNotExist(redisLock, DateTime.Now.ToStr()))
            {
                try
                {
                    var logs = ServerConfig.ApiDb.Query<MaterialLog>("SELECT * FROM `material_log`;").OrderBy(x => x.BillId).ThenBy(x => x.Time);
                    if (!logs.Any())
                    {
                        return;
                    }

                    var oldLogs = logs.Select(x =>
                    {
                        var log = x.ToJSON();
                        return JsonConvert.DeserializeObject<MaterialLog>(log);
                    }).ToList();
                    var oldMaterial = ServerConfig.ApiDb.Query<MaterialManagement>("SELECT * FROM `material_management`;");
                    var newMaterial = oldMaterial.ToDictionary(x => x.BillId, x => new MaterialManagement
                    {
                        BillId = x.BillId,
                        Number = 0
                    });

                    foreach (var log in logs)
                    {
                        if (!newMaterial.ContainsKey(log.BillId))
                        {
                            newMaterial.Add(log.BillId, new MaterialManagement()
                            {
                                BillId = log.BillId
                            });
                        }

                        var material = newMaterial[log.BillId];
                        log.OldNumber = material.Number;
                        // 1 入库; 2 出库;3 冲正;
                        switch (log.Type)
                        {
                            case 1:
                                material.Number += log.Number;
                                if (log.Number != 0)
                                {
                                    material.InTime = log.Time;
                                }
                                break;
                            case 2:
                                material.Number -= log.Number;
                                if (log.Number != 0)
                                {
                                    material.OutTime = log.Time;
                                }
                                break;
                            case 3:
                                material.Number = log.Number;
                                break;
                        }
                    }

                    var changes = oldMaterial.Where(x =>
                        newMaterial.ContainsKey(x.BillId) && ClassExtension.HaveChange(x, newMaterial[x.BillId])).ToDictionary(z => z.BillId);
                    var s = newMaterial.Where(x => changes.ContainsKey(x.Key)).Select(y => y.Value).ToList();
                    var all = ServerConfig.ApiDb.Query<MaterialManagement>("SELECT * FROM `material_management`;");
                    var exist = s.Where(x =>
                    {
                        if (all.Any(y => y.BillId == x.BillId))
                        {
                            if (ClassExtension.HaveChange(all.First(y => y.BillId == x.BillId), x))
                            {
                                return true;
                            }
                        }
                        return false;
                    });
                    var notExist = newMaterial.Values.Where(x => oldMaterial.All(y => y.BillId != x.BillId));
                    var changeLogs = logs.Where(x =>
                        oldLogs.Any(y => y.Id == x.Id) && oldLogs.First(y => y.Id == x.Id).OldNumber != x.OldNumber).ToList();
                    Console.WriteLine($"库存、日志数据修复: time: {DateTime.Now.ToDateStr()},  更新: {exist.Count()},  新增: {notExist.Count()},  日志: {changeLogs.Count()}");
                    if (exist.Any())
                    {
                        ServerConfig.ApiDb.Execute(
                            "UPDATE `material_management` SET " +
                            "`InTime` = IF(ISNULL(`InTime`) OR `InTime` != @InTime, @InTime, `InTime`), " +
                            "`OutTime` = IF(ISNULL(`OutTime`) OR `OutTime` != @OutTime, @OutTime, `OutTime`), " +
                            "`Number` = @Number WHERE `BillId` = @BillId;",

                            exist);
                    }

                    if (notExist.Any())
                    {
                        ServerConfig.ApiDb.Execute("INSERT INTO material_management (`BillId`, `InTime`, `OutTime`, `Number`) VALUES (@BillId, @InTime, @OutTime, @Number);", notExist);
                    }

                    if (changeLogs.Any())
                    {
                        ServerConfig.ApiDb.Execute("UPDATE `material_log` SET `OldNumber` = @OldNumber WHERE `Id` = @Id;", changeLogs);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                RedisHelper.Set(redisLock, $"{ DateTime.Now.ToStr()} Done", DateTime.Now.AddMinutes(60));
            }
        }

        /// <summary>
        /// 维修工自动排班
        /// </summary>
        private static void MaintainerSchedule()
        {
            var _pre = "MaintainerSchedule";
            var redisLock = $"{_pre}:Lock";
            if (RedisHelper.SetIfNotExist(redisLock, DateTime.Now.ToStr()))
            {
                RedisHelper.SetExpireAt(redisLock, DateTime.Now.AddMinutes(5 * 12));
                DoMaintainerSchedule();
                RedisHelper.Remove(redisLock);
            }
        }

        /// <summary>
        /// 维修工自动排班
        /// </summary>
        public static void DoMaintainerSchedule()
        {
            try
            {
                var now = DateTime.Now;
                var today = now.Date;
                var thisWeekBegin = today.WeekBeginTime().Date;
                var thisWeekEnd = today.WeekEndTime().Date;
                var nextWeekBegin = thisWeekBegin.AddDays(7).Date;
                var lastWeekBegin = thisWeekBegin.AddDays(-7).Date;
                var lastWeekEnd = thisWeekEnd.AddDays(-7).Date;
                var maintainers = ServerConfig.ApiDb.Query<Maintainer>("SELECT * FROM `maintainer` WHERE `MarkedDelete` = 0 AND `Order` != 0 ORDER BY `Order`, Id;").ToArray();
                var len = maintainers.Length;

                var schedules = ServerConfig.ApiDb.Query<MaintainerSchedule>(
                    "SELECT * FROM `maintainer_schedule` WHERE StartTime >= @Time1 AND StartTime < @Time2 ORDER BY StartTime;", new
                    {
                        Time1 = lastWeekBegin,
                        Time2 = nextWeekBegin
                    });
                var i = 0;
                var leftLen = 0;
                var _pre = "MaintainerSchedule";
                var lastWeekNightKey = $"{_pre}:LastWeekNight";
                var thisWeekNightKey = $"{_pre}:ThisWeekNight";
                //上周夜班
                var lastWeekNightMaintainerId = RedisHelper.Get<int>(lastWeekNightKey);
                if (lastWeekNightMaintainerId == 0)
                {
                    lastWeekNightMaintainerId = schedules.FirstOrDefault(x => (x.StartTime >= lastWeekEnd &&
                                                                               x.StartTime < lastWeekEnd.AddSeconds(GlobalConfig.Morning.TotalSeconds)))?.MaintainerId ?? 0;
                }

                //本周夜班 = 上周日夜班 20 - 24
                var thisWeekNightMaintainerId = RedisHelper.Get<int>(thisWeekNightKey);
                if (thisWeekNightMaintainerId == 0)
                {
                    thisWeekNightMaintainerId = schedules.FirstOrDefault(x => (x.StartTime >= lastWeekEnd.AddSeconds(GlobalConfig.Night20.TotalSeconds) &&
                                                                               x.StartTime < lastWeekEnd.AddDays(1)))?.MaintainerId ?? 0;
                }

                var nextWeekNightMaintainerId = 0;
                var thisWeekDayMaintainerId = 0;
                var thisWeekHave = false;
                if (thisWeekNightMaintainerId == 0)
                {
                    if (len > 0)
                    {
                        if (lastWeekNightMaintainerId != 0)
                        {
                            var maintainer = maintainers.FirstOrDefault(x => x.Id == lastWeekNightMaintainerId);
                            if (maintainer != null)
                            {
                                var index = maintainers.IndexOf(maintainer);
                                i = index + 1 >= len ? 0 : index + 1;
                                //上周夜班
                                lastWeekNightMaintainerId = thisWeekNightMaintainerId;
                                //本周夜班
                                thisWeekNightMaintainerId = maintainers[i].Id;
                                thisWeekHave = true;
                            }
                        }
                        if (!thisWeekHave)
                        {
                            thisWeekNightMaintainerId = maintainers[0].Id;
                        }
                    }
                }

                Maintainer[] leftMaintainers = null;
                var updateList = new List<int>();
                var deleteList = new List<int>();
                var newSchedules = new List<MaintainerSchedule>();
                //下周新排班
                if (schedules.Count(x => x.StartTime >= thisWeekBegin && x.StartTime < thisWeekEnd) == 0)
                {
                    #region 本周新排班表
                    leftMaintainers = maintainers.Where(x => x.Id != thisWeekNightMaintainerId).ToArray();
                    leftLen = leftMaintainers.Length;

                    var i17_20 = 0;
                    var temp = thisWeekBegin;
                    DateTime startTime;
                    DateTime endTime;
                    MaintainerSchedule schedule;
                    while (temp <= thisWeekEnd.AddDays(-1))
                    {
                        //0-8
                        startTime = temp;
                        endTime = temp.AddSeconds(GlobalConfig.Morning.TotalSeconds);
                        schedule = new MaintainerSchedule
                        {
                            StartTime = startTime,
                            EndTime = endTime,
                            MaintainerId = thisWeekNightMaintainerId
                        };
                        newSchedules.Add(schedule);

                        //8-17
                        startTime = temp.AddSeconds(GlobalConfig.Morning.TotalSeconds);
                        endTime = temp.AddSeconds(GlobalConfig.Evening.TotalSeconds);
                        newSchedules.AddRange(leftMaintainers.OrderBy(y => y.Id).Select(x => new MaintainerSchedule
                        {
                            StartTime = startTime,
                            EndTime = endTime,
                            MaintainerId = x.Id
                        }));

                        //17-20
                        startTime = temp.AddSeconds(GlobalConfig.Evening.TotalSeconds);
                        endTime = temp.AddSeconds(GlobalConfig.Night20.TotalSeconds);
                        //本周夜班
                        schedule = new MaintainerScheduleDetails
                        {
                            StartTime = startTime,
                            EndTime = endTime,
                            MaintainerId = leftLen > 0 ? leftMaintainers[i17_20].Id : 0
                        };
                        newSchedules.Add(schedule);
                        i17_20 = i17_20 + 1 >= leftLen ? 0 : i17_20 + 1;

                        //20-24
                        startTime = temp.AddSeconds(GlobalConfig.Night20.TotalSeconds);
                        endTime = temp.AddDays(1);
                        schedule = new MaintainerScheduleDetails
                        {
                            StartTime = startTime,
                            EndTime = endTime,
                            MaintainerId = thisWeekNightMaintainerId
                        };
                        newSchedules.Add(schedule);
                        temp = temp.AddDays(1);
                    }

                    var maintainer = maintainers.FirstOrDefault(x => x.Id == thisWeekNightMaintainerId);
                    if (maintainer != null)
                    {
                        var index = maintainers.IndexOf(maintainer);
                        i = index + 1 >= len ? 0 : index + 1;
                        nextWeekNightMaintainerId = maintainers[i].Id;
                        if (nextWeekNightMaintainerId == lastWeekNightMaintainerId)
                        {
                            nextWeekNightMaintainerId = 0;
                        }

                        i = i + 1 >= len ? 0 : i + 1;
                        thisWeekDayMaintainerId = maintainers[i].Id;
                        if (thisWeekDayMaintainerId == nextWeekNightMaintainerId)
                        {
                            thisWeekDayMaintainerId = 0;
                        }
                    }

                    //0-8
                    startTime = temp;
                    endTime = temp.AddSeconds(GlobalConfig.Morning.TotalSeconds);
                    newSchedules.Add(new MaintainerSchedule
                    {
                        StartTime = startTime,
                        EndTime = endTime,
                        MaintainerId = thisWeekNightMaintainerId
                    });

                    //8-17
                    startTime = temp.AddSeconds(GlobalConfig.Morning.TotalSeconds);
                    endTime = temp.AddSeconds(GlobalConfig.Evening.TotalSeconds);
                    newSchedules.Add(new MaintainerSchedule
                    {
                        StartTime = startTime,
                        EndTime = endTime,
                        MaintainerId = thisWeekDayMaintainerId
                    });

                    //17-20
                    startTime = temp.AddSeconds(GlobalConfig.Evening.TotalSeconds);
                    endTime = temp.AddSeconds(GlobalConfig.Night20.TotalSeconds);
                    //本周夜班
                    newSchedules.Add(new MaintainerScheduleDetails
                    {
                        StartTime = startTime,
                        EndTime = endTime,
                        MaintainerId = thisWeekDayMaintainerId
                    });

                    //20-24
                    startTime = temp.AddSeconds(GlobalConfig.Night20.TotalSeconds);
                    endTime = temp.AddDays(1);
                    newSchedules.Add(new MaintainerScheduleDetails
                    {
                        StartTime = startTime,
                        EndTime = endTime,
                        MaintainerId = nextWeekNightMaintainerId
                    });
                    #endregion

                    ServerConfig.ApiDb.Execute(
                        "INSERT INTO  `maintainer_schedule` (`StartTime`, `EndTime`, `MaintainerId`) VALUES (@StartTime, @EndTime, @MaintainerId) " +
                        "ON DUPLICATE KEY UPDATE `MaintainerId` = @MaintainerId;",
                        newSchedules);
                }
                else
                {
                    newSchedules.AddRange(schedules.Where(t => t.StartTime >= thisWeekBegin));
                    #region 检查本周是否有离职的或取消排班的，更新排班表
                    var nightSchedules = newSchedules.Where(d => d.StartTime > now && d.StartTime < thisWeekEnd.AddSeconds(GlobalConfig.Morning.TotalSeconds)
                                                                                   && ((d.StartTime >= d.StartTime.Date && d.StartTime < d.StartTime.Date.AddSeconds(GlobalConfig.Morning.TotalSeconds))
                                                                                       || (d.StartTime > d.StartTime.Date.AddSeconds(GlobalConfig.Night20.TotalSeconds) && d.StartTime < d.StartTime.Date.AddDays(1))));
                    //检查本周夜班是否有离职的
                    var thisWeekNightLiZhi = nightSchedules.Where(x => maintainers.All(y => y.Id != x.MaintainerId));
                    //本周夜班离职
                    if (thisWeekNightLiZhi.Any())
                    {
                        foreach (var schedule in thisWeekNightLiZhi)
                        {
                            schedule.MaintainerId = 0;
                            updateList.Add(schedule.Id);
                        }
                    }

                    //下周夜班
                    var nextWeekNightMaintainer = newSchedules.FirstOrDefault(d => d.StartTime > now && d.StartTime >= thisWeekBegin.AddSeconds(GlobalConfig.Night20.TotalSeconds));
                    //检查下周夜班是否有离职的
                    var isNextWeekNightLiZhi = maintainers.All(y => y.Id != nextWeekNightMaintainer?.MaintainerId);
                    //下周夜班离职
                    if (nextWeekNightMaintainer != null && isNextWeekNightLiZhi)
                    {
                        //var maintainer = maintainers.FirstOrDefault(x => x.Id == thisWeekNightMaintainerId);
                        //if (maintainer != null)
                        //{
                        //    var index = maintainers.IndexOf(maintainer);
                        //    i = index + 1 >= len ? 0 : index + 1;
                        //    nextWeekNightMaintainerId = maintainers[i].Id;
                        //    if (nextWeekNightMaintainerId == lastWeekNightMaintainerId)
                        //    {
                        //        nextWeekNightMaintainerId = 0;
                        //    }

                        //    i = i + 1 >= len ? 0 : i + 1;
                        //    thisWeekDayMaintainerId = maintainers[i].Id;
                        //    if (thisWeekDayMaintainerId == nextWeekNightMaintainerId)
                        //    {
                        //        thisWeekDayMaintainerId = 0;
                        //    }
                        //}
                        updateList.Add(nextWeekNightMaintainer.Id);
                        nextWeekNightMaintainer.MaintainerId = 0;
                        //leftMaintainers = maintainers.Where(x => x.Id != nextNightMaintainerId).ToArray();
                        //leftLen = leftMaintainers.Length;
                        //if (leftLen == 0)
                        //{
                        //    leftMaintainers = maintainers.ToArray();
                        //}
                    }
                    //本周白班 8 - 17
                    var day8_17Schedules = newSchedules.Where(d => d.StartTime > now && d.StartTime < thisWeekEnd.AddDays(-1).AddSeconds(GlobalConfig.Evening.TotalSeconds)
                                                                                   && (d.StartTime >= d.StartTime.Date.AddSeconds(GlobalConfig.Morning.TotalSeconds) && d.StartTime < d.StartTime.Date.AddSeconds(GlobalConfig.Evening.TotalSeconds)));

                    //检查本周白班是否有离职的
                    var day8_17LiZhi = day8_17Schedules.Where(x => maintainers.All(y => y.Id != x.MaintainerId));
                    //下周夜班离职
                    if (day8_17LiZhi.Any())
                    {
                        foreach (var schedule in day8_17LiZhi)
                        {
                            deleteList.Add(schedule.Id);
                        }
                    }

                    //本周白班 17 - 20
                    var day17_20Schedules = newSchedules.Where(d => d.StartTime > now && d.StartTime < thisWeekEnd.AddDays(-1).AddSeconds(GlobalConfig.Night20.TotalSeconds)
                                                                                      && (d.StartTime >= d.StartTime.Date.AddSeconds(GlobalConfig.Evening.TotalSeconds) && d.StartTime < d.StartTime.Date.AddSeconds(GlobalConfig.Night20.TotalSeconds)));

                    //检查本周白班是否有离职的
                    var day17_20SLiZhi = day17_20Schedules.Where(x => maintainers.All(y => y.Id != x.MaintainerId));
                    //下周夜班离职
                    if (day17_20SLiZhi.Any())
                    {
                        foreach (var schedule in day17_20SLiZhi)
                        {
                            schedule.MaintainerId = 0;
                            updateList.Add(schedule.Id);
                        }
                    }
                    #endregion
                    if (updateList.Any())
                    {
                        ServerConfig.ApiDb.Execute(
                        "UPDATE `maintainer_schedule` SET `MaintainerId` = @MaintainerId WHERE `Id` = @Id;",
                            newSchedules.Where(x => updateList.Any(y => y == x.Id)));
                    }
                    if (deleteList.Any())
                    {
                        ServerConfig.ApiDb.Execute(
                            "DELETE FROM `maintainer_schedule` WHERE `Id` = @Id;",
                            newSchedules.Where(x => deleteList.Any(y => y == x.Id)));
                    }
                }


                RedisHelper.SetForever(lastWeekNightKey, thisWeekNightMaintainerId);
                RedisHelper.SetForever(thisWeekNightKey, nextWeekNightMaintainerId);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        #region 账号获取
        public static void CheckAccount()
        {
            //var _createUserId = "ErpSystem";
            var checkAccountPre = "CheckAccount";
            var checkAccountLock = $"{checkAccountPre}:Lock";
            if (RedisHelper.SetIfNotExist(checkAccountLock, "lock"))
            {
                RedisHelper.SetExpireAt(checkAccountLock, DateTime.Now.AddMinutes(5));
                var f = HttpServer.Get(ServerConfig.ErpUrl, new Dictionary<string, string>
                {
                    { "type", "getUser" },
                });
                if (f == "fail")
                {
                    Log.ErrorFormat("CheckAccount 请求erp获取账号数据失败,url:{0}", ServerConfig.ErpUrl);
                }
                else
                {
                    try
                    {
                        var rr = HttpUtility.UrlDecode(f);
                        var res = JsonConvert.DeserializeObject<ErpAccount[]>(rr);
                        if (res.Any())
                        {
                            var accounts = AccountInfoHelper.GetAccountInfoByImport();
                            var add = res.Where(x => accounts.All(y => y.Account != x.f_username));
                            if (add.Any())
                            {
                                var role = ServerConfig.ApiDb.Query<int>("SELECT Id FROM `roles` WHERE New = 1;");
                                ServerConfig.ApiDb.Execute(
                                    "INSERT INTO accounts (`Number`, `Account`, `Name`, `Role`, `DeviceIds`, `MarkedDelete`, `IsErp`) VALUES (@Number, @Account, @Name, @Role, '', @MarkedDelete, 1);",
                                    add.Select(x => new AccountInfo
                                    {
                                        Number = x.f_ygbh,
                                        Account = x.f_username,
                                        Name = x.f_name,
                                        Role = role?.Join(",") ?? "",
                                        MarkedDelete = x.ifdelete,
                                    }));
                            }

                            var update = res.Where(x => accounts.Any(y => y.Account == x.f_username) &&
                                                        (accounts.First(y => y.Account == x.f_username).Name != x.f_name
                                                         || !accounts.First(y => y.Account == x.f_username).IsErp));
                            if (update.Any())
                            {
                                ServerConfig.ApiDb.Execute(
                                    "UPDATE accounts SET `Number` = @Number, `Name` = @Name, `IsErp` = @IsErp WHERE `Account` = @Account;",
                                    update.Select(x => new AccountInfo
                                    {
                                        Account = x.f_username,
                                        Number = x.f_ygbh,
                                        Name = x.f_name,
                                        IsErp = true
                                    }));
                            }

                            var delete = accounts.Where(x =>
                                         (res.All(y => y.f_username != x.Account) && !x.MarkedDelete)
                                          || (res.Any(y => y.f_username == x.Account) && res.First(y => y.f_username == x.Account).ifdelete && !x.MarkedDelete));
                            if (delete.Any())
                            {
                                AccountInfoHelper.Instance.Delete(delete.Select(x => x.Id));
                            }

                            //var duplicates = accounts.GroupBy(x => x.Account).ToDictionary(x => x.Key, x => x.ToList()).Where(x => x.Value.Count > 1).ToDictionary(x => x.Key, x => x.Value);
                            //if (duplicates.Any())
                            //{
                            //    foreach (var VARIABLE in COLLECTION)
                            //    {

                            //    }
                            //}
                        }
                    }
                    catch (Exception e)
                    {
                        Log.ErrorFormat("erp账号数据解析失败,原因:{0},错误:{1}", e.Message, e.StackTrace);
                    }
                }

                RedisHelper.Remove(checkAccountLock);
            }
        }

        public class ErpAccount
        {
            public string f_name;
            public string f_username;
            public string f_ifdelete;
            public bool ifdelete => f_ifdelete == "1";
            public string f_ygbh;
        }
        #endregion

        #region 工序统计
        private static int _dealLength = 1000;
        private static string StatisticProcessPre = "StatisticProcess";
        /// <summary>
        /// 工序统计
        /// </summary>
        private static void StatisticProcess()
        {
            var lockKey = $"{StatisticProcessPre}:Lock";
            var idKey = $"{StatisticProcessPre}:Id";
            if (RedisHelper.SetIfNotExist(lockKey, DateTime.Now.ToStr()))
            {
                try
                {
                    RedisHelper.SetExpireAt(lockKey, DateTime.Now.AddMinutes(10));

                    var now = DateTime.Now;
                    var workshops = WorkshopHelper.Instance.GetAll<Workshop>();
                    foreach (var workshop in workshops)
                    {
                        var timeKey = $"{StatisticProcessPre}:Time_{workshop.Id}";
                        var spTime = RedisHelper.Get<DateTime>(timeKey);
                        if (spTime == default(DateTime))
                        {
                            spTime = now;
                        }

                        var workDayTimes = DateTimeExtend.GetDayWorkDayRange(workshop.StatisticTimeList, spTime);
                        var mData = FlowCardReportGetHelper.GetReport(workshop.Id, workDayTimes.Item1.NoMinute(), workDayTimes.Item2.NextHour(0, 1).AddSeconds(-1));
                        //ServerConfig.ApiDb.Query<FlowCardReportGet>("SELECT * FROM `flowcard_report_get` WHERE Time >= @st AND Time < @ed ORDER BY Time;",
                        //var mData = ServerConfig.ApiDb.Query<FlowCardReportGet>("SELECT * FROM `flowcard_report_get` WHERE Id > @spId ORDER BY Id LIMIT @limit;",
                        //var mData = ServerConfig.ApiDb.Query<FlowCardReportGet>("SELECT * FROM `flowcard_report_get` WHERE Id >= 180114 and Id <= 180115 ORDER BY Id LIMIT @limit;",
                        //new
                        //{
                        //    st = workDayTimes.Item1,
                        //    ed = workDayTimes.Item2,
                        //});
                        //if (mData.Any(x => x.State == 0))
                        //{
                        //    var t = mData.OrderBy(x => x.Id).FirstOrDefault(x => x.State == 0);
                        //    mData = mData.Where(x => x.Id < t.Id);
                        //}
                        if (mData.Any())
                        {
                            var t = mData.Max(x => x.Time);
                            spTime = spTime.Max(t);
                            StatisticProcess(mData, workshop, workDayTimes);
                        }
                        if (!now.InSameWorkDay(workDayTimes))
                        {
                            spTime = workDayTimes.Item1.AddDays(1);
                        }

                        RedisHelper.SetForever(timeKey, spTime.ToStr());
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                RedisHelper.Remove(lockKey);
            }
        }

        private static string StatisticProcessAfterUpdatePre = "StatisticProcessAfter";
        /// <summary>
        /// 工序统计  报表更新后重新统计
        /// </summary>
        private static void StatisticProcessAfterUpdate()
        {
            var lockKey = $"{StatisticProcessAfterUpdatePre}:Lock";
            var idKey = $"{StatisticProcessAfterUpdatePre}:Id";
            if (RedisHelper.SetIfNotExist(lockKey, DateTime.Now.ToStr()))
            {
                try
                {
                    RedisHelper.SetExpireAt(lockKey, DateTime.Now.AddMinutes(10));

                    var now = DateTime.Now;
                    var pId = RedisHelper.Get<int>(idKey);

                    var data = ServerConfig.ApiDb.Query<FlowCardReportUpdate>("SELECT * FROM `flowcard_report_update` WHERE Id > @pId ORDER BY Id LIMIT @limit;",
                        new
                        {
                            pId,
                            limit = _dealLength
                        });

                    if (data.Any(x => x.State == 0 || x.IsUpdate == 0))
                    {
                        var t = data.OrderBy(x => x.Id).FirstOrDefault(x => x.State == 0 || x.IsUpdate == 0);
                        data = data.Where(x => x.Id < t.Id);
                        if (!data.Any())
                        {
                            RedisHelper.Remove(lockKey);
                            return;
                        }
                    }

                    if (data.Any())
                    {
                        pId = data.Max(x => x.Id);
                        var workshopIds = data.GroupBy(x => x.WorkshopId).Select(x => x.Key);
                        var workshops = WorkshopHelper.Instance.GetByIds<Workshop>(workshopIds);
                        foreach (var workshop in workshops)
                        {
                            var wData = data.Where(x => x.WorkshopId == workshop.Id);
                            var times = wData.GroupBy(x =>
                            {
                                var workDay = DateTimeExtend.GetDayWorkDayRange(workshop.StatisticTimeList, x.Time);
                                return new Tuple<DateTime, DateTime>(workDay.Item1, workDay.Item2);
                            }).Select(x => x.Key);

                            var timeKey = $"{StatisticProcessPre}:Time_{workshop.Id}";
                            var spTime = RedisHelper.Get<DateTime>(timeKey);
                            var workDayTimes = DateTimeExtend.GetDayWorkDayRange(workshop.StatisticTimeList, spTime);
                            foreach (var time in times)
                            {
                                if (time.Item1 == workDayTimes.Item1 && time.Item2 == workDayTimes.Item2)
                                {
                                    continue;
                                }

                                var mData = FlowCardReportGetHelper.GetReport(workshop.Id, time.Item1.NoMinute(), time.Item2.NextHour(0, 1).AddSeconds(-1));
                                //var mData = wData.Where(x => x.Time >= time.Item1 && x.Time <= time.Item2.AddSeconds(-1));
                                if (mData.Any(x => x.State == 0))
                                {
                                    var t = mData.OrderBy(x => x.Id).FirstOrDefault(x => x.State == 0);
                                    mData = mData.Where(x => x.Id < t.Id);
                                }
                                if (mData.Any())
                                {
                                    StatisticProcess(mData, workshop, time);
                                }
                            }
                        }

                        RedisHelper.SetForever(idKey, pId);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                RedisHelper.Remove(lockKey);
            }
        }

        private static void StatisticProcess(IEnumerable<FlowCardReportGet> reportGets, Workshop workshop, Tuple<DateTime, DateTime> workDays)
        {
            try
            {
                var sps = new List<StatisticProcessAll>();
                var notExist = new List<StatisticProcessAll>();
                sps.AddRange(StatisticProcessHelper.GetMax(workDays));
                notExist.AddRange(sps.Where(x => x.Type == StatisticProcessTimeEnum.小时
                                               && x.Time.InSameRange(new Tuple<DateTime, DateTime>(workDays.Item1.NoMinute(), workDays.Item2.NextHour(0, 1)))
                                               || (x.Type == StatisticProcessTimeEnum.日
                                                  && x.Time.InSameDay(workDays.Item1))).ToList());
                var timeTypes = EnumHelper.EnumToList<StatisticProcessTimeEnum>(true).Where(x => x.EnumValue < 2);
                var wData = reportGets.Where(x => x.WorkshopId == workshop.Id);
                foreach (var timeType in timeTypes)
                {
                    IEnumerable<IGrouping<dynamic, FlowCardReportGet>> gData = null;
                    var type = (StatisticProcessTimeEnum)timeType.EnumValue;
                    switch (type)
                    {
                        case StatisticProcessTimeEnum.小时:
                            #region 小时
                            gData = wData
                                .GroupBy(x => new
                                {
                                    Time = x.Time.NoMinute(),
                                    Step = x.Step,
                                    StepName = x.StepName,
                                    StepAbbrev = x.StepAbbrev,
                                    DeviceId = x.DeviceId,
                                    Code = x.Code,
                                    ProductionId = x.ProductionId,
                                    Production = x.Production,
                                    ProcessorId = x.ProcessorId,
                                    Processor = x.Processor
                                });
                            #endregion
                            break;
                        case StatisticProcessTimeEnum.日:
                            #region 日
                            gData = wData.Where(x => x.Time.InSameRange(workDays))
                               .GroupBy(x =>
                               {
                                    // 例：首班时间为8:00:00，统计延时30分钟时, 5月28日的数据为5月28日8:30:00(含)至5月29日8:30:00(不含)
                                    //var wdTime = DateTimeExtend.GetDayWorkDayStartTime(workshop.StatisticTimeList, x.Time);
                                    return new
                                   {
                                       Time = workDays.Item1.DayBeginTime(),
                                       Step = x.Step,
                                       StepName = x.StepName,
                                       StepAbbrev = x.StepAbbrev,
                                       DeviceId = x.DeviceId,
                                       Code = x.Code,
                                       ProductionId = x.ProductionId,
                                       Production = x.Production,
                                       ProcessorId = x.ProcessorId,
                                       Processor = x.Processor
                                   };
                               });
                            #endregion
                            break;
                            #region 周
                            //case StatisticProcessTimeEnum.周:
                            //    #region 周
                            //    gData = wData
                            //        .GroupBy(x => new
                            //        {
                            //            Time = x.Time.WeekBeginTime(),
                            //            Step = x.Step,
                            //            StepName = x.StepName,
                            //            StepAbbrev = x.StepAbbrev,
                            //            DeviceId = x.DeviceId,
                            //            Code = x.Code,
                            //            ProductionId = x.ProductionId,
                            //            Production = x.Production,
                            //            ProcessorId = x.ProcessorId,
                            //            Processor = x.Processor
                            //        });
                            //    #endregion
                            //    break;
                            //case StatisticProcessTimeEnum.月:
                            //    #region 月
                            //    gData = wData
                            //        .GroupBy(x => new
                            //        {
                            //            Time = x.Time.StartOfMonth(),
                            //            Step = x.Step,
                            //            StepName = x.StepName,
                            //            StepAbbrev = x.StepAbbrev,
                            //            DeviceId = x.DeviceId,
                            //            Code = x.Code,
                            //            ProductionId = x.ProductionId,
                            //            Production = x.Production,
                            //            ProcessorId = x.ProcessorId,
                            //            Processor = x.Processor
                            //        });
                            //    #endregion
                            //    break;
                            //case StatisticProcessTimeEnum.年:
                            //    #region 年
                            //    gData = wData
                            //        .GroupBy(x => new
                            //        {
                            //            Time = x.Time.StartOfYear(),
                            //            Step = x.Step,
                            //            StepName = x.StepName,
                            //            StepAbbrev = x.StepAbbrev,
                            //            DeviceId = x.DeviceId,
                            //            Code = x.Code,
                            //            ProductionId = x.ProductionId,
                            //            Production = x.Production,
                            //            ProcessorId = x.ProcessorId,
                            //            Processor = x.Processor
                            //        });
                            //    #endregion
                            //    break;
                            #endregion
                    }
                    #region 数据处理
                    var spData = sps.Where(x => x.WorkshopId == workshop.Id && x.Type == type);
                    var rgData = gData.ToDictionary(y => y.Key, y => new StatisticProcessAll
                    {
                        WorkshopId = workshop.Id,
                        Type = (StatisticProcessTimeEnum)timeType.EnumValue,
                        Time = y.Key.Time,
                        Step = y.Key.Step,
                        StepName = y.Key.StepName,
                        StepAbbrev = y.Key.StepAbbrev,
                        DeviceId = y.Key.DeviceId,
                        Code = y.Key.Code,
                        ProductionId = y.Key.ProductionId,
                        Production = y.Key.Production,
                        ProcessorId = y.Key.ProcessorId,
                        Processor = y.Key.Processor,

                        Total = (int)y.Sum(z => z.Total),
                        Qualified = (int)y.Sum(z => z.HeGe),
                        Unqualified = (int)y.Sum(z => z.LiePian),
                    }).ToList();
                    foreach (var (key, dData) in rgData)
                    {
                        var oldData =
                            spData.FirstOrDefault(x =>
                                x.Time == key.Time && x.Step == key.Step
                                && x.DeviceId == key.DeviceId && x.ProductionId == key.ProductionId && x.ProcessorId == key.ProcessorId);
                        if (oldData == null)
                        {
                            dData.Old = false;
                            sps.Add(dData);
                        }
                        else
                        {
                            notExist.Remove(oldData);
                            if (oldData.HaveChange(dData))
                            {
                                oldData.Update = true;
                                oldData.Total = dData.Total;
                                oldData.Qualified = dData.Qualified;
                                oldData.Unqualified = dData.Unqualified;
                            }
                        }
                    }
                    #endregion
                }

                var now = DateTime.Now;
                var add = sps.Where(x => !x.Old);
                if (add.Any())
                {
                    StatisticProcessHelper.Add(add.Select(x =>
                    {
                        x.MarkedDateTime = now;
                        return x;
                    }));
                }
                var update = sps.Where(x => x.Old && x.Update);
                if (update.Any())
                {
                    StatisticProcessHelper.Update(update.Select(x =>
                    {
                        x.MarkedDateTime = now;
                        return x;
                    }));
                }
                if (notExist.Any())
                {
                    StatisticProcessHelper.Delete(notExist);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
        #endregion
    }
}
