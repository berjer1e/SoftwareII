/*
 * Файл: IAuctionService.cs
 * Призначення: Контракт бізнес-логіки аукціонів: перегляд, створення лотів, ставки, викуп і керування балансами.
 */

using System;
using System.Collections.Generic;
using CompanyDataTransferObject;
using CompanyDataTransferObject.Roles;

namespace CompanyServiceLayer;

public interface IAuctionService
{
    /*
     * Метод: GetAllAuctions
     * Призначення: Отримує повний список аукціонів з бази даних.
     */
    List<AuctionDTO> GetAllAuctions();

    /*
     * Метод: GetAllUsers
     * Призначення: Повертає список усіх користувачів для керування балансами та вибору покупця.
     */
    List<UserDTO> GetAllUsers();

    /*
     * Метод: AddAuction (generic)
     * Призначення: Додає новий аукціон. Виклик дозволений виключно з типом ролі, що реалізує ICanManageAuctions.
     * Параметри типу:
     *   TRole - тип ролі, обмежений інтерфейсом здатності ICanManageAuctions
     * Параметри:
     *   auction - дані нового лоту
     * Повертає: true, якщо аукціон успішно створено.
     */
    bool AddAuction<TRole>(AuctionDTO auction) where TRole : ICanManageAuctions;

    /*
     * Метод: AddAuction
     * Призначення: Додає новий аукціон із перевіркою прав поточного користувача сесії в рантаймі.
     * Параметри:
     *   auction - дані нового лоту
     * Повертає: true, якщо поточний користувач має права менеджера та лот збережено.
     */
    bool AddAuction(AuctionDTO auction);

    /*
     * Метод: UpdateBalance
     * Призначення: Оновлює грошовий баланс вказаного користувача (доступно менеджеру).
     * Параметри:
     *   userId - ідентифікатор користувача
     *   newBalance - нове значення балансу
     * Повертає: true, якщо баланс успішно оновлено.
     */
    bool UpdateBalance(Guid userId, decimal newBalance);

    /*
     * Метод: BuyoutAuction
     * Призначення: Викупляє лот: списує суму з покупця, нараховує продавцю за мінусом комісії, а комісію — менеджеру.
     * Параметри:
     *   auctionId - ідентифікатор аукціону
     *   buyerId - ідентифікатор покупця
     *   commissionPercent - частка комісії (за замовчуванням 10%)
     * Повертає: true, якщо викуп успішно проведено.
     */
    bool BuyoutAuction(Guid auctionId, Guid buyerId, decimal commissionPercent = 0.10m);

    /*
     * Метод: PlaceBid
     * Призначення: Приймає ставку із захистом від конфлікту інтересів та shill bidding.
     * Параметри:
     *   auctionId - ідентифікатор лоту
     *   bidAmount - сума нової ставки
     * Повертає: true, якщо ставка успішно зафіксована.
     */
    bool PlaceBid(Guid auctionId, decimal bidAmount);
}
