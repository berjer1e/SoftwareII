/*
 * Файл: ProductTypeDAL.cs
 * Призначення: Клас та інтерфейс рівня доступу до даних для довідника категорій (типів) товарів (ProductType).
 */

using CompanyDataAccessLayer.Logging;
using CompanyDataTransferObject;
using Microsoft.EntityFrameworkCore;
using Ent = CompanyDataAccessLayer.Entities;

namespace CompanyDataAccessLayer.DAL
{
    public interface IProductTypeDAL : IBaseDAL<ProductTypeDTO> { }

    public class ProductTypeDAL : BaseDAL<Ent.ProductType, ProductTypeDTO>, IProductTypeDAL
    {
        /*
         * Ініціалізує ProductTypeDAL із рядком підключення до MS SQL Server.
         */
        public ProductTypeDAL(string connectionString, IAppLogger? logger = null) : base(connectionString, logger) { }

        /*
         * Ініціалізує ProductTypeDAL із параметрами DbContextOptions.
         */
        public ProductTypeDAL(DbContextOptions<Ent.SoftwrDbContext> options, IAppLogger? logger = null) : base(options, logger) { }

        /*
         * Перетворює сутність ProductType на об'єкт передачі даних ProductTypeDTO.
         */
        protected override ProductTypeDTO ToDto(Ent.ProductType e) => new()
        {
            Id = e.TypeId,
            Name = e.TypeName ?? string.Empty
        };

        /*
         * Перетворює об'єкт передачі даних ProductTypeDTO на сутність бази даних ProductType.
         */
        protected override Ent.ProductType ToEntity(ProductTypeDTO d) => new()
        {
            TypeId = d.Id,
            TypeName = d.Name
        };
    }
}
