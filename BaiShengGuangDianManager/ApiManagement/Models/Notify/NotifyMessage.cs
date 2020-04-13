using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.Notify
{
    public class NotifyMessage
    {
        public string Content { get; set; }
        public NotifyTypeEnum Type { get; set; }
        public NotifyPlatformEnum Platform { get; set; }
        public string[] atMobiles { get; set; }
        public bool isAtAll { get; set; }
    }
}
