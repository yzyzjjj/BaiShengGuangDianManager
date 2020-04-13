using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.Notify
{
    public class NotifyWebhook : CommonBase
    {
        public string Name { get; set; }
        public NotifyTypeEnum Type { get; set; }
        public NotifyPlatformEnum Platform { get; set; }
        public string Webhook { get; set; }
        public string Secret { get; set; }
    }
}
