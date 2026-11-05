/*
 * Файл: ProductDAL.cs
 * Призначення: Клас та інтерфейс рівня доступу до даних для сутності Product (товар).
 * Підтримує завантаження товарів разом із виробником та категорією, а також пошук і сортування.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using CompanyDataAccessLayer.Entities;
using CompanyDataAccessLayer.Logging;
using Microsoft.EntityFrameworkCore;
using DTO = CompanyDataTransferObject;

namespace CompanyDataAccessLayer.DAL
{
    public interface IProductDAL : IBaseDAL<DTO.ProductDTO>
    {
        /*
         * Виконує пошук товарів за назвою та сортування результатів за вибраним полем.
         */
        List<DTO.ProductDTO> SearchAndSort(string? searchTerm, string? sortBy, bool ascending = true);
    }

    public class ProductDAL : BaseDAL<Product, DTO.ProductDTO>, IProductDAL
    {
        /*
         * Ініціалізує ProductDAL із рядком підключення до MS SQL Server.
         */
        public ProductDAL(string connectionString, IAppLogger? logger = null) : base(connectionString, logger) { }

        /*
         * Ініціалізує ProductDAL із параметрами DbContextOptions.
         */
        public ProductDAL(DbContextOptions<SoftwrDbContext> options, IAppLogger? logger = null) : base(options, logger) { }

        /*
         * Повертає список усіх товарів разом із назвами їхніх виробників і типів.
         */
        public override List<DTO.ProductDTO> GetAll()
        {
            return SearchAndSort(searchTerm: null, sortBy: null, ascending: true);
        }

        /*
         * Знаходить і повертає товар за його унікальним ідентифікатором разом із виробником та категорією.
         */
        public override DTO.ProductDTO? GetById(Guid id)
        {
            try
            {
                using var db = CreateContext();
                var entity = db.Products
                    .Include(p => p.Manufacturer)
                    .Include(p => p.ProductType)
                    .FirstOrDefault(p => p.ProductId == id);

                return entity != null ? ToDto(entity) : null;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error retrieving product by ID {id}");
                return null;
            }
        }

        /*
         * Фільтрує товари за підрядком назви та впорядковує їх за назвою, ціною або кількістю.
         */
        public List<DTO.ProductDTO> SearchAndSort(string? searchTerm, string? sortBy, bool ascending = true)
        {
            try
            {
                using var db = CreateContext();
                IQueryable<Product> queryable = db.Products
                    .Include(p => p.Manufacturer)
                    .Include(p => p.ProductType);

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var trimmed = searchTerm.Trim();
                    queryable = queryable.Where(p => p.ProductName != null && p.ProductName.Contains(trimmed));
                }

                queryable = (sortBy?.ToLowerInvariant(), ascending) switch
                {
                    ("name" or "productname", true) => queryable.OrderBy(p => p.ProductName),
                    ("name" or "productname", false) => queryable.OrderByDescending(p => p.ProductName),

                    ("price" or "baseprice", true) => queryable.OrderBy(p => p.BasePrice),
                    ("price" or "baseprice", false) => queryable.OrderByDescending(p => p.BasePrice),

                    ("quantity", true) => queryable.OrderBy(p => p.Quantity),
                    ("quantity", false) => queryable.OrderByDescending(p => p.Quantity),

                    (_, true) => queryable.OrderBy(p => p.ProductName),
                    (_, false) => queryable.OrderByDescending(p => p.ProductName)
                };

                return queryable.ToList().Select(ToDto).ToList();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error querying products");
                return new List<DTO.ProductDTO>();
            }
        }

        /*
         * Перетворює сутність Product на об'єкт передачі даних ProductDTO.
         */
        protected override DTO.ProductDTO ToDto(Product entity) => new()
        {
            Id = entity.ProductId,
            Name = entity.ProductName ?? string.Empty,
            Price = entity.BasePrice ?? decimal.Zero,
            ProductTypeId = entity.ProductTypeId,
            Quantity = entity.Quantity,
            ManufacturerId = entity.ManufacturerId,
            ManufacturerName = entity.Manufacturer?.Name ?? string.Empty,
            ProductTypeName = entity.ProductType?.TypeName ?? string.Empty
        };

        /*
         * Перетворює об'єкт передачі даних ProductDTO на сутність бази даних Product.
         */
        protected override Product ToEntity(DTO.ProductDTO dto) => new()
        {
            ProductId = dto.Id,
            ProductName = dto.Name,
            ProductTypeId = dto.ProductTypeId,
            Quantity = dto.Quantity,
            BasePrice = dto.Price,
            ManufacturerId = dto.ManufacturerId
        };
    }
}
