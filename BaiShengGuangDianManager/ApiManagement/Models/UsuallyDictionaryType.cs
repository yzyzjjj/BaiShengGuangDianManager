using System;

namespace ApiManagement.Models
{
    public class UsuallyDictionaryType
    {
        public int Id { get; set; }
        public string CreateUserId { get; set; }
        public DateTime MarkedDateTime { get; set; }
        public bool MarkedDelete { get; set; }
        public int ModifyId { get; set; }
        public string VariableName { get; set; }
        public bool IsDetail { get; set; }
        public int StatisticType { get; set; }

    }

    public class UsuallyDictionaryTypeDetail : UsuallyDictionaryType
    {
        public int ScriptId { get; set; }
        public int VariableTypeId { get; set; }
        public int DictionaryId { get; set; }

    }
}
