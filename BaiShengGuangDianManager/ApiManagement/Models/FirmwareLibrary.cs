using System;

namespace ApiManagement.Models
{
    public class FirmwareLibrary
    {
        public int Id { get; set; }
        public string CreateUserId { get; set; }
        public DateTime MarkedDateTime { get; set; }
        public bool MarkedDelete { get; set; }
        public int ModifyId { get; set; }
        public string FirmwareName { get; set; }
        public int VarNumber { get; set; }
        public string CommunicationProtocol { get; set; }
        public string FilePath { get; set; }
        public string Description { get; set; }
    }
}
