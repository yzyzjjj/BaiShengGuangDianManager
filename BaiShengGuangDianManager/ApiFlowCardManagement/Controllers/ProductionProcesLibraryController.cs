using ApiFlowCardManagement.Base.Server;
using ApiFlowCardManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Linq;

namespace ApiFlowCardManagement.Controllers
{
    /// <summary>
    /// 计划号
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class ProductionProcessLibraryController : ControllerBase
    {

        // GET: api/ProductionProcessLibrary
        [HttpGet]
        public DataResult GetProductionProcessLibrary()
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.FlowcardDb.Query<ProductionProcessLibraryDetail>("SELECT a.*, b.FlowCardCount, c.QualifiedNumber, c.UnqualifiedNumber FROM " +
                                                                                          "`production_process_library` a LEFT JOIN ( SELECT ProductionProcessId, COUNT(1) " +
                                                                                          "FlowCardCount FROM `flowcard_library` GROUP BY ProductionProcessId ) b ON a.Id = b.ProductionProcessId " +
                                                                                          "LEFT JOIN ( SELECT ProductionProcessId, SUM(QualifiedNumber) QualifiedNumber, SUM(UnqualifiedNumber) " +
                                                                                          "UnqualifiedNumber FROM `process_step` WHERE ProcessorTime IS NOT NULL GROUP BY ProductionProcessId ) " +
                                                                                          "c ON a.Id = c.ProductionProcessId;"));
            return result;
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id">自增Id</param>
        /// <returns></returns>
        // GET: api/ProductionProcessLibrary/Id/5
        [HttpGet("Id/{id}")]
        public DataResult GetProductionProcessLibrary([FromRoute] int id)
        {
            var result = new DataResult();
            var data =
                ServerConfig.FlowcardDb.Query<ProductionProcessLibraryDetail>("SELECT a.*, b.FlowCardCount, c.QualifiedNumber, c.UnqualifiedNumber FROM " +
                                                                              "`production_process_library` a LEFT JOIN ( SELECT ProductionProcessId, COUNT(1) " +
                                                                              "FlowCardCount FROM `flowcard_library` GROUP BY ProductionProcessId ) b ON a.Id = b.ProductionProcessId " +
                                                                              "LEFT JOIN ( SELECT ProductionProcessId, SUM(QualifiedNumber) QualifiedNumber, SUM(UnqualifiedNumber) " +
                                                                              "UnqualifiedNumber FROM `process_step` WHERE ProcessorTime IS NOT NULL GROUP BY ProductionProcessId ) " +
                                                                              "c ON a.Id = c.ProductionProcessId WHERE Id = @id;", new { id }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.ProductionProcessLibraryNotExist;
                return result;
            }
            result.datas.Add(data);
            return result;
        }

        /// <summary>
        /// 计划号
        /// </summary>
        /// <param name="productionProcessName">计划号</param>
        /// <returns></returns>
        // GET: api/ProductionProcessLibrary/ProductionProcessName/5
        [HttpGet("ProductionProcessName/{productionProcessName}")]
        public DataResult GetProductionProcessLibrary([FromRoute] string productionProcessName)
        {
            var result = new DataResult();
            var data =
                ServerConfig.FlowcardDb.Query<ProductionProcessLibraryDetail>("SELECT a.*, b.FlowCardCount, c.QualifiedNumber, c.UnqualifiedNumber FROM " +
                                                                              "`production_process_library` a LEFT JOIN ( SELECT ProductionProcessId, COUNT(1) " +
                                                                              "FlowCardCount FROM `flowcard_library` GROUP BY ProductionProcessId ) b ON a.Id = b.ProductionProcessId " +
                                                                              "LEFT JOIN ( SELECT ProductionProcessId, SUM(QualifiedNumber) QualifiedNumber, SUM(UnqualifiedNumber) " +
                                                                              "UnqualifiedNumber FROM `process_step` WHERE ProcessorTime IS NOT NULL GROUP BY ProductionProcessId ) " +
                                                                              "c ON a.Id = c.ProductionProcessId WHERE ProductionProcessName = @productionProcessName;", new { productionProcessName }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.ProductionProcessLibraryNotExist;
                return result;
            }
            result.datas.Add(data);
            return result;
        }




        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id">自增Id</param>
        /// <param name="productionProcessLibrary"></param>
        /// <returns></returns>
        // PUT: api/ProductionProcessLibrary/Id/5
        [HttpPut("Id/{id}")]
        public Result PutProductionProcessLibrary([FromRoute] int id, [FromBody] ProductionProcessLibrary productionProcessLibrary)
        {
            var data =
                ServerConfig.FlowcardDb.Query<ProductionProcessLibrary>("SELECT * FROM `production_process_library` WHERE Id = @id;", new { id }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.ProductionProcessLibraryNotExist);
            }

            var cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `production_process_library` WHERE ProductionProcessName = @ProductionProcessName;", new { productionProcessLibrary.ProductionProcessName }).FirstOrDefault();
            if (cnt > 0)
            {
                if (!productionProcessLibrary.ProductionProcessName.IsNullOrEmpty() && data.ProductionProcessName != productionProcessLibrary.ProductionProcessName)
                {
                    return Result.GenError<Result>(Error.ProductionProcessLibraryIsExist);
                }
            }

            productionProcessLibrary.Id = id;
            productionProcessLibrary.CreateUserId = Request.GetIdentityInformation();
            productionProcessLibrary.MarkedDateTime = DateTime.Now;
            ServerConfig.FlowcardDb.Execute(
                "UPDATE production_process_library SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, " +
                "`MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, `ProductionProcessName` = @ProductionProcessName WHERE `Id` = @Id;", productionProcessLibrary);

            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 计划号
        /// </summary>
        /// <param name="productionProcessName">计划号</param>
        /// <param name="productionProcessLibrary"></param>
        /// <returns></returns>
        // PUT: api/ProductionProcessLibrary/ProductionProcessName/5
        [HttpPut("ProductionProcessName/{productionProcessName}")]
        public Result PutProductionProcessLibrary([FromRoute] string productionProcessName, [FromBody] ProductionProcessLibrary productionProcessLibrary)
        {
            var data =
                ServerConfig.FlowcardDb.Query<ProductionProcessLibrary>("SELECT `Id` FROM `production_process_library` WHERE ProductionProcessName = @productionProcessName;", new { productionProcessName }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.ProductionProcessLibraryNotExist);
            }

            var cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `production_process_library` WHERE ProductionProcessName = @ProductionProcessName;", new { productionProcessLibrary.ProductionProcessName }).FirstOrDefault();
            if (cnt > 0)
            {
                if (!productionProcessLibrary.ProductionProcessName.IsNullOrEmpty() && data.ProductionProcessName != productionProcessLibrary.ProductionProcessName)
                {
                    return Result.GenError<Result>(Error.ProductionProcessLibraryIsExist);
                }
            }
            productionProcessLibrary.Id = data.Id;
            productionProcessLibrary.CreateUserId = Request.GetIdentityInformation();
            productionProcessLibrary.MarkedDateTime = DateTime.Now;
            ServerConfig.FlowcardDb.Execute(
                "UPDATE production_process_library SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, " +
                "`MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, `ProductionProcessName` = @ProductionProcessName WHERE `Id` = @Id;", productionProcessLibrary);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/ProductionProcessLibrary
        [HttpPost]
        public Result PostProductionProcessLibrary([FromBody] ProductionProcessLibrary productionProcessLibrary)
        {
            var cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `production_process_library` WHERE ProductionProcessName = @ProductionProcessName;", new { productionProcessLibrary.ProductionProcessName }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.ProductionProcessLibraryIsExist);
            }

            productionProcessLibrary.CreateUserId = Request.GetIdentityInformation();
            productionProcessLibrary.MarkedDateTime = DateTime.Now;
            ServerConfig.FlowcardDb.Execute(
                "INSERT INTO production_process_library (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ProductionProcessName`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ProductionProcessName);",
                productionProcessLibrary);

            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // DELETE: api/ProductionProcessLibrary/Id/5
        [HttpDelete("Id/{id}")]
        public Result DeleteProductionProcessLibrary([FromRoute] int id)
        {
            var cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `production_process_library` WHERE Id = @id;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ProductionProcessLibraryNotExist);
            }

            ServerConfig.FlowcardDb.Execute(
                "UPDATE `production_process_library` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 计划号
        /// </summary>
        /// <param name="productionProcessName">计划号</param>
        /// <returns></returns>
        // DELETE: api/ProductionProcessLibrary/ProductionProcessName/5
        [HttpDelete("ProductionProcessName/{productionProcessName}")]
        public Result DeleteProductionProcessLibrary([FromRoute] string productionProcessName)
        {
            var cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `production_process_library` WHERE ProductionProcessName = @productionProcessName;", new { productionProcessName }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ProductionProcessLibraryNotExist);
            }

            ServerConfig.FlowcardDb.Execute(
                "UPDATE `production_process_library` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `ProductionProcessName`= @productionProcessName;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    productionProcessName
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}