/*
 * Файл: AuctionService.cs
 * Призначення: Реалізація бізнес-логіки аукціонів з перевіркою прав, керуванням балансами та викупом лотів.
 */

using System;
using System.Collections.Generic;
using CompanyDataAccessLayer.DAL;
using CompanyDataTransferObject;
using CompanyDataTransferObject.Roles;

namespace CompanyServiceLayer.Session;

public class AuctionService : IAuctionService
{
    private readonly IAuctionDAL _auctionDal;
    private readonly IUserSession _userSession;
    private readonly IUserDAL _userDal;

    /*
     * Конструктор: AuctionService
     * Призначення: Ініціалізує сервіс залежностями доступу до аукціонів, користувачів та поточної сесії.
     * Параметри:
     *   auctionDal - репозиторій доступу до аукціонів у базі даних
     *   userSession - сесія поточного авторизованого користувача
     *   userDal - репозиторій доступу до користувачів у базі даних
     */
    public AuctionService(IAuctionDAL auctionDal, IUserSession userSession, IUserDAL userDal)
    {
        _auctionDal = auctionDal;
        _userSession = userSession;
        _userDal = userDal;
    }

    /*
     * Метод: GetAllAuctions
     * Призначення: Повертає список усіх аукціонних лотів з бази даних.
     */
    public List<AuctionDTO> GetAllAuctions()
    {
        return _auctionDal.GetAll();
    }

    /*
     * Метод: GetAllUsers
     * Призначення: Повертає список усіх користувачів з бази даних.
     */
    public List<UserDTO> GetAllUsers()
    {
        return _userDal.GetAll();
    }

    /*
     * Метод: AddAuction (generic)
     * Призначення: Додає новий аукціонний лот. Компілятор дозволяє виклик тільки з ролями, які реалізують ICanManageAuctions.
     * Параметри типу:
     *   TRole - тип ролі користувача з обмеженням where TRole : ICanManageAuctions
     * Параметри:
     *   auction - DTO з даними створюваного аукціону
     * Повертає: true, якщо аукціон успішно додано до бази.
     */
    public bool AddAuction<TRole>(AuctionDTO auction) where TRole : ICanManageAuctions
    {
        return AddAuction(auction);
    }

    /*
     * Метод: AddAuction
     * Призначення: Додає новий аукціонний лот після перевірки прав менеджера у поточній сесії.
     * Параметри:
     *   auction - DTO з даними створюваного аукціону
     * Повертає: true, якщо користувач має права менеджера та лот збережено.
     */
    public bool AddAuction(AuctionDTO auction)
    {
        var currentUser = _userSession.CurrentUser;
        if (currentUser == null || !currentUser.IsManager)
        {
            return false;
        }

        auction.AuctionManagerId = currentUser.Id;
        auction.IsActive = true;

        return _auctionDal.Add(auction);
    }

    /*
     * Метод: UpdateBalance
     * Призначення: Оновлює баланс вказаного користувача (виконується менеджером).
     * Параметри:
     *   userId - ідентифікатор користувача
     *   newBalance - нове значення балансу (не може бути від'ємним)
     * Повертає: true, якщо баланс успішно оновлено.
     */
    public bool UpdateBalance(Guid userId, decimal newBalance)
    {
        if (newBalance < 0)
        {
            return false;
        }

        var user = _userDal.GetById(userId);
        if (user == null)
        {
            return false;
        }

        user.Balance = newBalance;
        bool updated = _userDal.Update(user);

        if (updated && _userSession.CurrentUser?.Id == userId)
        {
            _userSession.CurrentUser.Balance = newBalance;
        }

        return updated;
    }

    /*
     * Метод: BuyoutAuction
     * Призначення: Проводить викуп лоту: списує ціну з покупця, віддає продавцю суму без комісії, а комісію — менеджеру.
     * Параметри:
     *   auctionId - ідентифікатор аукціону
     *   buyerId - ідентифікатор покупця
     *   commissionPercent - частка комісії (за замовчуванням 10%)
     * Повертає: true, якщо викуп успішно проведено та баланси оновлено.
     */
    public bool BuyoutAuction(Guid auctionId, Guid buyerId, decimal commissionPercent = 0.10m)
    {
        var auction = _auctionDal.GetById(auctionId);
        if (auction == null || !auction.IsActive)
        {
            return false;
        }

        decimal buyoutPrice = auction.EndPrice > 0 ? auction.EndPrice : auction.StartPrice;
        var buyer = _userDal.GetById(buyerId);
        if (buyer == null || buyer.Balance < buyoutPrice)
        {
            return false;
        }

        decimal commission = Math.Round(buyoutPrice * commissionPercent, 2);
        decimal sellerAmount = buyoutPrice - commission;

        buyer.Balance -= buyoutPrice;
        _userDal.Update(buyer);

        var seller = _userDal.GetById(auction.AuctionManagerId);
        if (seller != null)
        {
            seller.Balance += sellerAmount;
            _userDal.Update(seller);
        }

        var currentManagerId = _userSession.CurrentUser?.Id;
        if (currentManagerId != null)
        {
            var manager = _userDal.GetById(currentManagerId.Value);
            if (manager != null)
            {
                manager.Balance += commission;
                _userDal.Update(manager);

                if (_userSession.CurrentUser != null)
                {
                    _userSession.CurrentUser.Balance = manager.Balance;
                }
            }
        }

        auction.EndPrice = buyoutPrice;
        auction.IsActive = false;
        return _auctionDal.Update(auction);
    }

    /*
     * Метод: PlaceBid
     * Призначення: Фіксує ставку на аукціоні з перевіркою конфлікту інтересів та shill bidding.
     * Параметри:
     *   auctionId - ідентифікатор лоту
     *   bidAmount - запропонована ціна
     * Повертає: true, якщо ставка відповідає всім правилам торгів і збережена в базі.
     */
    public bool PlaceBid(Guid auctionId, decimal bidAmount)
    {
        var currentUser = _userSession.CurrentUser;
        if (currentUser == null)
        {
            return false;
        }

        if (currentUser.IsManager)
        {
            return false;
        }

        var auction = _auctionDal.GetById(auctionId);
        if (auction == null || !auction.IsActive)
        {
            return false;
        }

        if (auction.AuctionManagerId == currentUser.Id)
        {
            return false;
        }

        decimal currentPrice = auction.EndPrice > 0 ? auction.EndPrice : auction.StartPrice;
        if (bidAmount <= currentPrice)
        {
            return false;
        }

        auction.EndPrice = bidAmount;
        return _auctionDal.Update(auction);
    }
}
