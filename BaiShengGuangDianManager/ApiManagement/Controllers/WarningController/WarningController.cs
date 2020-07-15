using ApiManagement.Base.Helper;
using ApiManagement.Base.Server;
using ApiManagement.Models.DeviceManagementModel;
using ApiManagement.Models.StatisticManagementModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.WarningController
{
    /// <summary>
    /// 预警管理
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class WarningController : ControllerBase
    {
        /// <summary>
        /// 看板数据
        /// </summary>
        /// <returns></returns>
        // POST: api/KanBan
        [HttpGet]
        public object GetSet([FromQuery]int type = -1)
        {
            if (type == -1)
            {
                var ids = new List<MonitoringKanBanSet>();
                ids.AddRange(ServerConfig.ApiDb.Query<MonitoringKanBanSet>("SELECT * FROM `npc_monitoring_kanban_set` WHERE `MarkedDelete` = 0 ORDER BY `Order`;"));
                return new
                {
                    errno = 0,
                    errmsg = "成功",
                    data = ids
                };
            }

            //var cnt = ServerConfig.ApiDb.Query<int>(
            //    "SELECT COUNT(1) FROM `npc_monitoring_kanban_set` WHERE MarkedDelete = 0 AND Id = @type;", new { type }).FirstOrDefault();

            //if (cnt == 0)
            //{
            //    return new
            //    {
            //        errno = 4,
            //        errmsg = "参数错误"
            //    };
            //}
            return new
            {
                errno = 0,
                errmsg = "成功",
                data = (AnalysisHelper.MonitoringKanBanDic.ContainsKey(type) ? AnalysisHelper.MonitoringKanBanDic[type] : new MonitoringKanBan
                {
                    Time = DateTime.Now
                })
            };
        }

        // PUT: api/KanBan/5
        [HttpPut]
        public Result PutSet([FromBody] MonitoringKanBanSet set)
        {
            var data =
                ServerConfig.ApiDb.Query<Site>("SELECT * FROM `npc_monitoring_kanban_set` WHERE MarkedDelete = 0 AND `Id` = @Id;", new { Id = set.Id }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.MonitoringKanBanSetNotExist);
            }

            set.CreateUserId = Request.GetIdentityInformation();
            set.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "UPDATE `npc_monitoring_kanban_set` SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `Name` = @Name, `IsShow` = @IsShow, `DeviceIds` = @DeviceIds, `Order` = @Order WHERE `Id` = @Id;", set);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/KanBan
        [HttpPost]
        public Result PostSet([FromBody] MonitoringKanBanSet site)
        {
            site.CreateUserId = Request.GetIdentityInformation();
            site.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
              "INSERT INTO  `npc_monitoring_kanban_set` (`CreateUserId`, `MarkedDateTime`, `Name`, `IsShow`, `DeviceIds`, `Order`) " +
              "VALUES (@CreateUserId, @MarkedDateTime, @Name, @IsShow, @DeviceIds, @Order);",
              site);

            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/KanBan/5
        [HttpDelete("{id}")]
        public Result DeleteSet([FromRoute] int id)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `npc_monitoring_kanban_set` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.MonitoringKanBanSetNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `npc_monitoring_kanban_set` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}