using System;
using System.Collections.Generic;

namespace ApiDataDictionaryManagement.Models
{
    public partial class DataNameDictionary
    {
        public int Id { get; set; }
        public string CreateUserId { get; set; }
        public DateTime MarkedDateTime { get; set; }
        public bool MarkedDelete { get; set; }
        public int ModifyId { get; set; }
        public int ScriptId { get; set; }
        public int VariableTypeId { get; set; }
        public int PointerAddress { get; set; }
        public string VariableName { get; set; }

    }
}
