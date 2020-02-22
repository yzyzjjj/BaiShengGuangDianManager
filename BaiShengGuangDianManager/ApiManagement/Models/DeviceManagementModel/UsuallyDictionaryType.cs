using System;
using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.DeviceManagementModel
{
    public class UsuallyDictionaryType : CommonBase
    {
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
