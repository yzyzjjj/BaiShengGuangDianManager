using System;

namespace ApiManagement.Models.BaseModel
{
    public class CommonBase
    {
        public int Id { get; set; }
        public string CreateUserId { get; set; } = "";
        public DateTime MarkedDateTime { get; set; }
        public bool MarkedDelete { get; set; } = false;
        public int ModifyId { get; set; } = 0;
    }
}
