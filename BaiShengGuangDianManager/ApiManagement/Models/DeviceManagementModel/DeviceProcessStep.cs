using ApiManagement.Base.Helper;
using ApiManagement.Models.BaseModel;
using ModelBase.Base.Utils;
using ModelBase.Models.BaseModel;
using Newtonsoft.Json;
using ServiceStack;
using System;
using System.Collections.Generic;

namespace ApiManagement.Models.DeviceManagementModel
{
    public class DeviceProcessStep : CommonBase
    {
        /// <summary>
        /// 工序名称
        /// </summary>
        public string StepName { get; set; }
        /// <summary>
        /// 简称
        /// </summary>
        public string Abbrev { get; set; }
        /// <summary>
        /// 设备类型
        /// </summary>
        public int DeviceCategoryId { get; set; }
        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// 是否是检验
        /// </summary>
        public bool IsSurvey { get; set; }
    }

    public class DeviceProcessStepDetail : DeviceProcessStep
    {
        public DeviceProcessStepDetail()
        {

        }
        public DeviceProcessStepDetail(HFlowCardHelper.ErpStep step, string createUserId, DateTime now)
        {
            CreateUserId = createUserId;
            MarkedDateTime = now;
            StepName = step.gxmc;
            Abbrev = step.gxid;
            IsSurvey = step.gxtype == "J";
            IsQualified = step.hg;
            StepType = step.gxtype == "F" ? ProcessStepType.Issue
                : (step.gxtype == "J" ? ProcessStepType.Inspection
                    : (step.gxtype == "T" ? ProcessStepType.Patch
                        : (step.gxtype == "G" ? ProcessStepType.Process : ProcessStepType.Default)));
            StepTypeStr = step.gxtype;
            Description = "";
            Errors = step.bllx.ToJSON();
            From = DataFrom.Erp;
        }

        public bool HaveChange(DeviceProcessStepDetail step)
        {
            return Abbrev != step.Abbrev || IsSurvey != step.IsSurvey || IsQualified != step.IsQualified
                   || StepType != step.StepType || StepTypeStr != step.StepTypeStr || Errors != step.Errors;
        }
        /// <summary>
        /// 描述
        /// </summary>
        public string CategoryName { get; set; }
        /// <summary>
        /// 是否有合格数字段
        /// </summary>
        public bool IsQualified { get; set; }
        /// <summary>
        /// 0 默认 1 ERP 
        /// </summary>
        public DataFrom From { get; set; }
        /// <summary>
        /// 自增
        /// </summary>
        public int FromId { get; set; }
        /// <summary>
        /// 工序类型
        /// </summary>
        public string StepTypeStr { get; set; }
        /// <summary>
        /// 不良类型
        /// </summary>
        public string Errors { get; set; }
        /// <summary>
        /// 不良类型
        /// </summary>
        public List<BadType> ErrorList => Errors.IsNullOrEmpty() ? new List<BadType>() : JsonConvert.DeserializeObject<List<BadType>>(Errors);
        /// <summary>
        /// 工序类型
        /// </summary>
        public ProcessStepType StepType { get; set; }
        /// <summary>
        /// 自增
        /// </summary>
        public int Api { get; set; }
    }

    public class BadType
    {
        public BadType() 
        {

        }
        public BadType(BadType bad)
        {
            name = bad.name;
            comment = bad.comment;
        }
        public string name;
        public string comment;
    }

    public class BadTypeCount : BadType
    {
        public BadTypeCount():base()
        {

        }
        public BadTypeCount(BadType bad, int count) : base(bad)
        {
            this.count = count;
        }
        public int count;
    }
}
