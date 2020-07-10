using ApiManagement.Base.Server;
using ApiManagement.Models.Notify;
using ModelBase.Base.Logger;
using ModelBase.Base.Utils;
using System;
using System.ComponentModel;
using System.Linq;
#if !DEBUG
using System.Net;
#endif
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace ApiManagement.Base.Helper
{
    /// <summary>
    /// 消息通知帮助类
    /// </summary>
    public class NotifyHelper
    {
        /// <summary>
        /// 消息通知
        /// </summary>
        /// <param name="content"></param>
        /// <param name="msgType"></param>
        /// <param name="notifyType"></param>
        /// <param name="atMobiles"></param>
        /// <param name="isAtAll"></param>
        public static void Notify(string content, NotifyMsgEnum msgEnum, NotifyTypeEnum notifyType, NotifyMsgTypeEnum msgType, string[] atMobiles, bool isAtAll = false)
        {
#if !DEBUG
            IPHostEntry host;
            string localIp = "?";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily.ToString() == "InterNetwork")
                {
                    localIp = ip.ToString();
                }
            }

            if (localIp == "192.168.1.184")
            {
                notifyType = NotifyTypeEnum.Test;
            }
#else
            notifyType = NotifyTypeEnum.Test;
#endif
            var groups = ServerConfig.ApiDb.Query<NotifyWebhook>(
                "SELECT * FROM `notify_webhook` WHERE MarkedDelete = 0 AND `Type` = @type;", new { type = notifyType });

            foreach (var group in groups)
            {

                switch (group.Platform)
                {
                    case NotifyPlatformEnum.DingDing:
                        NotifyDingDing(group, msgEnum, content, msgType, atMobiles, isAtAll); break;
                    case NotifyPlatformEnum.WeiXin:
                        NotifyWeiXin(group, msgEnum, content, msgType, atMobiles, isAtAll); break;
                    default: break;
                }
            }
        }

        /// <summary>
        /// 通知钉钉群
        /// </summary>
        private static void NotifyDingDing(NotifyWebhook notifyWebhook, NotifyMsgEnum msgEnum, string content, NotifyMsgTypeEnum msgType, string[] atMobiles, bool isAtAll)
        {
            var timestamp = DateTime.Now.ToTimestamp();
            var secret = notifyWebhook.Secret;
            var sign = Sign(timestamp, secret);
            var webHookUrl = notifyWebhook.Webhook;
            var fullUrl = $"{webHookUrl}&timestamp={timestamp}&sign={sign}";
            //Log.Error($"钉钉消息推送URL;{ webHookUrl}&timestamp ={timestamp}&sign ={sign}");
            object sendData = null;
            //msgType = NotifyMsgTypeEnum.markdown;
            var title = msgEnum.GetAttribute<DescriptionAttribute>()?.Description ?? "";
            switch (msgType)
            {
                case NotifyMsgTypeEnum.text:
                    sendData = new
                    {
                        msgtype = msgType.ToString(),
                        text = new { content = $"[{title}]\n  {content}({notifyWebhook.Url})" },
                        at = new
                        {
                            atMobiles,
                            isAtAll
                        },
                    }; break;
                case NotifyMsgTypeEnum.markdown:
                    sendData = new
                    {
                        msgtype = msgType.ToString(),
                        markdown = new
                        {
                            title,
                            text = $"#### **{title}** \n ##### {content} [查看]({notifyWebhook.Url})"
                        },
                        at = new
                        {
                            atMobiles,
                            isAtAll
                        },
                    }; break;
            }
            if (sendData == null)
            {
                return;
            }

            var httpClient = new HttpClient(fullUrl)
            {
                Verb = HttpVerb.POST,
                ContentType = HttpClient.ContentType_Json,
                RawData = sendData.ToJSON()
            };
            httpClient.AsyncGetString((s, e) =>
            {
                if (e != null)
                {
                    Log.ErrorFormat("钉钉消息推送失败;{0}", e);
                }
                else if (!s.ToLower().Contains("ok"))
                {
                    Log.ErrorFormat("钉钉消息推送失败;{0}", s);
                }
                else
                {
                    //Log.DebugFormat("钉钉消息推送成功:{0}", s);
                }
            });
        }

        /// <summary>
        /// 把timestamp+"\n"+密钥当做签名字符串，使用HmacSHA256算法计算签名，然后进行Base64 encode，最后再把签名参数再进行urlEncode，得到最终的签名（需要使用UTF-8字符集）。
        /// </summary>
        /// <param name="timestamp"></param>
        /// <param name="secret"></param>
        /// <returns></returns>
        private static string Sign(string timestamp, string secret)
        {
            var stringToSign = timestamp + "\n" + secret;
            string sign;
            using (HMACSHA256 mac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                byte[] hash = mac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
                sign = Convert.ToBase64String(hash);
                //signRet = ToHexString(hash); ;
            }
            return HttpUtility.UrlEncode(sign);
        }

        /// <summary>
        /// 通知企业微信群
        /// </summary>
        private static void NotifyWeiXin(NotifyWebhook notifyWebhook, NotifyMsgEnum msgEnum, string content, NotifyMsgTypeEnum msgType, string[] atMobiles, bool isAtAll)
        {
            var webHookUrl = notifyWebhook.Webhook;
            var fullUrl = $"{webHookUrl}";
            //Log.Error($"企业微信消息推送URL;{ webHookUrl}&timestamp ={timestamp}&sign ={sign}");
            object sendData = null;
            var mobiles = atMobiles.ToList();
            if (isAtAll)
            {
                mobiles.Add("@all");
            }

            //msgType = NotifyMsgTypeEnum.markdown;
            var title = msgEnum.GetAttribute<DescriptionAttribute>()?.Description ?? "";
            switch (msgType)
            {
                case NotifyMsgTypeEnum.text:
                    sendData = new
                    {
                        msgtype = msgType.ToString(),
                        text = new
                        {
                            content = $"[{title}]\n  {content}({notifyWebhook.Url})",
                            //mentioned_list = new[] { "wangqing", "@all" },
                            mentioned_mobile_list = mobiles
                        },
                    }; break;
                case NotifyMsgTypeEnum.markdown:
                    sendData = new
                    {
                        msgtype = msgType.ToString(),
                        markdown = new
                        {
                            content = $"#### <font color=\"warning\">{title}</font> \n ##### {content} [查看]({notifyWebhook.Url})",
                            //mentioned_list = new[] { "wangqing", "@all" },
                            mentioned_mobile_list = mobiles
                        },
                    }; break;
            }
            if (sendData == null)
            {
                return;
            }

            var httpClient = new HttpClient(fullUrl)
            {
                Verb = HttpVerb.POST,
                ContentType = HttpClient.ContentType_Json,
                RawData = sendData.ToJSON()
            };
            httpClient.AsyncGetString((s, e) =>
            {
                if (e != null)
                {
                    //Log.ErrorFormat("企业微信消息推送失败;{0}", e);
                }
                else if (!s.ToLower().Contains("ok"))
                {
                    Log.ErrorFormat("企业微信消息推送失败;{0}", s);
                }
                else
                {
                    //Log.DebugFormat("企业微信消息推送成功:{0}", s);
                }
            });
        }

        public static void NotifyLog()
        {

        }
    }
}
