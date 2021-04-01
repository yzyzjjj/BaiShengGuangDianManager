using ApiManagement.Models.AccountModel;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartOperator : AccountInfo
    {
        /// <summary>
        /// 车间Id
        /// </summary>
        public int WorkshopId { get; set; }
        /// <summary>
        /// 用户Id
        /// </summary>
        public int UserId { get; set; }
        /// <summary>
        /// 1 正常 2 休息
        /// </summary>
        public SmartOperatorState State { get; set; }
        public string StateStr => State.ToString();
        /// <summary>
        /// 人员等级Id
        /// </summary>
        public int LevelId { get; set; }
        /// <summary>
        /// 流程id
        /// </summary>
        public int ProcessId { get; set; }
        /// <summary>
        /// 被使用优先级/好用等级  越小越好
        /// </summary>
        public int Priority { get; set; }
    }

    public class SmartOperatorDetail : SmartOperator
    {
        /// <summary>
        /// 人员等级
        /// </summary>
        public string Level { get; set; }
        /// <summary>
        /// 标准流程
        /// </summary>
        public string Process { get; set; }
        /// <summary>
        /// 人员等级顺序，越大约靠前
        /// </summary>
        public int Order { get; set; }
    }
    public class SmartOperatorCount
    {
        /// <summary>
        /// 流程id
        /// </summary>
        public int ProcessId { get; set; }
        /// <summary>
        /// 人员等级Id
        /// </summary>
        public int LevelId { get; set; }
        /// <summary>
        /// 数量
        /// </summary>
        public int Count { get; set; }
    }
}
