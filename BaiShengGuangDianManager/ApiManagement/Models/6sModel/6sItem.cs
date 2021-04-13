using ModelBase.Models.BaseModel;

namespace ApiManagement.Models._6sModel
{
    /// <summary>
    /// 6s检查项
    /// </summary>
    public class _6sItem : CommonBase
    {
        public int Order { get; set; }
        public string Item { get; set; }
        public int GroupId { get; set; }
        public bool Enable { get; set; }
        /// <summary>
        /// 标准分
        /// </summary>
        public int Standard { get; set; }
        /// <summary>
        /// 检验要求
        /// </summary>
        public string Reference { get; set; }
        /// <summary>
        /// 0 不设置 1 天  2 周
        /// </summary>
        public int Interval { get; set; }
        public int Day { get; set; }
        public int Week { get; set; }
        /// <summary>
        /// 负责人
        /// </summary>
        public int Person { get; set; }

        public bool Change(_6sItem _6sItem)
        {
            if (_6sItem.Order != Order)
            {
                return true;
            }

            if (_6sItem.Item != Item)
            {
                return true;
            }

            if (_6sItem.GroupId != GroupId)
            {
                return true;
            }

            if (_6sItem.Enable != Enable)
            {
                return true;
            }

            if (_6sItem.Standard != Standard)
            {
                return true;
            }

            if (_6sItem.Reference != Reference)
            {
                return true;
            }

            if (_6sItem.Interval != Interval)
            {
                return true;
            }

            if (_6sItem.Day != Day)
            {
                return true;
            }

            if (_6sItem.Week != Week)
            {
                return true;
            }

            if (_6sItem.Person != Person)
            {
                return true;
            }

            return false;
        }

    }
    public class _6sItemDetail : _6sItem
    {
        public string SurveyorName { get; set; }
    }
}
