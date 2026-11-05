/*
 * Файл: ManufacturerDAL.cs
 * Призначення: Клас та інтерфейс рівня доступу до даних для довідника виробників товарів (Manufacturer).
 */

using CompanyDataAccessLayer.Logging;
using CompanyDataTransferObject;
using Microsoft.EntityFrameworkCore;
using Ent = CompanyDataAccessLayer.Entities;

namespace CompanyDataAccessLayer.DAL
{
    public interface IManufacturerDAL : IBaseDAL<ManufacturerDTO> { }

    public class ManufacturerDAL : BaseDAL<Ent.Manufacturer, ManufacturerDTO>, IManufacturerDAL
    {
        /*
         * Ініціалізує ManufacturerDAL із рядком підключення до MS SQL Server.
         */
        public ManufacturerDAL(string connectionString, IAppLogger? logger = null) : base(connectionString, logger) { }

        /*
         * Ініціалізує ManufacturerDAL із параметрами DbContextOptions.
         */
        public ManufacturerDAL(DbContextOptions<Ent.SoftwrDbContext> options, IAppLogger? logger = null) : base(options, logger) { }

        /*
         * Перетворює сутність Manufacturer на об'єкт передачі даних ManufacturerDTO.
         */
        protected override ManufacturerDTO ToDto(Ent.Manufacturer e) => new()
        {
            Id = e.ManufacturerId,
            Name = e.Name ?? string.Empty,
            Country = e.Country
        };

        /*
         * Перетворює об'єкт передачі даних ManufacturerDTO на сутність бази даних Manufacturer.
         */
        protected override Ent.Manufacturer ToEntity(ManufacturerDTO d) => new()
        {
            ManufacturerId = d.Id,
            Name = d.Name,
            Country = d.Country
        };
    }
}
