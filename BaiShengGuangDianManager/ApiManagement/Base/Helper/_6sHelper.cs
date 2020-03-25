using ApiManagement.Base.Server;
using ApiManagement.Models._6sModel;
using ApiManagement.Models.OtherModel;
using Microsoft.Extensions.Configuration;
using ModelBase.Base.Logger;
using ModelBase.Base.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ApiManagement.Base.Helper
{
    public class _6sHelper
    {
        private static Timer _checkTimer;
        private static bool _is_6s;

        private static readonly string _6sPre = "6s";
        private static readonly string _6sLock = $"{_6sPre}:Lock";
        public static void Init(IConfiguration configuration)
        {
            ServerConfig.RedisHelper.Remove(_6sLock);
            _checkTimer = new Timer(Check_6sItem, null, 5000, 1000 * 10 * 1);
        }

        private static void Check_6sItem(object state)
        {
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

        public static void Init_6sItem(int groupId = 0)
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
    }
}
