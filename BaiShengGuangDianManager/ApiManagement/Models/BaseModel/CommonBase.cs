using ModelBase.Base.Logic;
using System;

namespace ApiManagement.Models.BaseModel
{
    public class CommonBase
    {
        [IgnoreChange]
        public int Id { get; set; }
        [IgnoreChange]
        public string CreateUserId { get; set; } = "";
        [IgnoreChange]
        public DateTime MarkedDateTime { get; set; }
        [IgnoreChange]
        public bool MarkedDelete { get; set; } = false;
        [IgnoreChange]
        public int ModifyId { get; set; } = 0;
    }
}
