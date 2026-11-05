/*
 * Файл: UserRoleType.cs
 * Призначення: Перелік доступних ролей користувачів у системі керування аукціонами
 * для розмежування прав доступу між гостями, покупцями, продавцями, менеджерами та адміністраторами.
 */

namespace CompanyDataTransferObject.Enums;

public enum UserRoleType
{
    Guest = 0,
    Buyer = 1,
    Seller = 2,
    AuctionManager = 3,
    Admin = 4
}
