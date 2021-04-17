using ModelBase.Models.BaseModel;
using System;

namespace ApiManagement.Models.AccountManagementModel
{
    public class Site : CommonBase
    {
        /// <summary>
        /// 车间Id
        /// </summary>
        public int WorkshopId { get; set; }
        /// <summary>
        /// 具体位置
        /// </summary>
        public string Region { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
        /// <summary>
        /// 车间管理员
        /// </summary>
        [Obsolete]
        public string Manager { get; set; }
        ///// <summary>
        ///// 车间名称
        ///// </summary>
        //[Obsolete]
        //public string SiteName { get; set; }
        ///// <summary>
        ///// 具体位置
        ///// </summary>
        //[Obsolete]
        //public string RegionDescription { get; set; }
    }
    public class SiteDetail : Site
    {
        /// <summary>
        /// 车间名称
        /// </summary>
        public string WorkshopName { get; set; }
    }
}
