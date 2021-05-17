using ApiManagement.Base.Helper;
using ApiManagement.Base.Server;
using ApiManagement.Models.DeviceManagementModel;
using ApiManagement.Models.Notify;
using ApiManagement.Models.RepairManagementModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.BaseModel;
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
        /// <param name="grade"></param>
        /// <returns></returns>
        [HttpGet]
        public DataResult GetFaultDevice([FromQuery]DateTime startTime, DateTime endTime, int condition,
            string code, DateTime eStartTime, DateTime eEndTime, int qId, string maintainer = "-1", int faultType = -1, int priority = -1, int grade = -1, int state = -1)
        {
            var faults = FaultDeviceHelper.GetFaultDeviceDetails(startTime, endTime, condition,
                code, eStartTime, eEndTime, qId, maintainer, faultType, priority, grade, state);
            var result = new DataResult();
            result.datas.AddRange(faults.OrderByDescending(x => x.FaultTime).ThenByDescending(x => x.DeviceCode));
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
            string code, DateTime eStartTime, DateTime eEndTime, int qId, string maintainer = "-1", int faultType = -1, int priority = -1, int grade = -1, int state = -1)
        {
            var faults = FaultDeviceHelper.GetDeleteFaultDeviceDetails(startTime, endTime, condition,
                code, eStartTime, eEndTime, qId, maintainer, faultType, priority, grade, state);
            var result = new DataResult();
            result.datas.AddRange(faults.OrderByDescending(x => x.FaultTime).ThenByDescending(x => x.DeviceCode));
            return result;
        }

        ///// 机台号
        ///// <summary>
        ///// </summary>
        ///// <param name="code">机台号</param>
        ///// <returns></returns>
        //// GET: api/FaultDevice/Code/5
        //[HttpGet("Code/{code}")]
        //public DataResult GetFaultDeviceByCode([FromRoute] string code)
        //{
        //    var result = new DataResult();
        //    var field = FaultDevice.GetField(new List<string> { "DeviceCode" }, "a.");
        //    var data =
        //        ServerConfig.ApiDb.Query<FaultDeviceDetail>($"SELECT a.*, b.FaultTypeName FROM (SELECT {field}, IFNULL(b.`Code`, a.DeviceCode) DeviceCode FROM `fault_device_repair` a " +
        //                                                    $"LEFT JOIN `device_library` b ON a.DeviceId = b.Id) a JOIN `fault_type` b ON a.FaultTypeId = b.Id " +
        //                                                    $"WHERE a.MarkedDelete = 0 AND `State` != @state AND a.DeviceCode = @code;", new { state = RepairStateEnum.Complete, code });
        //    if (!data.Any())
        //    {
        //        result.errno = Error.FaultDeviceNotExist;
        //        return result;
        //    }
        //    result.datas.AddRange(data);
        //    return result;
        //}

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="faultDevices"></param>
        /// <returns></returns>
        // PUT: api/FaultDevice
        [HttpPut]
        public Result PutFaultDevice([FromBody] List<FaultDevice> faultDevices)
        {
            var ids = faultDevices.Select(x => x.Id);
            var oldFaultDevices = FaultDeviceHelper.GetDetails(ids);
            if (oldFaultDevices.Count() != faultDevices.Count)
            {
                return Result.GenError<Result>(Error.FaultDeviceNotExist);
            }
            var markedDateTime = DateTime.Now;
            foreach (var faultDevice in faultDevices)
            {
                faultDevice.MarkedDateTime = markedDateTime;
            }
            FaultDeviceHelper.Instance.Update<FaultDevice>(faultDevices);
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
            var ids = faultDevices.Select(x => x.Id);
            var oldFaultDevices = FaultDeviceHelper.GetDetails(ids);
            if (oldFaultDevices.Count() != faultDevices.Count)
            {
                return Result.GenError<Result>(Error.FaultDeviceNotExist);
            }

            var maintainerAccounts = faultDevices.SelectMany(x => x.Maintainers).GroupBy(y => y).Select(z => z.Key);
            var maintainers =
                ServerConfig.ApiDb.Query<Maintainer>("SELECT * FROM `maintainer` WHERE Account IN @Account AND `MarkedDelete` = 0;", new { Account = maintainerAccounts });
            if (maintainers.Count() != maintainerAccounts.Count())
            {
                return Result.GenError<Result>(Error.MaintainerNotExist);
            }

            var time = DateTime.Now;
            foreach (var faultDevice in faultDevices)
            {
                faultDevice.AssignTime = time;
            }
            ServerConfig.ApiDb.Execute(
                "UPDATE fault_device_repair SET `AssignTime` = @AssignTime, `Maintainer` = @Maintainer, `Priority` = @Priority, `Grade` = @Grade WHERE `Id` = @Id;", faultDevices);

            var faultTypes =
                ServerConfig.ApiDb.Query<FaultType>("SELECT Id, FaultTypeName FROM `fault_type` WHERE Id IN @Id;", new { Id = oldFaultDevices.Where(y => faultDevices.Any(z => z.Id == y.Id)).Select(x => x.FaultTypeId) });
            foreach (var faultDevice in oldFaultDevices)
            {
                var assignors = faultDevices.First(x => x.Id == faultDevice.Id).Maintainers;
                var atMobiles = maintainers.Where(x => assignors.Contains(x.Account)).Where(y => !y.Phone.IsNullOrEmpty()).Select(z => z.Phone).ToArray();
                var faultType = faultTypes.First(x => x.Id == faultDevice.FaultTypeId).FaultTypeName ?? "";
                faultDevice.FaultTypeName = faultType;
                var content = NotifyFormat.Format(faultDevice, NotifyMsgEnum.FaultAssign);
                NotifyHelper.Notify(content, NotifyMsgEnum.FaultAssign, NotifyTypeEnum.Repair, NotifyMsgTypeEnum.text, atMobiles);
            }
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
            var ids = faultDevices.Select(x => x.Id);
            var oldFaultDevices = FaultDeviceHelper.GetDetails(ids).Select(ClassExtension.ParentCopyToChild<FaultDevice, RepairRecord>);
            if (oldFaultDevices.Count() != faultDevices.Count)
            {
                return Result.GenError<Result>(Error.FaultDeviceNotExist);
            }

            var states = faultDevices.GroupBy(x => x.State).Select(y => y.Key);
            if (states.Any(x => x != RepairStateEnum.Confirm && x != RepairStateEnum.Repair && x != RepairStateEnum.Complete))
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            var stateDictionary = new Dictionary<RepairStateEnum, List<RepairStateEnum>>
            {
                {RepairStateEnum.Confirm, new List<RepairStateEnum>{ RepairStateEnum.Default}},
                {RepairStateEnum.Repair, new List<RepairStateEnum>{ RepairStateEnum.Default, RepairStateEnum.Confirm}},
                {RepairStateEnum.Complete, new List<RepairStateEnum>{ RepairStateEnum.Repair}},
            };

            var have = false;
            foreach (var state in stateDictionary)
            {
                if (states.All(x => x == state.Key))
                {
                    have = true;
                    if (oldFaultDevices.Any(x => !state.Value.Contains(x.State)))
                    {
                        return Result.GenError<Result>(Error.FaultDeviceStateError);
                    }
                }
            }

            if (!have)
            {
                return Result.GenError<Result>(Error.FaultDeviceStateError);
            }

            var markedDateTime = DateTime.Now;
            var info = Request.GetIdentityInformation();
            foreach (var faultDevice in faultDevices)
            {
                var old = oldFaultDevices.FirstOrDefault(x => x.Id == faultDevice.Id);
                if (!old.Maintainers.Contains(info))
                {
                    return Result.GenError<Result>(Error.FaultDeviceRepairMaintainerError);
                }

                if (faultDevice.State == RepairStateEnum.Confirm || faultDevice.State == RepairStateEnum.Repair && faultDevice.StartTime == default(DateTime))
                {
                    faultDevice.StartTime = markedDateTime;
                }
                faultDevice.MarkedDateTime = markedDateTime;
                faultDevice.Remark = faultDevice.Remark ?? old.Remark;
                faultDevice.FaultSolver = faultDevice.FaultSolver ?? old.FaultSolver;
                faultDevice.SolvePlan = faultDevice.SolvePlan ?? old.SolvePlan;
                faultDevice.FaultTypeId1 = faultDevice.FaultTypeId1 == 0 ? old.FaultTypeId1 : faultDevice.FaultTypeId1;
            }
            RepairRecordHelper.Instance.Update<RepairRecord>(faultDevices);
            return Result.GenError<Result>(Error.Success);
        }

        //// POST: api/FaultDevice
        //[HttpPost]
        //public Result PostFaultDevice([FromBody] FaultDevice faultDevice)
        //{
        //    DeviceLibraryDetail device = null;
        //    if (faultDevice.DeviceId == 0)
        //    {
        //        var cnt =
        //            ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `device_library` WHERE `Code` = @Code AND `MarkedDelete` = 0;", new { Code = faultDevice.DeviceCode }).FirstOrDefault();
        //        if (cnt > 0)
        //        {
        //            return Result.GenError<Result>(Error.ReportDeviceCodeIsExist);
        //        }
        //    }
        //    else
        //    {
        //        device = ServerConfig.ApiDb.Query<DeviceLibraryDetail>("SELECT * FROM `device_library` WHERE Id = @DeviceId AND MarkedDelete = 0;;", new { faultDevice.DeviceId }).FirstOrDefault();
        //    }

        //    faultDevice.CreateUserId = Request.GetIdentityInformation();
        //    faultDevice.MarkedDateTime = DateTime.Now;
        //    faultDevice.Administrator = device?.Administrator ?? "";
        //    faultDevice.Maintainer = device?.Administrator ?? "";

        //    ServerConfig.ApiDb.Execute(
        //        "INSERT INTO fault_device_repair (`CreateUserId`, `MarkedDateTime`, `DeviceId`, `DeviceCode`, `FaultTime`, `Proposer`, `Supplement`, `Priority`, `Grade`, `State`, `FaultTypeId`, `Administrator`, `Maintainer`, `IsReport`) " +
        //        "VALUES (@CreateUserId, @MarkedDateTime, @DeviceId, @DeviceCode, @FaultTime, @Proposer, @Supplement, @Priority, @Grade, @State, @FaultTypeId, @Administrator, @Maintainer, @IsReport);",
        //        faultDevice);

        //    return Result.GenError<Result>(Error.Success);
        //}

        // POST: api/FaultDevice/FaultDevices
        [HttpPost("FaultDevices")]
        public Result PostFaultDevice([FromBody] List<FaultDeviceDetail> faultDevices)
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
                devices = ServerConfig.ApiDb.Query<DeviceLibraryDetail>("SELECT a.*, IFNULL(b.Phone, '') Phone FROM `device_library` a JOIN maintainer b ON a.Administrator = b.Account WHERE a.Id IN @DeviceId AND a.MarkedDelete = 0 AND b.MarkedDelete = 0;",
                    new { DeviceId = faultDevices.Select(x => x.DeviceId) });
            }

            var createUserId = Request.GetIdentityInformation();
            var now = DateTime.Now;
            foreach (var faultDevice in faultDevices)
            {
                faultDevice.CreateUserId = createUserId;
                faultDevice.MarkedDateTime = now;
                faultDevice.IsReport = true;

                faultDevice.Administrator = "";
                faultDevice.Maintainer = "";
                faultDevice.Images = faultDevice.Images.IsNullOrEmpty() ? "[]" : faultDevice.Images;
                if (devices != null)
                {
                    faultDevice.DeviceCode = devices.FirstOrDefault(x => x.Id == faultDevice.DeviceId)?.Code ?? (faultDevice.DeviceCode ?? "");
                    faultDevice.Administrator = devices.FirstOrDefault(x => x.Id == faultDevice.DeviceId)?.Administrator ?? "";
                    faultDevice.Maintainer = devices.FirstOrDefault(x => x.Id == faultDevice.DeviceId)?.Administrator ?? "";
                    faultDevice.Phone = devices.FirstOrDefault(x => x.Id == faultDevice.DeviceId)?.Phone ?? "";
                }
            }

            FaultDeviceHelper.Instance.Add(faultDevices);

            var faultTypes =
                ServerConfig.ApiDb.Query<FaultType>("SELECT Id, FaultTypeName FROM `fault_type` WHERE Id IN @Id;", new { Id = faultDevices.Select(x => x.FaultTypeId) });
            foreach (var faultDevice in faultDevices)
            {
                var atMobiles = new string[] { };
                if (!faultDevice.Phone.IsNullOrEmpty())
                {
                    atMobiles = new[] { faultDevice.Phone };
                }

                var faultType = faultTypes.First(x => x.Id == faultDevice.FaultTypeId).FaultTypeName ?? "";
                faultDevice.FaultTypeName = faultType;
                var content = NotifyFormat.Format(faultDevice, NotifyMsgEnum.FaultReport);
                NotifyHelper.Notify(content, NotifyMsgEnum.FaultReport, NotifyTypeEnum.Repair, NotifyMsgTypeEnum.markdown, atMobiles);
            }
            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="batchDelete"></param>
        /// <returns></returns>
        // DELETE: api/FaultDevice/Id/5
        [HttpDelete]
        public Result DeleteFaultDevice([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var data =
                ServerConfig.ApiDb.Query<FaultDevice>("SELECT * FROM `fault_device_repair` WHERE Id IN @id AND `State` != @state AND MarkedDelete = 0;",
                    new { id = ids, state = RepairStateEnum.Complete });
            if (data.Count() != ids.Count())
            {
                return Result.GenError<Result>(Error.FaultDeviceNotExist);
            }

            FaultDeviceHelper.Cancel(ids);
            foreach (var d in data.GroupBy(x => x.FaultTime).Select(x => x.Key))
            {
                AnalysisHelper.FaultCal(d);
            }
            return Result.GenError<Result>(Error.Success);
        }
    }
}