/*
 * Файл: AppRoles.cs
 * Призначення: Статичний довідник ідентифікаторів ролей користувачів та допоміжних функцій
 * для перетворення між GUID-ідентифікаторами бази даних, переліком UserRoleType і текстовими назвами.
 */

using System;
using CompanyDataTransferObject.Enums;

namespace CompanyDataTransferObject.Constants;

public static class AppRoles
{
    public static readonly Guid AdminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid AuctionManagerRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid BuyerRoleId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid SellerRoleId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    /*
     * Визначає тип ролі користувача (UserRoleType) за переданим GUID-ідентифікатором ролі з бази даних.
     * Якщо ідентифікатор не співпадає з жодною відомою роллю або є порожнім, повертає роль Guest.
     */
    public static UserRoleType GetRoleType(Guid? roleId)
    {
        if (roleId == AdminRoleId) return UserRoleType.Admin;
        if (roleId == AuctionManagerRoleId) return UserRoleType.AuctionManager;
        if (roleId == BuyerRoleId) return UserRoleType.Buyer;
        if (roleId == SellerRoleId) return UserRoleType.Seller;
        return UserRoleType.Guest;
    }

    /*
     * Повертає фіксований GUID-ідентифікатор ролі для збереження в базі даних відповідно до значення UserRoleType.
     */
    public static Guid GetRoleId(UserRoleType roleType) => roleType switch
    {
        UserRoleType.Admin => AdminRoleId,
        UserRoleType.AuctionManager => AuctionManagerRoleId,
        UserRoleType.Buyer => BuyerRoleId,
        UserRoleType.Seller => SellerRoleId,
        _ => Guid.Empty
    };

    /*
     * Повертає локалізовану текстову назву ролі користувача для відображення в інтерфейсі термінала.
     */
    public static string GetDisplayName(UserRoleType roleType) => roleType switch
    {
        UserRoleType.Admin => "Адміністратор (Admin)",
        UserRoleType.AuctionManager => "Менеджер аукціонів (Manager)",
        UserRoleType.Buyer => "Покупець (Buyer)",
        UserRoleType.Seller => "Продавець (Seller)",
        _ => "Гість (Guest)"
    };
}
