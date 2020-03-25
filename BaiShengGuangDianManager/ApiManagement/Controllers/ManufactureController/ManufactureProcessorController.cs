using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using ApiManagement.Models.ManufactureModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.ManufactureController
{
    /// <summary>
    /// 生产分组员工
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class ManufactureProcessorController : ControllerBase
    {
        /// <summary>
        ///  
        /// </summary>
        /// <param name="groupId">生产分组id</param>
        /// <param name="menu"></param>
        /// <returns></returns>
        // GET: api/ManufactureProcessor?qId=0
        [HttpGet]
        public DataResult GetManufactureProcessor([FromQuery] int groupId, bool menu)
        {
            var result = new DataResult();
            if (menu)
            {
                var data = ServerConfig.ApiDb.Query<dynamic>(
                    $"SELECT a.Id, a.GroupId, a.ProcessorId, b.ProcessorName Processor, b.Account FROM `manufacture_processor` a JOIN `processor` b ON a.ProcessorId = b.Id WHERE a.MarkedDelete = 0{(groupId == 0 ? "" : " AND a.GroupId = @groupId")};",
                    new { groupId });
                result.datas.AddRange(data);
            }
            else
            {
                var data = ServerConfig.ApiDb.Query<ManufactureProcessorDetail>(
                    "SELECT a.*, b.ProcessorName Processor, b.Account FROM `manufacture_processor` a JOIN `processor` b ON a.ProcessorId = b.Id WHERE a.MarkedDelete = 0 AND a.GroupId = @groupId;",
                    new { groupId });
                result.datas.AddRange(data);
            }

            return result;
        }

        // POST: api/ManufactureProcessor
        [HttpPost]
        public Result PostManufactureProcessor([FromBody] IEnumerable<ManufactureProcessor> manufactureProcessors)
        {
            if (manufactureProcessors == null || !manufactureProcessors.Any())
            {
                return Result.GenError<Result>(Error.ManufactureProcessorNotExist);
            }

            var groupIdList = manufactureProcessors.GroupBy(x => x.GroupId).Select(y => y.Key);
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `manufacture_group` WHERE Id IN @groupIdList AND `MarkedDelete` = 0;", new { groupIdList }).FirstOrDefault();
            if (cnt != groupIdList.Count())
            {
                return Result.GenError<Result>(Error.ManufactureGroupNotExist);
            }
            cnt =
              ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `manufacture_processor` WHERE `GroupId` IN @groupId AND ProcessorId IN @processorId AND `MarkedDelete` = 0;",
                  new { groupId = manufactureProcessors.Select(x => x.GroupId), processorId = manufactureProcessors.Select(x => x.ProcessorId) }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.ManufactureProcessorIsExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var manufactureProcessor in manufactureProcessors)
            {
                manufactureProcessor.CreateUserId = createUserId;
                manufactureProcessor.MarkedDateTime = markedDateTime;
            }
            ServerConfig.ApiDb.Execute(
                "INSERT INTO manufacture_processor (`CreateUserId`, `MarkedDateTime`, `GroupId`, `ProcessorId`) VALUES (@CreateUserId, @MarkedDateTime, @GroupId, @ProcessorId);",
                manufactureProcessors);

            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/ManufactureProcessor
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteManufactureProcessor([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `manufacture_processor` WHERE Id IN @id AND `MarkedDelete` = 0;", new { id = ids }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ManufactureProcessorNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `manufacture_processor` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id` IN @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = ids
                });

            return Result.GenError<Result>(Error.Success);
        }
    }
}