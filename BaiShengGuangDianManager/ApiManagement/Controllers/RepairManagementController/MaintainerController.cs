using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using ApiManagement.Models.RepairManagementModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

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
        [HttpGet]
        public DataResult GetMaintainer([FromQuery] int qId, bool menu)
        {
            var result = new DataResult();
            string sql;
            if (menu)
            {
                sql =
                    $"SELECT Id, `Name`, `Account`, `Phone`, `Remark` FROM `maintainer` WHERE {(qId == 0 ? "" : "Id = @qId AND ")}`MarkedDelete` = 0;";
                var data = ServerConfig.ApiDb.Query<dynamic>(sql, new { qId });
                result.datas.AddRange(data);
            }
            else
            {
                sql =
                    $"SELECT * FROM `maintainer` WHERE {(qId == 0 ? "" : "Id = @qId AND ")}`MarkedDelete` = 0;";
                var data = ServerConfig.ApiDb.Query<Maintainer>(sql, new { qId });
                result.datas.AddRange(data);
            }

            if (qId != 0 && !result.datas.Any())
            {
                return Result.GenError<DataResult>(Error.MaintainerNotExist);
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
                    "UPDATE maintainer SET `MarkedDateTime` = @MarkedDateTime, `Phone` = @Phone, `Remark` = @Remark WHERE `Id` = @Id;";
                ServerConfig.ApiDb.Execute(sql, maintainers);
            }
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
                "INSERT INTO maintainer (`CreateUserId`, `MarkedDateTime`, `Name`, `Account`, `Phone`, `Remark`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @Name, @Account, @Phone, @Remark);";
            ServerConfig.ApiDb.Execute(sql, maintainers);

            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// account
        /// </summary>
        /// <returns></returns>
        // DELETE: api/Maintainer/Id/5
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
            return Result.GenError<Result>(Error.Success);
        }

    }
}