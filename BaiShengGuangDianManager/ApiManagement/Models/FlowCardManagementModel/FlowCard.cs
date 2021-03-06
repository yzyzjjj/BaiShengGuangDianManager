using System;
using System.Collections.Generic;
using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.FlowCardManagementModel
{
    public class FlowCard : CommonBase
    {
        /// <summary>
        /// 流程卡号
        /// </summary>
        public string FlowCardName { get; set; }
        /// <summary>
        /// 计划号
        /// </summary>
        public int ProductionProcessId { get; set; }
        /// <summary>
        /// 原料批号
        /// </summary>
        public int RawMateriaId { get; set; }
        /// <summary>
        /// 原料数量
        /// </summary>
        public int RawMaterialQuantity { get; set; }
        /// <summary>
        /// 发片人
        /// </summary>
        public string Sender { get; set; }
        /// <summary>
        /// 入库序号
        /// </summary>
        public string InboundNum { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks { get; set; }
        /// <summary>
        /// 优先级
        /// </summary>
        public int Priority { get; set; }
        public DateTime CreateTime { get; set; }
        public int FlowCardTypeId { get; set; }
        /// <summary>
        /// erpID
        /// </summary>
        public int FId { get; set; }
        public DateTime YanMoTime { get; set; }
        /// <summary>
        /// 加工设备
        /// </summary>
        public int YanMoDeviceId { get; set; }
        /// <summary>
        /// 研磨发出数
        /// </summary>
        public int YanMoFaChu { get; set; }
        /// <summary>
        /// 研磨合格数
        /// </summary>
        public int YanMoHeGe { get; set; }
        /// <summary>
        /// 研磨裂片数
        /// </summary>
        public int YanMoLiePian { get; set; }
        public string YanMoJiaGongRen { get; set; }
        public DateTime CuPaoTime { get; set; }
        /// <summary>
        /// 加工设备
        /// </summary>
        public int CuPaoDeviceId { get; set; }
        /// <summary>
        /// 粗抛发出数
        /// </summary>
        public int CuPaoFaChu { get; set; }
        /// <summary>
        /// 粗抛合格数
        /// </summary>
        public int CuPaoHeGe { get; set; }
        /// <summary>
        /// 粗抛裂片数
        /// </summary>
        public int CuPaoLiePian { get; set; }
        public string CuPaoJiaGongRen { get; set; }
        public DateTime JingPaoTime { get; set; }
        /// <summary>
        /// 加工设备
        /// </summary>
        public int JingPaoDeviceId { get; set; }
        /// <summary>
        /// 精抛发出数
        /// </summary>
        public int JingPaoFaChu { get; set; }
        /// <summary>
        /// 精抛合格数
        /// </summary>
        public int JingPaoHeGe { get; set; }
        /// <summary>
        /// 精抛裂片数
        /// </summary>
        public int JingPaoLiePian { get; set; }
        public string JingPaoJiaGongRen { get; set; }

        public List<FlowCardSpecification> Specifications = new List<FlowCardSpecification>();
        public List<FlowCardProcessStepDetail> ProcessSteps = new List<FlowCardProcessStepDetail>();
    }

    public class FlowCardDetail : FlowCard
    {
        /// <summary>
        /// 计划号
        /// </summary>
        public string ProductionProcessName { get; set; }
        /// <summary>
        /// 原料批号
        /// </summary>
        public string RawMateriaName { get; set; }
        public string CategoryName { get; set; }
        public string StepName { get; set; }
        public DateTime ProcessTime { get; set; }
        public string QualifiedNumber { get; set; }
        public int DeviceId { get; set; }
        public string Code { get; set; }
        public string TypeName { get; set; }
    }
}
