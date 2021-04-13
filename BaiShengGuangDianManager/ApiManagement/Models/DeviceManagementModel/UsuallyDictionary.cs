using ModelBase.Models.BaseModel;

namespace ApiManagement.Models.DeviceManagementModel
{
    public class UsuallyDictionary : CommonBase
    {
        public int ScriptId { get; set; }
        public int VariableNameId { get; set; }
        public int DictionaryId { get; set; }
        public int VariableTypeId { get; set; }

    }
    public class UsuallyDictionaryDetail : UsuallyDictionary
    {
        public int Did { get; set; }
        public string VariableName { get; set; }
        public string TypeName { get; set; }

    }
    public class UsuallyDictionaryPrecision : UsuallyDictionary
    {
        public string VariableName { get; set; }
        public int Precision { get; set; }

    }
}
