using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace RelayServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotifyController : ControllerBase
    {
        [HttpPost("DingDing")]
        public string SendToDingDing()
        {
            return "DingDing";
        }

        [HttpPost("DingDingBack")]
        public string DingDingBack()
        {
            return "DingDingBack";

        }

        [HttpPost("WeiXin")]
        public string SendToWeiXin()
        {
            return "WeiXin";

        }

        [HttpPost("WeiXinBack")]
        public string WeiXinBack()
        {
            return "WeiXinBack";

        }


    }
}