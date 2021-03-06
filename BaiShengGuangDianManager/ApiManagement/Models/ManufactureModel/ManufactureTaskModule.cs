﻿using ModelBase.Models.BaseModel;

namespace ApiManagement.Models.ManufactureModel
{
    /// <summary>
    /// 生产任务模块
    /// </summary>
    public class ManufactureTaskModule : CommonBase
    {
        public string Module { get; set; }
        public bool IsCheck { get; set; }
    }
}
