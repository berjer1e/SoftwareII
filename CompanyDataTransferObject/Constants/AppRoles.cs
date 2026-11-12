/*
 * Файл: AppRoles.cs
 * Призначення: Довідник ідентифікаторів ролей та методів приведення між Guid бази даних та enum UserRoleType.
 */

using System;
using CompanyDataTransferObject.Enums;

namespace CompanyDataTransferObject.Constants;

public static class AppRoles
{
    /*
     * Константа: AdminRoleId
     * Призначення: Guid ролі адміністратора системи.
     */
    public static readonly Guid AdminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /*
     * Константа: AuctionManagerRoleId
     * Призначення: Guid ролі менеджера аукціону (Варіант 21).
     */
    public static readonly Guid AuctionManagerRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    /*
     * Константа: BuyerRoleId
     * Призначення: Guid ролі покупця (учасника торгів).
     */
    public static readonly Guid BuyerRoleId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    /*
     * Константа: SellerRoleId
     * Призначення: Guid ролі продавця товарів.
     */
    public static readonly Guid SellerRoleId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    /*
     * Метод: GetRoleType
     * Призначення: Перетворює Guid ролі з бази даних у строго типізоване значення enum UserRoleType.
     * Параметри:
     *   roleId - ідентифікатор ролі з бази даних
     * Повертає: Відповідний елемент UserRoleType (або Guest, якщо роль невідома).
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
     * Метод: GetRoleId
     * Призначення: Повертає відповідний Guid для запису в базу даних на основі enum UserRoleType.
     * Параметри:
     *   roleType - тип ролі з переліку
     * Повертає: Guid ідентифікатор ролі.
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
     * Метод: GetDisplayName
     * Призначення: Повертає зрозумілу користувачу назву ролі для відображення в інтерфейсі.
     * Параметри:
     *   roleType - тип ролі
     * Повертає: Текстова назва ролі українською мовою.
     */
    public static string GetDisplayName(UserRoleType roleType) => roleType switch
    {
        UserRoleType.Admin => "Адміністратор",
        UserRoleType.AuctionManager => "Менеджер аукціону",
        UserRoleType.Buyer => "Покупець",
        UserRoleType.Seller => "Продавець",
        _ => "Гість"
    };
}
