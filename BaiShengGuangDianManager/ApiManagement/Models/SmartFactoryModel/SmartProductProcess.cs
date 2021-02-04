using System;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Models.BaseModel;
using ModelBase.Base.Logger;
using Newtonsoft.Json;
using ServiceStack;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartProductProcess : CommonBase
    {
        /// <summary>
        /// 车间Id
        /// </summary>
        public int WorkshopId { get; set; }
        /// <summary>
        /// 计划号id
        /// </summary>
        public int ProductId { get; set; }
        /// <summary>
        /// 流程编号id
        /// </summary>
        public int ProcessCodeId { get; set; }
        /// <summary>
        /// 流程id
        /// </summary>
        public int ProcessId { get; set; }
        /// <summary>
        /// 可否返工
        /// </summary>
        public bool ProcessRepeat { get; set; }
        /// <summary>
        /// 加工数量
        /// </summary>
        public int ProcessNumber { get; set; }
        /// <summary>
        /// 工艺数据
        /// </summary>
        public string ProcessData { get; set; }
        public List<SmartProcessCraft> Crafts
        {
            get
            {
                try
                {
                    if (!ProcessData.IsNullOrEmpty())
                    {
                        return JsonConvert.DeserializeObject<List<SmartProcessCraft>>(ProcessData);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(ProcessData);
                    Log.Error(e);
                }
                return new List<SmartProcessCraft>();
            }
        }
        public decimal TotalSecond => Crafts.Sum(x => x.TotalSecond);
    }

    public class SmartProductProcessDetail : SmartProductProcess
    {
        /// <summary>
        /// 流程
        /// </summary>
        public string Process { get; set; }
    }
}