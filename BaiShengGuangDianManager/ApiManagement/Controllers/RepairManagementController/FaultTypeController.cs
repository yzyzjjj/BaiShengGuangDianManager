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

namespace ApiManagement.Controllers.RepairManagementController
{
    /// <summary>
    /// 故障类型
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class FaultTypeController : ControllerBase
    {
        // GET: api/FaultType
        [HttpGet]
        public DataResult GetFaultType([FromQuery]int qId, bool menu)
        {
            var result = new DataResult();
            string sql;
            if (menu)
            {
                sql =
                    $"SELECT Id, FaultTypeName, FaultDescription FROM `fault_type` WHERE MarkedDelete = 0{(qId == 0 ? "" : " AND Id = @qId")};";
                result.datas.AddRange(ServerConfig.ApiDb.Query<dynamic>(sql, new { qId }));
            }
            else
            {
                sql = $"SELECT * FROM `fault_type` WHERE MarkedDelete = 0{(qId == 0 ? "" : " AND Id = @qId")};";
                result.datas.AddRange(ServerConfig.ApiDb.Query<FaultType>(sql, new { qId }));
            }

            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.FaultTypeNotExist;
                return result;
            }
            return result;
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id">自增Id</param>
        /// <param name="faultType"></param>
        /// <returns></returns>
        // PUT: api/FaultType/Id/5
        [HttpPut("{id}")]
        public Result PutFaultType([FromRoute] int id, [FromBody] FaultType faultType)
        {
            var data =
                ServerConfig.ApiDb.Query<FaultType>("SELECT * FROM `fault_type` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.FaultTypeNotExist);
            }

            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `fault_type` WHERE FaultTypeName = @FaultTypeName AND MarkedDelete = 0;", new { faultType.FaultTypeName }).FirstOrDefault();
            if (cnt > 0)
            {
                if (!faultType.FaultTypeName.IsNullOrEmpty() && data.FaultTypeName != faultType.FaultTypeName)
                {
                    return Result.GenError<Result>(Error.FaultTypeIsExist);
                }
            }

            faultType.Id = id;
            faultType.CreateUserId = Request.GetIdentityInformation();
            faultType.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "UPDATE fault_type SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, " +
                "`ModifyId` = @ModifyId, `FaultTypeName` = @FaultTypeName, `FaultDescription` = @FaultDescription WHERE `Id` = @Id;", faultType);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/FaultType
        [HttpPost]
        public Result PostFaultType([FromBody] FaultType faultType)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `fault_type` WHERE FaultTypeName = @FaultTypeName AND MarkedDelete = 0;", new { faultType.FaultTypeName }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.FaultTypeIsExist);
            }
            faultType.CreateUserId = Request.GetIdentityInformation();
            faultType.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "INSERT INTO fault_type (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `FaultTypeName`, `FaultDescription`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @FaultTypeName, @FaultDescription);",
                faultType);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/FaultType/FaultTypes
        [HttpPost("FaultTypes")]
        public Result PostFaultType([FromBody] List<FaultType> faultTypes)
        {
            var faultTypeName = faultTypes.GroupBy(x => x.FaultTypeName).Select(x => x.Key);
            if (faultTypeName.Any())
            {
                var cnt =
                    ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `fault_type` WHERE FaultTypeName IN @FaultTypeName AND MarkedDelete = 0;", new { FaultTypeName = faultTypeName }).FirstOrDefault();
                if (cnt > 0)
                {
                    return Result.GenError<Result>(Error.FaultTypeIsExist);
                }
            }
            foreach (var faultType in faultTypes)
            {
                faultType.CreateUserId = Request.GetIdentityInformation();
                faultType.MarkedDateTime = DateTime.Now;
            }
            ServerConfig.ApiDb.Execute(
                "INSERT INTO fault_type (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `FaultTypeName`, `FaultDescription`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @FaultTypeName, @FaultDescription);",
                faultTypes);

            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // DELETE: api/FaultType/Id/5
        [HttpDelete("{id}")]
        public Result DeleteFaultType([FromRoute] int id)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `fault_type` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.FaultTypeNotExist);
            }

            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `fault_device_repair` WHERE a.`State` != @state AND FaultTypeId = @id AND `MarkedDelete` = 0;", 
                    new { id, state = RepairStateEnum.Complete }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.FaultDeviceUseFaultType);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `fault_type` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });

            return Result.GenError<Result>(Error.Success);
        }

    }
}