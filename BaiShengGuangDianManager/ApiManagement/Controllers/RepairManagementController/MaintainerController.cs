using ApiManagement.Base.Helper;
using ApiManagement.Base.Server;
using ApiManagement.Models.RepairManagementModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using ModelBase.Models.BaseModel;

namespace ApiManagement.Controllers.RepairManagementController
{
    /// <summary>
    /// 维修工
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class MaintainerController : ControllerBase
    {
        // GET: api/Maintainer
        /// <summary>
        /// 
        /// </summary>
        /// <param name="qId"></param>
        /// <param name="menu"></param>
        /// <param name="arranged">-1 所有； 0 不排班； 1 排班</param>
        /// <returns></returns>
        [HttpGet]
        public DataResult GetMaintainer([FromQuery] int qId, bool menu, int arranged = -1)
        {
            var result = new DataResult();
            string sql;
            if (menu)
            {
                sql =
                    $"SELECT Id, `Name`, `Account`, `Phone`, `Remark`, `Order` FROM `maintainer` WHERE `MarkedDelete` = 0" +
                    $"{(qId == 0 ? "" : " AND Id = @qId ")} " +
                    $"{(arranged == -1 ? "" : (arranged == 0 ? " AND `Order` = 0" : " AND `Order` != 0"))}" +
                    $";";
                var data = ServerConfig.ApiDb.Query<dynamic>(sql, new { qId });
                result.datas.AddRange(data.Where(x => x.Order != 0).OrderBy(y => y.Order).ThenBy(z => z.Id));
                result.datas.AddRange(data.Where(x => x.Order == 0).OrderBy(y => y.Id));
            }
            else
            {
                sql =
                    $"SELECT * FROM `maintainer` WHERE WHERE `MarkedDelete` = 0" +
                    $"{(qId == 0 ? "" : " AND Id = @qId ")} " +
                    $"{(arranged == -1 ? "" : (arranged == 0 ? " AND `Order` = 0" : " AND `Order` != 0"))}" +
                    $";";
                var data = ServerConfig.ApiDb.Query<Maintainer>(sql, new { qId });
                result.datas.AddRange(data.Where(x => x.Order != 0).OrderBy(y => y.Order).ThenBy(z => z.Id));
                result.datas.AddRange(data.Where(x => x.Order == 0).OrderBy(y => y.Id));
            }

            if (qId != 0 && !result.datas.Any())
            {
                return Result.GenError<DataResult>(Error.MaintainerNotExist);
            }
            return result;
        }

        // GET: api/Maintainer/Schedule
        [HttpGet("Schedule")]
        public DataResult GetMaintainerSchedule([FromQuery]DateTime time)
        {
            var result = new DataResult();
            var today = time == default(DateTime) ? DateTime.Today : time;
            var weekBegin = today.WeekBeginTime().Date;
            var weekEnd = today.WeekEndTime().AddDays(1).Date;

            var schedules = ServerConfig.ApiDb.Query<MaintainerScheduleDetail>(
                "SELECT a.*, b.`Name`, b.Phone FROM `maintainer_schedule` a JOIN `maintainer` b ON a.MaintainerId = b.Id WHERE a.StartTime >= @Time1 AND a.StartTime < @Time2 ORDER BY a.StartTime;", new
                {
                    Time1 = weekBegin,
                    Time2 = weekEnd
                });
            //var scheduleAdjusts = ServerConfig.ApiDb.Query<MaintainerAdjust>(
            //    "SELECT * FROM `maintainer_adjust` WHERE StartTime >= @Time1 AND StartTime <= @Time2 ORDER BY a.StartTime;", new
            //    {
            //        Time1 = weekBegin,
            //        Time2 = weekEnd
            //    });
            var temp = weekBegin;
            while (temp < weekEnd)
            {
                //0-8
                var startTime = temp;
                var endTime = temp.AddSeconds(GlobalConfig.Morning.TotalSeconds);
                var schedule = new MaintainerScheduleDetails
                {
                    StartTime = startTime,
                    EndTime = endTime
                };
                schedule.Maintainers.AddRange(schedules.Where(x =>
                    x.StartTime >= startTime && x.StartTime < endTime));
                result.datas.Add(schedule);

                //8-17
                startTime = temp.AddSeconds(GlobalConfig.Morning.TotalSeconds);
                endTime = temp.AddSeconds(GlobalConfig.Evening.TotalSeconds);
                schedule = new MaintainerScheduleDetails
                {
                    StartTime = startTime,
                    EndTime = endTime
                };
                schedule.Maintainers.AddRange(schedules.Where(x =>
                    x.StartTime >= startTime && x.StartTime < endTime
                    && x.MaintainerId != 0));
                result.datas.Add(schedule);

                //17-20
                startTime = temp.AddSeconds(GlobalConfig.Evening.TotalSeconds);
                endTime = temp.AddSeconds(GlobalConfig.Night20.TotalSeconds);
                schedule = new MaintainerScheduleDetails
                {
                    StartTime = startTime,
                    EndTime = endTime
                };
                schedule.Maintainers.AddRange(schedules.Where(x =>
                    x.StartTime >= startTime && x.StartTime < endTime));
                result.datas.Add(schedule);

                //20-24
                startTime = temp.AddSeconds(GlobalConfig.Night20.TotalSeconds);
                endTime = temp.AddDays(1);
                schedule = new MaintainerScheduleDetails
                {
                    StartTime = startTime,
                    EndTime = endTime
                };
                schedule.Maintainers.AddRange(schedules.Where(x =>
                    x.StartTime >= startTime && x.StartTime < endTime));
                result.datas.Add(schedule);

                temp = temp.AddDays(1);
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        // PUT: api/Maintainer/
        [HttpPut]
        public Result PutMaintainer([FromBody] IEnumerable<Maintainer> maintainers)
        {
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            var sql = "";
            if (maintainers.Any(x => x.WebOp > 0))
            {
                var update = maintainers.Where(x => x.WebOp == 1);
                if (update.Any())
                {
                    foreach (var maintainer in update)
                    {
                        maintainer.MarkedDateTime = markedDateTime;
                    }
                    sql =
                        "UPDATE maintainer SET `MarkedDateTime` = @MarkedDateTime, `Name` = @Name, `Phone` = IF(@Phone = '', Phone, @Phone) WHERE `Account` = @Account AND `MarkedDelete` = 0;";
                    ServerConfig.ApiDb.Execute(sql, update);
                }
                var del = maintainers.Where(x => x.WebOp == 2);
                if (del.Any())
                {
                    foreach (var maintainer in del)
                    {
                        maintainer.MarkedDelete = true;
                        maintainer.MarkedDateTime = markedDateTime;
                    }
                    sql =
                        "UPDATE `maintainer` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Account` = @Account AND `MarkedDelete` = 0;";
                    ServerConfig.ApiDb.Execute(sql, del);
                }
                var add = maintainers.Where(x => x.WebOp == 3);
                if (add.Any())
                {
                    var existMaintainers =
                        ServerConfig.ApiDb.Query<Maintainer>("SELECT * FROM `maintainer` WHERE Account IN @Account AND `MarkedDelete` = 0;", add);
                    var exist = add.Where(x => existMaintainers.Any(y => y.Account == x.Account));
                    if (exist.Any())
                    {
                        foreach (var maintainer in exist)
                        {
                            maintainer.MarkedDelete = false;
                            maintainer.MarkedDateTime = markedDateTime;
                        }
                        sql =
                            "UPDATE maintainer SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete`= @MarkedDelete, `Name` = @Name, `Phone` = @Phone WHERE `Account` = @Account AND `MarkedDelete` = 0;";
                        ServerConfig.ApiDb.Execute(sql, exist);
                    }

                    var notExist = add.Where(x => existMaintainers.All(y => y.Account != x.Account));
                    if (notExist.Any())
                    {
                        foreach (var maintainer in notExist)
                        {
                            maintainer.CreateUserId = createUserId;
                            maintainer.MarkedDateTime = markedDateTime;
                        }

                        sql =
                            "INSERT INTO maintainer (`CreateUserId`, `MarkedDateTime`, `Name`, `Account`, `Phone`) " +
                            "VALUES (@CreateUserId, @MarkedDateTime, @Name, @Account, @Phone);";
                        ServerConfig.ApiDb.Execute(sql, notExist);
                    }
                }
            }
            else
            {
                var cnt =
                    ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `maintainer` WHERE Id IN @ids AND `MarkedDelete` = 0;", new { ids = maintainers.Select(x => x.Id) }).FirstOrDefault();
                if (cnt != maintainers.Count())
                {
                    return Result.GenError<Result>(Error.MaintainerNotExist);
                }
                //if (maintainers.Any(x => x.Phone.IsNullOrEmpty() || !x.Phone.IsPhone()))
                //{
                //    return Result.GenError<Result>(Error.PhoneError);
                //}

                foreach (var maintainer in maintainers)
                {
                    maintainer.MarkedDateTime = markedDateTime;
                    maintainer.Phone = (maintainer.Phone.IsNullOrEmpty() || !maintainer.Phone.IsPhone()) ? "" : maintainer.Phone;
                }
                sql =
                    "UPDATE maintainer SET `MarkedDateTime` = @MarkedDateTime, `Phone` = @Phone, `Remark` = @Remark, `Order` = @Order WHERE `Id` = @Id;";
                ServerConfig.ApiDb.Execute(sql, maintainers);
            }
            TimerHelper.DoMaintainerSchedule();
            return Result.GenError<Result>(Error.Success);
        }

        // GET: api/Maintainer/Schedule
        [HttpPut("Schedule")]
        public Result PutMaintainerSchedule([FromBody] IEnumerable<MaintainerSchedule> schedules)
        {
            if (schedules == null || !schedules.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            var now = DateTime.Now;
            var today = now.Date;
            var weekBegin = today.WeekBeginTime();
            var weekEnd = today.WeekEndTime();

            var oldSchedules = ServerConfig.ApiDb.Query<MaintainerScheduleDetail>(
                "SELECT a.*, b.`Name`, b.Phone FROM `maintainer_schedule` a JOIN `maintainer` b ON a.MaintainerId = b.Id WHERE a.StartTime >= @Time1 AND a.StartTime <= @Time2 ORDER BY a.StartTime;", new
                {
                    Time1 = weekBegin,
                    Time2 = weekEnd
                });

            var changes = new List<int>();
            foreach (var oldSchedule in oldSchedules)
            {
                var newSchedule = schedules.FirstOrDefault(x => x.Id == oldSchedule.Id);
                if (newSchedule != null && oldSchedule.MaintainerId != newSchedule.MaintainerId)
                {
                    if (oldSchedule.EndTime < now)
                    {
                        return Result.GenError<Result>(Error.OldMaintainerSchedule);
                    }

                    changes.Add(oldSchedule.Id);
                    oldSchedule.MaintainerId = newSchedule.MaintainerId;
                }
            }

            ServerConfig.ApiDb.Execute("UPDATE `maintainer_schedule` SET `MaintainerId` = @MaintainerId WHERE Id = @Id;", oldSchedules.Where(x => changes.Contains(x.Id)));
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/Maintainer/Maintainers
        [HttpPost]
        public Result PostMaintainer([FromBody] IEnumerable<Maintainer> maintainers)
        {
            var acc = maintainers.GroupBy(x => x.Account).Select(y => y.Key);
            if (maintainers.Count() != acc.Count())
            {
                return Result.GenError<Result>(Error.MaintainerDuplicate);
            }
            //if (maintainers.Any(x => x.Phone.IsNullOrEmpty() || !x.Phone.IsPhone()))
            //{
            //    return Result.GenError<Result>(Error.PhoneError);
            //}
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `maintainer` WHERE Account IN @Account AND `MarkedDelete` = 0;", new { Account = acc }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.MaintainerIsExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var time = DateTime.Now;
            foreach (var maintainer in maintainers)
            {
                maintainer.CreateUserId = createUserId;
                maintainer.MarkedDateTime = time;
                maintainer.Phone = (maintainer.Phone.IsNullOrEmpty() || !maintainer.Phone.IsPhone()) ? "" : maintainer.Phone;
                maintainer.Remark = maintainer.Remark ?? "";
            }

            var sql =
                "INSERT INTO maintainer (`CreateUserId`, `MarkedDateTime`, `Name`, `Account`, `Phone`, `Remark`, `Order`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @Name, @Account, @Phone, @Remark, @Order);";
            ServerConfig.ApiDb.Execute(sql, maintainers);

            TimerHelper.DoMaintainerSchedule();
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/Maintainer/Adjust
        [HttpPost("Adjust")]
        public Result PostMaintainerAdjust([FromBody] IEnumerable<MaintainerAdjust> maintainers)
        {
            if (maintainers.Any(x => x.MaintainerId == 0 || x.StartTime == default(DateTime) || x.EndTime == default(DateTime)))
            {
                return Result.GenError<Result>(Error.MaintainerNotExist);
            }
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `maintainer` WHERE Id IN @MaintainerId AND `MarkedDelete` = 0;", new { MaintainerId = maintainers.Select(x => x.MaintainerId) }).FirstOrDefault();
            if (cnt < maintainers.Count())
            {
                return Result.GenError<Result>(Error.MaintainerNotExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var time = DateTime.Now;
            foreach (var maintainer in maintainers)
            {
                maintainer.CreateUserId = createUserId;
                maintainer.MarkedDateTime = time;
                maintainer.Remark = maintainer.Remark ?? "";
            }

            var sql =
                "INSERT INTO `maintainer_adjust` (`CreateUserId`, `MarkedDateTime`, `Type`, `MaintainerId`, `Remark`, `StartTime`, `EndTime`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @Type, @MaintainerId, @Remark, @StartTime, @EndTime);";
            ServerConfig.ApiDb.Execute(sql, maintainers);

            TimerHelper.DoMaintainerSchedule();
            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// account
        /// </summary>
        /// <returns></returns>
        // DELETE: api/Maintainer/
        [HttpDelete]
        public Result DeleteMaintainer([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT * FROM `maintainer` WHERE Id IN @id AND `MarkedDelete` = 0;", new { id = ids }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.MaintainerIsExist);
            }
            ServerConfig.ApiDb.Execute(
                "UPDATE `maintainer` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE Id IN @id", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = ids
                });

            TimerHelper.DoMaintainerSchedule();
            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// account
        /// </summary>
        /// <returns></returns>
        // DELETE: api/Maintainer/Adjust
        [HttpDelete("Adjust")]
        public Result DeleteMaintainerAdjust([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT * FROM `maintainer_adjust` WHERE Id IN @id AND `MarkedDelete` = 0;", new { id = ids }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            ServerConfig.ApiDb.Execute(
                "UPDATE `maintainer_adjust` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE Id IN @id", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = ids
                });

            TimerHelper.DoMaintainerSchedule();
            return Result.GenError<Result>(Error.Success);
        }
    }
}