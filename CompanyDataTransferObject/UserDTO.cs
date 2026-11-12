/*
 * Файл: UserDTO.cs
 * Призначення: Модель передачі даних користувача між шарами системи.
 */

using System;
using CompanyDataTransferObject.Constants;
using CompanyDataTransferObject.Enums;

namespace CompanyDataTransferObject;

public class UserDTO : BaseDTO
{
    /*
     * Властивість: Name
     * Призначення: Логін або повне ім'я користувача.
     */
    public string Name { get; set; } = string.Empty;

    /*
     * Властивість: Email
     * Призначення: Контактна електронна адреса користувача.
     */
    public string Email { get; set; } = string.Empty;

    /*
     * Властивість: RegDate
     * Призначення: Дата реєстрації користувача в системі.
     */
    public DateOnly RegDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);

    /*
     * Властивість: IsActive
     * Призначення: Статус активності облікового запису.
     */
    public bool IsActive { get; set; } = true;

    /*
     * Властивість: RoleId
     * Призначення: Guid-ідентифікатор ролі з таблиці Roles у базі даних.
     */
    public Guid RoleId { get; set; }

    /*
     * Властивість: RoleType
     * Призначення: Строго типізоване значення переліку UserRoleType, обчислене на основі RoleId.
     */
    public UserRoleType RoleType => AppRoles.GetRoleType(RoleId);

    /*
     * Властивість: IsManager
     * Призначення: Показує, чи має користувач повноваження Менеджера аукціону або Адміністратора.
     */
    public bool IsManager => RoleType == UserRoleType.AuctionManager || RoleType == UserRoleType.Admin;

    /*
     * Властивість: IsBuyer
     * Призначення: Показує, чи є користувач звичайним покупцем/учасником торгів.
     */
    public bool IsBuyer => RoleType == UserRoleType.Buyer;

    /*
     * Властивість: PasswordHash
     * Призначення: Збережений хеш пароля користувача.
     */
    public string? PasswordHash { get; set; }

    /*
     * Властивість: Balance
     * Призначення: Грошовий баланс користувача.
     */
    public decimal Balance { get; set; } = 0.00m;
}
