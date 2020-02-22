using System;
using ApiManagement.Models.BaseModel;
using Newtonsoft.Json;

namespace ApiManagement.Models._6sModel
{
    /// <summary>
    /// 6s日志
    /// </summary>
    public class _6sLog : CommonBase
    {
        /// <summary>
        /// 上次修改人
        /// </summary>
        public string ModifyAccount { get; set; }
        public string ModifyName { get; set; }
        public DateTime PlannedTime { get; set; }
        public int Order { get; set; }
        public string Item { get; set; }
        public int GroupId { get; set; }
        public bool Enable { get; set; }
        public int Standard { get; set; }
        public string Reference { get; set; }
        public int Interval { get; set; }
        public int Day { get; set; }
        public int Week { get; set; }
        public int Person { get; set; }
        public string SurveyorIdSet { get; set; }
        public string SurveyorAccount { get; set; }
        public int SurveyorId { get; set; }
        public int Score { get; set; }
        /// <summary>
        /// 是否更新图片  图片单独更新
        /// </summary>
        public bool UpdateImage { get; set; }
        public string Images { get; set; }
        public DateTime CheckTime { get; set; }
        /// <summary>
        /// 评分说明
        /// </summary>
        public string Desc { get; set; }
        public bool Check { get; set; }
        public bool ImageCheck { get; set; }

        public string[] ImageList => Images != null ? JsonConvert.DeserializeObject<string[]>(Images) : new string[0];
        /// <summary>
        /// 是否过期
        /// </summary>
        [JsonIgnore]
        public bool Expired => PlannedTime < DateTime.Today.AddDays(1);
    }
    /// <summary>
    /// 6s周期生成类
    /// </summary>
    public class _6sItemPeriod : _6sLog
    {
        public string Group { get; set; }
        public DateTime LastCreateTime { get; set; }
    }
    /// <summary>
    ///  获取6s检查项
    /// </summary>
    public class _6sItemCheck : _6sLog
    {
        public string PersonName { get; set; }
    }
    /// <summary>
    ///  获取6s检查记录
    /// </summary>
    public class _6sLogDetail : _6sItemCheck
    {
        public string SurveyorName { get; set; }
    }
}
