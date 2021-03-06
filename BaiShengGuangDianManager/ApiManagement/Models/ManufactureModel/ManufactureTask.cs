﻿using System.Collections.Generic;
using ModelBase.Models.BaseModel;

namespace ApiManagement.Models.ManufactureModel
{
    /// <summary>
    /// 生产任务配置
    /// </summary>
    public class ManufactureTask : CommonBase
    {
        public string Task { get; set; }
    }
    public class ManufactureTaskItems : ManufactureTask
    {
        public IEnumerable<ManufactureTaskItem> Items { get; set; }
    }
    public class ManufactureTaskCopy : ManufactureTask
    {
        public int CopyId { get; set; }
    }
}
