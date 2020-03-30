using ApiManagement.Base.Helper;
using ApiManagement.Base.Server;
using ApiManagement.Models.DeviceManagementModel;
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
    /// 故障设备表
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class FaultDeviceController : ControllerBase
    {
        // GET: api/FaultDevice
        /// <summary>
        /// 
        /// </summary>
        /// <param name="startTime">故障时间</param>
        /// <param name="endTime">故障时间</param>
        /// <param name="condition">等于0 不等于1</param>
        /// <param name="code">机台号</param>
        /// <param name="faultType"></param>
        /// <param name="priority">优先级</param>
        /// <param name="state">状态</param>
        /// <param name="maintainer">维修工</param>
        /// <param name="eStartTime">预计解决时间</param>
        /// <param name="eEndTime">预计解决时间</param>
        /// <param name="qId"></param>
        /// <returns></returns>
        [HttpGet]
        public DataResult GetFaultDevice([FromQuery]DateTime startTime, DateTime endTime, int condition,
            string code, int faultType, int priority, int state, string maintainer, DateTime eStartTime, DateTime eEndTime, int qId)
        {
            var field = FaultDevice.GetField(new List<string> { "DeviceCode" }, "a.");
            var sql =
                $"SELECT a.*, b.FaultTypeName, IFNULL(c.`Name`, '') `Name`, IFNULL(c.`Account`, '') `Account`, IFNULL(c.`Phone`, '') `Phone` FROM (SELECT {field}, IFNULL(b.`Code`, a.DeviceCode) DeviceCode FROM `fault_device_repair` a " +
                $"LEFT JOIN `device_library` b ON a.DeviceId = b.Id) a JOIN `fault_type` b ON a.FaultTypeId = b.Id " +
                $"LEFT JOIN (SELECT * FROM(SELECT * FROM maintainer ORDER BY MarkedDelete) a GROUP BY a.Account) c ON a.Maintainer = c.Account ";

            sql += $"WHERE a.MarkedDelete = 0 AND `State` != @fState " +
                   $"{((startTime == default(DateTime) || endTime == default(DateTime)) ? "" : " AND a.FaultTime >= @startTime AND a.FaultTime <= @endTime")}" +
                   $"{(code.IsNullOrEmpty() ? "" : (" AND a.DeviceCode " + (condition == 0 ? "=" : "!=") + " @code"))}" +
                   $"{(faultType == -1 ? "" : (" AND a.FaultTypeId " + (condition == 0 ? "=" : "!=") + " @faultType"))}" +
                   $"{(priority == -1 ? "" : (" AND a.Priority " + (condition == 0 ? "=" : "!=") + " @priority"))}" +
                   $"{(state == -1 ? "" : (" AND a.State " + (condition == 0 ? "=" : "!=") + " @state"))}" +
                   $"{(maintainer.IsNullOrEmpty() ? "" : (" AND a.Maintainer " + (condition == 0 ? "=" : "!=") + " @maintainer"))}" +
                   $"{((eStartTime == default(DateTime) || eEndTime == default(DateTime)) ? "" : " AND a.EstimatedTime >= @eStartTime AND a.EstimatedTime <= @eEndTime")}" +
                   $"{(qId == 0 ? "" : (" AND a.Id " + (condition == 0 ? "=" : "!=") + " @qId"))}";

            var result = new DataResult();
            result.datas.AddRange(ServerConfig.ApiDb.Query<FaultDeviceDetail>(sql,
                    new { fState = RepairStateEnum.Complete, startTime, endTime, condition, code, faultType, priority, state, maintainer, eStartTime, eEndTime, qId })
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
        public DataResult GetFaultDeviceDeleteLog([FromQuery]DateTime startTime, DateTime endTime, int condition,
            string code, int faultType, int priority, int state, string maintainer, DateTime eStartTime, DateTime eEndTime, int qId)
        {
            var field = FaultDevice.GetField(new List<string> { "DeviceCode" }, "a.");
            var sql =
                $"SELECT a.*, b.FaultTypeName, IFNULL(c.`Name`, '') `Name`, IFNULL(c.`Account`, '') `Account`, IFNULL(c.`Phone`, '') `Phone` FROM (SELECT {field}, IFNULL(b.`Code`, a.DeviceCode) DeviceCode FROM `fault_device_repair` a " +
                $"LEFT JOIN `device_library` b ON a.DeviceId = b.Id) a JOIN `fault_type` b ON a.FaultTypeId = b.Id " +
                $"LEFT JOIN (SELECT * FROM(SELECT * FROM maintainer ORDER BY MarkedDelete) a GROUP BY a.Account) c ON a.Maintainer = c.Account ";

            sql += $"WHERE a.MarkedDelete = 0 AND `State` != @fState AND a.Cancel = 1 " +
                   $"{((startTime == default(DateTime) || endTime == default(DateTime)) ? "" : " AND a.FaultTime >= @startTime AND a.FaultTime <= @endTime")}" +
                   $"{(code.IsNullOrEmpty() ? "" : (" AND a.DeviceCode " + (condition == 0 ? "=" : "!=") + " @code"))}" +
                   $"{(faultType == -1 ? "" : (" AND a.FaultTypeId " + (condition == 0 ? "=" : "!=") + " @faultType"))}" +
                   $"{(priority == -1 ? "" : (" AND a.Priority " + (condition == 0 ? "=" : "!=") + " @priority"))}" +
                   $"{(state == -1 ? "" : (" AND a.State " + (condition == 0 ? "=" : "!=") + " @state"))}" +
                   $"{(maintainer.IsNullOrEmpty() ? "" : (" AND a.Maintainer " + (condition == 0 ? "=" : "!=") + " @maintainer"))}" +
                   $"{((eStartTime == default(DateTime) || eEndTime == default(DateTime)) ? "" : " AND a.EstimatedTime >= @eStartTime AND a.EstimatedTime <= @eEndTime")}" +
                   $"{(qId == 0 ? "" : (" AND a.Id " + (condition == 0 ? "=" : "!=") + " @qId"))}";

            var result = new DataResult();
            result.datas.AddRange(ServerConfig.ApiDb.Query<FaultDeviceDetail>(sql,
                    new { fState = RepairStateEnum.Complete, startTime, endTime, condition, code, faultType, priority, state, maintainer, eStartTime, eEndTime, qId })
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
                ServerConfig.ApiDb.Query<FaultDeviceDetail>($"SELECT a.*, b.FaultTypeName FROM (SELECT {field}, IFNULL(b.`Code`, a.DeviceCode) DeviceCode FROM `fault_device_repair` a " +
                                                            $"LEFT JOIN `device_library` b ON a.DeviceId = b.Id) a JOIN `fault_type` b ON a.FaultTypeId = b.Id " +
                                                            $"WHERE a.MarkedDelete = 0 AND `State` != @state AND a.DeviceCode = @code;", new { state = RepairStateEnum.Complete, code });
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
                ServerConfig.ApiDb.Query<FaultDevice>("SELECT * FROM `fault_device_repair` WHERE Id IN @id AND `State` != @state AND MarkedDelete = 0;",
                    new { id = faultDevices.Select(x => x.Id), state = RepairStateEnum.Complete });
            if (oldFaultDevices.Count() != faultDevices.Count)
            {
                return Result.GenError<Result>(Error.FaultDeviceNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE fault_device_repair SET `DeviceId` = @DeviceId, `DeviceCode` = @DeviceCode, `FaultTime` = @FaultTime, `Proposer` = @Proposer, " +
                "`FaultDescription` = @FaultDescription, `Priority` = @Priority, `Maintainer` = @Maintainer " +
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
        public Result RepairFaultDevice([FromBody] List<RepairRecord> faultDevices)
        {
            var oldFaultDevices =
                ServerConfig.ApiDb.Query<RepairRecord>("SELECT * FROM `fault_device_repair` WHERE Id IN @id AND `State` != @state AND MarkedDelete = 0;",
                    new { id = faultDevices.Select(x => x.Id), state = RepairStateEnum.Complete });
            if (oldFaultDevices.Count() != faultDevices.Count)
            {
                return Result.GenError<Result>(Error.FaultDeviceNotExist);
            }

            var info = Request.GetIdentityInformation();
            var time = DateTime.Now;
            foreach (var faultDevice in faultDevices)
            {
                var old = oldFaultDevices.FirstOrDefault(x => x.Id == faultDevice.Id);
                if (old.Maintainer != info)
                {
                    return Result.GenError<Result>(Error.FaultDeviceRepairMaintainerError);
                }

                if (faultDevice.State == RepairStateEnum.Confirm && old.State == RepairStateEnum.Default ||
                    faultDevice.State == RepairStateEnum.Repair && old.State == RepairStateEnum.Confirm ||
                    faultDevice.State == RepairStateEnum.Complete && old.State == RepairStateEnum.Repair)
                {
                    faultDevice.MarkedDateTime = time;
                    faultDevice.Maintainer = faultDevice.Maintainer ?? old.Maintainer;
                    faultDevice.Remark = faultDevice.Remark ?? old.Remark;
                }
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE fault_device_repair SET `EstimatedTime` = @EstimatedTime, `Remark` = @Remark, `Maintainer` = @Maintainer, `FaultTypeId1` = @FaultTypeId1 " +
                "WHERE `Id` = @Id;", faultDevices.Where(x => x.MarkedDateTime == time));

            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 指派
        /// </summary>
        /// <param name="faultDevices"></param>
        /// <returns></returns>
        // PUT: api/FaultDevice
        [HttpPut("Assign")]
        public Result AssignFaultDevice([FromBody] List<FaultDevice> faultDevices)
        {
            var oldFaultDevices =
                ServerConfig.ApiDb.Query<FaultDevice>("SELECT * FROM `fault_device_repair` WHERE Id IN @id AND `State` != @state AND MarkedDelete = 0;",
                    new { id = faultDevices.Select(x => x.Id), state = RepairStateEnum.Complete });
            if (oldFaultDevices.Count() != faultDevices.Count)
            {
                return Result.GenError<Result>(Error.FaultDeviceNotExist);
            }

            var maintainer = faultDevices.GroupBy(x => x.Maintainer).Select(y => y.Key);
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `maintainer` WHERE Account IN @Account AND `MarkedDelete` = 0;", new { Account = maintainer }).FirstOrDefault();
            if (cnt != maintainer.Count())
            {
                return Result.GenError<Result>(Error.MaintainerNotExist);
            }

            var time = DateTime.Now;
            foreach (var faultDevice in faultDevices)
            {
                faultDevice.AssignTime = time;
            }
            ServerConfig.ApiDb.Execute(
                "UPDATE fault_device_repair SET `AssignTime` = @AssignTime, `Maintainer` = @Maintainer WHERE `Id` = @Id;", faultDevices);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/FaultDevice
        [HttpPost]
        public Result PostFaultDevice([FromBody] FaultDevice faultDevice)
        {
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
                "INSERT INTO fault_device_repair (`CreateUserId`, `MarkedDateTime`, `DeviceId`, `DeviceCode`, `FaultTime`, `Proposer`, `FaultDescription`, `Priority`, `State`, `FaultTypeId`, `Administrator`, `Maintainer`, `IsReport`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @DeviceId, @DeviceCode, @FaultTime, @Proposer, @FaultDescription, @Priority, @State, @FaultTypeId, @Administrator, @Maintainer, @IsReport);",
                faultDevice);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/FaultDevice/FaultDevices
        [HttpPost("FaultDevices")]
        public Result PostFaultDevice([FromBody] List<FaultDevice> faultDevices)
        {
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
            foreach (var faultDevice in faultDevices)
            {
                faultDevice.CreateUserId = createUserId;
                faultDevice.IsReport = true;

                faultDevice.Administrator = "";
                faultDevice.Maintainer = "";
                faultDevice.Images = faultDevice.Images.IsNullOrEmpty() ? "[]" : faultDevice.Images;
                if (devices != null)
                {
                    faultDevice.DeviceCode = devices.FirstOrDefault(x => x.Id == faultDevice.DeviceId)?.Code ?? (faultDevice.DeviceCode ?? "");
                    faultDevice.Administrator = devices.FirstOrDefault(x => x.Id == faultDevice.DeviceId)?.Administrator ?? "";
                    faultDevice.Maintainer = devices.FirstOrDefault(x => x.Id == faultDevice.DeviceId)?.Administrator ?? "";
                }
            }
            ServerConfig.ApiDb.Execute(
                "INSERT INTO fault_device_repair (`CreateUserId`, `DeviceId`, `DeviceCode`, `FaultTime`, `Proposer`, `FaultDescription`, `Priority`, `FaultTypeId`, `Administrator`, `Maintainer`, `IsReport`, `Images`) " +
                "VALUES (@CreateUserId, @DeviceId, @DeviceCode, @FaultTime, @Proposer, @FaultDescription, @Priority, @FaultTypeId, @Administrator, @Maintainer, @IsReport, @Images);",
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
                ServerConfig.ApiDb.Query<FaultDevice>("SELECT * FROM `fault_device_repair` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.FaultDeviceNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `fault_device_repair` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete, `Cancel`= @Cancel WHERE `Id`= @Id;", new
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