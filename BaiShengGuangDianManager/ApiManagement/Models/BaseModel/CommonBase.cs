using ModelBase.Base.Logic;
using System;
using Newtonsoft.Json;

namespace ApiManagement.Models.BaseModel
{
    public class CommonBase
    {
        [IgnoreChange]
        public int Id { get; set; }
        [IgnoreChange]
        [JsonIgnore]
        public string CreateUserId { get; set; } = "";
        [IgnoreChange]
        public DateTime MarkedDateTime { get; set; }
        [IgnoreChange]
        [JsonIgnore]
        public bool MarkedDelete { get; set; } = false;
        [IgnoreChange]
        [JsonIgnore]
        public string ModifyUserId { get; set; } = "";
        [IgnoreChange]
        [JsonIgnore]
        public int ModifyId { get; set; } = 0;
    }
}
