using System;
using ApiManagement.Models.BaseModel;
using ApiManagement.Models.StatisticManagementModel;

namespace ApiManagement.Models.DeviceManagementModel
{
    public class DataNameDictionary : CommonBase
    {
        public int ScriptId { get; set; }
        /// <summary>
        /// 1 变量  2 输入   3  输出
        /// </summary>
        public int VariableTypeId { get; set; }
        /// <summary>
        /// 地址
        /// </summary>
        public int PointerAddress { get; set; }
        public string VariableName { get; set; } = "";
        public string Remark { get; set; } = "";
        /// <summary>
        /// 精度
        /// </summary>
        public int Precision { get; set; }

    }
    public class DataNameDictionaryDetail : DataNameDictionary
    {
        /// <summary>
        /// 常用变量类型id
        /// </summary>
        public int VariableNameId { get; set; }
    }
    public class DataNameDictionaryOrder : DataNameDictionary
    {
        /// <summary>
        /// 默认设备数据，0设备数据，1生产数据
        /// </summary>
        public int Type { get; set; } = 0;
        /// <summary>
        /// 
        /// </summary>
        public KanBanItemEnum ItemType { get; set; } = KanBanItemEnum.无;
        /// <summary>
        /// 顺序
        /// </summary>
        public int Order { get; set; }

    }
}
