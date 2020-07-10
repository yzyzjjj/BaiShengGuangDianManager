using System;
using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.DeviceManagementModel
{
    public class DataNameDictionary : CommonBase
    {
        public int ScriptId { get; set; }
        public int VariableTypeId { get; set; }
        public int PointerAddress { get; set; }
        public string VariableName { get; set; }
        public string Remark { get; set; } = "";
        public int Precision { get; set; }

    }
}
