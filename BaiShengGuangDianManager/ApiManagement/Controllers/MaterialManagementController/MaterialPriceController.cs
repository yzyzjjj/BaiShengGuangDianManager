using ApiManagement.Base.Server;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Models.Result;

namespace ApiManagement.Controllers.MaterialManagementController
{
    /// <summary>
    /// 货品规格
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class MaterialPriceController : ControllerBase
    {
        // GET: api/MaterialPrice?categoryId=0&nameId=0&supplierId=0&specificationId=0&menu=false
        [HttpGet]
        public DataResult GetMaterialPrice([FromQuery] int categoryId, int nameId, int supplierId, int specificationId)
        {
            var result = new DataResult();
            string sql;
            if (categoryId != 0 && nameId == 0 && supplierId == 0 && specificationId == 0)
            {
                sql =
                    "SELECT a.Price, b.CategoryId, b.Category, b.NameId, b.`Name`, b.SupplierId, b.Supplier, b.Specification FROM `material_bill` a " +
                    "JOIN (SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a " +
                    "JOIN (SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM `material_supplier` a " +
                    "JOIN (SELECT a.*, b.Category FROM `material_name` a " +
                    "JOIN `material_category` b ON a.CategoryId = b.Id WHERE a.CategoryId = @categoryId) b ON a.NameId = b.Id) b ON a.SupplierId = b.Id) b ON a.SpecificationId = b.Id " +
                    "WHERE a.`MarkedDelete` = 0;";
            }
            else if (nameId != 0 && supplierId == 0 && specificationId == 0)
            {
                sql =
                    "SELECT a.Price, b.CategoryId, b.Category, b.NameId, b.`Name`, b.SupplierId, b.Supplier, b.Specification FROM `material_bill` a " +
                    "JOIN (SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a " +
                    "JOIN (SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM `material_supplier` a " +
                    "JOIN (SELECT a.*, b.Category FROM `material_name` a " +
                    "JOIN `material_category` b ON a.CategoryId = b.Id) b ON a.NameId = b.Id WHERE a.NameId = @nameId) b ON a.SupplierId = b.Id) b ON a.SpecificationId = b.Id " +
                    "WHERE a.`MarkedDelete` = 0;";
            }
            else if (supplierId != 0 && specificationId == 0)
            {
                sql =
                    "SELECT a.Price, b.CategoryId, b.Category, b.NameId, b.`Name`, b.SupplierId, b.Supplier, b.Specification FROM `material_bill` a " +
                    "JOIN (SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a " +
                    "JOIN (SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM `material_supplier` a " +
                    "JOIN (SELECT a.*, b.Category FROM `material_name` a " +
                    "JOIN `material_category` b ON a.CategoryId = b.Id) b ON a.NameId = b.Id) b ON a.SupplierId = b.Id WHERE a.SupplierId = @supplierId) b ON a.SpecificationId = b.Id " +
                    "WHERE a.`MarkedDelete` = 0;";
            }
            else if (specificationId != 0)
            {
                sql =
                    "SELECT a.Price, b.CategoryId, b.Category, b.NameId, b.`Name`, b.SupplierId, b.Supplier, b.Specification FROM `material_bill` a " +
                    "JOIN (SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a " +
                    "JOIN (SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM `material_supplier` a " +
                    "JOIN (SELECT a.*, b.Category FROM `material_name` a " +
                    "JOIN `material_category` b ON a.CategoryId = b.Id) b ON a.NameId = b.Id) b ON a.SupplierId = b.Id) b ON a.SpecificationId = b.Id " +
                    "WHERE a.SpecificationId = @specificationId AND a.`MarkedDelete` = 0;";
            }
            else
            {
                sql =
                    "SELECT a.Price, b.CategoryId, b.Category, b.NameId, b.`Name`, b.SupplierId, b.Supplier, b.Specification FROM `material_bill` a " +
                    "JOIN (SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a " +
                    "JOIN (SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM `material_supplier` a " +
                    "JOIN (SELECT a.*, b.Category FROM `material_name` a " +
                    "JOIN `material_category` b ON a.CategoryId = b.Id) b ON a.NameId = b.Id) b ON a.SupplierId = b.Id) b ON a.SpecificationId = b.Id " +
                    "WHERE a.`MarkedDelete` = 0;";
            }
            var data = ServerConfig.ApiDb.Query<dynamic>(sql,
                new { categoryId, nameId, supplierId, specificationId });
            result.datas.AddRange(data);
            return result;
        }
    }
}