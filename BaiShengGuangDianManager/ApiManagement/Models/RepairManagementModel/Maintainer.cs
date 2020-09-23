using ApiManagement.Models.BaseModel;
using ModelBase.Base.Logic;
using System;
using System.Collections.Generic;

namespace ApiManagement.Models.RepairManagementModel
{
    public class Maintainer : CommonBase
    {
        /// <summary>
        /// 集中控制web 0 否  1 修改  2 删除  3 删除后恢复
        /// </summary>
        //[JsonIgnore]
        public int WebOp { get; set; }

        /// <summary>
        /// 维修工姓名
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 账号
        /// </summary>
        public string Account { get; set; }
        /// <summary>
        /// 手机号
        /// </summary>
        public string Phone { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
        /// <summary>
        /// 默认通知
        /// </summary>
        public bool Default { get; set; }
        /// <summary>
        /// 排班顺序为0
        /// </summary>
        public int Order { get; set; }
    }
    public class MaintainerScore : Maintainer
    {
        /// <summary>
        /// 评分
        /// </summary>
        public int Score { get; set; }
    }

    /// <summary>
    /// 维修工排班
    /// </summary>
    public class MaintainerSchedule
    {
        [IgnoreChange]
        public int Id { get; set; }
        /// <summary>
        /// 上班时间1
        /// </summary>
        public DateTime StartTime { get; set; }
        /// <summary>
        /// 上班时间2
        /// </summary>
        public DateTime EndTime { get; set; }
        /// <summary>
        /// 0表示未排班
        /// </summary>
        public int MaintainerId { get; set; }
    }
    /// <summary>
    /// 维修工排班
    /// </summary>
    public class MaintainerScheduleDetail : MaintainerSchedule
    {
        /// <summary>
        /// 维修工姓名
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// 手机号
        /// </summary>
        public string Phone { get; set; } = "";
    }
    /// <summary>
    /// 维修工排班
    /// </summary>
    public class MaintainerScheduleDetails : MaintainerSchedule
    {
        /// <summary>
        /// 维修工
        /// </summary>
        public List<MaintainerScheduleDetail> Maintainers = new List<MaintainerScheduleDetail>();
    }

    public enum MaintainerAdjustEnum
    {

    }

    /// <summary>
    /// 维修工调班
    /// </summary>
    public class MaintainerAdjust : CommonBase
    {
        /// <summary>
        /// 0请假
        /// </summary>
        public int Type { get; set; }
        /// <summary>
        /// 维修工
        /// </summary>
        public int MaintainerId { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; }
        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime EndTime { get; set; }

    }
}
