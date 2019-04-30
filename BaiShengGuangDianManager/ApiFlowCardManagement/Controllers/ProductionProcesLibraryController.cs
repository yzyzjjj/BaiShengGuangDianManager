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
            var productionLibraryDetails = ServerConfig.FlowCardDb.Query<ProductionLibraryDetail>("SELECT a.*, IFNULL(b.FlowCardCount, 0) FlowCardCount, IFNULL(b.RawMaterialQuantity, 0) AllRawMaterialQuantity, IFNULL(c.RawMaterialQuantity, 0) RawMaterialQuantity, IFNULL(c.Complete, 0) Complete, IFNULL(c.QualifiedNumber, 0) QualifiedNumber FROM `production_library` a LEFT JOIN ( SELECT ProductionProcessId, COUNT(1) FlowCardCount, SUM(RawMaterialQuantity) RawMaterialQuantity FROM `flowcard_library` GROUP BY ProductionProcessId ) b ON a.Id = b.ProductionProcessId LEFT JOIN ( SELECT a.ProductionProcessId, COUNT(1) Complete, SUM(a.QualifiedNumber) QualifiedNumber, SUM(a.RawMaterialQuantity) RawMaterialQuantity FROM ( SELECT * FROM ( SELECT b.ProductionProcessId, b.RawMaterialQuantity, FlowCardId, ProcessStepOrder, QualifiedNumber, ProcessTime FROM `flowcard_process_step` a JOIN `flowcard_library` b ON a.FlowCardId = b.Id WHERE a.MarkedDelete = 0 ORDER BY ProcessStepOrder DESC ) a GROUP BY a.FlowCardId ) a WHERE NOT ISNULL(a.ProcessTime) || a.ProcessTime = '0001-01-01 00:00:00' GROUP BY a.ProductionProcessId ) c ON a.Id = c.ProductionProcessId WHERE a.MarkedDelete = 0;");

            result.datas.AddRange(productionLibraryDetails);
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
                ServerConfig.FlowCardDb.Query<ProductionLibraryDetail>("SELECT * FROM `production_library` WHERE MarkedDelete = 0 AND Id = @id;", new { id }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.ProductionProcessLibraryNotExist;
                return result;
            }
            data.ProcessSteps.AddRange(ServerConfig.FlowCardDb.Query<ProductionProcessStep>("SELECT * FROM `production_process_step` WHERE ProductionProcessId = @ProductionProcessId AND MarkedDelete = 0;", new
            {
                ProductionProcessId = data.Id
            }));
            data.Specifications.AddRange(ServerConfig.FlowCardDb.Query<ProductionSpecification>("SELECT * FROM `production_specification` WHERE ProductionProcessId = @ProductionProcessId AND MarkedDelete = 0;", new
            {
                ProductionProcessId = data.Id
            }));
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
                ServerConfig.FlowCardDb.Query<ProductionLibraryDetail>("SELECT * FROM `production_library` WHERE MarkedDelete = 0 AND ProductionProcessName = @productionProcessName;", new { productionProcessName }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.ProductionProcessLibraryNotExist;
                return result;
            }
            data.ProcessSteps.AddRange(ServerConfig.FlowCardDb.Query<ProductionProcessStep>("SELECT * FROM `production_process_step` WHERE ProductionProcessId = @ProductionProcessId AND MarkedDelete = 0;", new
            {
                ProductionProcessId = data.Id
            }));
            data.Specifications.AddRange(ServerConfig.FlowCardDb.Query<ProductionSpecification>("SELECT * FROM `production_specification` WHERE ProductionProcessId = @ProductionProcessId AND MarkedDelete = 0;", new
            {
                ProductionProcessId = data.Id
            }));
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
        public Result PutProductionProcessLibrary([FromRoute] int id, [FromBody] ProductionLibrary productionProcessLibrary)
        {
            var data =
                ServerConfig.FlowCardDb.Query<ProductionLibrary>("SELECT * FROM `production_library` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.ProductionProcessLibraryNotExist);
            }

            var cnt =
                ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `production_library` WHERE ProductionProcessName = @ProductionProcessName AND MarkedDelete = 0;", new { productionProcessLibrary.ProductionProcessName }).FirstOrDefault();
            if (cnt > 0)
            {
                if (!productionProcessLibrary.ProductionProcessName.IsNullOrEmpty() && data.ProductionProcessName != productionProcessLibrary.ProductionProcessName)
                {
                    return Result.GenError<Result>(Error.ProductionProcessLibraryIsExist);
                }
            }

            var createUserId = Request.GetIdentityInformation();
            var time = DateTime.Now;
            productionProcessLibrary.Id = id;
            productionProcessLibrary.CreateUserId = createUserId;
            productionProcessLibrary.MarkedDateTime = time;
            ServerConfig.FlowCardDb.Execute(
                "UPDATE production_library SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, " +
                "`MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, `ProductionProcessName` = @ProductionProcessName WHERE `Id` = @Id;", productionProcessLibrary);
            var productionProcessSpecifications = productionProcessLibrary.Specifications;
            if (productionProcessSpecifications.Any())
            {
                foreach (var productionProcessSpecification in productionProcessSpecifications)
                {
                    productionProcessSpecification.ProductionProcessId = id;
                    productionProcessSpecification.CreateUserId = createUserId;
                    productionProcessSpecification.MarkedDateTime = time;
                }

                var exist = ServerConfig.FlowCardDb.Query<ProductionSpecification>("SELECT * FROM `production_specification` " +
                                                                                                           "WHERE MarkedDelete = 0 AND ProductionProcessId = @ProductionProcessId;", new { ProductionProcessId = id });
                ServerConfig.FlowCardDb.Execute(
                    "INSERT INTO production_specification (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ProductionProcessId`, `SpecificationName`, `SpecificationValue`) " +
                    "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ProductionProcessId, @SpecificationName, @SpecificationValue);",
                    productionProcessSpecifications.Where(x => x.Id == 0));

                var update = productionProcessSpecifications.Where(x => x.Id != 0 && exist.Any(y => y.Id == x.Id && (y.SpecificationName != x.SpecificationName || y.SpecificationValue != x.SpecificationValue))).ToList();
                update.AddRange(exist.Where(x => productionProcessSpecifications.All(y => x.Id != y.Id)).Select(x =>
                {
                    x.MarkedDateTime = DateTime.Now;
                    x.MarkedDelete = true;
                    return x;
                }));
                ServerConfig.FlowCardDb.Execute(
                    "UPDATE production_specification SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = " +
                    "@MarkedDelete, `ModifyId` = @ModifyId, `ProductionProcessId` = @ProductionProcessId, `SpecificationName` = @SpecificationName, " +
                    "`SpecificationValue` = @SpecificationValue WHERE `Id` = @Id;", update);
            }

            var processSteps = productionProcessLibrary.ProcessSteps;
            if (processSteps.Any())
            {
                foreach (var processStep in processSteps)
                {
                    processStep.ProductionProcessId = id;
                    processStep.CreateUserId = createUserId;
                    processStep.MarkedDateTime = time;
                }

                var exist = ServerConfig.FlowCardDb.Query<ProductionProcessStep>("SELECT * FROM `production_process_step` " +
                                                                                          "WHERE MarkedDelete = 0 AND ProductionProcessId = @ProductionProcessId;", new { ProductionProcessId = id });

                ServerConfig.FlowCardDb.Execute(
                    "INSERT INTO production_process_step (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ProductionProcessId`, `ProcessStepOrder`, `ProcessStepName`, `ProcessStepRequirements`) " +
                    "VALUES(@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ProductionProcessId, @ProcessStepOrder, @ProcessStepName, @ProcessStepRequirements); ",
                    processSteps.Where(x => x.Id == 0).OrderBy(x => x.ProcessStepOrder));


                var update = processSteps.Where(x => x.Id != 0 && exist.Any(y => y.Id == x.Id
                                    && (y.ProcessStepOrder != x.ProcessStepOrder || y.ProcessStepName != x.ProcessStepName || y.ProcessStepRequirements != x.ProcessStepRequirements))).ToList();
                update.AddRange(exist.Where(x => processSteps.All(y => x.Id != y.Id)).Select(x =>
                {
                    x.MarkedDateTime = DateTime.Now;
                    x.MarkedDelete = true;
                    return x;
                }));
                ServerConfig.FlowCardDb.Execute(
                    "UPDATE production_process_step SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, `ProductionProcessId` = " +
                    "@ProductionProcessId, `ProcessStepOrder` = @ProcessStepOrder, `ProcessStepName` = @ProcessStepName, `ProcessStepRequirements` = @ProcessStepRequirements " +
                    "WHERE `Id` = @Id;", update);
            }
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
        public Result PutProductionProcessLibrary([FromRoute] string productionProcessName, [FromBody] ProductionLibrary productionProcessLibrary)
        {
            var data =
                ServerConfig.FlowCardDb.Query<ProductionLibrary>("SELECT `Id` FROM `production_library` WHERE ProductionProcessName = @productionProcessName AND MarkedDelete = 0;", new { productionProcessName }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.ProductionProcessLibraryNotExist);
            }

            var id = data.Id;
            var cnt =
                ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `production_library` WHERE ProductionProcessName = @ProductionProcessName AND MarkedDelete = 0;", new { productionProcessLibrary.ProductionProcessName }).FirstOrDefault();
            if (cnt > 0)
            {
                if (!productionProcessLibrary.ProductionProcessName.IsNullOrEmpty() && data.ProductionProcessName != productionProcessLibrary.ProductionProcessName)
                {
                    return Result.GenError<Result>(Error.ProductionProcessLibraryIsExist);
                }
            }

            var createUserId = Request.GetIdentityInformation();
            var time = DateTime.Now;
            productionProcessLibrary.Id = id;
            productionProcessLibrary.CreateUserId = createUserId;
            productionProcessLibrary.MarkedDateTime = time;
            ServerConfig.FlowCardDb.Execute(
                "UPDATE production_library SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, " +
                "`MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, `ProductionProcessName` = @ProductionProcessName WHERE `Id` = @Id;", productionProcessLibrary);
            var productionProcessSpecifications = productionProcessLibrary.Specifications;
            if (productionProcessSpecifications.Any())
            {
                foreach (var productionProcessSpecification in productionProcessSpecifications)
                {
                    productionProcessSpecification.ProductionProcessId = id;
                    productionProcessSpecification.CreateUserId = createUserId;
                    productionProcessSpecification.MarkedDateTime = time;
                }

                var exist = ServerConfig.FlowCardDb.Query<ProductionSpecification>("SELECT * FROM `production_specification` " +
                                                                                                           "WHERE MarkedDelete = 0 AND ProductionProcessId = @ProductionProcessId;", new { ProductionProcessId = id });
                ServerConfig.FlowCardDb.Execute(
                    "INSERT INTO production_specification (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ProductionProcessId`, `SpecificationName`, `SpecificationValue`) " +
                    "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ProductionProcessId, @SpecificationName, @SpecificationValue);",
                    productionProcessSpecifications.Where(x => x.Id == 0));

                var update = productionProcessSpecifications.Where(x => x.Id != 0 && exist.Any(y => y.Id == x.Id && (y.SpecificationName != x.SpecificationName || y.SpecificationValue != x.SpecificationValue))).ToList();
                update.AddRange(exist.Where(x => productionProcessSpecifications.All(y => x.Id != y.Id)).Select(x =>
                {
                    x.MarkedDateTime = DateTime.Now;
                    x.MarkedDelete = true;
                    return x;
                }));
                ServerConfig.FlowCardDb.Execute(
                    "UPDATE production_specification SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = " +
                    "@MarkedDelete, `ModifyId` = @ModifyId, `ProductionProcessId` = @ProductionProcessId, `SpecificationName` = @SpecificationName, " +
                    "`SpecificationValue` = @SpecificationValue WHERE `Id` = @Id;", update);
            }

            var processSteps = productionProcessLibrary.ProcessSteps;
            if (processSteps.Any())
            {
                foreach (var processStep in processSteps)
                {
                    processStep.ProductionProcessId = id;
                    processStep.CreateUserId = createUserId;
                    processStep.MarkedDateTime = time;
                }

                var exist = ServerConfig.FlowCardDb.Query<ProductionProcessStep>("SELECT * FROM `production_process_step` " +
                                                                                          "WHERE MarkedDelete = 0 AND ProductionProcessId = @ProductionProcessId;", new { ProductionProcessId = id });

                ServerConfig.FlowCardDb.Execute(
                    "INSERT INTO production_process_step (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ProductionProcessId`, `ProcessStepOrder`, `ProcessStepName`, `ProcessStepRequirements`) " +
                    "VALUES(@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ProductionProcessId, @ProcessStepOrder, @ProcessStepName, @ProcessStepRequirements); ",
                    processSteps.Where(x => x.Id == 0).OrderBy(x => x.ProcessStepOrder));


                var update = processSteps.Where(x => x.Id != 0 && exist.Any(y => y.Id == x.Id
                                    && (y.ProcessStepOrder != x.ProcessStepOrder || y.ProcessStepName != x.ProcessStepName || y.ProcessStepRequirements != x.ProcessStepRequirements))).ToList();
                update.AddRange(exist.Where(x => processSteps.All(y => x.Id != y.Id)).Select(x =>
                {
                    x.MarkedDateTime = DateTime.Now;
                    x.MarkedDelete = true;
                    return x;
                }));
                ServerConfig.FlowCardDb.Execute(
                    "UPDATE production_process_step SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, `ProductionProcessId` = " +
                    "@ProductionProcessId, `ProcessStepOrder` = @ProcessStepOrder, `ProcessStepName` = @ProcessStepName, `ProcessStepRequirements` = @ProcessStepRequirements " +
                    "WHERE `Id` = @Id;", update);
            }
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/ProductionProcessLibrary
        [HttpPost]
        public Result PostProductionProcessLibrary([FromBody] ProductionLibrary productionProcessLibrary)
        {
            var cnt =
                ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `production_library` WHERE ProductionProcessName = @ProductionProcessName AND MarkedDelete = 0;", new { productionProcessLibrary.ProductionProcessName }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.ProductionProcessLibraryIsExist);
            }
            var createUserId = Request.GetIdentityInformation();
            var time = DateTime.Now;
            productionProcessLibrary.CreateUserId = createUserId;
            productionProcessLibrary.MarkedDateTime = time;

            var index = ServerConfig.FlowCardDb.Query<int>(
                "INSERT INTO production_library (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ProductionProcessName`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ProductionProcessName);SELECT LAST_INSERT_ID();",
                productionProcessLibrary).FirstOrDefault();

            if (productionProcessLibrary.Specifications.Any())
            {
                var productionProcessSpecifications = productionProcessLibrary.Specifications;
                foreach (var productionProcessSpecification in productionProcessSpecifications)
                {
                    productionProcessSpecification.ProductionProcessId = index;
                    productionProcessSpecification.CreateUserId = createUserId;
                    productionProcessSpecification.MarkedDateTime = time;
                }

                ServerConfig.FlowCardDb.Execute(
                    "INSERT INTO production_specification (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ProductionProcessId`, `SpecificationName`, `SpecificationValue`) " +
                    "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ProductionProcessId, @SpecificationName, @SpecificationValue);",
                    productionProcessSpecifications);
            }
            if (productionProcessLibrary.ProcessSteps.Any())
            {
                var processSteps = productionProcessLibrary.ProcessSteps;
                foreach (var processStep in processSteps)
                {
                    processStep.ProductionProcessId = index;
                    processStep.CreateUserId = createUserId;
                    processStep.MarkedDateTime = time;
                }

                ServerConfig.FlowCardDb.Execute(
                    "INSERT INTO production_process_step (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ProductionProcessId`, `ProcessStepOrder`, `ProcessStepName`, `ProcessStepRequirements`) " +
                    "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ProductionProcessId, @ProcessStepOrder, @ProcessStepName, @ProcessStepRequirements);",
                    processSteps.OrderBy(x => x.ProcessStepOrder));
            }

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
                ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `production_library` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ProductionProcessLibraryNotExist);
            }

            ServerConfig.FlowCardDb.Execute(
                "UPDATE `production_library` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            ServerConfig.FlowCardDb.Execute(
                "UPDATE `production_process_step` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `ProductionProcessId`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            ServerConfig.FlowCardDb.Execute(
                "UPDATE `production_specification` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `ProductionProcessId`= @Id;", new
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
            var data =
                ServerConfig.FlowCardDb.Query<ProductionLibrary>("SELECT * FROM `production_library` WHERE ProductionProcessName = @productionProcessName AND MarkedDelete = 0;", new { productionProcessName }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.ProductionProcessLibraryNotExist);
            }

            var id = data.Id;
            ServerConfig.FlowCardDb.Execute(
                "UPDATE `production_library` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            ServerConfig.FlowCardDb.Execute(
                "UPDATE `production_process_step` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `ProductionProcessId`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            ServerConfig.FlowCardDb.Execute(
                "UPDATE `production_specification` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `ProductionProcessId`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}