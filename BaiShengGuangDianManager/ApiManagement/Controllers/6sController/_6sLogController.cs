using ApiManagement.Base.Server;
using ApiManagement.Models._6sModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using ModelBase.Models.BaseModel;

namespace ApiManagement.Controllers._6sController
{
    /// <summary>
    /// 6s检查记录
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class _6sLogController : ControllerBase
    {
        /// <summary>
        /// 获取6s检查项
        /// </summary>
        /// <param name="qId"></param>
        /// <param name="gId">分组id</param>
        /// <returns></returns>
        // GET: api/_6sGroup/Item?qId=0
        [HttpGet("Item")]
        public DataResult Get_6sLogItem([FromQuery] int qId, int gId)
        {
            var sql = $"SELECT a.*, b.SurveyorName PersonName FROM `6s_log` a JOIN `surveyor` b ON a.Person = b.Id WHERE {(qId == 0 ? "" : "a.Id = @qId AND ")}{(gId == 0 ? "" : "a.GroupId = @gId AND ")}a.`MarkedDelete` = 0 AND PlannedTime >= @today ORDER BY `PlannedTime`, `Order`;";
            var result = new DataResult();
            var data = ServerConfig.ApiDb.Query<_6sItemCheck>(sql, new { qId, gId, today = DateTime.Today });
            if (qId != 0 && !data.Any())
            {
                return Result.GenError<DataResult>(Error._6sItemNotExist);
            }

            result.datas.AddRange(data);
            return result;
        }

        /// <summary>
        /// 6s检查汇报
        /// </summary>
        /// <param name="_6sLogs"></param>
        /// <returns></returns>
        // Put: api/_6sGroup/Report?qId=0&item=false&menu=false
        [HttpPut("Report")]
        public Result Put_6sLogReport([FromBody] IEnumerable<_6sLog> _6sLogs)
        {
            var ids = _6sLogs.GroupBy(x => x.Id).Select(y => y.Key);
            if (_6sLogs.Count() != ids.Count())
            {
                return Result.GenError<Result>(Error._6sItemNotExist);
            }

            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `6s_log` WHERE Id IN @id AND `MarkedDelete` = 0;", new { id = ids }).FirstOrDefault();
            if (cnt != _6sLogs.Count())
            {
                return Result.GenError<Result>(Error._6sItemNotExist);
            }

            var markedDateTime = DateTime.Now;
            if (_6sLogs.Any())
            {
                if (_6sLogs.Any(x => x.UpdateImage))
                {
                    var update_6sLogs = _6sLogs.Where(x => x.UpdateImage);
                    foreach (var _6sLog in update_6sLogs)
                    {
                        _6sLog.MarkedDateTime = markedDateTime;
                        _6sLog.Images = _6sLog.Images.IsNullOrEmpty() ? "[]" : _6sLog.Images;
                    }

                    ServerConfig.ApiDb.Execute(
                        "UPDATE 6s_log SET `MarkedDelete` = @MarkedDelete, `Images` = @Images, `ImageCheck` = true WHERE `Id` = @Id;", update_6sLogs);
                        //"UPDATE 6s_log SET `MarkedDelete` = @MarkedDelete, `Images` = @Images, `ImageCheck` = true WHERE `Id` = @Id AND `ImageCheck` = false;", update_6sLogs);
                }
                else
                {
                    var account = _6sLogs.First().SurveyorAccount;
                    var accountId =
                        ServerConfig.ApiDb.Query<int>("SELECT Id FROM `surveyor` WHERE Account = @account AND MarkedDelete = 0;", new { account }).FirstOrDefault();
                    if (accountId != 0)
                    {
                        foreach (var _6sLog in _6sLogs)
                        {
                            _6sLog.MarkedDateTime = markedDateTime;
                            _6sLog.SurveyorId = accountId;
                        }

                        ServerConfig.ApiDb.Execute(
                            "UPDATE 6s_log SET `MarkedDelete` = @MarkedDelete, `SurveyorId` = @SurveyorId, `Score` = @Score, `CheckTime` = @CheckTime, `Desc` = @Desc, `Check` = true WHERE `Id` = @Id AND `Check` = false;", _6sLogs);
                    }
                }
            }

            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 获取6s检查记录
        /// </summary>
        /// <param name="qId"></param>
        /// <param name="gId">分组id</param>
        /// <param name="pId">负责人Id</param>
        /// <param name="sTime">开始</param>
        /// <param name="eTime">结束</param>
        /// <returns></returns>
        // GET: api/_6sGroup?qId=0
        [HttpGet]
        public DataResult Get_6sLog([FromQuery] int qId, int gId, int pId, DateTime sTime, DateTime eTime)
        {
            var sql = "SELECT a.*, b.SurveyorName PersonName, IFNULL(c.SurveyorName, '') SurveyorName FROM `6s_log` a JOIN `surveyor` b ON a.Person = b.Id LEFT JOIN `surveyor` c ON a.SurveyorId = c.Id" +
                      $" WHERE {(qId == 0 ? "" : "a.Id = @qId AND ")}{(gId == 0 ? "" : "a.GroupId = @gId AND ")}{(pId == 0 ? "" : "a.Person = @pId AND ")}{((sTime == default(DateTime) || eTime == default(DateTime)) ? "" : "PlannedTime >= @sTime AND PlannedTime <= @eTime AND ")}a.`MarkedDelete` = 0 ORDER BY `PlannedTime`, `Order`;";
            var result = new DataResult();
            var data = ServerConfig.ApiDb.Query<_6sLogDetail>(sql, new { qId, gId, pId, sTime, eTime });
            if (qId != 0 && !data.Any())
            {
                return Result.GenError<DataResult>(Error._6sLogNotExist);
            }

            result.datas.AddRange(data);
            return result;
        }

        /// <summary>
        /// 更新6s检查记录
        /// </summary>
        /// <param name="_6sLogs"></param>
        /// <returns></returns>
        // Put: api/_6sGroup?qId=0&gId=0
        [HttpPut]
        public Result Put_6sLog([FromBody] IEnumerable<_6sLog> _6sLogs)
        {
            var ids = _6sLogs.GroupBy(x => x.Id).Select(y => y.Key);
            if (_6sLogs.Count() != ids.Count())
            {
                return Result.GenError<Result>(Error._6sItemNotExist);
            }

            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `6s_log` WHERE Id IN @id AND `MarkedDelete` = 0;", new { id = ids }).FirstOrDefault();
            if (cnt != _6sLogs.Count())
            {
                return Result.GenError<Result>(Error._6sItemNotExist);
            }

            var markedDateTime = DateTime.Now;
            if (_6sLogs.Any(x => x.UpdateImage))
            {
                var update_6sLogs = _6sLogs.Where(x => x.UpdateImage);
                foreach (var _6sLog in update_6sLogs)
                {
                    _6sLog.MarkedDateTime = markedDateTime;
                    _6sLog.Images = _6sLog.Images.IsNullOrEmpty() ? "[]" : _6sLog.Images;
                }

                ServerConfig.ApiDb.Execute(
                    "UPDATE 6s_log SET `MarkedDelete` = @MarkedDelete, `Images` = @Images, `ImageCheck` = true, `ModifyName` = @ModifyName, `ModifyId` = @ModifyId WHERE `Id` = @Id;", update_6sLogs);
            }
            else
            {
                var account = _6sLogs.First().ModifyAccount;
                var accountId =
                    ServerConfig.ApiDb.Query<int>("SELECT Id FROM `surveyor` WHERE Account = @account AND MarkedDelete = 0;", new { account }).FirstOrDefault();
                if (accountId != 0)
                {
                    foreach (var _6sLog in _6sLogs)
                    {
                        _6sLog.MarkedDateTime = markedDateTime;
                        _6sLog.ModifyId = accountId;
                    }

                    ServerConfig.ApiDb.Execute(
                        "UPDATE 6s_log SET `MarkedDelete` = @MarkedDelete, `SurveyorId` = @SurveyorId, `Score` = @Score, `CheckTime` = @CheckTime, `Desc` = @Desc, `Check` = true, `ModifyName` = @ModifyName, `ModifyId` = @ModifyId WHERE `Id` = @Id;", _6sLogs);
                }
            }

            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="batchDelete">ids</param>
        /// <returns></returns>
        // Delete: api/_6sGroup
        [HttpDelete]
        public Result Delete_6sLog([FromQuery] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `6s_log` WHERE Id IN @id AND `MarkedDelete` = 0;", new { id = ids }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error._6sLogNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `6s_log` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id` IN @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = ids
                });

            return Result.GenError<Result>(Error.Success);
        }


        /// <summary>
        /// 获取6s检查分组排名
        /// </summary>
        /// <param name="sTime"></param>
        /// <param name="eTime"></param>
        /// <returns></returns>
        // GET: api/_6sGroup/GroupRank?qId=0
        [HttpGet("GroupRank")]
        public DataResult Get_6sGroupRank([FromQuery] DateTime sTime, DateTime eTime)
        {
            var sql = "SELECT Id, `Group`, b.Score FROM 6s_group a " +
                      "JOIN (SELECT GroupId, SUM(Score) Score FROM 6s_log WHERE `MarkedDelete` = 0 AND PlannedTime >= @sTime AND PlannedTime <= @eTime GROUP BY GroupId) b ON a.Id = b.GroupId " +
                      "ORDER BY b.Score DESC;";
            var result = new DataResult();
            var data = ServerConfig.ApiDb.Query<dynamic>(sql, new { sTime, eTime });
            result.datas.AddRange(data);
            return result;
        }

        /// <summary>
        /// 获取6s检查分组员工排名
        /// </summary>
        /// <param name="gId"></param>
        /// <param name="sTime"></param>
        /// <param name="eTime">分组id</param>
        /// <returns></returns>
        // GET: api/_6sGroup/Item?qId=0
        [HttpGet("GroupPersonRank")]
        public DataResult Get_6sGroupPersonRank([FromQuery]int gId, DateTime sTime, DateTime eTime)
        {
            var sql = "SELECT b.Id, b.SurveyorName, a.Score FROM (SELECT Person, SUM(Score) Score FROM 6s_log WHERE GroupId = @gId AND PlannedTime >= @sTime AND PlannedTime <= @eTime GROUP BY Person) a " +
                      "JOIN surveyor b ON a.Person = b.Id ORDER BY a.Score DESC";
            var result = new DataResult();
            var data = ServerConfig.ApiDb.Query<dynamic>(sql, new { gId, sTime, eTime });
            result.datas.AddRange(data);
            return result;
        }
    }
}