/*
 * Файл: UserDTO.cs
 * Призначення: Об'єкт передачі даних користувача системи. Містить облікові дані,
 * баланс рахунку, ідентифікатор ролі, хеш пароля та властивості для визначення прав доступу.
 */

using System;
using CompanyDataTransferObject.Constants;
using CompanyDataTransferObject.Enums;

namespace CompanyDataTransferObject
{
    public class UserDTO : BaseDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateOnly RegDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
        public bool IsActive { get; set; } = true;
        public Guid RoleId { get; set; }
        public decimal Balance { get; set; } = 1000.00m;

        public string? PasswordHash { get; set; }

        public DateTime RowInsertTime { get; set; } = DateTime.UtcNow;
        public DateTime? RowUpdateTime { get; set; }

        public UserRoleType RoleType => AppRoles.GetRoleType(RoleId);
        public string RoleName => AppRoles.GetDisplayName(RoleType);
    }
}
