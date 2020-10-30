using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartUser : CommonBase
    {
        /// <summary>
        /// 员工工号
        /// </summary>
        public string Number { get; set; }
        /// <summary>
        /// 账号
        /// </summary>
        public string Account { get; set; }
        /// <summary>
        /// 姓名
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
        /// <summary>
        /// 最大同时加工次数
        /// </summary>
        public int ProcessCount { get; set; }
    }
}
