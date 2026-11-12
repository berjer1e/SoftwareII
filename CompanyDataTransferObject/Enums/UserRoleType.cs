/*
 * Файл: UserRoleType.cs
 * Призначення: Перелік типів ролей користувачів для чіткого розмежування прав доступу в системі.
 */

namespace CompanyDataTransferObject.Enums;

/*
 * Перелік: UserRoleType
 * Призначення: Визначає рівні доступу користувачів (Гість, Покупець, Продавець, Менеджер, Адмін).
 */
public enum UserRoleType
{
    Guest = 0,
    Buyer = 1,
    Seller = 2,
    AuctionManager = 3,
    Admin = 4
}
