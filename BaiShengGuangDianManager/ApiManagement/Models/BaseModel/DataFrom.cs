using System;
using System.Collections.Generic;
using System.ComponentModel;
using ApiManagement.Base.Helper;
using ApiManagement.Base.Server;
using ApiManagement.Models.AccountManagementModel;
using ModelBase.Base.Logger;

namespace ApiManagement.Models.BaseModel
{
    public enum DataFrom
    {
        Default = 0,
        [Description("Erp")]
        Erp,
    }
    public enum ProcessStepType
    {
        Default = 0,
        /// <summary>
        /// 发出
        /// </summary>
        [Description("发出")]
        Issue,
        /// <summary>
        /// 加工
        /// </summary>
        [Description("加工")]
        Process,
        /// <summary>
        /// 检验
        /// </summary>
        [Description("检验")]
        Inspection,
        /// <summary>
        /// 补片
        /// </summary>
        [Description("补片")]
        Patch,
    }
}
