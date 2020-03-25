using ApiManagement.Models.BaseModel;
using Newtonsoft.Json;

namespace ApiManagement.Models.RepairManagementModel
{
    public class Maintainer : CommonBase
    {
        public string Name { get; set; }
        public string Account { get; set; }
        public string Phone { get; set; }
        public string Remark { get; set; }
        /// <summary>
        /// 集中控制web 0 否  1 修改  2 删除  3 删除后恢复
        /// </summary>
        //[JsonIgnore]
        public int WebOp { get; set; }
    }
}
