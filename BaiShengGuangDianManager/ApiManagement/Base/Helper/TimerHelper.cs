﻿using ApiManagement.Base.Server;
using ApiManagement.Models._6sModel;
using ApiManagement.Models.DeviceSpotCheckModel;
using ApiManagement.Models.ManufactureModel;
using ApiManagement.Models.MaterialManagementModel;
using ApiManagement.Models.OtherModel;
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
            Console.WriteLine("TimerHelper 调试模式已开启");
#else
            _totalTimer = new Timer(DoSth, null, 5000, 1000 * 10 * 1);
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
            AccountHelper.CheckAccount();
        }

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

        private static void GetErpDepartment()
        {
            var _pre = "GetErpDepartment";
            var lockKey = $"{_pre}:Lock";
            if (ServerConfig.RedisHelper.SetIfNotExist(lockKey, DateTime.Now.ToStr()))
            {
                try
                {
                    ServerConfig.RedisHelper.SetExpireAt(lockKey, DateTime.Now.AddMinutes(5));
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
                ServerConfig.RedisHelper.Remove(lockKey);
            }
        }
        private class ErpDepartment
        {
            public string name { get; set; }
            public List<string> member { get; set; }
        }

        private static void GetErpValuer()
        {
            var _pre = "GetErpValuer";
            var lockKey = $"{_pre}:Lock";
            if (ServerConfig.RedisHelper.SetIfNotExist(lockKey, DateTime.Now.ToStr()))
            {
                try
                {
                    ServerConfig.RedisHelper.SetExpireAt(lockKey, DateTime.Now.AddMinutes(5));
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
                ServerConfig.RedisHelper.Remove(lockKey);
            }
        }

        private static void GetErpPurchase()
        {
            var _pre = "GetErpPurchase";
            var lockKey = $"{_pre}:Lock";
            if (ServerConfig.RedisHelper.SetIfNotExist(lockKey, DateTime.Now.ToStr()))
            {
                try
                {
                    ServerConfig.RedisHelper.SetExpireAt(lockKey, DateTime.Now.AddMinutes(5));

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
                            "LEFT JOIN (SELECT * FROM `material_purchase` WHERE State IN @state AND MarkedDelete = 0) b ON a.Id = b.DepartmentId WHERE a.Get = 1 AND a.MarkedDelete = 0 GROUP BY a.Id",
                            new { state = validState });

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
                            //var bz = res.GroupBy(x => x.f_bz).Select(y => y.Key).Join();
                            //var zt = res.GroupBy(x => x.f_zt).Select(y => y.Key).Join();
                            var havePurchases =
                                ServerConfig.ApiDb.Query<MaterialPurchase>(
                                    "SELECT * FROM `material_purchase` WHERE DepartmentId = @Id AND ErpId >= @ErpId AND MarkedDelete = 0;", new { department.Id, department.ErpId });
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
                                    var purchase = new MaterialPurchase
                                    {
                                        CreateUserId = _createUserId,
                                        MarkedDateTime = now,
                                        Time = DateTime.Parse(p.f_date),
                                        IsErp = true,
                                        ErpId = p.f_id,
                                        DepartmentId = dep.Id,
                                        Purchase = p.f_title,
                                        Number = p.f_name,
                                        Name = p.f_ygxm,
                                        Valuer = p.f_hjry ?? "",
                                        Step = p.f_bz,
                                        State = state,
                                        IsDesign = p.f_istz == "是",
                                        Priority = priority,
                                    };
                                    var existPurchase = havePurchases.FirstOrDefault(x => x.ErpId == p.f_id);
                                    if (existPurchase != null)
                                    {
                                        purchase.Id = existPurchase.Id;
                                        if (purchase.HaveChange(existPurchase))
                                        {
                                            purchases.Add(purchase);
                                        }

                                        if (p.goods == null)
                                        {
                                            continue;
                                        }
                                        var existPurchaseItems = havePurchaseItems.Where(x => x.PurchaseId == purchase.Id);
                                        var existPurchaseItemsStock = existPurchaseItems.Where(y => y.Stock == 0);
                                        if (purchase.State == MaterialPurchaseStateEnum.撤销)
                                        {
                                            //删除
                                            updatePurchaseItems.AddRange(existPurchaseItemsStock.Select(z =>
                                                {
                                                    z.MarkedDateTime = now;
                                                    z.MarkedDelete = true;
                                                    return z;
                                                }));
                                        }
                                        else
                                        {
                                            IEnumerable<MaterialPurchaseItem> l;
                                            if (p.goods.Any(x => x.f_id == 0))
                                            {
                                                var erpPurchaseItems = p.goods.Select(good =>
                                                    new MaterialPurchaseItem(purchase.Id, good, _createUserId, now, _urlFile));

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
                                                        updatePurchaseItems.AddRange(l);
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
                                                                    if (erpCode.HaveChange(myCode))
                                                                    {
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
                                                                    addPurchaseItems.Add(erpCode);
                                                                }
                                                            }
                                                            else
                                                            {
                                                                erpDic[i] = true;
                                                                var findI = myCodes.IndexOf(myCode);
                                                                myDic[findI] = true;
                                                                erpCode.Id = myCode.Id;
                                                                if (erpCode.HaveChange(myCode))
                                                                {
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
                                                            updatePurchaseItems.AddRange(l);
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
                                                    updatePurchaseItems.AddRange(l);
                                                }
                                                //更新
                                                var existGoods = p.goods.Where(x =>
                                                    existPurchaseItems.Any(y => y.ErpId == x.f_id));
                                                foreach (var good in existGoods)
                                                {
                                                    var g = new MaterialPurchaseItem(purchase.Id, good, _createUserId, now, _urlFile);
                                                    var existGood = existPurchaseItems.FirstOrDefault(x => x.ErpId == good.f_id);
                                                    if (existGood != null)
                                                    {
                                                        g.Id = existGood.Id;
                                                        g.PurchaseId = existGood.PurchaseId;
                                                        if (g.HaveChange(existGood))
                                                        {
                                                            updatePurchaseItems.Add(g);
                                                        }
                                                    }
                                                }
                                                //新增
                                                l = p.goods.Where(x => existPurchaseItems.All(y => y.ErpId != x.f_id))
                                                    .Select(z => new MaterialPurchaseItem(purchase.Id, z, _createUserId, now, _urlFile));
                                                if (l.Any())
                                                {
                                                    addPurchaseItems.AddRange(l);
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
                                    purchases.Add(new MaterialPurchase
                                    {
                                        CreateUserId = _createUserId,
                                        MarkedDateTime = now,
                                        Time = DateTime.Parse(p.f_date),
                                        IsErp = true,
                                        ErpId = p.f_id,
                                        DepartmentId = dep.Id,
                                        Purchase = p.f_title,
                                        Number = p.f_name,
                                        Name = p.f_ygxm,
                                        Step = p.f_bz,
                                        State = state,
                                        IsDesign = p.f_istz == "是",
                                        Priority = priority,
                                    });
                                }
                                if (purchases.Any())
                                {
                                    ServerConfig.ApiDb.Execute(
                                    "INSERT INTO `material_purchase` (`CreateUserId`, `MarkedDateTime`, `Time`, `IsErp`, `ErpId`, `DepartmentId`, `Purchase`, `Number`, `Name`, `Step`, `State`, `IsDesign`, `Priority`) " +
                                    "VALUES (@CreateUserId, @MarkedDateTime, @Time, @IsErp, @ErpId, @DepartmentId, @Purchase, @Number, @Name, @Step, @State, @IsDesign, @Priority);", purchases);

                                    havePurchases =
                                        ServerConfig.ApiDb.Query<MaterialPurchase>(
                                            "SELECT * FROM `material_purchase` WHERE DepartmentId = @Id AND ErpId >= @ErpId AND MarkedDelete = 0;", new { department.Id, department.ErpId });

                                    var validNotExistPurchases = res.Where(x => purchases.Any(y => y.ErpId == x.f_id));
                                    foreach (var p in validNotExistPurchases)
                                    {
                                        var purchase = havePurchases.FirstOrDefault(x => x.ErpId == p.f_id);

                                        //新增
                                        var l = p.goods.Select(good => new MaterialPurchaseItem(purchase.Id, good, _createUserId, now, _urlFile));
                                        if (l.Any())
                                        {
                                            addPurchaseItems.AddRange(l);
                                        }
                                    }
                                }
                            }

                            if (addPurchaseItems.Any())
                            {
                                ServerConfig.ApiDb.Execute(
                                    "INSERT INTO `material_purchase_item` (`CreateUserId`, `MarkedDateTime`, `Time`, `IsErp`, `ErpId`, `PurchaseId`, `Code`, `Class`, `Category`, `Name`, `Supplier`, `Specification`, `Number`, `Unit`, `Remark`, `Purchaser`, `Order`, `EstimatedTime`, `ArrivalTime`, `File`, `FileUrl`, `IsInspection`, `Currency`, `Payment`, `Transaction`, `Invoice`, `TaxPrice`, `TaxAmount`, `Price`) " +
                                    "VALUES (@CreateUserId, @MarkedDateTime, @Time, @IsErp, @ErpId, @PurchaseId, @Code, @Class, @Category, @Name, @Supplier, @Specification, @Number, @Unit, @Remark, @Purchaser, @Order, @EstimatedTime, @ArrivalTime, @File, @FileUrl, @IsInspection, @Currency, @Payment, @Transaction, @Invoice, @TaxPrice, @TaxAmount, @Price);",
                                    addPurchaseItems);
                            }

                            if (updatePurchaseItems.Any())
                            {
                                ServerConfig.ApiDb.Execute(
                                    "UPDATE `material_purchase_item` SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `Class` = @Class, `Category` = @Category, `Name` = @Name, `Supplier` = @Supplier, " +
                                    "`Specification` = @Specification, `Number` = @Number, `Unit` = @Unit, `Remark` = @Remark, `Purchaser` = @Purchaser, `Order` = @Order, " +
                                    "`EstimatedTime` = @EstimatedTime, `ArrivalTime` = @ArrivalTime, `File` = @File, `FileUrl` = @FileUrl, `IsInspection` = @IsInspection, " +
                                    "`Currency` = @Currency, `Payment` = @Payment, `Transaction` = @Transaction, `Invoice` = @Invoice, `TaxPrice` = @TaxPrice, `TaxAmount` = @TaxAmount, " +
                                    "`Price` = @Price WHERE `Id` = @Id;", updatePurchaseItems);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                ServerConfig.RedisHelper.Remove(lockKey);
            }
        }

        private class ErpPurchase
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
    }

}
