using ApiManagement.Base.Server;
using ApiManagement.Models;
using Microsoft.Extensions.Configuration;
using ModelBase.Base.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ApiManagement.Models.DeviceSpotCheckModel;
using ApiManagement.Models.OtherModel;

namespace ApiManagement.Base.Helper
{
    public class SpotCheckHelper
    {
        private static Timer _checkTimer;
        private static bool _isCheckPlan;

        private static readonly string CheckPlanPre = "CheckPlan";
        private static readonly string CheckPlanLock = $"{CheckPlanPre}:Lock";
        public static void Init(IConfiguration configuration)
        {
            _checkTimer = new Timer(CheckSpotCheckDevice, null, 5000, 1000 * 10 * 1);
        }

        private static void CheckSpotCheckDevice(object state)
        {
            //#if !DEBUG
            //            if (ServerConfig.RedisHelper.Get<int>("Debug") != 0)
            //            {
            //                return;
            //            }
            //#endif
            //Thread.Sleep(5000);
            //ServerConfig.RedisHelper.Remove(CheckPlanLock);
            if (ServerConfig.RedisHelper.SetIfNotExist(CheckPlanLock, "lock"))
            {
                try
                {
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
                ServerConfig.RedisHelper.Remove(CheckPlanLock);
            }
        }
    }
}
