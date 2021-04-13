using ApiManagement.Base.Helper;
using ApiManagement.Base.Server;
using ApiManagement.Models.SmartFactoryModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Models.BaseModel;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.SmartFactoryController.SmartFactoryFolder
{
    /// <summary>
    /// 
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class SmartFactoryController : ControllerBase
    {
        /// <summary>
        /// 获取工单
        /// </summary>
        /// <param name="qId">工单id</param>
        /// <param name="workOrder">工单</param>
        /// <param name="page">第几页</param>
        /// <param name="limit">条数</param>
        /// <returns></returns>
        // GET: api/SmartFactory/WorkOrder
        [HttpGet("WorkOrder")]
        public SmartResult GetWorkOrder([FromQuery]int qId, string workOrder, int page, int limit = 30)
        {
            page = page < 0 ? 0 : page;
            limit = limit < 0 ? 30 : limit;
            var result = new SmartResult();
            var sql = $"SELECT * FROM t_work_order WHERE MarkedDelete = 0" +
                      $"{(qId == 0 ? "" : " AND Id = @qId")}" +
                      $"{(workOrder.IsNullOrEmpty() ? "" : " AND WorkOrder Like @workOrder")}" +
                      $" LIMIT @page, @limit;";
            result.datas.AddRange(ServerConfig.ApiDb.Query<SmartWorkOrder>(sql, new { qId, workOrder = workOrder + "%", page, limit }));

            sql = $"SELECT COUNT(1) FROM t_work_order WHERE MarkedDelete = 0" +
                  $"{(qId == 0 ? "" : " AND Id = @qId")}" +
                  $"{(workOrder.IsNullOrEmpty() ? "" : " AND WorkOrder Like @workOrder")};";
            result.Count = ServerConfig.ApiDb.Query<int>(sql, new { qId, workOrder = workOrder + "%" }).FirstOrDefault();
            return result;
        }

        /// <summary>
        /// 获取工单 生产线
        /// </summary>
        /// <param name="qId">工单id</param>
        /// <returns></returns>
        // GET: api/SmartFactory/WorkOrderLine
        [HttpGet("WorkOrderLine")]
        public DataResult GetWorkOrderLine([FromQuery]int qId)
        {
            var result = new DataResult();
            //工单
            var workOrder = SmartWorkOrderHelper.Instance.Get<SmartWorkOrder>(qId);
            if (workOrder == null)
            {
                result.errno = Error.SmartWorkOrderNotExist;
                return result;
            }

            //工单生产线 标准流程
            var smartLineWorkOrders = SmartLineWorkOrderHelper.GetSmartLineWorkOrderDetailsByWorkOrderId(qId);
            if (!smartLineWorkOrders.Any())
            {
                return result;
            }
            //流程卡
            var flowCards = SmartFlowCardHelper.GetSmartFlowCardsByWorkOrderId(qId);
            if (!flowCards.Any())
            {
                return result;
            }

            //流程编号id
            var processCodeIds = flowCards.Select(x => x.ProcessCodeId);
            //流程编号
            var processCodes = SmartProcessCodeHelper.Instance.GetByIds<SmartProcessCode>(processCodeIds);
            if (!processCodes.Any())
            {
                return result;
            }

            //流程卡id
            var flowCardIds = flowCards.Select(x => x.Id);
            //流程卡流程错误
            var processFaults = SmartProcessFaultHelper.GetSmartProcessFaultDetailsByFlowCardIds(flowCardIds);

            //标准生产线
            var tempLines = smartLineWorkOrders.GroupBy(x => x.ProcessCodeCategoryId).ToDictionary(x => x.Key);
            foreach (var processFault in processFaults)
            {
                var processCodeId = flowCards.FirstOrDefault(x => x.Id == processFault.FlowCardId)?.ProcessCodeId ?? 0;
                if (processCodeId == 0)
                {
                    continue;
                }

                var processCodeCategoryId = processCodes.FirstOrDefault(x => x.Id == processCodeId)?.CategoryId ?? 0;
                if (processCodeCategoryId == 0)
                {
                    continue;
                }

                if (!tempLines.ContainsKey(processCodeCategoryId))
                {
                    continue;
                }
                var line = tempLines[processCodeCategoryId];
                var process = line.FirstOrDefault(x => x.ProcessId == processFault.ProcessId);
                process.Faults.Add(processFault);
            }
            var lines = new List<SmartWorkOrderLine>();
            foreach (var tempLine in tempLines)
            {
                var line = new SmartWorkOrderLine();
                line.Processes.AddRange(tempLine.Value);
                lines.Add(line);
            }
            result.datas.AddRange(lines);
            return result;
        }

        /// <summary>
        /// 获取工单 报警数量
        /// </summary>
        // GET: api/SmartProcessFault
        [HttpGet("WorkOrderFaultCount")]
        public SmartResult GetWorkOrderFaultCount([FromQuery]int page, int limit = 30)
        {
            page = page < 0 ? 0 : page;
            limit = limit < 0 ? 30 : limit;
            var result = new SmartResult();
            var sql = $"SELECT b.WorkOrderId, b.WorkOrder, COUNT(1) Count FROM `t_process_fault` a " +
                      $"JOIN (SELECT a.*, b.WorkOrder, b.WorkOrderId FROM `t_flow_card` a " +
                      $"JOIN (SELECT a.*, b.WorkOrder FROM `t_task_order` a " +
                      $"JOIN `t_work_order` b ON a.WorkOrderId = b.Id WHERE a.MarkedDelete = 0 AND b.MarkedDelete = 0) b ON a.TaskOrderId = b.Id) b ON a.FlowCardId = b.Id " +
                      $"WHERE a.MarkedDelete = 0 AND Type != @type AND IsDeal = FALSE GROUP BY b.WorkOrderId ORDER BY b.WorkOrderId DESC" +
                      $" LIMIT @page, @limit;";
            result.datas.AddRange(ServerConfig.ApiDb.Query<SmartWorkProcessFaultCount>(sql, new { type = ProcessFault.设备故障, page, limit }));
            sql = $"SELECT COUNT(1) FROM (SELECT b.WorkOrderId FROM `t_process_fault` a " +
                  $"JOIN (SELECT a.*, b.WorkOrder, b.WorkOrderId FROM `t_flow_card` a " +
                  $"JOIN (SELECT a.*, b.WorkOrder FROM `t_task_order` a " +
                  $"JOIN `t_work_order` b ON a.WorkOrderId = b.Id WHERE b.MarkedDelete = 0) b ON a.TaskOrderId = b.Id) b ON a.FlowCardId = b.Id " +
                  $"WHERE a.MarkedDelete = 0 AND Type != @type AND IsDeal = FALSE GROUP BY b.WorkOrderId) a;";
            result.Count = ServerConfig.ApiDb.Query<int>(sql, new { type = ProcessFault.设备故障 }).FirstOrDefault();
            return result;
        }

        /// <summary>
        /// 获取工单 中断数量
        /// </summary>
        // GET: api/SmartProcessFault
        [HttpGet("WorkOrderStopFaultCount")]
        public SmartResult GetWorkOrderStopFaultCount([FromQuery]int page, int limit = 30)
        {
            page = page < 0 ? 0 : page;
            limit = limit < 0 ? 30 : limit;
            var result = new SmartResult();
            var sql = $"SELECT b.WorkOrderId, b.WorkOrder, COUNT(1) Count FROM `t_process_fault` a " +
                      $"JOIN (SELECT a.*, b.WorkOrder, b.WorkOrderId FROM `t_flow_card` a " +
                      $"JOIN (SELECT a.*, b.WorkOrder FROM `t_task_order` a " +
                      $"JOIN `t_work_order` b ON a.WorkOrderId = b.Id WHERE a.MarkedDelete = 0 AND b.MarkedDelete = 0) b ON a.TaskOrderId = b.Id) b ON a.FlowCardId = b.Id " +
                      $"WHERE a.MarkedDelete = 0 AND Type = @type AND IsDeal = FALSE GROUP BY b.WorkOrderId ORDER BY b.WorkOrderId DESC" +
                      $" LIMIT @page, @limit;";
            result.datas.AddRange(ServerConfig.ApiDb.Query<SmartWorkProcessFaultCount>(sql, new { type = ProcessFault.设备故障, page, limit }));
            sql = $"SELECT COUNT(1) FROM (SELECT b.WorkOrderId FROM `t_process_fault` a " +
                  $"JOIN (SELECT a.*, b.WorkOrder, b.WorkOrderId FROM `t_flow_card` a " +
                  $"JOIN (SELECT a.*, b.WorkOrder FROM `t_task_order` a " +
                  $"JOIN `t_work_order` b ON a.WorkOrderId = b.Id WHERE a.MarkedDelete = 0 AND b.MarkedDelete = 0) b ON a.TaskOrderId = b.Id) b ON a.FlowCardId = b.Id " +
                  $"WHERE a.MarkedDelete = 0 AND Type = @type AND IsDeal = FALSE GROUP BY b.WorkOrderId) a;";
            result.Count = ServerConfig.ApiDb.Query<int>(sql, new { type = ProcessFault.设备故障 }).FirstOrDefault();
            return result;
        }

        /// <summary>
        /// 获取工单 报警记录
        /// </summary>
        // GET: api/SmartProcessFault
        [HttpGet("WorkOrderFault")]
        public SmartResult GetWorkOrderFault([FromQuery]int workOrderId, int page, int limit = 30)
        {
            page = page < 0 ? 0 : page;
            limit = limit < 0 ? 30 : limit;
            var result = new SmartResult();
            var sql = $"SELECT a.*, b.WorkOrder, b.FlowCard, c.Process FROM `t_process_fault` a " +
                      $"JOIN (SELECT a.*, b.WorkOrder, b.WorkOrderId FROM `t_flow_card` a " +
                      $"JOIN (SELECT a.*, b.WorkOrder FROM `t_task_order` a " +
                      $"JOIN `t_work_order` b ON a.WorkOrderId = b.Id WHERE a.MarkedDelete = 0 AND b.MarkedDelete = 0 AND a.WorkOrderId = @workOrderId) b ON a.TaskOrderId = b.Id) b ON a.FlowCardId = b.Id " +
                      $"JOIN (SELECT a.Id, b.ProcessId, b.Process FROM `t_flow_card_process` a " +
                      $"JOIN (SELECT a.Id, b.ProcessId, b.Process FROM `t_product_process` a " +
                      $"JOIN (SELECT a.Id, a.ProcessId, b.Process FROM `t_process_code_category_process` a " +
                      $"JOIN `t_process` b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) c ON a.ProcessId = c.Id " +
                      $"WHERE a.MarkedDelete = 0 AND Type != @type AND IsDeal = FALSE ORDER BY a.Id DESC" +
                      $" LIMIT @page, @limit;";
            result.datas.AddRange(ServerConfig.ApiDb.Query<SmartWorkOrderFault>(sql, new { workOrderId, type = ProcessFault.设备故障, page, limit }));
            sql = $"SELECT COUNT(1) FROM (SELECT b.WorkOrderId FROM `t_process_fault` a " +
                  $"JOIN (SELECT a.*, b.WorkOrder, b.WorkOrderId FROM `t_flow_card` a " +
                  $"JOIN (SELECT a.*, b.WorkOrder FROM `t_task_order` a " +
                  $"JOIN `t_work_order` b ON a.WorkOrderId = b.Id WHERE a.MarkedDelete = 0 AND b.MarkedDelete = 0 AND a.WorkOrderId = @workOrderId) b ON a.TaskOrderId = b.Id) b ON a.FlowCardId = b.Id " +
                  $"JOIN (SELECT a.Id, b.ProcessId, b.Process FROM `t_flow_card_process` a " +
                  $"JOIN (SELECT a.Id, b.ProcessId, b.Process FROM `t_product_process` a " +
                  $"JOIN (SELECT a.Id, a.ProcessId, b.Process FROM `t_process_code_category_process` a " +
                  $"JOIN `t_process` b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) c ON a.ProcessId = c.Id " +
                  $"WHERE a.MarkedDelete = 0 AND Type != @type AND IsDeal = FALSE) a;";
            result.Count = ServerConfig.ApiDb.Query<int>(sql, new { workOrderId, type = ProcessFault.设备故障 }).FirstOrDefault();
            return result;
        }

        /// <summary>
        /// 获取工单 中断记录
        /// </summary>
        // GET: api/SmartProcessFault
        [HttpGet("WorkOrderStopFault")]
        public SmartResult GetWorkOrderStopFault([FromQuery]int workOrderId, int page, int limit = 30)
        {
            page = page < 0 ? 0 : page;
            limit = limit < 0 ? 30 : limit;
            var result = new SmartResult();
            var sql = $"SELECT a.*, b.WorkOrder, b.FlowCard, c.Process FROM `t_process_fault` a " +
                      $"JOIN (SELECT a.*, b.WorkOrder, b.WorkOrderId FROM `t_flow_card` a " +
                      $"JOIN (SELECT a.*, b.WorkOrder FROM `t_task_order` a " +
                      $"JOIN `t_work_order` b ON a.WorkOrderId = b.Id WHERE a.MarkedDelete = 0 AND b.MarkedDelete = 0 AND a.WorkOrderId = @workOrderId) b ON a.TaskOrderId = b.Id) b ON a.FlowCardId = b.Id " +
                      $"JOIN (SELECT a.Id, b.ProcessId, b.Process FROM `t_flow_card_process` a " +
                      $"JOIN (SELECT a.Id, b.ProcessId, b.Process FROM `t_product_process` a " +
                      $"JOIN (SELECT a.Id, a.ProcessId, b.Process FROM `t_process_code_category_process` a " +
                      $"JOIN `t_process` b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) c ON a.ProcessId = c.Id " +
                      $"WHERE a.MarkedDelete = 0 AND Type = @type AND IsDeal = FALSE ORDER BY a.Id DESC" +
                      $" LIMIT @page, @limit;";
            result.datas.AddRange(ServerConfig.ApiDb.Query<SmartWorkOrderFault>(sql, new { workOrderId, type = ProcessFault.设备故障, page, limit }));
            sql = $"SELECT COUNT(1) FROM (SELECT b.WorkOrderId FROM `t_process_fault` a " +
                  $"JOIN (SELECT a.*, b.WorkOrder, b.WorkOrderId FROM `t_flow_card` a " +
                  $"JOIN (SELECT a.*, b.WorkOrder FROM `t_task_order` a " +
                  $"JOIN `t_work_order` b ON a.WorkOrderId = b.Id WHERE a.MarkedDelete = 0 AND b.MarkedDelete = 0 AND a.WorkOrderId = @workOrderId) b ON a.TaskOrderId = b.Id) b ON a.FlowCardId = b.Id " +
                  $"JOIN (SELECT a.Id, b.ProcessId, b.Process FROM `t_flow_card_process` a " +
                  $"JOIN (SELECT a.Id, b.ProcessId, b.Process FROM `t_product_process` a " +
                  $"JOIN (SELECT a.Id, a.ProcessId, b.Process FROM `t_process_code_category_process` a " +
                  $"JOIN `t_process` b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) c ON a.ProcessId = c.Id " +
                  $"WHERE a.MarkedDelete = 0 AND Type = @type AND IsDeal = FALSE) a;";
            result.Count = ServerConfig.ApiDb.Query<int>(sql, new { workOrderId, type = ProcessFault.设备故障 }).FirstOrDefault();
            return result;
        }




        /// <summary>
        /// 获取任务单
        /// </summary>
        /// <param name="qId">工单id</param>
        /// <param name="taskOrder">任务单</param>
        /// <param name="workOrderId">工单id</param>
        /// <param name="page">第几页</param>
        /// <param name="limit">条数</param>
        /// <returns></returns>
        // GET: api/SmartFactory/TaskOrder
        [HttpGet("TaskOrder")]
        public SmartResult GetTaskOrder([FromQuery]int qId, string taskOrder, int workOrderId, int page, int limit = 30)
        {
            page = page < 0 ? 0 : page;
            limit = limit < 0 ? 30 : limit;
            var result = new SmartResult();
            var sql = $"SELECT a.*, b.Product FROM t_task_order a JOIN t_product b ON a.ProductId = b.Id WHERE a.MarkedDelete = 0" +
                      $"{(qId == 0 ? "" : " AND a.Id = @qId")}" +
                      $"{(taskOrder.IsNullOrEmpty() ? "" : " AND a.TaskOrder Like @taskOrder")}" +
                      $"{(workOrderId == 0 ? "" : " AND a.WorkOrderId = @workOrderId")}" +
                      $" LIMIT @page, @limit;";
            result.datas.AddRange(ServerConfig.ApiDb.Query<SmartTaskOrderDetail>(sql, new { qId, taskOrder = taskOrder + "%", workOrderId, page, limit }));

            sql = $"SELECT COUNT(1) FROM t_task_order WHERE MarkedDelete = 0" +
                  $"{(qId == 0 ? "" : " AND Id = @qId")}" +
                  $"{(taskOrder.IsNullOrEmpty() ? "" : " AND TaskOrder Like @taskOrder")}" +
                  $"{(workOrderId == 0 ? "" : " AND WorkOrderId = @workOrderId")}";
            result.Count = ServerConfig.ApiDb.Query<int>(sql, new { qId, taskOrder = taskOrder + "%", workOrderId }).FirstOrDefault();
            return result;
        }

        /// <summary>
        /// 获取任务单 生产线
        /// </summary>
        /// <param name="qId">任务id</param>
        /// <returns></returns>
        // GET: api/SmartFactory/TaskOrderLine
        [HttpGet("TaskOrderLine")]
        public DataResult GetTaskOrderLine([FromQuery]int qId)
        {
            var result = new DataResult();
            //任务单
            var taskOrder = SmartTaskOrderHelper.Instance.Get<SmartTaskOrder>(qId);
            if (taskOrder == null)
            {
                result.errno = Error.SmartTaskOrderNotExist;
                return result;
            }
            //任务单生产线 标准流程
            var smartLineTaskOrders = SmartLineTaskOrderHelper.GetSmartLineTaskOrderDetailsByTaskOrderId(qId);
            if (!smartLineTaskOrders.Any())
            {
                return result;
            }
            //流程卡
            var flowCards = SmartFlowCardHelper.GetSmartFlowCardsByTaskOrderId(qId);
            if (!flowCards.Any())
            {
                return result;
            }

            //流程编号id
            var processCodeIds = flowCards.Select(x => x.ProcessCodeId);
            //流程编号
            var processCodes = SmartProcessCodeHelper.Instance.GetByIds<SmartProcessCode>(processCodeIds);
            if (!processCodes.Any())
            {
                return result;
            }

            //流程卡id
            var flowCardIds = flowCards.Select(x => x.Id);
            //流程卡流程错误
            var processFaults = SmartProcessFaultHelper.GetSmartProcessFaultDetailsByFlowCardIds(flowCardIds);

            //标准生产线
            var tempLines = smartLineTaskOrders.GroupBy(x => x.ProcessCodeCategoryId).ToDictionary(x => x.Key);
            foreach (var processFault in processFaults)
            {
                var processCodeId = flowCards.FirstOrDefault(x => x.Id == processFault.FlowCardId)?.ProcessCodeId ?? 0;
                if (processCodeId == 0)
                {
                    continue;
                }

                var processCodeCategoryId = processCodes.FirstOrDefault(x => x.Id == processCodeId)?.CategoryId ?? 0;
                if (processCodeCategoryId == 0)
                {
                    continue;
                }

                if (!tempLines.ContainsKey(processCodeCategoryId))
                {
                    continue;
                }
                var line = tempLines[processCodeCategoryId];
                var process = line.FirstOrDefault(x => x.ProcessId == processFault.ProcessId);
                process.Faults.Add(processFault);
            }
            var lines = new List<SmartTaskOrderLine>();
            foreach (var tempLine in tempLines)
            {
                var line = new SmartTaskOrderLine();
                line.Processes.AddRange(tempLine.Value);
                lines.Add(line);
            }
            result.datas.AddRange(lines);
            return result;
        }

        /// <summary>
        /// 获取任务单 报警数量
        /// </summary>
        // GET: api/SmartProcessFault
        [HttpGet("TaskOrderFaultCount")]
        public SmartResult GetTaskOrderFaultCount([FromQuery]int page, int limit = 30)
        {
            page = page < 0 ? 0 : page;
            limit = limit < 0 ? 30 : limit;
            var result = new SmartResult();
            var sql = $"SELECT b.TaskOrderId, b.TaskOrder, COUNT(1) Count FROM `t_process_fault` a " +
                      $"JOIN (SELECT a.*, b.TaskOrder FROM `t_flow_card` a " +
                      $"JOIN `t_task_order` b ON a.TaskOrderId = b.Id WHERE b.MarkedDelete = 0) b ON a.FlowCardId = b.Id " +
                      $"WHERE a.MarkedDelete = 0 AND Type != @type AND IsDeal = FALSE GROUP BY b.TaskOrderId ORDER BY b.TaskOrderId DESC" +
                      $" LIMIT @page, @limit;";
            result.datas.AddRange(ServerConfig.ApiDb.Query<SmartTaskProcessFaultCount>(sql, new { type = ProcessFault.设备故障, page, limit }));
            sql = $"SELECT COUNT(1) FROM (SELECT b.TaskOrderId FROM `t_process_fault` a " +
                  $"JOIN (SELECT a.*, b.TaskOrder FROM `t_flow_card` a " +
                  $"JOIN `t_task_order` b ON a.TaskOrderId = b.Id WHERE b.MarkedDelete = 0) b ON a.FlowCardId = b.Id " +
                  $"WHERE a.MarkedDelete = 0 AND Type != @type AND IsDeal = FALSE GROUP BY b.TaskOrderId) a;";
            result.Count = ServerConfig.ApiDb.Query<int>(sql, new { type = ProcessFault.设备故障 }).FirstOrDefault();
            return result;
        }

        /// <summary>
        /// 获取任务单 中断数量
        /// </summary>
        // GET: api/SmartProcessFault
        [HttpGet("TaskOrderStopFaultCount")]
        public SmartResult GetTaskOrderStopFaultCount([FromQuery]int page, int limit = 30)
        {
            page = page < 0 ? 0 : page;
            limit = limit < 0 ? 30 : limit;
            var result = new SmartResult();
            var sql = $"SELECT b.TaskOrderId, b.TaskOrder, COUNT(1) Count FROM `t_process_fault` a " +
                      $"JOIN (SELECT a.*, b.TaskOrder FROM `t_flow_card` a " +
                      $"JOIN `t_task_order` b ON a.TaskOrderId = b.Id WHERE b.MarkedDelete = 0) b ON a.FlowCardId = b.Id " +
                      $"WHERE a.MarkedDelete = 0 AND Type = @type AND IsDeal = FALSE GROUP BY b.TaskOrderId ORDER BY b.TaskOrderId DESC" +
                      $" LIMIT @page, @limit;";
            result.datas.AddRange(ServerConfig.ApiDb.Query<SmartTaskProcessFaultCount>(sql, new { type = ProcessFault.设备故障, page, limit }));
            sql = $"SELECT COUNT(1) FROM (SELECT b.TaskOrderId FROM `t_process_fault` a " +
                  $"JOIN (SELECT a.*, b.TaskOrder FROM `t_flow_card` a " +
                  $"JOIN `t_task_order` b ON a.TaskOrderId = b.Id WHERE b.MarkedDelete = 0) b ON a.FlowCardId = b.Id " +
                  $"WHERE a.MarkedDelete = 0 AND Type = @type AND IsDeal = FALSE GROUP BY b.TaskOrderId) a;";
            result.Count = ServerConfig.ApiDb.Query<int>(sql, new { type = ProcessFault.设备故障 }).FirstOrDefault();
            return result;
        }

        /// <summary>
        /// 获取任务单 中断记录
        /// </summary>
        // GET: api/SmartProcessFault
        [HttpGet("TaskOrderFault")]
        public SmartResult GetTaskOrderFault([FromQuery]int taskOrderId, int page, int limit = 30)
        {
            page = page < 0 ? 0 : page;
            limit = limit < 0 ? 30 : limit;
            var result = new SmartResult();
            var sql = $"SELECT a.*, b.TaskOrder, b.FlowCard, c.Process FROM `t_process_fault` a " +
                      $"JOIN (SELECT a.*, b.TaskOrder FROM `t_flow_card` a " +
                      $"JOIN `t_task_order` b ON a.TaskOrderId = b.Id WHERE a.MarkedDelete = 0 AND b.MarkedDelete = 0 AND a.TaskOrderId = @taskOrderId) b ON a.FlowCardId = b.Id " +
                      $"JOIN (SELECT a.Id, b.ProcessId, b.Process FROM `t_flow_card_process` a " +
                      $"JOIN (SELECT a.Id, b.ProcessId, b.Process FROM `t_product_process` a " +
                      $"JOIN (SELECT a.Id, a.ProcessId, b.Process FROM `t_process_code_category_process` a " +
                      $"JOIN `t_process` b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) c ON a.ProcessId = c.Id " +
                      $"WHERE a.MarkedDelete = 0 AND Type != @type AND IsDeal = FALSE ORDER BY b.TaskOrderId DESC" +
                      $" LIMIT @page, @limit;";
            result.datas.AddRange(ServerConfig.ApiDb.Query<SmartTaskOrderFault>(sql, new { taskOrderId, type = ProcessFault.设备故障, page, limit }));
            sql = $"SELECT COUNT(1) FROM (SELECT b.TaskOrderId FROM `t_process_fault` a " +
                  $"JOIN (SELECT a.*, b.TaskOrder FROM `t_flow_card` a " +
                  $"JOIN `t_task_order` b ON a.TaskOrderId = b.Id WHERE a.MarkedDelete = 0 AND b.MarkedDelete = 0 AND a.TaskOrderId = @taskOrderId) b ON a.FlowCardId = b.Id " +
                  $"JOIN (SELECT a.Id, b.ProcessId, b.Process FROM `t_flow_card_process` a " +
                  $"JOIN (SELECT a.Id, b.ProcessId, b.Process FROM `t_product_process` a " +
                  $"JOIN (SELECT a.Id, a.ProcessId, b.Process FROM `t_process_code_category_process` a " +
                  $"JOIN `t_process` b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) c ON a.ProcessId = c.Id " +
                  $"WHERE a.MarkedDelete = 0 AND Type != @type AND IsDeal = FALSE) a;";
            result.Count = ServerConfig.ApiDb.Query<int>(sql, new { taskOrderId, type = ProcessFault.设备故障 }).FirstOrDefault();
            return result;
        }

        /// <summary>
        /// 获取任务单 中断记录
        /// </summary>
        // GET: api/SmartProcessFault
        [HttpGet("TaskOrderStopFault")]
        public SmartResult GetTaskOrderStopFault([FromQuery]int taskOrderId, int page, int limit = 30)
        {
            page = page < 0 ? 0 : page;
            limit = limit < 0 ? 30 : limit;
            var result = new SmartResult();
            var sql = $"SELECT a.*, b.TaskOrder, b.FlowCard, c.Process FROM `t_process_fault` a " +
                      $"JOIN (SELECT a.*, b.TaskOrder FROM `t_flow_card` a " +
                      $"JOIN `t_task_order` b ON a.TaskOrderId = b.Id WHERE a.MarkedDelete = 0 AND b.MarkedDelete = 0 AND a.TaskOrderId = @taskOrderId) b ON a.FlowCardId = b.Id " +
                      $"JOIN (SELECT a.Id, b.ProcessId, b.Process FROM `t_flow_card_process` a " +
                      $"JOIN (SELECT a.Id, b.ProcessId, b.Process FROM `t_product_process` a " +
                      $"JOIN (SELECT a.Id, a.ProcessId, b.Process FROM `t_process_code_category_process` a " +
                      $"JOIN `t_process` b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) c ON a.ProcessId = c.Id " +
                      $"WHERE a.MarkedDelete = 0 AND Type = @type AND IsDeal = FALSE ORDER BY b.TaskOrderId DESC" +
                      $" LIMIT @page, @limit;";
            result.datas.AddRange(ServerConfig.ApiDb.Query<SmartTaskOrderFault>(sql, new { taskOrderId, type = ProcessFault.设备故障, page, limit }));
            sql = $"SELECT COUNT(1) FROM (SELECT b.TaskOrderId FROM `t_process_fault` a " +
                  $"JOIN (SELECT a.*, b.TaskOrder FROM `t_flow_card` a " +
                      $"JOIN `t_task_order` b ON a.TaskOrderId = b.Id WHERE a.MarkedDelete = 0 AND b.MarkedDelete = 0 AND a.TaskOrderId = @taskOrderId) b ON a.FlowCardId = b.Id " +
                  $"JOIN (SELECT a.Id, b.ProcessId, b.Process FROM `t_flow_card_process` a " +
                  $"JOIN (SELECT a.Id, b.ProcessId, b.Process FROM `t_product_process` a " +
                  $"JOIN (SELECT a.Id, a.ProcessId, b.Process FROM `t_process_code_category_process` a " +
                  $"JOIN `t_process` b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) c ON a.ProcessId = c.Id " +
                  $"WHERE a.MarkedDelete = 0 AND Type = @type AND IsDeal = FALSE) a;";
            result.Count = ServerConfig.ApiDb.Query<int>(sql, new { taskOrderId, type = ProcessFault.设备故障 }).FirstOrDefault();
            return result;
        }







        /// <summary>
        /// 获取流程卡
        /// </summary>
        /// <param name="qId">工单id</param>
        /// <param name="flowCard">流程卡</param>
        /// <param name="taskOrderId">任务单id</param>
        /// <param name="page">第几页</param>
        /// <param name="limit">条数</param>
        /// <returns></returns>
        // GET: api/SmartFactory/SmartFlowCard
        [HttpGet("SmartFlowCard")]
        public DataResult GetFlowCard([FromQuery]int qId, string flowCard, int taskOrderId, int page, int limit = 30)
        {
            var result = new SmartResult();
            var sql = $"SELECT * FROM t_flow_card WHERE MarkedDelete = 0" +
                      $"{(qId == 0 ? "" : " AND Id = @qId")}" +
                      $"{(flowCard.IsNullOrEmpty() ? "" : " AND FlowCard Like @flowCard")}" +
                      $"{(taskOrderId == 0 ? "" : " AND TaskOrderId = @taskOrderId")}" +
                      $" LIMIT @page, @limit;";
            var flowCards = ServerConfig.ApiDb.Query<SmartFlowCardUI>(sql, new { qId, flowCard = flowCard + "%", taskOrderId, page, limit });

            var processes = new List<SmartFlowCardProcessDetail>();
            var undoFlowCardIds = flowCards.Where(x => x.State == SmartFlowCardState.未加工).Select(x => x.Id);
            if (undoFlowCardIds.Any())
            {
                processes.AddRange(ServerConfig.ApiDb.Query<SmartFlowCardProcessDetail>(
                "SELECT * FROM (SELECT a.*, b.Process FROM `t_flow_card_process` a " +
                "JOIN (SELECT a.Id, b.Process FROM `t_product_process` a " +
                "JOIN (SELECT a.Id, b.Process FROM `t_process_code_category_process` a " +
                "JOIN `t_process` b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id " +
                "WHERE a.MarkedDelete = 0 AND a.FlowCardId IN @undoFlowCardIds ORDER BY a.Id DESC) a GROUP BY a.FlowCardId;",
                new { undoFlowCardIds }));
            }

            var doFlowCardIds = flowCards.Where(x => x.State != SmartFlowCardState.未加工).Select(x => x.Id);
            if (doFlowCardIds.Any())
            {
                processes.AddRange(ServerConfig.ApiDb.Query<SmartFlowCardProcessDetail>(
                "SELECT * FROM (SELECT a.*, b.Process FROM `t_flow_card_process` a " +
                "JOIN (SELECT a.Id, b.Process FROM `t_product_process` a " +
                "JOIN (SELECT a.Id, b.Process FROM `t_process_code_category_process` a " +
                "JOIN `t_process` b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id " +
                "WHERE a.MarkedDelete = 0 AND (a.State != 0 OR NOT ISNULL(a.StartTime)) AND a.FlowCardId IN @doFlowCardIds ORDER BY a.Id DESC) a GROUP BY a.FlowCardId;",
                new { doFlowCardIds }));
            }

            foreach (var process in processes)
            {
                var fc = flowCards.FirstOrDefault(x => x.Id == process.FlowCardId);
                if (fc != null)
                {
                    fc.Process = process.Process;
                    fc.Progress = process.Progress;
                }
            }

            result.datas.AddRange(flowCards);
            sql = $"SELECT COUNT(1) FROM t_flow_card WHERE MarkedDelete = 0" +
                  $"{(qId == 0 ? "" : " AND Id = @qId")}" +
                  $"{(flowCard.IsNullOrEmpty() ? "" : " AND FlowCard Like @flowCard")}" +
                  $"{(taskOrderId == 0 ? "" : " AND TaskOrderId = @taskOrderId")}";
            result.Count = ServerConfig.ApiDb.Query<int>(sql, new { qId, flowCard = flowCard + "%", taskOrderId }).FirstOrDefault();
            return result;
        }

        /// <summary>
        /// 获取流程卡 生产线
        /// </summary>
        /// <param name="qId">流程卡id</param>
        /// <returns></returns>
        // GET: api/SmartFactory/FlowCardLine
        [HttpGet("FlowCardLine")]
        public DataResult GetFlowCardLine([FromQuery]int qId)
        {
            var result = new DataResult();
            //流程卡
            var flowCard = SmartFlowCardHelper.Instance.Get<SmartFlowCard>(qId);
            if (flowCard == null)
            {
                return result;
            }

            //流程卡流程错误
            var processFaults = SmartProcessFaultHelper.GetSmartProcessFaultDetails(qId);
            //流程卡流程
            var smartFlowCardProcesses =
                SmartLineFlowCardHelper.GetSmartLineFlowCardsByFlowCardId(qId);

            foreach (var processFault in processFaults)
            {
                var process = smartFlowCardProcesses.FirstOrDefault(x => x.ProcessId == processFault.ProcessId);
                process.Faults.Add(processFault);
            }
            result.datas.Add(smartFlowCardProcesses);
            return result;
        }

        /// <summary>
        /// 获取流程卡 报警数量
        /// </summary>
        // GET: api/SmartProcessFault
        [HttpGet("FlowCardFaultCount")]
        public SmartResult GetFlowCardFaultCount([FromQuery]int page, int limit = 30)
        {
            page = page < 0 ? 0 : page;
            limit = limit < 0 ? 30 : limit;
            var result = new SmartResult();
            var sql = $"SELECT a.FlowCardId, b.FlowCard, COUNT(1) Count FROM `t_process_fault` a " +
                      $"JOIN (SELECT * FROM `t_flow_card` WHERE MarkedDelete = 0) b ON a.FlowCardId = b.Id " +
                      $"WHERE a.MarkedDelete = 0 AND Type != @type AND IsDeal = FALSE GROUP BY a.FlowCardId ORDER BY a.FlowCardId DESC" +
                      $" LIMIT @page, @limit;";
            result.datas.AddRange(ServerConfig.ApiDb.Query<SmartFlowCardProcessFaultCount>(sql, new { type = ProcessFault.设备故障, page, limit }));
            sql = $"SELECT COUNT(1) FROM (SELECT a.FlowCardId FROM `t_process_fault` a " +
                  $"JOIN (SELECT * FROM `t_flow_card` WHERE MarkedDelete = 0) b ON a.FlowCardId = b.Id " +
                  $"WHERE a.MarkedDelete = 0 AND Type != @type AND IsDeal = FALSE GROUP BY a.FlowCardId) a;";
            result.Count = ServerConfig.ApiDb.Query<int>(sql, new { type = ProcessFault.设备故障 }).FirstOrDefault();
            return result;
        }

        /// <summary>
        /// 获取流程卡 中断数量
        /// </summary>
        // GET: api/SmartProcessFault
        [HttpGet("FlowCardStopFaultCount")]
        public SmartResult GetFlowCardProcessFaultCount([FromQuery]int page, int limit = 30)
        {
            page = page < 0 ? 0 : page;
            limit = limit < 0 ? 30 : limit;
            var result = new SmartResult();
            var sql = $"SELECT a.FlowCardId, b.FlowCard, COUNT(1) Count FROM `t_process_fault` a " +
                      $"JOIN (SELECT * FROM `t_flow_card` WHERE MarkedDelete = 0) b ON a.FlowCardId = b.Id " +
                      $"WHERE a.MarkedDelete = 0 AND Type = @type AND IsDeal = FALSE GROUP BY a.FlowCardId ORDER BY a.FlowCardId DESC" +
                      $" LIMIT @page, @limit;";
            result.datas.AddRange(ServerConfig.ApiDb.Query<SmartFlowCardProcessFaultCount>(sql, new { type = ProcessFault.设备故障, page, limit }));
            sql = $"SELECT COUNT(1) FROM (SELECT a.FlowCardId FROM `t_process_fault` a " +
                  $"JOIN (SELECT * FROM `t_flow_card` WHERE MarkedDelete = 0) b ON a.FlowCardId = b.Id " +
                  $"WHERE a.MarkedDelete = 0 AND Type = @type AND IsDeal = FALSE GROUP BY a.FlowCardId) a;";
            result.Count = ServerConfig.ApiDb.Query<int>(sql, new { type = ProcessFault.设备故障 }).FirstOrDefault();
            return result;
        }

        /// <summary>
        /// 获取流程卡 报警记录
        /// </summary>
        // GET: api/SmartProcessFault
        [HttpGet("FlowCardFault")]
        public SmartResult GetFlowCardFault([FromQuery]int flowCardId, int page, int limit = 30)
        {
            page = page < 0 ? 0 : page;
            limit = limit < 0 ? 30 : limit;
            var result = new SmartResult();
            var sql = $"SELECT a.*, b.FlowCard, c.Process FROM `t_process_fault` a " +
                      $"JOIN (SELECT * FROM `t_flow_card` WHERE MarkedDelete = 0) b ON a.FlowCardId = b.Id " +
                      $"JOIN (SELECT a.Id, b.ProcessId, b.Process FROM `t_flow_card_process` a " +
                      $"JOIN (SELECT a.Id, b.ProcessId, b.Process FROM `t_product_process` a " +
                      $"JOIN (SELECT a.Id, a.ProcessId, b.Process FROM `t_process_code_category_process` a " +
                      $"JOIN `t_process` b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) c ON a.ProcessId = c.Id " +
                      $"WHERE a.MarkedDelete = 0 AND Type != @type AND IsDeal = FALSE AND a.FlowCardId = @flowCardId ORDER BY a.FlowCardId DESC" +
                      $" LIMIT @page, @limit;";
            result.datas.AddRange(ServerConfig.ApiDb.Query<SmartFlowCardFault>(sql, new { flowCardId, type = ProcessFault.设备故障, page, limit }));
            sql = $"SELECT COUNT(1) FROM (SELECT a.FlowCardId FROM `t_process_fault` a " +
                  $"JOIN (SELECT * FROM `t_flow_card` WHERE MarkedDelete = 0) b ON a.FlowCardId = b.Id " +
                  $"JOIN (SELECT a.Id, b.ProcessId, b.Process FROM `t_flow_card_process` a " +
                  $"JOIN (SELECT a.Id, b.ProcessId, b.Process FROM `t_product_process` a " +
                  $"JOIN (SELECT a.Id, a.ProcessId, b.Process FROM `t_process_code_category_process` a " +
                  $"JOIN `t_process` b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) c ON a.ProcessId = c.Id " +
                  $"WHERE a.MarkedDelete = 0 AND Type != @type AND IsDeal = FALSE AND a.FlowCardId = @flowCardId) a;";
            result.Count = ServerConfig.ApiDb.Query<int>(sql, new { flowCardId, type = ProcessFault.设备故障 }).FirstOrDefault();
            return result;
        }

        /// <summary>
        /// 获取流程卡 中断记录
        /// </summary>
        // GET: api/SmartProcessFault
        [HttpGet("FlowCardStopFault")]
        public SmartResult GetFlowCardProcessFault([FromQuery]int flowCardId, int page, int limit = 30)
        {
            page = page < 0 ? 0 : page;
            limit = limit < 0 ? 30 : limit;
            var result = new SmartResult();
            var sql = $"SELECT a.*, b.FlowCard, c.Process FROM `t_process_fault` a " +
                      $"JOIN (SELECT * FROM `t_flow_card` WHERE MarkedDelete = 0) b ON a.FlowCardId = b.Id " +
                      $"JOIN (SELECT a.Id, b.ProcessId, b.Process FROM `t_flow_card_process` a " +
                      $"JOIN (SELECT a.Id, b.ProcessId, b.Process FROM `t_product_process` a " +
                      $"JOIN (SELECT a.Id, a.ProcessId, b.Process FROM `t_process_code_category_process` a " +
                      $"JOIN `t_process` b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) c ON a.ProcessId = c.Id " +
                      $"WHERE a.MarkedDelete = 0 AND Type = @type AND IsDeal = FALSE AND a.FlowCardId = @flowCardId ORDER BY a.FlowCardId DESC" +
                      $" LIMIT @page, @limit;";
            result.datas.AddRange(ServerConfig.ApiDb.Query<SmartFlowCardFault>(sql, new { flowCardId, type = ProcessFault.设备故障, page, limit }));
            sql = $"SELECT COUNT(1) FROM (SELECT a.FlowCardId FROM `t_process_fault` a " +
                  $"JOIN (SELECT * FROM `t_flow_card` WHERE MarkedDelete = 0) b ON a.FlowCardId = b.Id " +
                  $"JOIN (SELECT a.Id, b.ProcessId, b.Process FROM `t_flow_card_process` a " +
                  $"JOIN (SELECT a.Id, b.ProcessId, b.Process FROM `t_product_process` a " +
                  $"JOIN (SELECT a.Id, a.ProcessId, b.Process FROM `t_process_code_category_process` a " +
                  $"JOIN `t_process` b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) c ON a.ProcessId = c.Id " +
                  $"WHERE a.MarkedDelete = 0 AND Type = @type AND IsDeal = FALSE AND a.FlowCardId = @flowCardId) a;";
            result.Count = ServerConfig.ApiDb.Query<int>(sql, new { flowCardId, type = ProcessFault.设备故障 }).FirstOrDefault();
            return result;
        }






        /// <summary>
        /// 排程
        /// </summary>
        /// <returns></returns>
        // POST: api/SmartFactory
        [HttpGet("Schedule")]
        public DataResult GetDeviceSchedule()
        {
            var result = new DataResult();
            var deviceList = SmartDeviceHelper.Instance.GetAll<SmartDevice>();
            var categories = SmartDeviceCategoryHelper.Instance.GetAll<SmartDeviceCategory>();
            var devices = SimulateHelper.Devices();
            var data = new List<SmartProcessDeviceDetail>();
            data.AddRange(devices.Select(ClassExtension.ParentCopyToChild<SmartProcessDevice, SmartProcessDeviceDetail>));
            foreach (var device in data)
            {
                var d = deviceList.FirstOrDefault(x => x.Id == device.Id);
                if (d != null)
                {
                    device.Code = d.Code;
                }
                var c = categories.FirstOrDefault(x => x.Id == device.CategoryId);
                if (c != null)
                {
                    device.Category = c.Category;
                }
            }
            var processId = devices.SelectMany(x => x.NextProcesses.Select(y => y.Item1));
            if (processId.Any())
            {
                var flowCards = ServerConfig.ApiDb.Query<SmartFlowCardProcessDetail>(
                    "SELECT a.Id, b.FlowCard, a.`Before`, a.`Qualified`, a.`Unqualified` FROM `t_flow_card_process` a " +
                    "JOIN `t_flow_card` b ON a.FlowCardId = b.Id WHERE a.MarkedDelete = 0 AND a.Id IN @processId;",
                    new { processId });

                foreach (var device in data)
                {
                    foreach (var process in device.NextProcesses)
                    {
                        var flowCard = flowCards.FirstOrDefault(x => x.Id == process.Item1);
                        if (flowCard != null)
                        {
                            device.NextProcess.Add(new Tuple<string, string>(flowCard.FlowCard, flowCard.DeliveryTime.ToStr()));
                        }
                    }
                }
            }
            result.datas.AddRange(data);
            return result;
        }

        /// <summary>
        /// 处理问题
        /// </summary>
        /// <param name="smartProcessFault"></param>
        /// <returns></returns>
        // POST: api/SmartFactory
        [HttpPost]
        public Result PostSmartFactory([FromBody] SmartProcessFault smartProcessFault)
        {
            smartProcessFault.IsDeal = true;
            SmartProcessFaultHelper.Instance.Update(smartProcessFault);
            SmartFlowCardProcessHelper.UpdateSmartFlowCardProcessFault(smartProcessFault.ProcessId);
            return Result.GenError<Result>(Error.Success);
        }

    }
}