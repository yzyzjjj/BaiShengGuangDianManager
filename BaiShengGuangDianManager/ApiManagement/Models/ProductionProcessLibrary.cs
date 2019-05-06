using System;

namespace ApiManagement.Models
{
    public class ProductionProcessLibrary
    {
        public int Id { get; set; }
        public string CreateUserId { get; set; }
        public DateTime MarkedDateTime { get; set; }
        public bool MarkedDelete { get; set; }
        public int ModifyId { get; set; }
        public string ProductionProcessName { get; set; }
        public string Thickness { get; set; }
        public string Shape { get; set; }
    }

}
