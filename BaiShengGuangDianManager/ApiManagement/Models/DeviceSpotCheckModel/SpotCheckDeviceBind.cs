using System.Collections.Generic;
using System.Linq;
using ModelBase.Models.BaseModel;

namespace ApiManagement.Models.DeviceSpotCheckModel
{
    public class SpotCheckDeviceBind : CommonBase
    {
        public int DeviceId { get; set; }
        public int PlanId { get; set; }
    }

    /// <summary>
    /// 设备点检设置
    /// </summary>
    public class SpotCheckDeviceBindPlan
    {
        public int PlanId { get; set; }
        /// <summary>
        /// 绑定设备Id   1,2,3
        /// </summary>
        public string DeviceId { get; set; }

        public IEnumerable<int> DeviceList => DeviceId.Split(",").Select(x => int.TryParse(x, out _) ? int.Parse(x) : 0).Where(y => y != 0);
        /// <summary>
        /// 负责人
        /// </summary>
        public IEnumerable<SpotCheckDevice> SpotCheckDevices { get; set; }
    }
}
