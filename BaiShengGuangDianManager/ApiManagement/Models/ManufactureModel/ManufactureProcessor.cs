using ModelBase.Models.BaseModel;

namespace ApiManagement.Models.ManufactureModel
{
    /// <summary>
    /// 生产分组成员
    /// </summary>
    public class ManufactureProcessor : CommonBase
    {
        public int GroupId { get; set; }
        public int ProcessorId { get; set; }
    }
    public class ManufactureProcessorDetail : ManufactureProcessor
    {
        public string Group { get; set; }
        public string Processor { get; set; }
        public string Account { get; set; }
    }
}
