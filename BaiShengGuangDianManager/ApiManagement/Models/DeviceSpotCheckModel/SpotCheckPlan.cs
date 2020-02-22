using System.Collections.Generic;
using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.DeviceSpotCheckModel
{
    public class SpotCheckPlan : CommonBase
    {
        public string Plan { get; set; }
        public IEnumerable<SpotCheckItem> SpotCheckItems { get; set; }
    }
}
