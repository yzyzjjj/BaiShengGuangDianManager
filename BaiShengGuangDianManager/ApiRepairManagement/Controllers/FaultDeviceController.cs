using ApiRepairManagement.Base.Server;
using ApiRepairManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.ServerConfig.Enum;
using ModelBase.Models.Result;
using System;
using System.Collections.Generic;
using System.Linq;
using ModelBase.Base.Utils;

namespace ApiRepairManagement.Controllers
{
    /// <summary>
    /// 故障设备表
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class FaultDeviceController : ControllerBase
    {
        // GET: api/FaultDevice
        [HttpGet]
        public DataResult GetFaultDevice()
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.RepairDb.Query<FaultDevice>("SELECT * FROM `fault_device`;"));
            return result;
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id">自增Id</param>
        /// <returns></returns>
        // GET: api/FaultDevice/5
        [HttpGet("{id}")]
        public DataResult GetFaultDevice([FromRoute] int id)
        {
            var result = new DataResult();
            var data =
                ServerConfig.RepairDb.Query<FaultDevice>("SELECT * FROM `fault_device` WHERE Id = @id;", new { id }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.FaultDeviceNotExist;
                return result;
            }
            result.datas.Add(data);
            return result;
        }

        /// <summary>
        /// 机台号
        /// </summary>
        /// <param name="code">机台号</param>
        /// <returns></returns>
        // GET: api/FaultDevice/Code/5
        [HttpGet("Code/{code}")]
        public DataResult GetFaultDeviceByCode([FromRoute] string code)
        {
            var result = new DataResult();
            var datas =
                ServerConfig.RepairDb.Query<FaultDevice>("SELECT * FROM `fault_device` WHERE find_in_set(DeviceCode, @code);", new { code });
            if (!datas.Any())
            {
                result.errno = Error.FaultDeviceNotExist;
                return result;
            }
            result.datas.AddRange(datas);
            return result;
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id">自增Id</param>
        /// <param name="faultDevice"></param>
        /// <returns></returns>
        // PUT: api/FaultDevice/Id/5
        [HttpPut("{id}")]
        public Result PutFaultDevice([FromRoute] int id, [FromBody] FaultDevice faultDevice)
        {
            var cnt =
                ServerConfig.RepairDb.Query<int>("SELECT COUNT(1) FROM `fault_device` WHERE Id = @id;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.FaultDeviceNotExist);
            }

            faultDevice.Id = id;
            faultDevice.CreateUserId = Request.GetIdentityInformation();
            faultDevice.MarkedDateTime = DateTime.Now;
            ServerConfig.RepairDb.Execute(
                "UPDATE fault_device SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, " +
                "`DeviceCode` = @DeviceCode, `FaultTime` = @FaultTime, `Proposer` = @Proposer, `FaultDescription` = @FaultDescription, `Priority` = @Priority WHERE `Id` = @Id;", faultDevice);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/FaultDevice
        [HttpPost]
        public Result PostFaultDevice([FromBody] FaultDevice faultDevice)
        {
            faultDevice.CreateUserId = Request.GetIdentityInformation();
            faultDevice.MarkedDateTime = DateTime.Now;
            ServerConfig.RepairDb.Execute(
                "INSERT INTO fault_device (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `DeviceCode`, `FaultTime`, `Proposer`, `FaultDescription`, `Priority`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @DeviceCode, @FaultTime, @Proposer, @FaultDescription, @Priority);",
                faultDevice);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/FaultDevice/FaultDevices
        [HttpPost("FaultDevices")]
        public Result PostFaultDevice([FromBody] List<FaultDevice> faultDevices)
        {
            foreach (var faultDevice in faultDevices)
            {
                faultDevice.CreateUserId = Request.GetIdentityInformation();
                faultDevice.MarkedDateTime = DateTime.Now;
            }
            ServerConfig.RepairDb.Execute(
                "INSERT INTO fault_device (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `DeviceCode`, `FaultTime`, `Proposer`, `FaultDescription`, `Priority`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @DeviceCode, @FaultTime, @Proposer, @FaultDescription, @Priority);",
                faultDevices);

            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // DELETE: api/FaultDevice/Id/5
        [HttpDelete("{id}")]
        public Result DeleteFaultDevice([FromRoute] int id)
        {
            var cnt =
                ServerConfig.RepairDb.Query<int>("SELECT COUNT(1) FROM `fault_device` WHERE Id = @id;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.FaultDeviceNotExist);
            }

            ServerConfig.RepairDb.Execute(
                "UPDATE `fault_device` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }

    }
}