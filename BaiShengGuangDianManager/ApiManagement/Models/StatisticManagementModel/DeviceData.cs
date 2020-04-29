using System.Collections.Generic;

namespace ApiManagement.Models.StatisticManagementModel
{
    public class DeviceData
    {
        public DeviceData()
        {
            vals = new List<int>();
            ins = new List<int>();
            outs = new List<int>();
        }
        public List<int> vals;
        public List<int> ins;
        public List<int> outs;
    }
}
