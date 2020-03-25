using ApiManagement.Base.Helper;
using ApiManagement.Base.Server;
using ApiManagement.Models.DeviceManagementModel;
using ApiManagement.Models.RepairManagementModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.RepairManagementController
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
        public DataResult GetFaultDevice([FromQuery]DateTime startTime, DateTime endTime, int qId)
        {
            string sql;
            var field = FaultDevice.GetField(new List<string> { "DeviceCode" }, "a.");
            if (startTime == default(DateTime) || endTime == default(DateTime))
            {
                sql =
                    $"SELECT a.*, b.FaultTypeName, IFNULL(c.`Name`, '') `Name`, IFNULL(c.`Account`, '') `Account`, IFNULL(c.`Phone`, '') `Phone` FROM (SELECT {field}, IFNULL(b.`Code`, a.DeviceCode) DeviceCode FROM `fault_device` a " +
                    $"LEFT JOIN `device_library` b ON a.DeviceId = b.Id) a JOIN `fault_type` b ON a.FaultTypeId = b.Id " +
                    $"LEFT JOIN (SELECT * FROM(SELECT * FROM maintainer ORDER BY MarkedDelete) a GROUP BY a.Account) c ON a.Maintainer = c.Account " +
                    $"WHERE a.MarkedDelete = 0{(qId == 0 ? "" : " AND a.Id = @qId")};";
            }
            else
            {
                sql =
                    $"SELECT a.*, b.FaultTypeName, IFNULL(c.`Name`, '') `Name`, IFNULL(c.`Account`, '') `Account`, IFNULL(c.`Phone`, '') `Phone` FROM (SELECT {field}, IFNULL(b.`Code`, a.DeviceCode) DeviceCode FROM `fault_device` a " +
                    $"LEFT JOIN `device_library` b ON a.DeviceId = b.Id) a JOIN `fault_type` b ON a.FaultTypeId = b.Id " +
                    $"LEFT JOIN (SELECT * FROM(SELECT * FROM maintainer ORDER BY MarkedDelete) a GROUP BY a.Account) c ON a.Maintainer = c.Account " +
                    $"WHERE a.MarkedDelete = 0 AND a.FaultTime >= @startTime AND a.FaultTime <= @endTime{(qId == 0 ? "" : " AND a.Id = @qId")};";
            }
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.ApiDb.Query<FaultDeviceDetail>(sql, new { startTime, endTime, qId })
                .OrderByDescending(x => x.FaultTime).ThenByDescending(x => x.DeviceCode));
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.FaultDeviceNotExist;
                return result;
            }
            return result;
        }

        /// <summary>
        ///
        /// </summary>
        // GET: api/FaultDevice/DeleteLog
        [HttpGet("DeleteLog")]
        public DataResult GetFaultDeviceDeleteLog([FromQuery]DateTime startTime, DateTime endTime)
        {
            string sql;
            var field = FaultDevice.GetField(new List<string> { "DeviceCode" }, "a.");
            if (startTime == default(DateTime) || endTime == default(DateTime))
            {
                sql =
                    $"SELECT a.*, b.FaultTypeName, IFNULL(c.`Name`, '') `Name`, IFNULL(c.`Account`, '') `Account`, IFNULL(c.`Phone`, '') `Phone` FROM (SELECT {field}, IFNULL(b.`Code`, a.DeviceCode) DeviceCode FROM `fault_device` a " +
                    $"LEFT JOIN `device_library` b ON a.DeviceId = b.Id) a JOIN `fault_type` b ON a.FaultTypeId = b.Id " +
                    $"LEFT JOIN (SELECT * FROM(SELECT * FROM maintainer ORDER BY MarkedDelete) a GROUP BY a.Account) c ON a.Maintainer = c.Account " +
                    $"WHERE a.MarkedDelete = 1 AND a.Cancel = 1;";
            }
            else
            {
                sql =
                    $"SELECT a.*, b.FaultTypeName, IFNULL(c.`Name`, '') `Name`, IFNULL(c.`Account`, '') `Account`, IFNULL(c.`Phone`, '') `Phone` FROM (SELECT {field}, IFNULL(b.`Code`, a.DeviceCode) DeviceCode FROM `fault_device` a " +
                    $"LEFT JOIN `device_library` b ON a.DeviceId = b.Id) a JOIN `fault_type` b ON a.FaultTypeId = b.Id " +
                    $"LEFT JOIN (SELECT * FROM(SELECT * FROM maintainer ORDER BY MarkedDelete) a GROUP BY a.Account) c ON a.Maintainer = c.Account " +
                    $"WHERE a.MarkedDelete = 1 AND a.Cancel = 1 AND a.FaultTime >= @startTime AND a.FaultTime <= @endTime;";
            }
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.ApiDb.Query<FaultDeviceDetail>(sql, new { startTime, endTime })
                .OrderByDescending(x => x.FaultTime).ThenByDescending(x => x.DeviceCode));
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
            var field = FaultDevice.GetField(new List<string> { "DeviceCode" }, "a.");
            var data =
                ServerConfig.ApiDb.Query<FaultDeviceDetail>($"SELECT a.*, b.FaultTypeName FROM (SELECT {field}, IFNULL(b.`Code`, a.DeviceCode) DeviceCode FROM `fault_device` a LEFT JOIN `device_library` b ON a.DeviceId = b.Id) a JOIN `fault_type` b ON a.FaultTypeId = b.Id WHERE a.MarkedDelete = 0 AND a.DeviceCode = @code;", new { code });
            if (!data.Any())
            {
                result.errno = Error.FaultDeviceNotExist;
                return result;
            }
            result.datas.AddRange(data);
            return result;
        }

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="faultDevices"></param>
        /// <returns></returns>
        // PUT: api/FaultDevice
        [HttpPut]
        public Result PutFaultDevice([FromBody] List<FaultDevice> faultDevices)
        {
            var oldFaultDevices =
                ServerConfig.ApiDb.Query<FaultDevice>("SELECT COUNT(1) FROM `fault_device` WHERE Id IN @id AND MarkedDelete = 0;", new { id = faultDevices.Select(x => x.Id) });
            if (oldFaultDevices.Count() != faultDevices.Count)
            {
                return Result.GenError<Result>(Error.FaultDeviceNotExist);
            }

            foreach (var faultDevice in faultDevices)
            {
                faultDevice.MarkedDateTime = DateTime.Now;
            }
            ServerConfig.ApiDb.Execute(
                "UPDATE fault_device SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `DeviceId` = @DeviceId, `DeviceCode` = @DeviceCode, `FaultTime` = @FaultTime, " +
                "`Proposer` = @Proposer, `FaultDescription` = @FaultDescription, `Priority` = @Priority,`State` = @State " +
                "WHERE `Id` = @Id;", faultDevices);

            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 维修
        /// </summary>
        /// <param name="faultDevices"></param>
        /// <returns></returns>
        // PUT: api/FaultDevice/Repair
        [HttpPut("Repair")]
        public Result RepairFaultDevice([FromBody] List<FaultDevice> faultDevices)
        {
            var oldFaultDevices =
                ServerConfig.ApiDb.Query<FaultDevice>("SELECT COUNT(1) FROM `fault_device` WHERE Id IN @id AND MarkedDelete = 0;", new { id = faultDevices.Select(x => x.Id) });
            if (oldFaultDevices.Count() != faultDevices.Count)
            {
                return Result.GenError<Result>(Error.FaultDeviceNotExist);
            }

            var info = Request.GetIdentityInformation();
            var time = DateTime.Now;
            foreach (var faultDevice in faultDevices)
            {
                faultDevice.MarkedDateTime = time;
                var old = oldFaultDevices.FirstOrDefault(x => x.Id == faultDevice.Id);
                if (old.Maintainer != info)
                {
                    return Result.GenError<Result>(Error.FaultDeviceRepairMaintainerError);
                }

                if (faultDevice.State == 3)
                {
                    faultDevice.MarkedDelete = true;
                }
                faultDevice.Maintainer = info;
            }
            ServerConfig.ApiDb.Execute(
                "UPDATE fault_device SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `EstimatedTime` = @EstimatedTime, `Remark` = @Remark, `Maintainer` = @Maintainer WHERE `Id` = @Id;", faultDevices);

            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="faultDevices"></param>
        /// <returns></returns>
        // PUT: api/FaultDevice
        [HttpPut("Assign")]
        public Result AssignFaultDevice([FromBody] List<FaultDevice> faultDevices)
        {
            var oldFaultDevices =
                ServerConfig.ApiDb.Query<FaultDevice>("SELECT COUNT(1) FROM `fault_device` WHERE Id IN @id AND MarkedDelete = 0;", new { id = faultDevices.Select(x => x.Id) });
            if (oldFaultDevices.Count() != faultDevices.Count)
            {
                return Result.GenError<Result>(Error.FaultDeviceNotExist);
            }

            var info = Request.GetIdentityInformation();
            foreach (var faultDevice in faultDevices)
            {
                faultDevice.MarkedDateTime = DateTime.Now;
                //if (repair)
                {
                    var old = oldFaultDevices.FirstOrDefault(x => x.Id == faultDevice.Id);
                    if (old.Maintainer != info)
                    {
                        return Result.GenError<Result>(Error.FaultDeviceRepairMaintainerError);
                    }
                }
            }
            ServerConfig.ApiDb.Execute(
                "UPDATE fault_device SET `MarkedDateTime` = @MarkedDateTime, `DeviceId` = @DeviceId, `DeviceCode` = @DeviceCode, `FaultTime` = @FaultTime, " +
                "`Proposer` = @Proposer, `FaultDescription` = @FaultDescription, `Priority` = @Priority, `State` = @State WHERE `Id` = @Id;", faultDevices);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/FaultDevice
        [HttpPost]
        public Result PostFaultDevice([FromBody] FaultDevice faultDevice)
        {
            //var cnt =
            //    ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `fault_device` WHERE MarkedDelete = 0 AND DeviceCode = @DeviceCode;", new { faultDevice.DeviceCode }).FirstOrDefault();
            //if (cnt > 0)
            //{
            //    return Result.GenError<Result>(Error.FaultDeviceIsExist);
            //}
            DeviceLibraryDetail device = null;
            if (faultDevice.DeviceId == 0)
            {
                var cnt =
                    ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `device_library` WHERE `Code` = @Code AND `MarkedDelete` = 0;", new { Code = faultDevice.DeviceCode }).FirstOrDefault();
                if (cnt > 0)
                {
                    return Result.GenError<Result>(Error.ReportDeviceCodeIsExist);
                }
            }
            else
            {
                device = ServerConfig.ApiDb.Query<DeviceLibraryDetail>("SELECT * FROM `device_library` WHERE Id = @DeviceId AND MarkedDelete = 0;;", new { faultDevice.DeviceId }).FirstOrDefault();
            }

            faultDevice.CreateUserId = Request.GetIdentityInformation();
            faultDevice.MarkedDateTime = DateTime.Now;
            faultDevice.Administrator = device?.Administrator ?? "";
            faultDevice.Maintainer = device?.Administrator ?? "";

            ServerConfig.ApiDb.Execute(
                "INSERT INTO fault_device (`CreateUserId`, `MarkedDateTime`, `DeviceId`, `DeviceCode`, `FaultTime`, `Proposer`, `FaultDescription`, `Priority`, `State`, `FaultTypeId`, `Administrator`, `Maintainer`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @DeviceId, @DeviceCode, @FaultTime, @Proposer, @FaultDescription, @Priority, @State, @FaultTypeId, @Administrator, @Maintainer);",
                faultDevice);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/FaultDevice/FaultDevices
        [HttpPost("FaultDevices")]
        public Result PostFaultDevice([FromBody] List<FaultDevice> faultDevices)
        {
            //var cnt =
            //    ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `fault_device` WHERE DeviceCode IN @DeviceCode AND MarkedDelete = 0;", new { DeviceCode = faultDevices.Select(x => x.DeviceCode) }).FirstOrDefault();
            //if (cnt > 0)
            //{
            //    return Result.GenError<Result>(Error.FaultDeviceIsExist);
            //}
            IEnumerable<DeviceLibraryDetail> devices = null;
            if (faultDevices.Any(x => x.DeviceId == 0))
            {
                var cnt =
                    ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `device_library` WHERE `Code` IN @Code AND `MarkedDelete` = 0;",
                        new { Code = faultDevices.Where(x => x.DeviceId == 0).Select(y => y.DeviceCode) }).FirstOrDefault();
                if (cnt > 0)
                {
                    return Result.GenError<Result>(Error.ReportDeviceCodeIsExist);
                }
            }
            else
            {
                devices = ServerConfig.ApiDb.Query<DeviceLibraryDetail>("SELECT * FROM `device_library` WHERE Id IN @DeviceId AND MarkedDelete = 0;;",
                    new { DeviceId = faultDevices.Select(x => x.DeviceId) });
            }

            var createUserId = Request.GetIdentityInformation();
            var time = DateTime.Now;
            foreach (var faultDevice in faultDevices)
            {
                faultDevice.CreateUserId = createUserId;
                faultDevice.MarkedDateTime = time;

                faultDevice.Administrator = "";
                faultDevice.Maintainer = "";
                if (devices != null)
                {
                    faultDevice.Administrator = devices.FirstOrDefault(x => x.Id == faultDevice.DeviceId)?.Administrator ?? "";
                    faultDevice.Maintainer = devices.FirstOrDefault(x => x.Id == faultDevice.DeviceId)?.Administrator ?? "";
                }
            }
            ServerConfig.ApiDb.Execute(
                "INSERT INTO fault_device (`CreateUserId`, `MarkedDateTime`, `DeviceId`, `DeviceCode`, `FaultTime`, `Proposer`, `FaultDescription`, `Priority`, `FaultTypeId`, `Administrator`, `Maintainer`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @DeviceId, @DeviceCode, @FaultTime, @Proposer, @FaultDescription, @Priority, @FaultTypeId, @Administrator, @Maintainer);",
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
            var data =
                ServerConfig.ApiDb.Query<FaultDevice>("SELECT * FROM `fault_device` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.FaultDeviceNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `fault_device` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete, `Cancel`= @Cancel WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Cancel = true,
                    Id = id
                });

            AnalysisHelper.FaultCal(data.FaultTime);
            return Result.GenError<Result>(Error.Success);
        }
    }
}