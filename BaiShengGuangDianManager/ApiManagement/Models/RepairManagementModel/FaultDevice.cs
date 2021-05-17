using ModelBase.Base.Utils;
using ModelBase.Models.BaseModel;
using Newtonsoft.Json;
using ServiceStack;
using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace ApiManagement.Models.RepairManagementModel
{
    public class FaultDevice : CommonBase
    {
        /// <summary>
        /// 车间id
        /// </summary>
        public int WorkshopId { get; set; }
        /// <summary>
        /// 是否取消
        /// </summary>
        public bool Cancel { get; set; }
        public int DeviceId { get; set; }
        /// <summary>
        /// 机台号
        /// </summary>
        public string DeviceCode { get; set; }
        /// <summary>
        /// 故障时间
        /// </summary>
        public DateTime FaultTime { get; set; }
        /// <summary>
        /// 报修人
        /// </summary>
        public string Proposer { get; set; }
        /// <summary>
        /// 报修故障描述补充
        /// </summary>
        public string Supplement { get; set; } = "";
        /// <summary>
        /// 优先级
        /// </summary>
        public int Priority { get; set; }
        /// <summary>
        /// 故障等级
        /// </summary>
        public int Grade { get; set; }
        /// <summary>
        /// 故障类型表Id
        /// </summary>
        public int FaultTypeId { get; set; }
        /// <summary>
        /// 设备管理员
        /// </summary>
        public string Administrator { get; set; }
        /// <summary>
        /// 维修工
        /// </summary>
        public string Maintainer { get; set; }
        /// <summary>
        /// 指派时间
        /// </summary>
        public DateTime AssignTime { get; set; }
        /// <summary>
        /// 是否是上报
        /// </summary>
        public bool IsReport { get; set; }
        /// <summary>
        /// 状态
        /// </summary>
        public RepairStateEnum State { get; set; }

        [Ignore]
        public string StateDesc => State.GetAttribute<DescriptionAttribute>()?.Description ?? "";
        /// <summary>
        /// 维修开始处理时间
        /// </summary>
        public DateTime StartTime { get; set; }
        /// <summary>
        /// 预计时间
        /// </summary>
        public DateTime EstimatedTime { get; set; }
        /// <summary>
        /// 维修备注
        /// </summary>
        public string Remark { get; set; }
        /// <summary>
        /// 图片
        /// </summary>
        public string Images { get; set; }
        private bool Cancel1 { get; set; }
        private bool Cancel2 { get; set; }
        [Ignore]
        public List<string> Maintainers => Maintainer.IsNullOrEmpty() ? new List<string>() : Maintainer.Split(",").ToList();
        [Ignore]
        public string[] ImageList => Images != null ? JsonConvert.DeserializeObject<string[]>(Images) : new string[0];

        private static IEnumerable<string> GetMembers(IEnumerable<string> except = null)
        {
            var result = typeof(FaultDevice).GetProperties().Where(x => (IgnoreAttribute)x.GetCustomAttributes(typeof(IgnoreAttribute), false).FirstOrDefault() == null).Select(y => y.Name);
            //var result = typeof(FaultDevice).GetProperties().Select(x => x.Name);
            return except == null ? result : result.Where(x => !except.Contains(x));
        }

        public static string GetField(IEnumerable<string> except = null, string pre = "")
        {
            return GetMembers(except).Select(x => $"{pre}`{x}`").Join(", ");
        }
        /// <summary>
        /// 报修故障类型
        /// </summary>
        [Ignore]
        public string FaultTypeName { get; set; }
        /// <summary>
        /// 报修故障描述
        /// </summary>
        [Ignore]
        public string FaultDescription { get; set; }
    }

    public class FaultDeviceDetail : FaultDevice
    {
        public string SiteName { get; set; }
        public string Name { get; set; }
        public string Account { get; set; }
        public string Phone { get; set; }
    }
}
