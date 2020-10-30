using ApiManagement.Models.BaseModel;
using System.Collections.Generic;

namespace ApiManagement.Models.SmartFactoryModel
{
    /// <summary>
    /// 流程卡生产线单步流程
    /// </summary>
    public class SmartLineFlowCard : SmartFlowCardProcessDetail
    {
        /// <summary>
        /// 流程故障列表
        /// </summary>
        public List<SmartProcessFaultDetail> Faults { get; set; } = new List<SmartProcessFaultDetail>();
    }
    /// <summary>
    /// 流程卡生产线
    /// </summary>
    public class SmartFlowCardLine
    {
        public SmartFlowCardLine()
        {
            LineProcesses = new List<SmartLineFlowCard>();
        }
        /// <summary>
        /// 流程列表
        /// </summary>
        public List<SmartLineFlowCard> LineProcesses { get; set; }
    }

}
