using ApiManagement.Models.DeviceManagementModel;
using System;

namespace ApiManagement.Models.OtherModel
{
    public class FlowCardReportUpdate : FlowCardReportGet
    {
        public FlowCardReportUpdate()
        {
        }

        public FlowCardReportUpdate(ErpFlowCardReportGet report, DeviceProcessStepDetail step, DateTime now)
            : base(report, step, now)
        {

        }
        /// <summary>
        /// 是否已更新老数据
        /// </summary>
        public int IsUpdate { get; set; }
        /// <summary>
        /// 老数据
        /// </summary>
        public string OldData { get; set; } = "";
    }

    public class ErpFlowCardReportUpdate : ErpFlowCardReportGet
    {
    }

    public class ErpUpdateFlowCardUpdate : ErpUpdateFlowCardGet
    {

    }
}