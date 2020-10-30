using ApiManagement.Base.Server;
using ApiManagement.Models._6sModel;
using ApiManagement.Models.DeviceSpotCheckModel;
using ApiManagement.Models.ManufactureModel;
using ApiManagement.Models.MaterialManagementModel;
using ApiManagement.Models.OtherModel;
using ApiManagement.Models.RepairManagementModel;
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
using System.Web;

namespace ApiManagement.Base.Helper
{
    public class TimerHelper
    {
        private static readonly string Debug = "Debug";
        private static string _url = ServerConfig.ErpUrl;
        private static string _urlFile = "http://192.168.1.100/lc/uploads/";
        private static string _createUserId = "ErpSystem";
        private static Timer _totalTimer;
        public static void Init()
        {
            if (!ServerConfig.RedisHelper.Exists(Debug))
            {
                ServerConfig.RedisHelper.SetForever(Debug, 0);
            }
#if DEBUG
            _totalTimer = new Timer(DoSthTest, null, 5000, 1000 * 60 * 1);
            Console.WriteLine("TimerHelper 调试模式已开启");
#else
            _totalTimer = new Timer(DoSth, null, 5000, 1000 * 10 * 1);
            Console.WriteLine("TimerHelper 发布模式已开启");
#endif
        }

        private static void DoSth(object state)
        {

#if !DEBUG
            if (ServerConfig.RedisHelper.Get<int>("Debug") != 0)
            {
                return;
            }
#endif
            //Log.Debug("GetErpDepartment 调试模式已开启");
            GetErpDepartment();
            //Log.Debug("GetErpPurchase 调试模式已开启");
            GetErpPurchase();
            //Log.Debug("GetErpValuer 调试模式已开启");
            GetErpValuer();
            //Log.Debug("CheckSpotCheckDevice 调试模式已开启");
            CheckSpotCheckDevice();
            //Log.Debug("CheckManufacturePlan 调试模式已开启");
            CheckManufacturePlan();
            //Log.Debug("Check_6sItem 调试模式已开启");
            Check_6sItem();
            //Log.Debug("DayBalanceRecovery 调试模式已开启");
            DayBalanceRecovery();
            //Log.Debug("GetDayBalance 调试模式已开启");
            GetDayBalance();
            //Log.Debug("MaterialRecovery 调试模式已开启");
            MaterialRecovery();
            AccountHelper.CheckAccount();
            MaintainerSchedule();
        }

        private static void DoSthTest(object state)
        {

#if !DEBUG
            if (ServerConfig.RedisHelper.Get<int>("Debug") != 0)
            {
                return;
            }
#endif
            //Log.Debug("GetErpDepartment 调试模式已开启");
            GetErpDepartment();
            //Log.Debug("GetErpPurchase 调试模式已开启");
            GetErpPurchase();
            //Log.Debug("GetErpValuer 调试模式已开启");
            GetErpValuer();
            //Log.Debug("CheckSpotCheckDevice 调试模式已开启");
            CheckSpotCheckDevice();
            //Log.Debug("CheckManufacturePlan 调试模式已开启");
            CheckManufacturePlan();
            //Log.Debug("Check_6sItem 调试模式已开启");
            Check_6sItem();
            //Log.Debug("DayBalanceRecovery 调试模式已开启");
            DayBalanceRecovery();
            //Log.Debug("GetDayBalance 调试模式已开启");
            GetDayBalance();
            //Log.Debug("MaterialRecovery 调试模式已开启");
            MaterialRecovery();
            AccountHelper.CheckAccount();
            MaintainerSchedule();
        }

        /// <summary>
        /// 检查点检设备的点检安排
        /// </summary>
        private static void CheckSpotCheckDevice()
        {
            var checkPlanPre = "CheckPlan";
            var checkPlanLock = $"{checkPlanPre}:Lock";
            if (ServerConfig.RedisHelper.SetIfNotExist(checkPlanLock, "lock"))
            {
                try
                {
                    ServerConfig.RedisHelper.SetExpireAt(checkPlanLock, DateTime.Now.AddMinutes(5));
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
                                    time = detail.PlannedTime.Date.AddDays(detail.Day).AddMonths(detail.Month).AddHours(detail.NormalHour);
                                    break;
                                case IntervalEnum.Week:
                                    time = detail.PlannedTime.Date.AddDays(detail.Week * 7).AddHours(detail.WeekHour);
                                    break;
                            }

                            detail.PlannedTime = time;
                            detail.LogId = 0;
                        }

                        ServerConfig.ApiDb.Execute("UPDATE `spot_check_device` SET `MarkedDateTime`= @MarkedDateTime, `PlannedTime` = @PlannedTime, `LogId` = @LogId WHERE `Id` = @Id;", details);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                ServerConfig.RedisHelper.Remove(checkPlanLock);
            }
        }

        /// <summary>
        /// 检查计划管理的计划状态
        /// </summary>
        private static void CheckManufacturePlan()
        {
            var manufacturePlanPre = "CheckManufacturePlan";
            var manufacturePlanLock = $"{manufacturePlanPre}:Lock";
            if (ServerConfig.RedisHelper.SetIfNotExist(manufacturePlanLock, "lock"))
            {
                ServerConfig.RedisHelper.SetExpireAt(manufacturePlanLock, DateTime.Now.AddMinutes(5));
                try
                {
                    var sql = "SELECT a.*, IFNULL(b.Sum, 0) Sum FROM `manufacture_plan` a " +
                              "LEFT JOIN (SELECT PlanId, SUM(1) Sum FROM `manufacture_plan_task` WHERE MarkedDelete = 0 AND State NOT IN @state GROUP BY PlanId) b ON a.Id = b.PlanId WHERE MarkedDelete = 0;";
                    var plans = ServerConfig.ApiDb.Query<ManufacturePlanCondition>(sql, new { state = new[] { ManufacturePlanTaskState.Done, ManufacturePlanTaskState.Stop } });
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
                            ServerConfig.ApiDb.Execute("UPDATE `manufacture_plan` SET `State`= @State WHERE `Id` = @Id;", plans);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                ServerConfig.RedisHelper.Remove(manufacturePlanLock);
            }
        }

        private static void Check_6sItem()
        {
            var _6sPre = "6s";
            var _6sLock = $"{_6sPre}:Lock";
            if (ServerConfig.RedisHelper.SetIfNotExist(_6sLock, "lock"))
            {
                try
                {
                    ServerConfig.RedisHelper.SetExpireAt(_6sLock, DateTime.Now.AddMinutes(5));
                    Init_6sItem();
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                ServerConfig.RedisHelper.Remove(_6sLock);
            }
        }

        private static void Init_6sItem(int groupId = 0)
        {
            var sql = "SELECT a.*, b.`Group`, b.SurveyorId SurveyorIdSet FROM `6s_item` a JOIN `6s_group` b ON a.GroupId = b.Id " +
                      $"WHERE a.MarkedDelete = 0 AND b.MarkedDelete = 0 AND a.Enable = 1{(groupId == 0 ? "" : " AND a.GroupId= @groupId")}";
            var _6sItems = ServerConfig.ApiDb.Query<_6sItemPeriod>(sql, new { groupId });

            if (_6sItems.Any())
            {
                var today = DateTime.Today;
                var now = DateTime.Now;
                var logs = new List<_6sLog>();
                var old_6sItems = _6sItems.Where(x => x.LastCreateTime != default(DateTime) && x.LastCreateTime < today);
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

                ServerConfig.ApiDb.Execute("UPDATE 6s_item SET `LastCreateTime` = @LastCreateTime WHERE `Id` = @Id;;", _6sItems);
            }
        }

        /// <summary>
        /// 获取请购单和请购列表
        /// </summary>
        private static void GetErpDepartment()
        {
            var _pre = "GetErpDepartment";
            var redisLock = $"{_pre}:Lock";
            if (ServerConfig.RedisHelper.SetIfNotExist(redisLock, DateTime.Now.ToStr()))
            {
                try
                {
                    ServerConfig.RedisHelper.SetExpireAt(redisLock, DateTime.Now.AddMinutes(5));
                    var f = HttpServer.Get(_url, new Dictionary<string, string>
                    {
                        { "type", "getDepartment" },
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
                ServerConfig.RedisHelper.Remove(redisLock);
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
            if (ServerConfig.RedisHelper.SetIfNotExist(redisLock, DateTime.Now.ToStr()))
            {
                try
                {
                    ServerConfig.RedisHelper.SetExpireAt(redisLock, DateTime.Now.AddMinutes(5));
                    var f = HttpServer.Get(_url, new Dictionary<string, string>
                    {
                        { "type", "getHjry" },
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
                            ServerConfig.ApiDb.Query<MaterialValuer>("SELECT * FROM `material_valuer` WHERE MarkedDelete = 0;");

                        var notExistValuers = res.Where(x => haveValuers.All(y => y.Valuer != x)).Where(z => !z.IsNullOrEmpty());

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
                ServerConfig.RedisHelper.Remove(redisLock);
            }
        }

        private static void GetErpPurchase()
        {
            var _pre = "GetErpPurchase";
            var redisLock = $"{_pre}:Lock";
            if (ServerConfig.RedisHelper.SetIfNotExist(redisLock, DateTime.Now.ToStr()))
            {
                ServerConfig.RedisHelper.SetExpireAt(redisLock, DateTime.Now.AddMinutes(10));
                try
                {
                    var validState = new List<MaterialPurchaseStateEnum>
                    {
                        MaterialPurchaseStateEnum.正常,
                        //MaterialPurchaseStateEnum.中止,
                        MaterialPurchaseStateEnum.审核完成,
                        MaterialPurchaseStateEnum.开始采购,
                        //MaterialPurchaseStateEnum.订单完成,
                    };
                    var departments =
                        ServerConfig.ApiDb.Query<dynamic>(
                            "SELECT a.Id, a.Department, IFNULL(MIN(b.ErpId), 0) ErpId FROM `material_department` a " +
                            "LEFT JOIN (SELECT * FROM `material_purchase` WHERE State IN @validState AND MarkedDelete = 0) b ON a.Id = b.DepartmentId WHERE a.Get = 1 AND a.MarkedDelete = 0 GROUP BY a.Id",
                            new { validState });

                    var haveDepartments =
                        ServerConfig.ApiDb.Query<MaterialDepartment>(
                            "SELECT * FROM `material_department` WHERE MarkedDelete = 0;");
                    foreach (var department in departments)
                    {
                        var f = HttpServer.Get(_url, new Dictionary<string, string>
                        {
                            { "type", "getPurchase" },
                            { "department", department.Department },
                            { "id", (department.ErpId-1).ToString() },
                        });
                        if (f == "fail")
                        {
                            Log.ErrorFormat("GetErpPurchase 请求erp部门请购单数据失败,url:{0}", _url);
                        }
                        else
                        {
                            var now = DateTime.Now;
                            var rr = HttpUtility.UrlDecode(f);
                            var res = JsonConvert.DeserializeObject<IEnumerable<ErpPurchase>>(rr).OrderBy(x => x.f_id);
                            var updatePurchaseItems = new List<MaterialPurchaseItem>();
                            var addPurchaseItems = new List<MaterialPurchaseItem>();
                            var updateMaterialBill = new List<MaterialBill>();
                            //var bz = res.GroupBy(x => x.f_bz).Select(y => y.Key).Join();
                            //var zt = res.GroupBy(x => x.f_zt).Select(y => y.Key).Join();
                            var havePurchases =
                                ServerConfig.ApiDb.Query<MaterialPurchase>(
                                    "SELECT * FROM `material_purchase` WHERE DepartmentId = @Id AND ErpId >= @ErpId AND MarkedDelete = 0;",
                                    new { department.Id, department.ErpId });
                            var existPurchases = res.Where(x => havePurchases.Any(y => y.ErpId == x.f_id));
                            if (existPurchases.Any())
                            {
                                var havePurchaseItems = ServerConfig.ApiDb.Query<MaterialPurchaseItem>(
                                    "SELECT * FROM `material_purchase_item` WHERE MarkedDelete = 0 AND PurchaseId IN @PurchaseId;",
                                    new { PurchaseId = havePurchases.Where(x => existPurchases.Any(y => y.f_id == x.ErpId)).Select(z => z.Id) });
                                var purchases = new List<MaterialPurchase>();
                                foreach (var p in existPurchases)
                                {
                                    var dep = haveDepartments.FirstOrDefault(x => x.Department == p.f_bm);
                                    if (dep == null)
                                    {
                                        continue;
                                    }

                                    if (!EnumHelper.TryParseStr(p.f_zt, out MaterialPurchaseStateEnum state, true))
                                    {
                                        continue;
                                    }

                                    if (!EnumHelper.TryParseStr(p.f_dj, out MaterialPurchasePriorityEnum priority, true))
                                    {
                                        continue;
                                    }
                                    var purchase = new MaterialPurchase(dep.Id, p, _createUserId, now, state, priority);
                                    var existPurchase = havePurchases.FirstOrDefault(x => x.ErpId == p.f_id);
                                    if (existPurchase != null)
                                    {
                                        purchase.Id = existPurchase.Id;
                                        if (ClassExtension.HaveChange(purchase, existPurchase) && existPurchase.State != MaterialPurchaseStateEnum.订单完成)
                                        {
                                            purchases.Add(purchase);
                                        }

                                        if (p.goods == null)
                                        {
                                            continue;
                                        }
                                        var existPurchaseItems = havePurchaseItems.Where(x => x.PurchaseId == purchase.Id);
                                        var existPurchaseItemsStock = existPurchaseItems.Where(y => y.Stock == 0);
                                        if (purchase.State == MaterialPurchaseStateEnum.撤销 && existPurchase.State != MaterialPurchaseStateEnum.撤销)
                                        {
                                            //删除
                                            updatePurchaseItems.AddRange(existPurchaseItemsStock.Select(z =>
                                            //updatePurchaseItems.AddRange(existPurchaseItems.Select(z =>
                                            {
                                                z.MarkedDateTime = now;
                                                z.MarkedDelete = true;
                                                return z;
                                            }));
                                        }
                                        else
                                        {
                                            IEnumerable<MaterialPurchaseItem> l;
                                            var erpPurchaseItems = p.goods.Select(good =>
                                                new MaterialPurchaseItem(purchase.Id, good, _createUserId, now, _urlFile));

                                            if (p.goods.Any(x => x.f_id == 0))
                                            {
                                                if (erpPurchaseItems.Any())
                                                {
                                                    var wlCode = erpPurchaseItems.GroupBy(x => x.Code).Select(y => y.Key);

                                                    //删除不存在的code
                                                    l = existPurchaseItemsStock
                                                        .Where(x => wlCode.Any(y => y != x.Code)).Select(z =>
                                                        {
                                                            z.MarkedDateTime = now;
                                                            z.MarkedDelete = true;
                                                            return z;
                                                        });
                                                    if (l.Any())
                                                    {
                                                        updatePurchaseItems.AddRange(l.Select(x =>
                                                        {
                                                            x.CreateUserId = _createUserId + "608";
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
                                                            var myCode = myCodes.FirstOrDefault(x => x.ErpId != 0 && x.ErpId == erpCode.ErpId);
                                                            if (myCode == null)
                                                            {
                                                                myCode = myCodes.FirstOrDefault(x => x.ErpId == 0 && x.Number == erpCode.Number);
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
                                                                        erpCode.CreateUserId = _createUserId + "647";
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
                                                                    erpCode.CreateUserId = _createUserId + "659";
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
                                                                    erpCode.CreateUserId = _createUserId + "671";
                                                                    updatePurchaseItems.Add(erpCode);
                                                                }
                                                            }
                                                        }

                                                        //删除多余的的ErpId
                                                        l = myCodes
                                                            .Where(x => myDic.Where(md => !md.Value).Any(y => myCodes.IndexOf(x) == y.Key)).Select(z =>
                                                            {
                                                                z.MarkedDateTime = now;
                                                                z.MarkedDelete = true;
                                                                return z;
                                                            });
                                                        if (l.Any())
                                                        {
                                                            updatePurchaseItems.AddRange(l.Select(x =>
                                                            {
                                                                x.CreateUserId = _createUserId + "689";
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
                                                        z.MarkedDateTime = now;
                                                        z.MarkedDelete = true;
                                                        return z;
                                                    });
                                                if (l.Any())
                                                {
                                                    updatePurchaseItems.AddRange(l.Select(x =>
                                                    {
                                                        x.CreateUserId = _createUserId + "710";
                                                        return x;
                                                    }));
                                                }

                                                //删除存在的code但是请购id变了
                                                var idChange = existPurchaseItems.Where(x =>
                                                {
                                                    var erpPurchaseItem = erpPurchaseItems.FirstOrDefault(y => y.IsSame(x) && y.ErpId == x.ErpId);
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
                                                        z.MarkedDateTime = now;
                                                        z.MarkedDelete = true;
                                                        return z;
                                                    });
                                                    updatePurchaseItems.AddRange(s.Select(x =>
                                                    {
                                                        x.CreateUserId = _createUserId + "727";
                                                        return x;
                                                    }));
                                                }

                                                //更新
                                                var existGoods = p.goods.Where(x => existPurchaseItems.Any(y => y.ErpId == x.f_id));
                                                foreach (var good in existGoods)
                                                {
                                                    var g = new MaterialPurchaseItem(purchase.Id, good, _createUserId, now, _urlFile);
                                                    var existGood = existPurchaseItems.FirstOrDefault(x => x.ErpId == good.f_id);
                                                    var change = idChange.FirstOrDefault(x => x.Code == good.f_wlbm);
                                                    if (existGood != null)
                                                    {
                                                        g.Id = existGood.Id;
                                                        g.PurchaseId = existGood.PurchaseId;
                                                        if (ClassExtension.HaveChange(g, existGood))
                                                        {
                                                            g.CreateUserId = _createUserId + "745";
                                                            updatePurchaseItems.Add(g);
                                                        }
                                                        if (change != null && change.BillId > 0)
                                                        {
                                                            g.Stock = change.Stock;
                                                            g.BillId = change.BillId;
                                                            if (g.Price != change.Price)
                                                            {
                                                                updateMaterialBill.Add(new MaterialBill
                                                                {
                                                                    Id = g.BillId,
                                                                    MarkedDateTime = now,
                                                                    Price = g.Price
                                                                });
                                                            }
                                                            g.CreateUserId = _createUserId + "761";
                                                            updatePurchaseItems.Add(g);
                                                        }
                                                    }
                                                }
                                                //新增
                                                l = p.goods.Where(x => existPurchaseItems.All(y => y.ErpId != x.f_id))
                                                    .Select(z => new MaterialPurchaseItem(purchase.Id, z, _createUserId, now, _urlFile));
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
                                                                ll.BillId = change.BillId;
                                                                if (ll.Price != change.Price)
                                                                {
                                                                    updateMaterialBill.Add(new MaterialBill
                                                                    {
                                                                        Id = ll.BillId,
                                                                        MarkedDateTime = now,
                                                                        Price = ll.Price
                                                                    });
                                                                }
                                                            }
                                                            ll.CreateUserId = _createUserId + "790";
                                                            addPurchaseItems.Add(ll);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        addPurchaseItems.AddRange(l.Select(x =>
                                                        {
                                                            x.CreateUserId = _createUserId + "798";
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
                                        "`State` = @State, `IsDesign` = @IsDesign, `Priority` = @Priority WHERE `Id` = @Id;", purchases);
                                }
                            }

                            var notExistPurchases = res.Where(x => havePurchases.All(y => y.ErpId != x.f_id));
                            if (notExistPurchases.Any())
                            {
                                var purchases = new List<MaterialPurchase>();
                                foreach (var p in notExistPurchases)
                                {
                                    var dep = haveDepartments.FirstOrDefault(x => x.Department == p.f_bm);
                                    if (dep == null)
                                    {
                                        continue;
                                    }

                                    if (!EnumHelper.TryParseStr(p.f_zt, out MaterialPurchaseStateEnum state, true))
                                    {
                                        continue;
                                    }

                                    if (!EnumHelper.TryParseStr(p.f_dj, out MaterialPurchasePriorityEnum priority, true))
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
                                    "VALUES (@CreateUserId, @MarkedDateTime, @Time, @IsErp, @ErpId, @DepartmentId, @Purchase, @Number, @Name, @Valuer, @Step, @State, @IsDesign, @Priority);", purchases);

                                    havePurchases =
                                        ServerConfig.ApiDb.Query<MaterialPurchase>(
                                            "SELECT * FROM `material_purchase` WHERE DepartmentId = @Id AND ErpId >= @ErpId AND MarkedDelete = 0;", new { department.Id, department.ErpId });

                                    var validNotExistPurchases = res.Where(x => purchases.Any(y => y.ErpId == x.f_id));
                                    foreach (var p in validNotExistPurchases)
                                    {
                                        var purchase = havePurchases.FirstOrDefault(x => x.ErpId == p.f_id);

                                        //新增
                                        var l = p.goods.Select(good => new MaterialPurchaseItem(purchase.Id, good, _createUserId + "3", now, _urlFile));
                                        if (l.Any())
                                        {
                                            addPurchaseItems.AddRange(l.Select(x =>
                                            {
                                                x.CreateUserId = _createUserId + "863";
                                                return x;
                                            }));
                                        }
                                    }
                                }
                            }

                            if (updateMaterialBill.Any())
                            {
                                ServerConfig.ApiDb.Execute(
                                    "UPDATE `material_bill` SET `MarkedDateTime` = @MarkedDateTime, `Price` = @Price WHERE `Id` = @Id;",
                                    updateMaterialBill);
                            }

                            if (updatePurchaseItems.Any())
                            {
                                ServerConfig.ApiDb.Execute(
                                    "UPDATE `material_purchase_item` SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `Class` = @Class, `Category` = @Category, `Name` = @Name, `Supplier` = @Supplier, `SupplierFull` = @SupplierFull, " +
                                    "`Specification` = @Specification, `Number` = @Number, `Unit` = @Unit, `Remark` = @Remark, `Purchaser` = @Purchaser, `PurchasingCompany` = @PurchasingCompany, `Order` = @Order, " +
                                    "`EstimatedTime` = @EstimatedTime, `ArrivalTime` = @ArrivalTime, `File` = @File, `FileUrl` = @FileUrl, `IsInspection` = @IsInspection, " +
                                    "`Currency` = @Currency, `Payment` = @Payment, `Transaction` = @Transaction, `Invoice` = @Invoice, `TaxPrice` = @TaxPrice, `TaxAmount` = @TaxAmount, " +
                                    "`Price` = @Price, `Stock` = IF(@BillId > 0, @Stock, `Stock`), `BillId` = IF(@BillId > 0, @BillId, `BillId`) WHERE `Id` = @Id;", updatePurchaseItems);
                            }

                            if (addPurchaseItems.Any())
                            {
                                ServerConfig.ApiDb.Execute(
                                    "INSERT INTO `material_purchase_item` (`CreateUserId`, `MarkedDateTime`, `Time`, `IsErp`, `ErpId`, `PurchaseId`, `Code`, `Class`, `Category`, `Name`, `Supplier`, `SupplierFull`, `Specification`, `Number`, `Unit`, `Remark`, `Purchaser`, `PurchasingCompany`, `Order`, `EstimatedTime`, `ArrivalTime`, `File`, `FileUrl`, `IsInspection`, `Currency`, `Payment`, `Transaction`, `Invoice`, `TaxPrice`, `TaxAmount`, `Price`, `Stock`, `BillId`) " +
                                    "VALUES (@CreateUserId, @MarkedDateTime, @Time, @IsErp, @ErpId, @PurchaseId, @Code, @Class, @Category, @Name, @Supplier, @SupplierFull, @Specification, @Number, @Unit, @Remark, @Purchaser, @PurchasingCompany, @Order, @EstimatedTime, @ArrivalTime, @File, @FileUrl, @IsInspection, @Currency, @Payment, @Transaction, @Invoice, @TaxPrice, @TaxAmount, @Price, @Stock, @BillId);",
                                    addPurchaseItems);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                ServerConfig.RedisHelper.Remove(redisLock);
                //ServerConfig.RedisHelper.SetExpireAt(redisLock, DateTime.Now.AddMinutes(2));
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

        /// <summary>
        /// 获取每日库存结存
        /// </summary>
        private static void GetDayBalance()
        {
            var _pre = "GetDayBalance";
            var redisLock = $"{_pre}:Lock";
            var timeKey = $"{_pre}:Time";
            if (ServerConfig.RedisHelper.SetIfNotExist(redisLock, DateTime.Now.ToStr()))
            {
                try
                {
                    ServerConfig.RedisHelper.SetExpireAt(redisLock, DateTime.Now.AddMinutes(5 * 12));
                    var now = DateTime.Now;
                    if (!ServerConfig.RedisHelper.Exists(timeKey))
                    {
                        ServerConfig.RedisHelper.SetForever(timeKey, now.ToStr());
                    }

                    var calTime = ServerConfig.RedisHelper.Get<DateTime>(timeKey);
                    calTime = calTime == default(DateTime) ? now.Date : calTime.Date;
                    if (!now.InSameDay(calTime))
                    {
                        calTime = now.AddDays(-1).DayEndTime();
                    }

                    var yesterday = calTime.AddDays(-1);
                    var tomorrow = calTime.AddDays(1);
                    var materialStatistics = ServerConfig.ApiDb.Query<MaterialStatistic>(
                        "SELECT a.`Id` BillId, a.`Code`, b.`CategoryId`, b.`Category`, b.`NameId`, b.`Name`, b.`SupplierId`, b.`Supplier`, a.`SpecificationId`, b.`Specification`, a.`SiteId`, d.Site, " +
                        "a.`Unit`, a.`Stock`, IFNULL(c.Number, 0) TodayNumber, a.`Price` TodayPrice, IFNULL(c.Number, 0) * a.`Price` TodayAmount FROM `material_bill` a JOIN (SELECT a.*, b.CategoryId, " +
                        "b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a JOIN (SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM `material_supplier` a JOIN (SELECT a.*, " +
                        "b.Category FROM `material_name` a JOIN `material_category` b ON a.CategoryId = b.Id) b ON a.NameId = b.Id) b ON a.SupplierId = b.Id) b ON a.SpecificationId = b.Id LEFT JOIN " +
                        //"`material_management` c ON a.Id = c.BillId JOIN `material_site` d ON a.SiteId = d.Id WHERE a.`MarkedDelete` = 0 AND IFNULL(c.Number, 0) > 0 ORDER BY a.Id;").ToList();
                        "`material_management` c ON a.Id = c.BillId JOIN `material_site` d ON a.SiteId = d.Id WHERE a.`MarkedDelete` = 0 ORDER BY a.Id;").ToList();

                    //昨日单价数量总价
                    var yesterdayMaterialStatistics = ServerConfig.ApiDb.Query<MaterialStatistic>(
                        "SELECT BillId, TodayNumber, TodayPrice, TodayAmount FROM `material_balance` WHERE Time = @Time ORDER BY BillId;",
                        new { Time = yesterday });

                    IEnumerable<int> notExist;
                    if (yesterdayMaterialStatistics.Any())
                    {
                        notExist = yesterdayMaterialStatistics.Where(x => materialStatistics.All(y => y.BillId != x.BillId)).Select(z => z.BillId);
                        if (notExist.Any())
                        {
                            materialStatistics.AddRange(ServerConfig.ApiDb.Query<MaterialStatistic>(
                                "SELECT a.`Id` BillId, a.`Code`, b.`CategoryId`, b.`Category`, b.`NameId`, b.`Name`, b.`SupplierId`, b.`Supplier`, a.`SpecificationId`, b.`Specification`, a.`SiteId`, d.Site, a.`Unit`, a.`Stock`, IFNULL(c.Number, 0) TodayNumber, a.`Price` TodayPrice, IFNULL(c.Number, 0) * a.`Price` TodayAmount FROM `material_bill` a JOIN (SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a JOIN (SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM `material_supplier` a JOIN (SELECT a.*, b.Category FROM `material_name` a JOIN `material_category` b ON a.CategoryId = b.Id) b ON a.NameId = b.Id) b ON a.SupplierId = b.Id) b ON a.SpecificationId = b.Id LEFT JOIN `material_management` c ON a.Id = c.BillId JOIN `material_site` d ON a.SiteId = d.Id WHERE a.Id IN @Id ORDER BY a.Id;",
                                new { Id = notExist }));
                        }
                        foreach (var ne in yesterdayMaterialStatistics)
                        {
                            var bill = materialStatistics.FirstOrDefault(x => x.BillId == ne.BillId);
                            if (bill != null)
                            {
                                bill.LastNumber = ne.TodayNumber;
                                bill.LastPrice = ne.TodayPrice;
                                bill.LastAmount = ne.TodayAmount;
                            }
                        }
                    }

                    //今日入库领用
                    var todayMaterialStatistics = ServerConfig.ApiDb.Query<MaterialStatistic>(
                       "SELECT a.BillId, SUM(IF(a.Type = 1, a.Number, 0)) Increase,  SUM(IF(a.Type = 2, a.Number, 0)) Consume, SUM(IF(a.Type = 3 AND `Mode` = 0, a.Number, 0)) CorrectIn, " +
                       "SUM(IF(a.Type = 3 AND `Mode` = 1, a.Number, 0)) CorrectCon, SUM(IF(a.Type = 3, a.Number, 0)) Correct FROM material_log a JOIN material_bill b ON a.BillId = b.Id " +
                       "WHERE DATE(Time) = @Time GROUP BY a.BillId;",
                       new { Time = calTime });

                    if (todayMaterialStatistics.Any())
                    {
                        notExist = todayMaterialStatistics.Where(x => materialStatistics.All(y => y.BillId != x.BillId)).Select(z => z.BillId);
                        if (notExist.Any())
                        {
                            materialStatistics.AddRange(ServerConfig.ApiDb.Query<MaterialStatistic>(
                                "SELECT a.`Id` BillId, a.`Code`, b.`CategoryId`, b.`Category`, b.`NameId`, b.`Name`, b.`SupplierId`, b.`Supplier`, a.`SpecificationId`, b.`Specification`, a.`SiteId`, d.Site, a.`Unit`, a.`Stock`, IFNULL(c.Number, 0) TodayNumber, a.`Price` TodayPrice, IFNULL(c.Number, 0) * a.`Price` TodayAmount FROM `material_bill` a JOIN (SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a JOIN (SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM `material_supplier` a JOIN (SELECT a.*, b.Category FROM `material_name` a JOIN `material_category` b ON a.CategoryId = b.Id) b ON a.NameId = b.Id) b ON a.SupplierId = b.Id) b ON a.SpecificationId = b.Id LEFT JOIN `material_management` c ON a.Id = c.BillId JOIN `material_site` d ON a.SiteId = d.Id WHERE a.Id IN @Id ORDER BY a.Id;",
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

                    var oldValid = ServerConfig.ApiDb.Query<int>(
                        "SELECT BillId FROM `material_balance` WHERE Time = @Time AND TodayNumber + LastNumber + Increase + Consume + Increase + Consume + CorrectIn + CorrectCon + Correct != 0;",
                        new { Time = calTime });

                    var valid = materialStatistics.Where(x => x.Valid() || oldValid.Contains(x.BillId));
                    ServerConfig.ApiDb.Execute(
                        "INSERT INTO `material_balance` (`Time`, `Code`, `BillId`, `CategoryId`, `Category`, `NameId`, `Name`, `SupplierId`, `Supplier`, `SpecificationId`, " +
                        "`Specification`, `SiteId`, `Site`, `Unit`, `Stock`, `LastNumber`, `LastPrice`, `LastAmount`, `TodayNumber`, `TodayPrice`, " +
                        "`TodayAmount`, `Increase`, `IncreaseAmount`, `Consume`, `ConsumeAmount`, `CorrectIn`, `CorrectInAmount`, `CorrectCon`, `CorrectConAmount`, " +
                        "`Correct`, `CorrectAmount`) " +
                        "VALUES (@Time, @Code, @BillId, @CategoryId, @Category, @NameId, @Name, @SupplierId, @Supplier, @SpecificationId, @Specification, " +
                        "@SiteId, @Site, @Unit, @Stock, @LastNumber, @LastPrice, @LastAmount, @TodayNumber, @TodayPrice, " +
                        "@TodayAmount, @Increase, @IncreaseAmount, @Consume, @ConsumeAmount, @CorrectIn, @CorrectInAmount, @CorrectCon, @CorrectConAmount, " +
                        "@Correct, @CorrectAmount) " +
                        "ON DUPLICATE KEY UPDATE " +
                        "`Code` = @Code, `CategoryId` = @CategoryId, `Category` = @Category, `NameId` = @NameId, `Name` = @Name, `SupplierId` = @SupplierId, " +
                        "`Supplier` = @Supplier, `SpecificationId` = @SpecificationId, `Specification` = @Specification, `SiteId` = @SiteId, `Site` = @Site, `Unit` = @Unit, " +
                        "`Stock` = @Stock, `LastNumber` = @LastNumber, `LastPrice` = @LastPrice, `LastAmount` = @LastAmount, `TodayNumber` = @TodayNumber, " +
                        "`TodayPrice` = @TodayPrice, `TodayAmount` = @TodayAmount, `Increase` = @Increase, `IncreaseAmount` = @IncreaseAmount, `Consume` = @Consume, " +
                        "`ConsumeAmount` = @ConsumeAmount, `CorrectIn` = @CorrectIn, `CorrectInAmount` = @CorrectInAmount, `CorrectCon` = @CorrectCon, `CorrectConAmount` = @CorrectConAmount, " +
                        "`Correct` = @Correct, `CorrectAmount` = @CorrectAmount", valid.Select(x =>
                        {
                            x.Time = calTime;
                            return x;
                        }));

                    //ServerConfig.ApiDb.Execute(
                    //    "UPDATE `material_balance` SET `LastNumber` = @TodayNumber, `LastPrice` = @TodayPrice, `LastAmount` = @TodayAmount WHERE `Time` = @Time AND `BillId` = @BillId;",
                    //    materialStatistics.Select(x =>
                    //    {
                    //        x.Time = tomorrow;
                    //        return x;
                    //    }));
                    ServerConfig.ApiDb.Execute("DELETE FROM `material_balance` WHERE TodayNumber = 0 AND LastNumber = 0 AND Increase = 0 AND Consume = 0 AND Increase = 0 AND Consume = 0 AND CorrectIn = 0 AND CorrectCon = 0 AND Correct = 0;");

                    ServerConfig.RedisHelper.SetForever(timeKey, now.ToStr());
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                ServerConfig.RedisHelper.Remove(redisLock);
            }
        }

        /// <summary>
        /// 获取每日库存结存
        /// </summary>
        public static void DayBalance(IEnumerable<DateTime> calTimes)
        {
            if (calTimes != null && calTimes.Any())
            {
                ServerConfig.ApiDb.Execute("DELETE FROM `material_balance` WHERE Time IN @Time;", new { Time = calTimes.Select(x => x.Date) });
            }
        }

        private static bool _runing = false;
        /// <summary>
        /// 日库存结存老数据恢复
        /// </summary>
        private static void DayBalanceRecovery()
        {
            var _pre = "DayBalanceRecovery";
            var redisLock = $"{_pre}:Lock";
            var _pre1 = "GetDayBalance";
            var timeKey = $"{_pre1}:Time";
            if (ServerConfig.RedisHelper.SetIfNotExist(redisLock, DateTime.Now.ToStr()))
            {
                try
                {
                    if (_runing)
                    {
                        ServerConfig.RedisHelper.SetExpireAt(redisLock, DateTime.Now.AddMinutes(10));
                        return;
                    }

                    _runing = true;
                    ServerConfig.RedisHelper.SetExpireAt(redisLock, DateTime.Now.AddMinutes(5 * 12));

                    var minLogDay = ServerConfig.ApiDb.Query<DateTime>("SELECT MIN(Time) FROM `material_log`;")
                        .FirstOrDefault();
                    if (minLogDay == default(DateTime))
                    {
                        ServerConfig.RedisHelper.Remove(redisLock);
                        return;
                    }

                    minLogDay = minLogDay.Date;
                    var balanceDays = ServerConfig.ApiDb.Query<DateTime>("SELECT Time FROM `material_balance` GROUP BY Time ORDER BY Time;");
                    if (!balanceDays.Any())
                    {
                        ServerConfig.RedisHelper.Remove(redisLock);
                        return;
                    }
                    var maxBalanceDay = balanceDays.LastOrDefault().Date;

                    var calTime = ServerConfig.RedisHelper.Get<DateTime>(timeKey);
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

                    timeDic = timeDic.OrderByDescending(x => x.Key).ToDictionary(y => y.Key, y => y.Value);
                    if (timeDic.Any(x => x.Value == 0))
                    {
                        var materialStatistics = new List<MaterialStatistic>();
                        for (var i = 0; i < timeDic.Count; i++)
                        {
                            var time = timeDic.ElementAt(i);
                            //}
                            //foreach (var time in timeDic)
                            //{
                            if (time.Value == 0)
                            {
                                var t = time.Key;
                                var tomorrow = t.AddDays(1);
                                if (timeDic[tomorrow] != 1)
                                {
                                    continue;
                                }
                                materialStatistics.Clear();
                                var tomorrowStatistics = ServerConfig.ApiDb.Query<MaterialStatistic>(
                                    "SELECT * FROM `material_balance` WHERE Time = @tomorrow", new { tomorrow }).ToList();

                                IEnumerable<int> notExist;
                                if (tomorrowStatistics.Any())
                                {
                                    materialStatistics.AddRange(tomorrowStatistics.Select(x =>
                                    {
                                        x.Init();
                                        x.Time = t;
                                        return x;
                                    }));
                                    //明日入库领用
                                    var tomorrowLogs = ServerConfig.ApiDb.Query<MaterialLog>("SELECT * FROM `material_log` WHERE Time >= @Time1 AND Time <= @Time2 Order By Time DESC;", new
                                    {
                                        Time1 = tomorrow.DayBeginTime(),
                                        Time2 = tomorrow.InSameDay(calTime) && tomorrow.DayEndTime() > calTime ? calTime : tomorrow.DayEndTime(),
                                    });

                                    notExist = tomorrowLogs.Where(x => materialStatistics.All(y => y.BillId != x.BillId)).Select(z => z.BillId);

                                    if (notExist.Any())
                                    {
                                        materialStatistics.AddRange(ServerConfig.ApiDb.Query<MaterialStatistic>(
                                            "SELECT a.`Id` BillId, a.`Code`, b.`CategoryId`, b.`Category`, b.`NameId`, b.`Name`, b.`SupplierId`, b.`Supplier`, a.`SpecificationId`, b.`Specification`, a.`SiteId`, d.Site, a.`Unit`, a.`Stock`, IFNULL(c.Number, 0) TodayNumber, a.`Price` TodayPrice, IFNULL(c.Number, 0) * a.`Price` TodayAmount FROM `material_bill` a JOIN (SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a JOIN (SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM `material_supplier` a JOIN (SELECT a.*, b.Category FROM `material_name` a JOIN `material_category` b ON a.CategoryId = b.Id) b ON a.NameId = b.Id) b ON a.SupplierId = b.Id) b ON a.SpecificationId = b.Id LEFT JOIN `material_management` c ON a.Id = c.BillId JOIN `material_site` d ON a.SiteId = d.Id WHERE a.Id IN @Id ORDER BY a.Id;",
                                            new { Id = notExist }).Select(x =>
                                            {
                                                x.Time = t;
                                                return x;
                                            }));
                                    }

                                    foreach (var log in tomorrowLogs)
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
                                                material.TodayNumber -= log.Number;
                                                break;
                                            case 2:
                                                material.TodayNumber += log.Number;
                                                break;
                                            case 3:
                                                material.TodayNumber = log.OldNumber;
                                                break;
                                        }
                                        material.TodayAmount = material.TodayPrice * material.TodayNumber;
                                    }
                                }

                                //今日入库领用
                                var todayMaterialStatistics = ServerConfig.ApiDb.Query<MaterialStatistic>(
                                   "SELECT a.BillId, SUM(IF(a.Type = 1, a.Number, 0)) Increase,  SUM(IF(a.Type = 2, a.Number, 0)) Consume, SUM(IF(a.Type = 3 AND `Mode` = 0, a.Number, 0)) CorrectIn, SUM(IF(a.Type = 3 AND `Mode` = 1, a.Number, 0)) CorrectCon, SUM(IF(a.Type = 3, a.Number, 0)) Correct FROM material_log a JOIN material_bill b ON a.BillId = b.Id WHERE DATE(Time) = @Time GROUP BY a.BillId;", new
                                   {
                                       Time = t
                                   });
                                //"SELECT a.BillId, SUM(IF(a.Type = 1, a.Number, 0)) Increase,  SUM(IF(a.Type = 1, a.Number, 0)) * b.Price IncreaseAmount,  SUM(IF(a.Type = 2, a.Number, 0)) Consume, SUM(IF(a.Type = 2, a.Number, 0)) * b.Price ConsumeAmount, SUM(IF(a.Type = 3 AND `Mode` = 0, a.Number, 0)) CorrectIn,  SUM(IF(a.Type = 3 AND `Mode` = 0, a.Number, 0)) * b.Price CorrectInAmount, SUM(IF(a.Type = 3 AND `Mode` = 1, a.Number, 0)) CorrectCon,  SUM(IF(a.Type = 3 AND `Mode` = 1, a.Number, 0)) * b.Price CorrectConAmount, SUM(IF(a.Type = 3, a.Number, 0)) Correct,  SUM(IF(a.Type = 3, a.Number, 0)) * b.Price CorrectAmount FROM material_log a JOIN material_bill b ON a.BillId = b.Id WHERE DATE(Time) = @Time GROUP BY a.BillId;", new { Time = lastDay });
                                notExist = todayMaterialStatistics.Where(x => materialStatistics.All(y => y.BillId != x.BillId)).Select(z => z.BillId);
                                if (notExist.Any())
                                {
                                    materialStatistics.AddRange(ServerConfig.ApiDb.Query<MaterialStatistic>(
                                        "SELECT a.`Id` BillId, a.`Code`, b.`CategoryId`, b.`Category`, b.`NameId`, b.`Name`, b.`SupplierId`, b.`Supplier`, a.`SpecificationId`, b.`Specification`, a.`SiteId`, d.Site, a.`Unit`, a.`Stock`, IFNULL(c.Number, 0) TodayNumber, a.`Price` TodayPrice, IFNULL(c.Number, 0) * a.`Price` TodayAmount FROM `material_bill` a JOIN (SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a JOIN (SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM `material_supplier` a JOIN (SELECT a.*, b.Category FROM `material_name` a JOIN `material_category` b ON a.CategoryId = b.Id) b ON a.NameId = b.Id) b ON a.SupplierId = b.Id) b ON a.SpecificationId = b.Id LEFT JOIN `material_management` c ON a.Id = c.BillId JOIN `material_site` d ON a.SiteId = d.Id WHERE a.Id IN @Id ORDER BY a.Id;",
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

                                var valid = materialStatistics.Where(x => x.Valid());
                                ServerConfig.ApiDb.Execute(
                                    "INSERT INTO `material_balance` (`Time`, `Code`, `BillId`, `CategoryId`, `Category`, `NameId`, `Name`, `SupplierId`, `Supplier`, `SpecificationId`, " +
                                    "`Specification`, `SiteId`, `Site`, `Unit`, `Stock`, `LastNumber`, `LastPrice`, `LastAmount`, `TodayNumber`, `TodayPrice`, " +
                                    "`TodayAmount`, `Increase`, `IncreaseAmount`, `Consume`, `ConsumeAmount`, `CorrectIn`, `CorrectInAmount`, `CorrectCon`, `CorrectConAmount`, " +
                                    "`Correct`, `CorrectAmount`) " +
                                    "VALUES (@Time, @Code, @BillId, @CategoryId, @Category, @NameId, @Name, @SupplierId, @Supplier, @SpecificationId, @Specification, " +
                                    "@SiteId, @Site, @Unit, @Stock, @LastNumber, @LastPrice, @LastAmount, @TodayNumber, @TodayPrice, " +
                                    "@TodayAmount, @Increase, @IncreaseAmount, @Consume, @ConsumeAmount, @CorrectIn, @CorrectInAmount, @CorrectCon, @CorrectConAmount, " +
                                    "@Correct, @CorrectAmount);", valid);

                                ServerConfig.ApiDb.Execute(
                                    "UPDATE `material_balance` SET `LastNumber` = @TodayNumber, `LastPrice` = @TodayPrice, `LastAmount` = @TodayAmount WHERE `Time` = @Time AND `BillId` = @BillId;",
                                    materialStatistics.Select(x =>
                                    {
                                        x.Time = tomorrow;
                                        return x;
                                    }));
                                timeDic[t] = 1;
                            }
                        }
                    }
                    //ServerConfig.ApiDb.Execute("DELETE FROM `material_balance` WHERE TodayNumber + LastNumber + Increase + Consume + Increase + Consume + CorrectIn + CorrectCon + Correct = 0;");

                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                ServerConfig.RedisHelper.Remove(redisLock);
                _runing = false;
            }
        }

        /// <summary>
        /// 库存、日志数据修复
        /// </summary>
        private static void MaterialRecovery()
        {
            var _pre = "MaterialRecovery";
            var redisLock = $"{_pre}:Lock";
            if (ServerConfig.RedisHelper.SetIfNotExist(redisLock, DateTime.Now.ToStr()))
            {
                try
                {
                    var logs = ServerConfig.ApiDb.Query<MaterialLog>("SELECT * FROM `material_log` ORDER BY Time;");
                    if (!logs.Any())
                    {
                        return;
                    }

                    var oldLogs = ServerConfig.ApiDb.Query<MaterialLog>("SELECT * FROM `material_log` ORDER BY Time;");
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
                        newMaterial.ContainsKey(x.BillId) && x.Number != newMaterial[x.BillId].Number).ToDictionary(z => z.BillId);
                    var s = newMaterial.Where(x => changes.ContainsKey(x.Key)).Select(y => y.Value);
                    if (s.Any())
                    {
                        ServerConfig.ApiDb.Execute(
                            "UPDATE `material_management` SET " +
                            "`InTime` = IF(ISNULL(`InTime`) OR `InTime` != @InTime, @InTime, `InTime`), " +
                            "`OutTime` = IF(ISNULL(`OutTime`) OR `OutTime` != @OutTime, @OutTime, `OutTime`), " +
                            "`Number` = @Number WHERE `BillId` = @BillId;",
                            s);
                    }

                    var changeLogs = logs.Where(x =>
                        oldLogs.Any(y => y.Id == x.Id) && oldLogs.First(y => y.Id == x.Id).OldNumber != x.OldNumber);
                    if (changeLogs.Any())
                    {
                        ServerConfig.ApiDb.Execute("UPDATE `material_log` SET `OldNumber` = @OldNumber WHERE `Id` = @Id;", logs);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                ServerConfig.RedisHelper.Remove(redisLock);
            }
        }

        /// <summary>
        /// 维修工自动排班
        /// </summary>
        private static void MaintainerSchedule()
        {
            var _pre = "MaintainerSchedule";
            var redisLock = $"{_pre}:Lock";
            if (ServerConfig.RedisHelper.SetIfNotExist(redisLock, DateTime.Now.ToStr()))
            {
                ServerConfig.RedisHelper.SetExpireAt(redisLock, DateTime.Now.AddMinutes(5 * 12));
                DoMaintainerSchedule();
                ServerConfig.RedisHelper.Remove(redisLock);
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
                var lastWeekNightMaintainerId = ServerConfig.RedisHelper.Get<int>(lastWeekNightKey);
                if (lastWeekNightMaintainerId == 0)
                {
                    lastWeekNightMaintainerId = schedules.FirstOrDefault(x => (x.StartTime >= lastWeekEnd &&
                                                                               x.StartTime < lastWeekEnd.AddSeconds(GlobalConfig.Morning.TotalSeconds)))?.MaintainerId ?? 0;
                }

                //本周夜班 = 上周日夜班 20 - 24
                var thisWeekNightMaintainerId = ServerConfig.RedisHelper.Get<int>(thisWeekNightKey);
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


                ServerConfig.RedisHelper.SetForever(lastWeekNightKey, thisWeekNightMaintainerId);
                ServerConfig.RedisHelper.SetForever(thisWeekNightKey, nextWeekNightMaintainerId);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
    }
}
