using System;
using ServiceStack;
using System.Collections.Generic;
using System.Linq;
using ModelBase.Models.BaseModel;

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
        /// 设备单次加工数量
        /// </summary>
        public string DeviceSingle { get; set; }
        /// <summary>
        /// 单设备合格率
        /// </summary>
        public string DeviceRate { get; set; }
        /// <summary>
        /// 单设备总工时(秒)
        /// </summary>
        public string DeviceWorkTime { get; set; }
        /// <summary>
        /// 单设备单次工时(秒)
        /// </summary>
        public string DeviceProductTime { get; set; }
        /// <summary>
        /// 单设备日加工次数
        /// </summary>
        public string DeviceSingleCount { get; set; }
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
                var capacities = new List<SmartDeviceCapacity>();
                var deviceModels = DeviceModel.IsNullOrEmpty() ? new List<int>() : DeviceModel.Split(",").Select(int.Parse);
                var deviceSingles = DeviceSingle.IsNullOrEmpty() ? new List<int>() : DeviceSingle.Split(",").Select(int.Parse);
                var deviceRates = DeviceRate.IsNullOrEmpty() ? new List<decimal>() : DeviceRate.Split(",").Select(decimal.Parse);
                var deviceWorkTimes = DeviceWorkTime.IsNullOrEmpty() ? new List<int>() : DeviceWorkTime.Split(",").Select(int.Parse);
                var deviceProductTimes = DeviceProductTime.IsNullOrEmpty() ? new List<int>() : DeviceProductTime.Split(",").Select(int.Parse);
                var deviceSingleCounts = DeviceSingleCount.IsNullOrEmpty() ? new List<int>() : DeviceSingleCount.Split(",").Select(int.Parse);
                var deviceNumbers = DeviceNumber.IsNullOrEmpty() ? new List<int>() : DeviceNumber.Split(",").Select(int.Parse);
                for (var i = 0; i < deviceModels.Count(); i++)
                {
                    var ModelId = deviceModels.ElementAt(i);
                    var number = 0;
                    if (deviceNumbers.Count() > i)
                    {
                        number = deviceNumbers.ElementAt(i);
                    }
                    var single = 0;
                    if (deviceSingles.Count() > i)
                    {
                        single = deviceSingles.ElementAt(i);
                    }
                    decimal rate = 0;
                    if (deviceRates.Count() > i)
                    {
                        rate = deviceRates.ElementAt(i);
                    }
                    var wTime = 0;
                    if (deviceWorkTimes.Count() > i)
                    {
                        wTime = deviceWorkTimes.ElementAt(i);
                    }
                    var pTime = 0;
                    if (deviceProductTimes.Count() > i)
                    {
                        pTime = deviceProductTimes.ElementAt(i);
                    }
                    var singleCount = 0;
                    if (deviceSingleCounts.Count() > i)
                    {
                        singleCount = deviceSingleCounts.ElementAt(i);
                    }
                    capacities.Add(new SmartDeviceCapacity
                    {
                        ModelId = ModelId,
                        Number = number,
                        Single = single,
                        Rate = rate,
                        WorkTime = wTime,
                        ProductTime = pTime,
                        SingleCount = singleCount
                    });
                }
                return capacities;
            }
        }
        /// <summary>
        /// 人员等级
        /// </summary>
        public string OperatorLevel { get; set; }
        /// <summary>
        /// 人员单次加工数量
        /// </summary>
        public string OperatorSingle { get; set; }
        /// <summary>
        /// 单人员合格率
        /// </summary>
        public string OperatorRate { get; set; }
        /// <summary>
        /// 单人员总工时(秒)
        /// </summary>
        public string OperatorWorkTime { get; set; }
        /// <summary>
        /// 单人员单次工时(秒)
        /// </summary>
        public string OperatorProductTime { get; set; }
        /// <summary>
        /// 人员单次加工次数
        /// </summary>
        public string OperatorSingleCount { get; set; }
        /// <summary>
        /// 人员产能
        /// </summary>
        public string OperatorNumber { get; set; }
        /// <summary>
        /// 人员产能
        /// </summary>
        public IEnumerable<SmartOperatorCapacity> OperatorList
        {
            get
            {
                var capacities = new List<SmartOperatorCapacity>();
                var operatorLevels = OperatorLevel.IsNullOrEmpty() ? new List<int>() : OperatorLevel.Split(",").Select(int.Parse);
                var operatorNumbers = OperatorNumber.IsNullOrEmpty() ? new List<int>() : OperatorNumber.Split(",").Select(int.Parse);
                var operatorSingles = OperatorSingle.IsNullOrEmpty() ? new List<int>() : OperatorSingle.Split(",").Select(int.Parse);
                var operatorRates = OperatorRate.IsNullOrEmpty() ? new List<decimal>() : OperatorRate.Split(",").Select(decimal.Parse);
                var operatorWorkTimes = OperatorWorkTime.IsNullOrEmpty() ? new List<int>() : OperatorWorkTime.Split(",").Select(int.Parse);
                var operatorProductTimes = OperatorProductTime.IsNullOrEmpty() ? new List<int>() : OperatorProductTime.Split(",").Select(int.Parse);
                var operatorSingleCounts = OperatorSingleCount.IsNullOrEmpty() ? new List<int>() : OperatorSingleCount.Split(",").Select(int.Parse);
                for (var i = 0; i < operatorLevels.Count(); i++)
                {
                    var levelId = operatorLevels.ElementAt(i);
                    var number = 0;
                    if (operatorNumbers.Count() > i)
                    {
                        number = operatorNumbers.ElementAt(i);
                    }
                    var single = 0;
                    if (operatorSingles.Count() > i)
                    {
                        single = operatorSingles.ElementAt(i);
                    }
                    decimal rate = 0;
                    if (operatorRates.Count() > i)
                    {
                        rate = operatorRates.ElementAt(i);
                    }
                    var wTime = 0;
                    if (operatorWorkTimes.Count() > i)
                    {
                        wTime = operatorWorkTimes.ElementAt(i);
                    }
                    var pTime = 0;
                    if (operatorProductTimes.Count() > i)
                    {
                        pTime = operatorProductTimes.ElementAt(i);
                    }
                    var singleCount = 0;
                    if (operatorSingleCounts.Count() > i)
                    {
                        singleCount = operatorSingleCounts.ElementAt(i);
                    }
                    capacities.Add(new SmartOperatorCapacity
                    {
                        LevelId = levelId,
                        Number = number,
                        Single = single,
                        Rate = rate,
                        WorkTime = wTime,
                        ProductTime = pTime,
                        SingleCount = singleCount
                    });
                }
                return capacities;
            }
        }
        /// <summary>
        /// 是否设置产能
        /// </summary>
        /// <returns></returns>
        public bool IsSet()
        {
            return (!DeviceModel.IsNullOrEmpty()
                    && !DeviceNumber.IsNullOrEmpty()
                    && !DeviceSingle.IsNullOrEmpty()
                    && !DeviceRate.IsNullOrEmpty()
                    && !DeviceWorkTime.IsNullOrEmpty()
                    && !DeviceProductTime.IsNullOrEmpty()
                    && !DeviceSingleCount.IsNullOrEmpty())
                   || (!OperatorLevel.IsNullOrEmpty()
                       && !OperatorNumber.IsNullOrEmpty()
                       && !OperatorSingle.IsNullOrEmpty()
                       && !OperatorRate.IsNullOrEmpty()
                       && !OperatorWorkTime.IsNullOrEmpty()
                       && !OperatorProductTime.IsNullOrEmpty()
                       && !OperatorSingleCount.IsNullOrEmpty());
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
        public string Process { get; set; } = string.Empty;
        /// <summary>
        /// 设备类型id
        /// </summary>
        public int DeviceCategoryId { get; set; }
        /// <summary>
        /// 设备类型
        /// </summary>
        public string Category { get; set; } = string.Empty;
        /// <summary>
        /// 设备型号
        /// </summary>
        public string DeviceModel { get; set; } = string.Empty;
        /// <summary>
        /// 设备产能
        /// </summary>
        public string DeviceNumber { get; set; } = string.Empty;
        /// <summary>
        /// 设备单次加工数量
        /// </summary>
        public string DeviceSingle { get; set; } = string.Empty;
        /// <summary>
        /// 单设备合格率
        /// </summary>
        public string DeviceRate { get; set; } = string.Empty;
        /// <summary>
        /// 单设备总工时(秒)
        /// </summary>
        public string DeviceWorkTime { get; set; } = string.Empty;
        /// <summary>
        /// 单设备单次工时(秒)
        /// </summary>
        public string DeviceProductTime { get; set; } = string.Empty;
        /// <summary>
        /// 单设备日加工次数
        /// </summary>
        public string DeviceSingleCount { get; set; } = string.Empty;

        /// <summary>
        /// 人员等级
        /// </summary>
        public string OperatorLevel { get; set; } = string.Empty;
        /// <summary>
        /// 人员产能
        /// </summary>
        public string OperatorNumber { get; set; } = string.Empty;
        /// <summary>
        /// 人员单次加工数量
        /// </summary>
        public string OperatorSingle { get; set; } = string.Empty;
        /// <summary>
        /// 单人员合格率
        /// </summary>
        public string OperatorRate { get; set; } = string.Empty;
        /// <summary>
        /// 单人员总工时(秒)
        /// </summary>
        public string OperatorWorkTime { get; set; } = string.Empty;
        /// <summary>
        /// 单人员单次工时(秒)
        /// </summary>
        public string OperatorProductTime { get; set; } = string.Empty;
        /// <summary>
        /// 单人员每日加工次数
        /// </summary>
        public string OperatorSingleCount { get; set; } = string.Empty;
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
        /// <summary>
        /// 顺序
        /// </summary>
        public int Order { get; set; }
        /// <summary>
        /// 流程id
        /// </summary>
        public int PId { get; set; }

        /// <summary>
        /// 日最大产能 该日产能为末道工序最大产能
        /// </summary>
        public int Number { get; set; }
        /// <summary>
        /// 设备日产能
        /// </summary>
        public int DNumber { get; set; }
        /// <summary>
        /// 人员日产能
        /// </summary>
        public int ONumber { get; set; }
    }

    public class SmartDeviceCapacityBase
    {
        /// <summary>
        /// 该型号设备/人员数量
        /// </summary>
        public int Count { get; set; } = 0;
        /// <summary>
        /// 单次数量
        /// </summary>
        public int Single { get; set; } = 0;
        /// <summary>
        /// 合格率
        /// </summary>
        public decimal Rate { get; set; } = 0;
        /// <summary>
        /// 总工时(秒)
        /// </summary>
        public int WorkTime { get; set; } = 0;
        /// <summary>
        /// 单次工时(秒)
        /// </summary>
        public int ProductTime { get; set; } = 0;
        /// <summary>
        /// 单日加工次数
        /// </summary>
        public int SingleCount { get; set; } = 0;
        /// <summary>
        /// 单台日产能
        /// </summary>
        public int Number { get; set; } = 0;
        /// <summary>
        /// 日总产能
        /// </summary>
        public int Total => Count * Number;
    }
    public class SmartDeviceCapacity : SmartDeviceCapacityBase
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
    }
    public class SmartOperatorCapacity : SmartDeviceCapacityBase
    {
        /// <summary>
        /// 人员等级
        /// </summary>
        public string Level { get; set; }
        /// <summary>
        /// 人员等级id
        /// </summary>
        public int LevelId { get; set; }
    }
}
