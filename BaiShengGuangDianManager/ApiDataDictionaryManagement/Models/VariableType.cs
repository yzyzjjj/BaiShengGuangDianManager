using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiDataDictionaryManagement.Models
{
    public class VariableType
    {
        public int Id { get; set; }
        public string CreateUserId { get; set; }
        public DateTime MarkedDateTime { get; set; }
        public bool MarkedDelete { get; set; }
        public int ModifyId { get; set; }
        public string TypeName { get; set; }

    }
}
