using ApiManagement.Base.Server;
using ApiManagement.Models.DeviceSpotCheckModel;
using ApiManagement.Models.ManufactureModel;
using ApiManagement.Models.OtherModel;
using Microsoft.Extensions.Configuration;
using ModelBase.Base.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ApiManagement.Base.Helper
{
    public class ServerTimerHelper
    {
        private static Timer _totalTimer;
        public static void Init(IConfiguration configuration)
        {
            _totalTimer = new Timer(DoSth, null, 5000, 1000 * 10 * 1);
        }

        private static void DoSth(object state)
        {
            CheckSpotCheckDevice();
            CheckManufacturePlan();

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
                    var sql = "SELECT a.*, b.Sum FROM `manufacture_plan` a " +
                              "JOIN (SELECT PlanId, SUM(1) Sum FROM `manufacture_plan_task` WHERE MarkedDelete = 0 AND State NOT IN @state GROUP BY PlanId) b ON a.Id = b.PlanId WHERE MarkedDelete = 0;";
                    var plans = ServerConfig.ApiDb.Query<ManufacturePlanCondition>(sql, new { state = new[] { ManufacturePlanItemState.Done, ManufacturePlanItemState.Stop } });
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
    }
}
