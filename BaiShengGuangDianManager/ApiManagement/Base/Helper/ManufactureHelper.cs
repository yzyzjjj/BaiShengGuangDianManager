using ApiManagement.Base.Server;
using ApiManagement.Models.ManufactureModel;
using Microsoft.Extensions.Configuration;
using ModelBase.Base.Logger;
using System;
using System.Linq;
using System.Threading;

namespace ApiManagement.Base.Helper
{
    public class ManufactureHelper
    {
        private static Timer _checkTimer;
        private static bool _isCheckManufacturePlan;

        private static readonly string ManufacturePlanPre = "CheckManufacturePlan";
        private static readonly string ManufacturePlanLock = $"{ManufacturePlanPre}:Lock";
        public static void Init(IConfiguration configuration)
        {
            _checkTimer = new Timer(CheckManufacturePlan, null, 5000, 1000 * 10 * 1);
        }

        private static void CheckManufacturePlan(object param)
        {
            if (ServerConfig.RedisHelper.SetIfNotExist(ManufacturePlanLock, "lock"))
            {
                ServerConfig.RedisHelper.SetExpireAt(ManufacturePlanLock, DateTime.Now.AddMinutes(5));
                try
                {
                    var sql = "SELECT a.*, b.Sum FROM `manufacture_plan` a " +
                              "JOIN (SELECT PlanId, SUM(1) Sum FROM `manufacture_plan_task` WHERE MarkedDelete = 0 AND State NOT IN @state GROUP BY PlanId) b ON a.Id = b.PlanId WHERE MarkedDelete = 0;";
                    var plans = ServerConfig.ApiDb.Query<ManufacturePlanCondition>(sql, new { state = new []{ ManufacturePlanItemState.Done, ManufacturePlanItemState.Stop } });
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
                ServerConfig.RedisHelper.Remove(ManufacturePlanLock);
            }
        }
    }
}
