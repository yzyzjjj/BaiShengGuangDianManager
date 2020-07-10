using ApiManagement.Base.Server;
using ApiManagement.Models.OtherModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Logger;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using System;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Models.Notify;

namespace ApiManagement.Controllers.OtherController
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotifyController : ControllerBase
    {
        // GET: api/Notify
        /// <summary>
        /// 
        /// </summary>
        /// <param name="content">内容</param>
        /// <param name="msgEnum"></param>
        /// <param name="notifyType"></param>
        /// <param name="msgType"></param>
        /// <param name="atMobiles"></param>
        /// <param name="isAtAll"></param>
        /// <returns></returns>
        [HttpGet]
        public Result Notify([FromQuery] string content, NotifyMsgEnum msgEnum, NotifyTypeEnum notifyType, NotifyMsgTypeEnum msgType, string[] atMobiles, bool isAtAll = false)
        {
            

            

            return Result.GenError<Result>(Error.Success);
        }
    }
}