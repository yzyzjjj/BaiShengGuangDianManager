using ApiManagement.Models.BaseModel;
using ModelBase.Base.Utils;

namespace ApiManagement.Models.ManufactureModel
{
    /// <summary>
    /// 生产检验配置
    /// </summary>
    public class ManufactureTaskItem : CommonBase
    {
        public int TaskId { get; set; }
        public int Order { get; set; }
        public int Person { get; set; }
        public int ModuleId { get; set; }
        public bool IsCheck { get; set; }
        public int CheckId { get; set; }
        public string Item { get; set; }
        public int EstimatedHour { get; set; }
        public int EstimatedMin { get; set; }
        public string EstimatedTime => DateTimeExtend.ToTimeStr(EstimatedHour * 3600 + EstimatedMin * 60);
        public int Score { get; set; }
        public string Desc { get; set; }
        public int Relation { get; set; }
    }
    public class ManufactureTaskItemDetail : ManufactureTaskItem
    {
        public int Task { get; set; }
        public int GroupId { get; set; }
        public string Group { get; set; }
        public string Processor { get; set; }
        public string Module { get; set; }
    }
}
