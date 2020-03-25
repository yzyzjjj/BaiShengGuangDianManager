using ApiManagement.Base.Server;
using ApiManagement.Models._6sModel;
using ApiManagement.Models.BaseModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers._6sController
{
    /// <summary>
    /// 6s分组
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class _6sGroupController : ControllerBase
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="qId"></param>
        /// <param name="menu">下拉框</param>
        /// <param name="account"></param>
        /// <returns></returns>
        // GET: api/_6sGroup?qId=0&item=false&accountId=0
        [HttpGet]
        public DataResult Get_6sGroup([FromQuery] int qId, bool menu, string account)
        {
            var result = new DataResult();
            var accountId = 0;
            if (!account.IsNullOrEmpty())
            {
                accountId = ServerConfig.ApiDb.Query<int>("SELECT Id FROM `surveyor` WHERE Account = @account AND MarkedDelete = 0;", new { account }).FirstOrDefault();
                if (accountId == 0)
                {
                    return result;
                }
            }

            string sql;
            if (menu)
            {
                sql =
                    $"SELECT Id, `Group` FROM `6s_group` WHERE {(qId == 0 ? "" : "Id = @qId AND ")}{(accountId == 0 ? "" : "FIND_IN_SET(@accountId, SurveyorId) AND ")}`MarkedDelete` = 0;";
                var data = ServerConfig.ApiDb.Query<dynamic>(sql, new { qId, accountId });
                result.datas.AddRange(data);
            }
            else
            {
                sql =
                    $"SELECT * FROM `6s_group` WHERE {(qId == 0 ? "" : "Id = @qId AND ")}{(accountId == 0 ? "" : "FIND_IN_SET(@accountId, SurveyorId) AND ")}`MarkedDelete` = 0;";
                var data = ServerConfig.ApiDb.Query<_6sGroup>(sql, new { qId, accountId });
                result.datas.AddRange(data);
            }
            if (qId != 0 && !result.datas.Any())
            {
                return Result.GenError<DataResult>(Error._6sGroupNotExist);
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="qId"></param>
        /// <returns></returns>
        // GET: api/_6sGroup?qId=0&item=false&menu=false
        [HttpGet("Item")]
        public DataResult Get_6sGroupItem([FromQuery] int qId)
        {
            var result = new DataResult();
            var data = ServerConfig.ApiDb.Query<_6sItemDetail>("SELECT a.*, b.SurveyorName FROM `6s_item` a JOIN `surveyor` b ON a.Person = b.Id " +
                                                               "WHERE GroupId = @qId AND a.`MarkedDelete` = 0 AND b.`MarkedDelete` = 0 ORDER BY `Order`;", new { qId });
            result.datas.AddRange(data);
            return result;
        }

        // PUT: api/_6sGroup
        [HttpPut]
        public Result Put_6sGroup([FromBody] _6sGroupItems _6sGroup)
        {
            if (_6sGroup.Id == 0)
            {
                return Result.GenError<Result>(Error._6sGroupNotExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            if (_6sGroup.Group != null || _6sGroup.SurveyorId != null)
            {
                var _6sGroupOld =
                    ServerConfig.ApiDb.Query<_6sGroup>("SELECT * FROM `6s_group` WHERE Id = @Id AND MarkedDelete = 0;",
                        new { _6sGroup.Id }).FirstOrDefault();
                if (_6sGroupOld == null)
                {
                    return Result.GenError<DataResult>(Error._6sGroupNotExist);
                }

                _6sGroup.Group = _6sGroup.Group ?? _6sGroupOld.Group;
                _6sGroup.SurveyorId = _6sGroup.SurveyorId ?? _6sGroupOld.SurveyorId;

                if (_6sGroup.Group.IsNullOrEmpty())
                {
                    return Result.GenError<DataResult>(Error._6sGroupNotEmpty);
                }
                if (_6sGroupOld.Group != _6sGroup.Group || _6sGroupOld.SurveyorId != _6sGroup.SurveyorId)
                {
                    _6sGroup.MarkedDateTime = markedDateTime;
                    ServerConfig.ApiDb.Execute(
                        "UPDATE 6s_group SET `MarkedDateTime` = @MarkedDateTime, `Group` = @Group, `SurveyorId` = @SurveyorId WHERE `Id` = @Id;", _6sGroup);
                }
            }

            _6sGroup.Items = _6sGroup.Items ?? new List<_6sItem>();
            var _6sItemsOld = ServerConfig.ApiDb.Query<_6sItem>("SELECT * FROM `6s_item` WHERE GroupId = @Id AND `MarkedDelete` = 0;", new { _6sGroup.Id });
            var _6sItemsAdd = _6sGroup.Items.Where(x => x.Id == 0);
            if (_6sItemsAdd.Any())
            {
                foreach (var _6sItem in _6sItemsAdd)
                {
                    _6sItem.CreateUserId = createUserId;
                    _6sItem.MarkedDateTime = markedDateTime;
                    _6sItem.GroupId = _6sGroup.Id;
                    _6sItem.Reference = _6sItem.Reference ?? "";
                }
                ServerConfig.ApiDb.Execute(
                    "INSERT INTO 6s_item (`CreateUserId`, `MarkedDateTime`, `Order`, `Item`, `GroupId`, `Enable`, `Standard`, `Reference`, `Interval`, `Day`, `Week`, `Person`) " +
                    "VALUES (@CreateUserId, @MarkedDateTime, @Order, @Item, @GroupId, @Enable, @Standard, @Reference, @Interval, @Day, @Week, @Person);",
                    _6sItemsAdd);
            }

            var _6sItemsDel = _6sItemsOld.Where(x => _6sGroup.Items.All(y => y.Id != x.Id));
            if (_6sItemsDel.Any())
            {
                foreach (var _6sItem in _6sItemsDel)
                {
                    _6sItem.MarkedDateTime = markedDateTime;
                    _6sItem.MarkedDelete = true;
                }

                ServerConfig.ApiDb.Execute("UPDATE 6s_item SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete WHERE `Id` = @Id;", _6sItemsDel);
            }

            var _6sItemsUpdate = _6sGroup.Items.Where(x => x.Id != 0 && _6sItemsOld.First(y => y.Id == x.Id).Change(x));
            if (_6sItemsUpdate.Any())
            {
                foreach (var _6sItem in _6sItemsUpdate)
                {
                    _6sItem.MarkedDateTime = markedDateTime;
                }

                ServerConfig.ApiDb.Execute("UPDATE 6s_item SET `MarkedDateTime` = @MarkedDateTime, `Order` = @Order, `Item` = @Item, `Enable` = @Enable, " +
                    "`Standard` = @Standard, `Reference` = @Reference, `Interval` = @Interval, `Day` = @Day, `Week` = @Week, `Person` = @Person WHERE `Id` = @Id;", _6sItemsUpdate);
            }
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/_6sGroup
        [HttpPost]
        public DataResult Post_6sGroup([FromBody] _6sGroupItems _6sGroup)
        {
            if (_6sGroup.Group.IsNullOrEmpty())
            {
                return Result.GenError<DataResult>(Error._6sGroupNotEmpty);
            }
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `6s_group` WHERE `Group` = @Group AND MarkedDelete = 0;",
                    new { _6sGroup.Group }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<DataResult>(Error._6sGroupIsExist);
            }

            IEnumerable<_6sItem> _6sItems = null;
            if (_6sGroup.Items != null && _6sGroup.Items.Any())
            {
                _6sItems = _6sGroup.Items;
                if (_6sItems.Any(x => x.Item.IsNullOrEmpty()))
                {
                    return Result.GenError<DataResult>(Error._6sItemNotEmpty);
                }
                var sameItems = _6sGroup.Items.GroupBy(x => x.Item).Where(y => y.Count() > 1).Select(z => z.Key);
                var result = new DataResult();
                if (sameItems.Any())
                {
                    result.datas.AddRange(sameItems);
                    return result;
                }
            }
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            _6sGroup.CreateUserId = createUserId;
            _6sGroup.MarkedDateTime = markedDateTime;
            _6sGroup.SurveyorId = _6sGroup.SurveyorId ?? "";
            var id = ServerConfig.ApiDb.Query<int>("INSERT INTO 6s_group (`CreateUserId`, `MarkedDateTime`, `Group`, `SurveyorId`) " +
                                                "VALUES (@CreateUserId, @MarkedDateTime, @Group, @SurveyorId);SELECT LAST_INSERT_ID();", _6sGroup).FirstOrDefault();

            if (_6sItems != null)
            {
                foreach (var _6sItem in _6sItems)
                {
                    _6sItem.CreateUserId = createUserId;
                    _6sItem.MarkedDateTime = markedDateTime;
                    _6sItem.GroupId = id;
                    _6sItem.Reference = _6sItem.Reference ?? "";
                }
                ServerConfig.ApiDb.Execute(
                    "INSERT INTO 6s_item (`CreateUserId`, `MarkedDateTime`, `Order`, `Item`, `GroupId`, `Enable`, `Standard`, `Reference`, `Interval`, `Day`, `Week`, `Person`) " +
                    "VALUES (@CreateUserId, @MarkedDateTime, @Order, @Item, @GroupId, @Enable, @Standard, @Reference, @Interval, @Day, @Week, @Person);",
                    _6sItems);
            }

            return Result.GenError<DataResult>(Error.Success);
        }

        // DELETE: api/_6sGroup
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result Delete_6sGroup([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `6s_group` WHERE Id IN @id AND `MarkedDelete` = 0;", new { id = ids }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error._6sGroupNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `6s_group` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id` IN @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = ids
                });

            ServerConfig.ApiDb.Execute(
                "UPDATE `6s_item` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `GroupId` IN @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = ids
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}