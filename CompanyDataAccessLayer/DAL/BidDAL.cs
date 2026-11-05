/*
 * Файл: BidDAL.cs
 * Призначення: Клас та інтерфейс рівня доступу до даних для сутності Bid (ставка на аукціоні).
 * Дозволяє зберігати ставки, отримувати історію ставок за аукціонами та користувачами, а також знаходити найвищу ставку.
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
    public interface IBidDAL : IBaseDAL<BidDTO>
    {
        /*
         * Повертає список усіх ставок для заданого аукціону, відсортований за спаданням суми та часу.
         */
        List<BidDTO> GetByAuctionId(Guid auctionId);

        /*
         * Повертає список усіх ставок, зроблених конкретним користувачем.
         */
        List<BidDTO> GetByUserId(Guid userId);

        /*
         * Повертає найвищу за сумою ставку для вказаного аукціону або null, якщо ставок ще немає.
         */
        BidDTO? GetHighestBid(Guid auctionId);
    }

    public class BidDAL : BaseDAL<Ent.Bid, BidDTO>, IBidDAL
    {
        /*
         * Ініціалізує BidDAL із рядком підключення до бази даних MS SQL Server.
         */
        public BidDAL(string connectionString, IAppLogger? logger = null) : base(connectionString, logger) { }

        /*
         * Ініціалізує BidDAL із параметрами DbContextOptions для роботи з тестовим або зовнішнім контекстом.
         */
        public BidDAL(DbContextOptions<Ent.SoftwrDbContext> options, IAppLogger? logger = null) : base(options, logger) { }

        /*
         * Отримує з бази даних усі ставки за ідентифікатором аукціону разом з інформацією про учасників.
         */
        public List<BidDTO> GetByAuctionId(Guid auctionId)
        {
            try
            {
                using var db = CreateContext();
                return db.Bids
                    .Include(b => b.BidOwnerNavigation)
                    .Where(b => b.AuctionId == auctionId)
                    .OrderByDescending(b => b.Price)
                    .ThenByDescending(b => b.TimeStamp)
                    .ToList()
                    .Select(ToDto)
                    .ToList();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error retrieving bids for auction {auctionId}");
                return new List<BidDTO>();
            }
        }

        /*
         * Отримує з бази даних усі ставки, подані вказаним користувачем, у хронологічному порядку.
         */
        public List<BidDTO> GetByUserId(Guid userId)
        {
            try
            {
                using var db = CreateContext();
                return db.Bids
                    .Include(b => b.BidOwnerNavigation)
                    .Where(b => b.BidOwner == userId)
                    .OrderByDescending(b => b.TimeStamp)
                    .ToList()
                    .Select(ToDto)
                    .ToList();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error retrieving bids for user {userId}");
                return new List<BidDTO>();
            }
        }

        /*
         * Визначає та повертає ставку з максимальною ціною для заданого аукціону.
         */
        public BidDTO? GetHighestBid(Guid auctionId)
        {
            try
            {
                using var db = CreateContext();
                var entity = db.Bids
                    .Include(b => b.BidOwnerNavigation)
                    .Where(b => b.AuctionId == auctionId)
                    .OrderByDescending(b => b.Price)
                    .FirstOrDefault();

                return entity != null ? ToDto(entity) : null;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error retrieving highest bid for auction {auctionId}");
                return null;
            }
        }

        /*
         * Повертає всі ставки в системі разом із даними про авторів ставок.
         */
        public override List<BidDTO> GetAll()
        {
            try
            {
                using var db = CreateContext();
                return db.Bids
                    .Include(b => b.BidOwnerNavigation)
                    .OrderByDescending(b => b.TimeStamp)
                    .ToList()
                    .Select(ToDto)
                    .ToList();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error retrieving all bids");
                return new List<BidDTO>();
            }
        }

        /*
         * Знаходить і повертає ставку за її унікальним ідентифікатором разом із даними про учасника.
         */
        public override BidDTO? GetById(Guid id)
        {
            try
            {
                using var db = CreateContext();
                var entity = db.Bids
                    .Include(b => b.BidOwnerNavigation)
                    .FirstOrDefault(b => b.BidId == id);

                return entity != null ? ToDto(entity) : null;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error retrieving bid by ID {id}");
                return null;
            }
        }

        /*
         * Перетворює сутність Bid на об'єкт передачі даних BidDTO.
         */
        protected override BidDTO ToDto(Ent.Bid e) => new()
        {
            Id = e.BidId,
            AuctionId = e.AuctionId,
            BidOwner = e.BidOwner,
            Price = e.Price,
            TimeStamp = e.TimeStamp,
            IsBuyout = e.IsBuyout,
            RowInsertTime = e.RowInsertTime,
            RowUpdateTime = e.RowUpdateTime,
            BidderName = e.BidOwnerNavigation?.UserName ?? string.Empty,
            BidderEmail = e.BidOwnerNavigation?.ContactEmail ?? string.Empty
        };

        /*
         * Перетворює об'єкт передачі даних BidDTO на сутність бази даних Bid.
         */
        protected override Ent.Bid ToEntity(BidDTO d) => new()
        {
            BidId = d.Id,
            AuctionId = d.AuctionId,
            BidOwner = d.BidOwner,
            Price = d.Price,
            TimeStamp = d.TimeStamp == default ? DateTime.UtcNow : d.TimeStamp,
            IsBuyout = d.IsBuyout,
            RowInsertTime = d.RowInsertTime == default ? DateTime.UtcNow : d.RowInsertTime,
            RowUpdateTime = d.RowUpdateTime ?? (d.RowInsertTime == default ? DateTime.UtcNow : d.RowInsertTime)
        };
    }
}
