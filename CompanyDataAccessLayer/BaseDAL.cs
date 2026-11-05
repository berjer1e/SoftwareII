/*
 * Файл: BaseDAL.cs
 * Призначення: Абстрактний базовий клас доступу до даних на основі Entity Framework Core.
 * Реалізує типові CRUD-операції, підтримує роботу як через рядок підключення SQL Server,
 * так і через DbContextOptions (для модульного тестування в пам'яті), а також централізоване логування помилок.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using CompanyDataAccessLayer.Entities;
using CompanyDataAccessLayer.Logging;
using Microsoft.EntityFrameworkCore;
using DTO = CompanyDataTransferObject;

namespace CompanyDataAccessLayer
{
    public abstract class BaseDAL<TEntity, TDTO> : IBaseDAL<TDTO>
        where TEntity : class
        where TDTO : DTO.BaseDTO
    {
        private readonly string? _connectionString;
        private readonly DbContextOptions<SoftwrDbContext>? _options;
        protected readonly IAppLogger Logger;

        /*
         * Ініціалізує екземпляр DAL із рядком підключення до MS SQL Server та необов'язковим логером.
         */
        protected BaseDAL(string connectionString, IAppLogger? logger = null)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            Logger = logger ?? FileAppLogger.Instance;
        }

        /*
         * Ініціалізує екземпляр DAL із готовими параметрами DbContextOptions (зокрема для InMemory-тестів) та логером.
         */
        protected BaseDAL(DbContextOptions<SoftwrDbContext> options, IAppLogger? logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            Logger = logger ?? FileAppLogger.Instance;
        }

        /*
         * Створює та повертає новий екземпляр контексту бази даних SoftwrDbContext для виконання поточного запиту.
         */
        protected SoftwrDbContext CreateContext()
        {
            if (_options != null)
            {
                return new SoftwrDbContext(_options);
            }

            return new SoftwrDbContext(new DbContextOptionsBuilder<SoftwrDbContext>()
                .UseSqlServer(_connectionString!)
                .Options);
        }

        /*
         * Перетворює сутність бази даних Entity Framework у відповідний об'єкт передачі даних (DTO).
         */
        protected abstract TDTO ToDto(TEntity entity);

        /*
         * Перетворює об'єкт передачі даних (DTO) у сутність Entity Framework для збереження в базі даних.
         */
        protected abstract TEntity ToEntity(TDTO dto);

        /*
         * Додає новий об'єкт DTO до бази даних, попередньо генеруючи GUID за потреби, і перехоплює помилки збереження.
         */
        public bool Add(TDTO item)
        {
            try
            {
                if (item.Id == Guid.Empty) item.Id = Guid.NewGuid();
                using var db = CreateContext();
                var entity = ToEntity(item);
                db.Add(entity);
                db.SaveChanges();
                return true;
            }
            catch (DbUpdateException ex)
            {
                Logger.LogError(ex, $"Database constraint error while adding {typeof(TEntity).Name} (Id={item.Id})");
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Unexpected error while adding {typeof(TEntity).Name} (Id={item.Id})");
                return false;
            }
        }

        /*
         * Шукає сутність за первинним ключем та видаляє її з бази даних. Повертає false, якщо сутність не знайдено або виникла помилка.
         */
        public bool Delete(Guid id)
        {
            try
            {
                using var db = CreateContext();
                var entity = db.Set<TEntity>().Find(id);
                if (entity == null)
                {
                    return false;
                }
                db.Remove(entity);
                db.SaveChanges();
                return true;
            }
            catch (DbUpdateException ex)
            {
                Logger.LogError(ex, $"Database constraint error while deleting {typeof(TEntity).Name} (Id={id})");
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Unexpected error while deleting {typeof(TEntity).Name} (Id={id})");
                return false;
            }
        }

        /*
         * Отримує всі записи таблиці з бази даних і перетворює їх у список об'єктів DTO.
         */
        public virtual List<TDTO> GetAll()
        {
            try
            {
                using var db = CreateContext();
                var entities = db.Set<TEntity>().ToList();
                return entities.Select(ToDto).ToList();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error retrieving all {typeof(TEntity).Name} records");
                return new List<TDTO>();
            }
        }

        /*
         * Отримує один запис за його унікальним ідентифікатором і конвертує його в DTO.
         */
        public virtual TDTO? GetById(Guid id)
        {
            try
            {
                using var db = CreateContext();
                var entity = db.Set<TEntity>().Find(id);
                return entity != null ? ToDto(entity) : null;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error retrieving {typeof(TEntity).Name} by ID ({id})");
                return null;
            }
        }

        /*
         * Оновлює дані існуючого запису в базі даних на основі переданого об'єкта DTO.
         */
        public bool Update(TDTO item)
        {
            try
            {
                using var db = CreateContext();
                var entity = ToEntity(item);
                db.Update(entity);
                db.SaveChanges();
                return true;
            }
            catch (DbUpdateException ex)
            {
                Logger.LogError(ex, $"Database constraint error while updating {typeof(TEntity).Name} (Id={item.Id})");
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Unexpected error while updating {typeof(TEntity).Name} (Id={item.Id})");
                return false;
            }
        }
    }
}
