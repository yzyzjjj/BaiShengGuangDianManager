using ApiManagement.Base.Server;
using ApiManagement.Models.FlowCardManagementModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.FlowCardManagementController
{
    /// <summary>
    /// 计划号
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]"), ApiController]
    //[Authorize]
    public class ProductionLibraryController : ControllerBase
    {

        // GET: api/ProductionLibrary
        /// <summary>
        /// 
        /// </summary>
        /// <param name="menu"></param>
        /// <param name="qId"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="bFlowCard">总流程卡号</param>
        /// <param name="bComplete">已完成流程卡数</param>
        /// <param name="bRawMaterial">总原料批次</param>
        /// <param name="bCompleteRawMaterial">已完成原料数</param>
        /// <param name="bQualifiedNumber">总产量</param>
        /// <param name="bPassRate">总合格率</param>
        /// <returns></returns>
        [HttpGet]
        public DataResult GetProductionLibrary([FromQuery]bool menu, int qId, DateTime startTime, DateTime endTime,
            bool bFlowCard, bool bComplete, bool bRawMaterial, bool bCompleteRawMaterial, bool bQualifiedNumber, bool bPassRate)
        {
            var result = new DataResult();
            if (menu)
            {
                result.datas.AddRange(ProductionHelper.GetMenu(qId));
            }
            else
            {
                //if (!productionName.IsNullOrEmpty() && startTime != default(DateTime) && endTime != default(DateTime))
                //{
                //    sql =
                //        "SELECT a.*, IFNULL(b.FlowCardCount, 0) FlowCardCount, IFNULL(b.RawMaterialQuantity, 0) AllRawMaterialQuantity, IFNULL(c.RawMaterialQuantity, 0) RawMaterialQuantity, IFNULL(c.Complete, 0) Complete, IFNULL(c.QualifiedNumber, 0) QualifiedNumber FROM `production_library` a LEFT JOIN ( SELECT ProductionProcessId, COUNT(1) FlowCardCount, SUM(RawMaterialQuantity) RawMaterialQuantity FROM `flowcard_library` WHERE MarkedDelete = 0 GROUP BY ProductionProcessId ) b ON a.Id = b.ProductionProcessId LEFT JOIN ( SELECT a.ProductionProcessId, COUNT(1) Complete, SUM(a.QualifiedNumber) QualifiedNumber, SUM(a.RawMaterialQuantity) RawMaterialQuantity FROM ( SELECT * FROM ( SELECT b.ProductionProcessId, b.RawMaterialQuantity, FlowCardId, ProcessStepOrder, QualifiedNumber, ProcessTime FROM `flowcard_process_step` a JOIN `flowcard_library` b ON a.FlowCardId = b.Id WHERE a.MarkedDelete = 0 ORDER BY ProcessStepOrder DESC ) a GROUP BY a.FlowCardId ) a WHERE NOT ISNULL(a.ProcessTime) || a.ProcessTime = '0001-01-01 00:00:00' GROUP BY a.ProductionProcessId ) c ON a.Id = c.ProductionProcessId WHERE a.ProductionProcessName = @ProductionProcessName AND a.MarkedDateTime >= @StartTime AND a.MarkedDateTime <= @EndTime AND a.MarkedDelete = 0;";
                //}
                //else if (!productionName.IsNullOrEmpty())
                //{
                //    sql =
                //        "SELECT a.*, IFNULL(b.FlowCardCount, 0) FlowCardCount, IFNULL(b.RawMaterialQuantity, 0) AllRawMaterialQuantity, IFNULL(c.RawMaterialQuantity, 0) RawMaterialQuantity, IFNULL(c.Complete, 0) Complete, IFNULL(c.QualifiedNumber, 0) QualifiedNumber FROM `production_library` a LEFT JOIN ( SELECT ProductionProcessId, COUNT(1) FlowCardCount, SUM(RawMaterialQuantity) RawMaterialQuantity FROM `flowcard_library` WHERE MarkedDelete = 0 GROUP BY ProductionProcessId ) b ON a.Id = b.ProductionProcessId LEFT JOIN ( SELECT a.ProductionProcessId, COUNT(1) Complete, SUM(a.QualifiedNumber) QualifiedNumber, SUM(a.RawMaterialQuantity) RawMaterialQuantity FROM ( SELECT * FROM ( SELECT b.ProductionProcessId, b.RawMaterialQuantity, FlowCardId, ProcessStepOrder, QualifiedNumber, ProcessTime FROM `flowcard_process_step` a JOIN `flowcard_library` b ON a.FlowCardId = b.Id WHERE a.MarkedDelete = 0 ORDER BY ProcessStepOrder DESC ) a GROUP BY a.FlowCardId ) a WHERE NOT ISNULL(a.ProcessTime) || a.ProcessTime = '0001-01-01 00:00:00' GROUP BY a.ProductionProcessId ) c ON a.Id = c.ProductionProcessId WHERE a.ProductionProcessName = @ProductionProcessName AND a.MarkedDelete = 0;";
                //}
                //else if (startTime != default(DateTime) && endTime != default(DateTime))
                //{
                //    sql =
                //        "SELECT a.*, IFNULL(b.FlowCardCount, 0) FlowCardCount, IFNULL(b.RawMaterialQuantity, 0) AllRawMaterialQuantity, IFNULL(c.RawMaterialQuantity, 0) RawMaterialQuantity, IFNULL(c.Complete, 0) Complete, IFNULL(c.QualifiedNumber, 0) QualifiedNumber FROM `production_library` a LEFT JOIN ( SELECT ProductionProcessId, COUNT(1) FlowCardCount, SUM(RawMaterialQuantity) RawMaterialQuantity FROM `flowcard_library` WHERE MarkedDelete = 0 GROUP BY ProductionProcessId ) b ON a.Id = b.ProductionProcessId LEFT JOIN ( SELECT a.ProductionProcessId, COUNT(1) Complete, SUM(a.QualifiedNumber) QualifiedNumber, SUM(a.RawMaterialQuantity) RawMaterialQuantity FROM ( SELECT * FROM ( SELECT b.ProductionProcessId, b.RawMaterialQuantity, FlowCardId, ProcessStepOrder, QualifiedNumber, ProcessTime FROM `flowcard_process_step` a JOIN `flowcard_library` b ON a.FlowCardId = b.Id WHERE a.MarkedDelete = 0 ORDER BY ProcessStepOrder DESC ) a GROUP BY a.FlowCardId ) a WHERE NOT ISNULL(a.ProcessTime) || a.ProcessTime = '0001-01-01 00:00:00' GROUP BY a.ProductionProcessId ) c ON a.Id = c.ProductionProcessId WHERE a.MarkedDelete = 0 AND a.MarkedDateTime >= @StartTime AND a.MarkedDateTime <= @EndTime;";
                //}
                //else
                //{
                //    sql =
                //        "SELECT a.*, IFNULL(b.FlowCardCount, 0) FlowCardCount, IFNULL(b.RawMaterialQuantity, 0) AllRawMaterialQuantity, IFNULL(c.RawMaterialQuantity, 0) RawMaterialQuantity, IFNULL(c.Complete, 0) Complete, IFNULL(c.QualifiedNumber, 0) QualifiedNumber FROM `production_library` a LEFT JOIN ( SELECT ProductionProcessId, COUNT(1) FlowCardCount, SUM(RawMaterialQuantity) RawMaterialQuantity FROM `flowcard_library` WHERE MarkedDelete = 0 GROUP BY ProductionProcessId ) b ON a.Id = b.ProductionProcessId LEFT JOIN ( SELECT a.ProductionProcessId, COUNT(1) Complete, SUM(a.QualifiedNumber) QualifiedNumber, SUM(a.RawMaterialQuantity) RawMaterialQuantity FROM ( SELECT * FROM ( SELECT b.ProductionProcessId, b.RawMaterialQuantity, FlowCardId, ProcessStepOrder, QualifiedNumber, ProcessTime FROM `flowcard_process_step` a JOIN `flowcard_library` b ON a.FlowCardId = b.Id WHERE a.MarkedDelete = 0 ORDER BY ProcessStepOrder DESC ) a GROUP BY a.FlowCardId ) a WHERE NOT ISNULL(a.ProcessTime) || a.ProcessTime = '0001-01-01 00:00:00' GROUP BY a.ProductionProcessId ) c ON a.Id = c.ProductionProcessId WHERE a.MarkedDelete = 0;";
                //}
                var productionLibraryDetails = ProductionHelper.GetDetail(qId, startTime, endTime);
                if (qId != 0 && productionLibraryDetails.Any())
                {
                    var production = productionLibraryDetails.First();
                    production.ProcessSteps.AddRange(ServerConfig.ApiDb.Query<ProductionProcessStep>(
                        "SELECT a.*, b.CategoryName, b.StepName FROM `production_process_step` a " +
                        "JOIN ( SELECT a.Id, a.StepName, b.CategoryName FROM `device_process_step` a JOIN `device_category` b ON a.DeviceCategoryId = b.Id WHERE a.MarkedDelete = 0 ) b ON a.ProcessStepId = b.Id WHERE ProductionProcessId = @ProductionProcessId AND a.MarkedDelete = 0 ORDER BY a.Id;", new
                        {
                            ProductionProcessId = production.Id
                        }));
                    production.Specifications.AddRange(ServerConfig.ApiDb.Query<ProductionSpecification>(
                        "SELECT * FROM `production_specification` WHERE ProductionProcessId = @ProductionProcessId AND MarkedDelete = 0 ORDER BY Id;", new
                        {
                            ProductionProcessId = production.Id
                        }));
                }
                result.datas.AddRange(productionLibraryDetails);
            }
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.ProductionLibraryNotExist;
                return result;
            }
            return result;
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="production"></param>
        /// <returns></returns>
        // PUT: api/ProductionLibrary/Id/5
        [HttpPut]
        public Result PutProductionLibrary([FromBody] Production production)
        {
            if (production == null)
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (production.ProductionProcessName.IsNullOrEmpty())
            {
                return Result.GenError<Result>(Error.ProductionLibraryNotExist);
            }

            var sames = new List<string> { production.ProductionProcessName };
            var ids = new List<int> { production.Id };
            if (ProductionHelper.GetHaveSame(sames, ids))
            {
                return Result.GenError<Result>(Error.ProductionLibraryIsExist);
            }

            var data = ProductionHelper.Instance.Get<Production>(production.Id);
            if (data == null)
            {
                return Result.GenError<Result>(Error.ProductionLibraryNotExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var time = DateTime.Now;
            var change = false;

            var specifications = production.Specifications;
            //if (productionProcessSpecifications.Any())
            {
                foreach (var specification in specifications)
                {
                    specification.ProductionProcessId = production.Id;
                    specification.CreateUserId = createUserId;
                    specification.MarkedDateTime = time;
                }

                if (specifications.Any(x => x.Id == 0))
                {
                    change = true;
                    ServerConfig.ApiDb.Execute(
                    "INSERT INTO production_specification (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ProductionProcessId`, `SpecificationName`, `SpecificationValue`) " +
                    "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ProductionProcessId, @SpecificationName, @SpecificationValue);",
                    specifications.Where(x => x.Id == 0));
                }

                var existSpecifications = ServerConfig.ApiDb.Query<ProductionSpecification>("SELECT * FROM `production_specification` " +
                                                                                            new { ProductionProcessId = production.Id });
                var updateSpecifications = specifications.Where(x => x.Id != 0 && existSpecifications.Any(y => y.Id == x.Id && (y.SpecificationName != x.SpecificationName || y.SpecificationValue != x.SpecificationValue))).ToList();
                updateSpecifications.AddRange(existSpecifications.Where(x => specifications.All(y => x.Id != y.Id)).Select(x =>
                {
                    x.MarkedDateTime = DateTime.Now;
                    x.MarkedDelete = true;
                    return x;
                }));
                if (updateSpecifications.Any())
                {
                    change = true;
                    ServerConfig.ApiDb.Execute(
                        "UPDATE production_specification SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = " +
                    "@MarkedDelete, `ModifyId` = @ModifyId, `ProductionProcessId` = @ProductionProcessId, `SpecificationName` = @SpecificationName, " +
                    "`SpecificationValue` = @SpecificationValue WHERE `Id` = @Id;", updateSpecifications);
                }
            }
            var processSteps = production.ProcessSteps;
            //if (processSteps.Any())
            {
                foreach (var processStep in processSteps)
                {
                    processStep.ProductionProcessId = production.Id;
                    processStep.CreateUserId = createUserId;
                    processStep.MarkedDateTime = time;
                }

                if (processSteps.Any(x => x.Id == 0))
                {
                    change = true;
                    ServerConfig.ApiDb.Execute(
                    "INSERT INTO production_process_step (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ProductionProcessId`, `ProcessStepOrder`, `ProcessStepId`, `ProcessStepRequirements`, `ProcessStepRequirementMid`) " +
                    "VALUES(@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ProductionProcessId, @ProcessStepOrder, @ProcessStepId, @ProcessStepRequirements, @ProcessStepRequirementMid); ",
                    processSteps.Where(x => x.Id == 0).OrderBy(x => x.ProcessStepOrder));
                }


                var exist = ServerConfig.ApiDb.Query<ProductionProcessStep>("SELECT * FROM `production_process_step` " +
                                                                            "WHERE MarkedDelete = 0 AND ProductionProcessId = @ProductionProcessId;", new { ProductionProcessId = production.Id });

                var update = processSteps.Where(x => x.Id != 0 && exist.Any(y => y.Id == x.Id
                                    && (y.ProcessStepOrder != x.ProcessStepOrder || y.ProcessStepId != x.ProcessStepId || y.ProcessStepRequirements != x.ProcessStepRequirements || y.ProcessStepRequirementMid != x.ProcessStepRequirementMid))).ToList();
                update.AddRange(exist.Where(x => processSteps.All(y => x.Id != y.Id)).Select(x =>
                {
                    x.MarkedDateTime = DateTime.Now;
                    x.MarkedDelete = true;
                    return x;
                }));

                if (update.Any())
                {
                    change = true;
                    ServerConfig.ApiDb.Execute(
                        "UPDATE production_process_step SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, `ProductionProcessId` = " +
                    "@ProductionProcessId, `ProcessStepOrder` = @ProcessStepOrder, `ProcessStepId` = @ProcessStepId, `ProcessStepRequirements` = @ProcessStepRequirements, `ProcessStepRequirementMid` = @ProcessStepRequirementMid " +
                    "WHERE `Id` = @Id;", update);
            }
            }

            if (change || ClassExtension.HaveChange(production, data))
            {
                production.MarkedDateTime = time;
                ProductionHelper.Instance.Update(production);
            }
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/ProductionLibrary
        [HttpPost]
        public Result PostProductionLibrary([FromBody] Production production)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `production_library` WHERE ProductionProcessName = @ProductionProcessName AND MarkedDelete = 0;", new { production.ProductionProcessName }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.ProductionLibraryIsExist);
            }
            var createUserId = Request.GetIdentityInformation();
            var time = DateTime.Now;
            production.CreateUserId = createUserId;
            production.MarkedDateTime = time;

            var index = ServerConfig.ApiDb.Query<int>(
                "INSERT INTO production_library (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ProductionProcessName`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ProductionProcessName);SELECT LAST_INSERT_ID();",
                production).FirstOrDefault();

            if (production.Specifications.Any())
            {
                var productionProcessSpecifications = production.Specifications;
                foreach (var productionProcessSpecification in productionProcessSpecifications)
                {
                    productionProcessSpecification.ProductionProcessId = index;
                    productionProcessSpecification.CreateUserId = createUserId;
                    productionProcessSpecification.MarkedDateTime = time;
                }

                ServerConfig.ApiDb.Execute(
                    "INSERT INTO production_specification (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ProductionProcessId`, `SpecificationName`, `SpecificationValue`) " +
                    "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ProductionProcessId, @SpecificationName, @SpecificationValue);",
                    productionProcessSpecifications);
            }
            if (production.ProcessSteps.Any())
            {
                var processSteps = production.ProcessSteps;
                foreach (var processStep in processSteps)
                {
                    processStep.ProductionProcessId = index;
                    processStep.CreateUserId = createUserId;
                    processStep.MarkedDateTime = time;
                }

                ServerConfig.ApiDb.Execute(
                    "INSERT INTO production_process_step (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ProductionProcessId`, `ProcessStepOrder`, `ProcessStepId`, `ProcessStepRequirements`, `ProcessStepRequirementMid`) " +
                    "VALUES(@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ProductionProcessId, @ProcessStepOrder, @ProcessStepId, @ProcessStepRequirements, @ProcessStepRequirementMid); ",
                    processSteps.OrderBy(x => x.ProcessStepOrder));
            }

            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // DELETE: api/ProductionLibrary/5
        [HttpDelete("{id}")]
        public Result DeleteProductionLibrary([FromRoute] int id)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `production_library` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ProductionLibraryNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `production_library` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            ServerConfig.ApiDb.Execute(
                "UPDATE `production_process_step` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `ProductionProcessId`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            ServerConfig.ApiDb.Execute(
                "UPDATE `production_specification` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `ProductionProcessId`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            ServerConfig.ApiDb.Execute(
                "UPDATE `process_management` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `ProductModels`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}