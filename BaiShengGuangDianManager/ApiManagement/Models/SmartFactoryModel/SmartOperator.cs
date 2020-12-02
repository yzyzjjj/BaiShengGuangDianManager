namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartOperator : SmartUser
    {
        /// <summary>
        /// 用户Id
        /// </summary>
        public int UserId { get; set; }
        /// <summary>
        /// 1 正常 2 休息
        /// </summary>
        public OperatorState State { get; set; }
        public string StateStr => State.ToString();
        /// <summary>
        /// 人员等级Id
        /// </summary>
        public int LevelId { get; set; }
        /// <summary>
        /// 标准流程id
        /// </summary>
        public int ProcessId { get; set; }
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
    }
    public class SmartOperatorCount
    {
        /// <summary>
        /// 标准流程id
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
