using ApiManagement.Models.BaseModel;
using System.Collections.Generic;

namespace ApiManagement.Models._6sModel
{
    /// <summary>
    /// 6s分组
    /// </summary>
    public class _6sGroup : CommonBase
    {
        public string Group { get; set; }
        public string SurveyorId { get; set; }
    }

    public class _6sGroupItems : _6sGroup
    {
        public IEnumerable<_6sItem> Items { get; set; }
    }
}
