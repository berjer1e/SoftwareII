/*
 * Файл: UserDAL.cs
 * Призначення: Клас та інтерфейс рівня доступу до даних для сутності User (користувач).
 * Забезпечує пошук користувачів за електронною поштою або іменем для автентифікації та оновлення балансу рахунку.
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
    public interface IUserDAL : IBaseDAL<UserDTO>
    {
        /*
         * Знаходить користувача за адресою електронної пошти без урахування регістру символів.
         */
        UserDTO? GetByEmail(string email);

        /*
         * Знаходить користувача за його іменем (логіном) без урахування регістру символів.
         */
        UserDTO? GetByName(string name);

        /*
         * Оновлює поточний грошовий баланс вказаного користувача в базі даних.
         */
        bool UpdateBalance(Guid userId, decimal newBalance);
    }

    public class UserDAL : BaseDAL<Ent.User, UserDTO>, IUserDAL
    {
        /*
         * Ініціалізує UserDAL із рядком підключення до MS SQL Server.
         */
        public UserDAL(string connectionString, IAppLogger? logger = null) : base(connectionString, logger) { }

        /*
         * Ініціалізує UserDAL із параметрами DbContextOptions.
         */
        public UserDAL(DbContextOptions<Ent.SoftwrDbContext> options, IAppLogger? logger = null) : base(options, logger) { }

        /*
         * Повертає список усіх зареєстрованих користувачів системи.
         */
        public override List<UserDTO> GetAll()
        {
            try
            {
                using var db = CreateContext();
                return db.Users
                    .ToList()
                    .Select(ToDto)
                    .ToList();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error retrieving users");
                return new List<UserDTO>();
            }
        }

        /*
         * Знаходить і повертає користувача за його унікальним ідентифікатором.
         */
        public override UserDTO? GetById(Guid id)
        {
            try
            {
                using var db = CreateContext();
                var entity = db.Users
                    .FirstOrDefault(u => u.UserId == id);

                return entity != null ? ToDto(entity) : null;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error retrieving user by ID {id}");
                return null;
            }
        }

        /*
         * Шукає користувача в базі даних за його електронною поштою, очищеною від зайвих пробілів.
         */
        public UserDTO? GetByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;

            try
            {
                using var db = CreateContext();
                var trimmed = email.Trim().ToLower();
                var entity = db.Users
                    .FirstOrDefault(u => u.ContactEmail != null && u.ContactEmail.Trim().ToLower() == trimmed);

                return entity != null ? ToDto(entity) : null;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error finding user by email {email}");
                return null;
            }
        }

        /*
         * Шукає користувача в базі даних за його іменем (UserName), очищеним від пробілів.
         */
        public UserDTO? GetByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            try
            {
                using var db = CreateContext();
                var trimmed = name.Trim().ToLower();
                var entity = db.Users
                    .FirstOrDefault(u => u.UserName != null && u.UserName.Trim().ToLower() == trimmed);

                return entity != null ? ToDto(entity) : null;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error finding user by name {name}");
                return null;
            }
        }

        /*
         * Змінює значення поля Balance для користувача за його ідентифікатором і фіксує час оновлення запису.
         */
        public bool UpdateBalance(Guid userId, decimal newBalance)
        {
            try
            {
                using var db = CreateContext();
                var entity = db.Users.Find(userId);
                if (entity == null) return false;

                entity.Balance = newBalance;
                entity.RowUpdateTime = DateTime.UtcNow;
                db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error updating balance for user {userId}");
                return false;
            }
        }

        /*
         * Перетворює сутність User на об'єкт передачі даних UserDTO.
         */
        protected override UserDTO ToDto(Ent.User e) => new()
        {
            Id = e.UserId,
            Name = e.UserName ?? string.Empty,
            Email = e.ContactEmail ?? string.Empty,
            RegDate = e.RegDate,
            IsActive = e.IsActive,
            RoleId = e.UserRole ?? Guid.Empty,
            Balance = e.Balance,
            PasswordHash = e.PasswordHash,
            RowInsertTime = e.RowInsertTime,
            RowUpdateTime = e.RowUpdateTime
        };

        /*
         * Перетворює об'єкт передачі даних UserDTO на сутність бази даних User.
         */
        protected override Ent.User ToEntity(UserDTO d) => new()
        {
            UserId = d.Id,
            UserName = d.Name,
            ContactEmail = d.Email,
            RegDate = d.RegDate,
            IsActive = d.IsActive,
            UserRole = d.RoleId == Guid.Empty ? null : d.RoleId,
            Balance = d.Balance,
            PasswordHash = d.PasswordHash,
            RowInsertTime = d.RowInsertTime == default ? DateTime.UtcNow : d.RowInsertTime,
            RowUpdateTime = d.RowUpdateTime ?? (d.RowInsertTime == default ? DateTime.UtcNow : d.RowInsertTime)
        };
    }
}
