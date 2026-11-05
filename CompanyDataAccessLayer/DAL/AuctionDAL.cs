/*
 * Файл: AuctionDAL.cs
 * Призначення: Клас та інтерфейс рівня доступу до даних для сутності Auction (аукціон).
 * Забезпечує завантаження аукціонів разом із пов'язаними товарами, менеджерами, власниками й ставками,
 * а також підтримує деактивацію, верифікацію лотів, пошук і сортування.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using CompanyDataAccessLayer.Logging;
using CompanyDataTransferObject;
using Microsoft.EntityFrameworkCore;
using Ent = CompanyDataAccessLayer.Entities;

namespace CompanyDataAccessLayer.DAL
{
    public interface IAuctionDAL : IBaseDAL<AuctionDTO>
    {
        /*
         * Переводить аукціон у неактивний стан без фізичного видалення запису з бази даних.
         */
        bool Deactivate(Guid id);

        /*
         * Підтверджує запропонований користувачем лот і робить аукціон активним для торгів.
         */
        bool VerifyAuction(Guid id);

        /*
         * Повертає список усіх активних та підтверджених аукціонів, термін дії яких ще не минув.
         */
        List<AuctionDTO> GetActive();

        /*
         * Повертає список завершених або деактивованих аукціонів.
         */
        List<AuctionDTO> GetFinished();

        /*
         * Повертає список аукціонів, які очікують на перевірку та схвалення менеджером.
         */
        List<AuctionDTO> GetPendingVerification();

        /*
         * Виконує пошук аукціонів за назвою товару, фільтрацію за активністю та сортування за вибраним полем.
         */
        List<AuctionDTO> SearchAndSort(string? searchTerm, bool? isActive, string? sortBy, bool ascending = true);
    }

    public class AuctionDAL : BaseDAL<Ent.Auction, AuctionDTO>, IAuctionDAL
    {
        /*
         * Ініціалізує AuctionDAL із рядком підключення до бази даних MS SQL Server.
         */
        public AuctionDAL(string connectionString, IAppLogger? logger = null) : base(connectionString, logger) { }

        /*
         * Ініціалізує AuctionDAL із параметрами DbContextOptions для роботи з тестовим або зовнішнім контекстом.
         */
        public AuctionDAL(DbContextOptions<Ent.SoftwrDbContext> options, IAppLogger? logger = null) : base(options, logger) { }

        /*
         * Встановлює прапорець IsActive = false (та фіксує розгляд менеджером IsVerified = true)
         * для вказаного аукціону й оновлює мітку часу зміни запису.
         */
        public bool Deactivate(Guid id)
        {
            try
            {
                using var db = CreateContext();
                var entity = db.Auctions.Find(id);
                if (entity == null) return false;

                entity.IsActive = false;
                entity.IsVerified = true;
                entity.RowUpdateTime = DateTime.UtcNow;
                db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error deactivating auction {id}");
                return false;
            }
        }

        /*
         * Встановлює прапорці IsVerified = true та IsActive = true для схвалення лота менеджером.
         */
        public bool VerifyAuction(Guid id)
        {
            try
            {
                using var db = CreateContext();
                var entity = db.Auctions.Find(id);
                if (entity == null) return false;

                entity.IsVerified = true;
                entity.IsActive = true;
                entity.RowUpdateTime = DateTime.UtcNow;
                db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error verifying auction {id}");
                return false;
            }
        }

        /*
         * Повертає перелік активних і верифікованих аукціонів за допомогою універсальної вибірки SearchAndSort.
         */
        public List<AuctionDTO> GetActive()
        {
            return SearchAndSort(searchTerm: null, isActive: true, sortBy: null, ascending: true);
        }

        /*
         * Повертає перелік неактивних або прострочених аукціонів.
         */
        public List<AuctionDTO> GetFinished()
        {
            return SearchAndSort(searchTerm: null, isActive: false, sortBy: null, ascending: true);
        }

        /*
         * Завантажує всі аукціони зі статусом IsVerified = false разом із навігаційними даними про товар і власника.
         */
        public List<AuctionDTO> GetPendingVerification()
        {
            try
            {
                using var db = CreateContext();
                return db.Auctions
                    .Include(a => a.Product).ThenInclude(p => p.Manufacturer)
                    .Include(a => a.Product).ThenInclude(p => p.ProductType)
                    .Include(a => a.AuctionManager)
                    .Include(a => a.ProductOwner)
                    .Include(a => a.LastBid!).ThenInclude(b => b.BidOwnerNavigation)
                    .Where(a => !a.IsVerified)
                    .OrderByDescending(a => a.RowInsertTime)
                    .ToList()
                    .Select(ToDto)
                    .ToList();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error querying pending verification auctions");
                return new List<AuctionDTO>();
            }
        }

        /*
         * Повертає всі аукціони в системі разом із даними про пов'язані товари, учасників та ставки.
         */
        public override List<AuctionDTO> GetAll()
        {
            return SearchAndSort(searchTerm: null, isActive: null, sortBy: null, ascending: true);
        }

        /*
         * Знаходить і повертає аукціон за його унікальним ідентифікатором із завантаженням усіх пов'язаних сутностей.
         */
        public override AuctionDTO? GetById(Guid id)
        {
            try
            {
                using var db = CreateContext();
                var entity = db.Auctions
                    .Include(a => a.Product).ThenInclude(p => p.Manufacturer)
                    .Include(a => a.Product).ThenInclude(p => p.ProductType)
                    .Include(a => a.AuctionManager)
                    .Include(a => a.ProductOwner)
                    .Include(a => a.LastBid!).ThenInclude(b => b.BidOwnerNavigation)
                    .FirstOrDefault(a => a.AuctionId == id);

                return entity != null ? ToDto(entity) : null;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error retrieving auction by ID {id}");
                return null;
            }
        }

        /*
         * Формує LINQ-запит до таблиці аукціонів із фільтрацією за назвою товару та станом активності й сортуванням за вказаним критерієм.
         */
        public List<AuctionDTO> SearchAndSort(string? searchTerm, bool? isActive, string? sortBy, bool ascending = true)
        {
            try
            {
                using var db = CreateContext();
                IQueryable<Ent.Auction> queryable = db.Auctions
                    .Include(a => a.Product).ThenInclude(p => p.Manufacturer)
                    .Include(a => a.Product).ThenInclude(p => p.ProductType)
                    .Include(a => a.AuctionManager)
                    .Include(a => a.ProductOwner)
                    .Include(a => a.LastBid!).ThenInclude(b => b.BidOwnerNavigation);

                if (isActive.HasValue)
                {
                    if (isActive.Value)
                    {
                        queryable = queryable.Where(a => a.IsActive && a.IsVerified && a.EndDate >= DateTime.Now);
                    }
                    else
                    {
                        queryable = queryable.Where(a => a.IsVerified && (!a.IsActive || a.EndDate < DateTime.Now));
                    }
                }

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var trimmed = searchTerm.Trim();
                    queryable = queryable.Where(a => a.Product.ProductName != null && a.Product.ProductName.Contains(trimmed));
                }

                queryable = (sortBy?.ToLowerInvariant(), ascending) switch
                {
                    ("productname" or "product" or "name", true) => queryable.OrderBy(a => a.Product.ProductName),
                    ("productname" or "product" or "name", false) => queryable.OrderByDescending(a => a.Product.ProductName),

                    ("startdate", true) => queryable.OrderBy(a => a.StartDate),
                    ("startdate", false) => queryable.OrderByDescending(a => a.StartDate),

                    ("enddate" or "date", true) => queryable.OrderBy(a => a.EndDate),
                    ("enddate" or "date", false) => queryable.OrderByDescending(a => a.EndDate),

                    ("startprice" or "price", true) => queryable.OrderBy(a => a.StartPrice),
                    ("startprice" or "price", false) => queryable.OrderByDescending(a => a.StartPrice),

                    ("endprice" or "finalprice" or "buyoutprice", true) => queryable.OrderBy(a => a.FinalPrice),
                    ("endprice" or "finalprice" or "buyoutprice", false) => queryable.OrderByDescending(a => a.FinalPrice),

                    ("isactive" or "status", true) => queryable.OrderBy(a => a.IsActive),
                    ("isactive" or "status", false) => queryable.OrderByDescending(a => a.IsActive),

                    (_, true) => queryable.OrderBy(a => a.StartDate),
                    (_, false) => queryable.OrderByDescending(a => a.StartDate)
                };

                return queryable.ToList().Select(ToDto).ToList();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error querying auctions");
                return new List<AuctionDTO>();
            }
        }

        /*
         * Відображає сутність бази даних Auction та її навігаційні властивості у транспортний об'єкт AuctionDTO.
         */
        protected override AuctionDTO ToDto(Ent.Auction e) => new()
        {
            Id = e.AuctionId,
            ProductId = e.ProductId,
            AuctionManagerId = e.AuctionManagerId,
            ProductOwnerId = e.ProductOwnerId,
            StartDate = e.StartDate,
            EndDate = e.EndDate,
            StartPrice = e.StartPrice,
            EndPrice = e.FinalPrice,
            MinBid = e.MinBid,
            BuyoutPrice = e.BuyoutPrice,
            IsVerified = e.IsVerified,
            IsActive = e.IsActive,
            LastBidId = e.LastBidId,
            RowInsertTime = e.RowInsertTime,
            RowUpdateTime = e.RowUpdateTime,
            ProductName = e.Product?.ProductName ?? string.Empty,
            ProductTypeName = e.Product?.ProductType?.TypeName ?? string.Empty,
            ManufacturerName = e.Product?.Manufacturer?.Name ?? string.Empty,
            ManagerName = e.AuctionManager?.UserName ?? string.Empty,
            AuctionManagerName = e.AuctionManager?.UserName ?? string.Empty,
            ManagerEmail = e.AuctionManager?.ContactEmail ?? string.Empty,
            ProductOwnerName = e.ProductOwner?.UserName ?? string.Empty,
            LastBidderName = e.LastBid?.BidOwnerNavigation?.UserName ?? string.Empty,
            ProductBasePrice = e.Product?.BasePrice
        };

        /*
         * Створює сутність бази даних Auction на основі полів об'єкта передачі даних AuctionDTO.
         */
        protected override Ent.Auction ToEntity(AuctionDTO d) => new()
        {
            AuctionId = d.Id,
            ProductId = d.ProductId,
            AuctionManagerId = d.AuctionManagerId,
            ProductOwnerId = d.ProductOwnerId,
            StartDate = d.StartDate,
            EndDate = d.EndDate,
            StartPrice = d.StartPrice,
            FinalPrice = d.EndPrice,
            MinBid = d.MinBid,
            BuyoutPrice = d.BuyoutPrice,
            IsVerified = d.IsVerified,
            IsActive = d.IsActive,
            LastBidId = d.LastBidId,
            RowInsertTime = d.RowInsertTime == default ? DateTime.UtcNow : d.RowInsertTime,
            RowUpdateTime = d.RowUpdateTime ?? (d.RowInsertTime == default ? DateTime.UtcNow : d.RowInsertTime)
        };
    }
}
