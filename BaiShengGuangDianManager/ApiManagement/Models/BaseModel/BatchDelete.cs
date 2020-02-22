using System.Collections.Generic;

namespace ApiManagement.Models.BaseModel
{
    public class BatchDelete
    {
        public IEnumerable<int> ids { get; set; }
    }
}
