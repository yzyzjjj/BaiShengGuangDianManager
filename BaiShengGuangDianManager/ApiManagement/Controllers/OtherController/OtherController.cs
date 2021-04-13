using ApiManagement.Base.Helper;
using ApiManagement.Base.Server;
using ApiManagement.Models.Notify;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Models.Result;

namespace ApiManagement.Controllers.OtherController
{
    [Route("api/[controller]")]
    [ApiController]
    public class OtherController : ControllerBase
    {
        // GET: api/Other
        /// <summary>
        /// 刷新管理员权限
        /// </summary>
        /// <returns></returns>
        [HttpGet("UpdatePermission")]
        public Result UpdatePermission()
        {
            var sql =
                "UPDATE roles SET Permissions = (SELECT GROUP_CONCAT(Id) FROM permissions_group ORDER BY Id) WHERE Id = 1";

            ServerConfig.ApiDb.Execute(sql);
            RedisHelper.PublishToTable();

            return Result.GenError<Result>(Error.Success);
        }
    }
}