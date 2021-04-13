using ApiManagement.Base.Helper;
using ApiManagement.Base.Server;
using ApiManagement.Models.AccountModel;
using ApiManagement.Models.DeviceManagementModel;
using ApiManagement.Models.FlowCardManagementModel;
using ApiManagement.Models.OtherModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Logger;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Models.StatisticManagementModel;

namespace ApiManagement.Controllers.OtherController
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]"), ApiController]
    public class FlowCardReportController : ControllerBase
    {
        // GET: api/FlowCardReport
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lck">流程卡</param>
        /// <param name="jth">机台号</param>
        /// <param name="gx">工序 研磨1 粗抛 2  精抛 3</param>
        /// <param name="jgqty">加工数</param>
        /// <param name="qty">合格数</param>
        /// <param name="lpqty">裂片数</param>
        /// <param name="back"></param>
        /// <param name="jgr"></param>
        /// <returns></returns>
        [HttpGet]
        public Result GetFlowCardReport([FromQuery] string lck, string jth, int gx, int jgqty, int qty, int lpqty, bool back = true, string jgr = "", string reason = "", bool last = false)
        {
            var time = DateTime.Now;
            //研磨1 粗抛 2  精抛 3
            Console.WriteLine($"时间:{time.ToStr()}, 流程卡:{lck}, 机台号:{jth}, 工序:{gx}, 加工数:{jgqty}, 合格数:{qty}, 裂片数:{lpqty}, 加工人:{jgr}, 原因:{reason}, 末道:{last}, {back}");
            Log.Debug($"时间:{time.ToStr()}, 流程卡:{lck}, 机台号:{jth}, 工序:{gx}, 加工数:{jgqty}, 合格数:{qty}, 裂片数:{lpqty}, 加工人:{jgr}, 原因:{reason}, 末道:{last}, {back}");
            var flowCardReport = new FlowCardReport
            {
                ProcessType = ProcessType.Process,
                Time = time,
                FlowCard = lck,
                Code = jth,
                Step = gx,
                Processor = jgr,
                Back = back,
                Total = jgqty,
                HeGe = qty,
                LiePian = lpqty,
                Last = last,
                Reason = reason,
            };
            FlowCardReportHelper.Instance.Add(flowCardReport);
            return Result.GenError<Result>(Error.Success);
        }


        // GET: api/FlowCardReport
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lck">流程卡</param>
        /// <param name="jth">机台号</param>
        /// <param name="gx">工序 研磨1 粗抛 2  精抛 3</param>
        /// <param name="jgqty">加工数</param>
        /// <param name="qty">合格数</param>
        /// <param name="lpqty">裂片数</param>
        /// <param name="back"></param>
        /// <param name="jgr"></param>
        /// <returns></returns>
        [HttpPost]
        public Result PostFlowCardReport([FromBody] IEnumerable<ErpFlowCardReport> fcs)
        {
            try
            {
                if (fcs == null || !fcs.Any())
                {
                    return Result.GenError<Result>(Error.Fail);
                }
                if (fcs.Any(x => x.time == default(DateTime)))
                {
                    return Result.GenError<Result>(Error.Fail);
                }
                var time = DateTime.Now;
                Console.WriteLine($"批量上报，时间:{time.ToStr()}, 流程卡:{fcs.Count()}");
                Log.Debug($"批量上报，时间:{time.ToStr()}, 流程卡:{fcs.Count()}");
                var addFcs = new List<FlowCardReport>();
                foreach (var fc in fcs)
                {
                    //研磨1 粗抛 2  精抛 3
                    Console.WriteLine($"时间:{fc.time.ToStr()}, 流程卡:{fc.lck}, 机台号:{fc.jth}, 工序:{fc.gx}, 加工数:{fc.jgqty}, 合格数:{fc.qty}, 裂片数:{fc.lpqty}, 加工人:{fc.jgr}, 原因:{fc.reason}, 末道:{fc.last}, {fc.back}");
                    Log.Debug($"时间:{fc.time.ToStr()}, 流程卡:{fc.lck}, 机台号:{fc.jth}, 工序:{fc.gx}, 加工数:{fc.jgqty}, 合格数:{fc.qty}, 裂片数:{fc.lpqty}, 加工人:{fc.jgr}, 原因:{fc.reason}, 末道:{fc.last}, {fc.back}");
                    var flowCardReport = new FlowCardReport
                    {
                        ProcessType = ProcessType.Process,
                        Time = time,
                        FlowCard = fc.lck,
                        Code = fc.jth,
                        Step = fc.gx,
                        Processor = fc.jgr,
                        Total = fc.jgqty,
                        HeGe = fc.qty,
                        LiePian = fc.lpqty,
                        Back = fc.back,
                        Last = fc.last,
                        Reason = fc.reason,
                    };
                    addFcs.Add(flowCardReport);
                }
                if (addFcs.Any())
                {
                    FlowCardReportHelper.Instance.Add(addFcs);
                }

                Console.WriteLine("批量上报完成");
                Log.Debug("批量上报完成");
                return Result.GenError<Result>(Error.Success);
            }
            catch (Exception e)
            {
                Log.Error(e);
                return Result.GenError<Result>(Error.Fail);
            }
        }
    }
}