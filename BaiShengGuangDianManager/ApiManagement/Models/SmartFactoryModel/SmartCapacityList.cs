using ApiManagement.Models.BaseModel;
using ServiceStack;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartCapacityList : CommonBase
    {
        /// <summary>
        /// 产能类型id
        /// </summary>
        public int CapacityId { get; set; }
        /// <summary>
        /// 标准流程id
        /// </summary>
        public int ProcessId { get; set; }
        /// <summary>
        /// 设备型号
        /// </summary>
        public string DeviceModel { get; set; }
        /// <summary>
        /// 设备产能
        /// </summary>
        public string DeviceNumber { get; set; }
        /// <summary>
        /// 设备产能
        /// </summary>
        public IEnumerable<SmartDeviceCapacity> DeviceList
        {
            get
            {
                var deviceCapacities = new List<SmartDeviceCapacity>();
                var deviceModels = DeviceModel.IsNullOrEmpty() ? new List<int>() : DeviceModel.Split(",").Select(int.Parse);
                var deviceNumbers = DeviceNumber.IsNullOrEmpty() ? new List<int>() : DeviceNumber.Split(",").Select(int.Parse);
                for (var i = 0; i < deviceModels.Count(); i++)
                {
                    var ModelId = deviceModels.ElementAt(i);
                    var number = 0;
                    if (deviceNumbers.Count() > i)
                    {
                        number = deviceNumbers.ElementAt(i);
                    }
                    deviceCapacities.Add(new SmartDeviceCapacity
                    {
                        ModelId = ModelId,
                        Number = number
                    });
                }
                return deviceCapacities;
            }
        }
        /// <summary>
        /// 人员等级
        /// </summary>
        public string OperatorLevel { get; set; }
        /// <summary>
        /// 人员产能
        /// </summary>
        public string OperatorNumber { get; set; }
        /// <summary>
        /// 设备产能
        /// </summary>
        public IEnumerable<SmartOperatorCapacity> OperatorList
        {
            get
            {
                var deviceCapacities = new List<SmartOperatorCapacity>();
                var operatorLevels = OperatorLevel.IsNullOrEmpty() ? new List<int>() : OperatorLevel.Split(",").Select(int.Parse);
                var OperatorNumbers = OperatorNumber.IsNullOrEmpty() ? new List<int>() : OperatorNumber.Split(",").Select(int.Parse);
                for (var i = 0; i < operatorLevels.Count(); i++)
                {
                    var levelId = operatorLevels.ElementAt(i);
                    var number = 0;
                    if (OperatorNumbers.Count() > i)
                    {
                        number = OperatorNumbers.ElementAt(i);
                    }
                    deviceCapacities.Add(new SmartOperatorCapacity
                    {
                        LevelId = levelId,
                        Number = number
                    });
                }
                return deviceCapacities;
            }
        }
        /// <summary>
        /// 是否设置产能
        /// </summary>
        /// <returns></returns>
        public bool IsSet()
        {
            return (!DeviceModel.IsNullOrEmpty() && !DeviceNumber.IsNullOrEmpty())
                   || (!OperatorLevel.IsNullOrEmpty() && !OperatorNumber.IsNullOrEmpty());
        }
    }

    public class SmartCapacityListCol
    {
        /// <summary>
        /// 清单设置id
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// 标准流程id
        /// </summary>
        public int ProcessId { get; set; }
        /// <summary>
        /// 标准流程
        /// </summary>
        public string Process { get; set; }
        /// <summary>
        /// 设备类型id
        /// </summary>
        public int DeviceCategoryId { get; set; }
        /// <summary>
        /// 设备类型
        /// </summary>
        public string Category { get; set; }
        /// <summary>
        /// 设备型号
        /// </summary>
        public string DeviceModel { get; set; }
        /// <summary>
        /// 设备产能
        /// </summary>
        public string DeviceNumber { get; set; }
        /// <summary>
        /// 人员等级
        /// </summary>
        public string OperatorLevel { get; set; }
        /// <summary>
        /// 人员产能
        /// </summary>
        public string OperatorNumber { get; set; }
    }

    public class SmartCapacityListDetail : SmartCapacityList
    {
        /// <summary>
        /// 标准流程
        /// </summary>
        public string Process { get; set; }
        /// <summary>
        /// 设备类型id
        /// </summary>
        public int CategoryId { get; set; }
        /// <summary>
        /// 设备类型
        /// </summary>
        public string Category { get; set; }
    }

    public class SmartDeviceCapacity
    {
        /// <summary>
        /// 设备类型
        /// </summary>
        public string Category { get; set; }
        /// <summary>
        /// 设备类型id
        /// </summary>
        public int CategoryId { get; set; }
        /// <summary>
        /// 设备型号
        /// </summary>
        public string Model { get; set; }
        /// <summary>
        /// 设备型号id
        /// </summary>
        public int ModelId { get; set; }
        /// <summary>
        /// 该型号设备数量
        /// </summary>
        public int Count { get; set; } = 0;
        /// <summary>
        /// 单台日产能
        /// </summary>
        public int Number { get; set; } = 0;
        /// <summary>
        /// 日总产能
        /// </summary>
        public int Total => Count * Number;
    }
    public class SmartOperatorCapacity
    {
        /// <summary>
        /// 人员等级
        /// </summary>
        public string Level { get; set; }
        /// <summary>
        /// 人员等级id
        /// </summary>
        public int LevelId { get; set; }
        /// <summary>
        /// 员工数量
        /// </summary>
        public int Count { get; set; } = 0;
        /// <summary>
        /// 单人日产能
        /// </summary>
        public int Number { get; set; } = 0;
        /// <summary>
        /// 日总产能
        /// </summary>
        public int Total => Count * Number;
    }
}
