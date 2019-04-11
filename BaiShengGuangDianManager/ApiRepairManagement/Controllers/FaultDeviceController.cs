using ApiRepairManagement.Base.Server;
using ApiRepairManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using System;
using System.Collections.Generic;
using System.Linq;

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
            result.datas.AddRange(ServerConfig.RepairDb.Query<FaultDevice>("SELECT * FROM `fault_device` WHERE MarkedDelete = 0;"));
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
                ServerConfig.RepairDb.Query<FaultDevice>("SELECT * FROM `fault_device` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
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
                ServerConfig.RepairDb.Query<FaultDevice>("SELECT * FROM `fault_device` WHERE DeviceCode = @code AND MarkedDelete = 0;", new { code });
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
        /// <param name="faultDevices"></param>
        /// <returns></returns>
        // PUT: api/FaultDevice
        [HttpPut]
        public Result PutFaultDevice([FromBody] List<FaultDevice> faultDevices)
        {
            var cnt =
                ServerConfig.RepairDb.Query<int>("SELECT COUNT(1) FROM `fault_device` WHERE Id IN @id AND MarkedDelete = 0;", new { id = faultDevices.Select(x => x.Id) }).FirstOrDefault();
            if (cnt != faultDevices.Count)
            {
                return Result.GenError<Result>(Error.FaultDeviceNotExist);
            }

            var info = Request.GetIdentityInformation();
            foreach (var faultDevice in faultDevices)
            {
                faultDevice.CreateUserId = info;
                faultDevice.MarkedDateTime = DateTime.Now;
            }
            ServerConfig.RepairDb.Execute(
                "UPDATE fault_device SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, " +
                "`DeviceCode` = @DeviceCode, `FaultTime` = @FaultTime, `Proposer` = @Proposer, `FaultDescription` = @FaultDescription, `Priority` = @Priority, " +
                "`State` = @State WHERE `Id` = @Id;", faultDevices);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/FaultDevice
        [HttpPost]
        public Result PostFaultDevice([FromBody] FaultDevice faultDevice)
        {
            var cnt =
                ServerConfig.RepairDb.Query<int>("SELECT COUNT(1) FROM `fault_device` WHERE MarkedDelete = 0 AND DeviceCode = @DeviceCode;", new { faultDevice.DeviceCode }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.FaultDeviceIsExist);
            }
            faultDevice.CreateUserId = Request.GetIdentityInformation();
            faultDevice.MarkedDateTime = DateTime.Now;
            ServerConfig.RepairDb.Execute(
                "INSERT INTO fault_device (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `DeviceCode`, `FaultTime`, `Proposer`, `FaultDescription`, `Priority`, `State`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @DeviceCode, @FaultTime, @Proposer, @FaultDescription, @Priority, @State);",
                faultDevice);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/FaultDevice/FaultDevices
        [HttpPost("FaultDevices")]
        public Result PostFaultDevice([FromBody] List<FaultDevice> faultDevices)
        {
            var cnt =
                ServerConfig.RepairDb.Query<int>("SELECT COUNT(1) FROM `fault_device` WHERE DeviceCode IN @DeviceCode AND MarkedDelete = 0;", new { DeviceCode = faultDevices.Select(x => x.DeviceCode) }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.FaultDeviceIsExist);
            }

            foreach (var faultDevice in faultDevices)
            {
                faultDevice.CreateUserId = Request.GetIdentityInformation();
                faultDevice.MarkedDateTime = DateTime.Now;
            }
            ServerConfig.RepairDb.Execute(
                "INSERT INTO fault_device (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `DeviceCode`, `FaultTime`, `Proposer`, `FaultDescription`, `Priority`, `State`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @DeviceCode, @FaultTime, @Proposer, @FaultDescription, @Priority, @State);",
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
                ServerConfig.RepairDb.Query<int>("SELECT COUNT(1) FROM `fault_device` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
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