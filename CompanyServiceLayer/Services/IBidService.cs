/*
 * Файл: IBidService.cs
 * Призначення: Інтерфейс сервісу бізнес-логіки для роботи зі ставками та миттєвим викупом лотів на аукціоні.
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CompanyDataTransferObject;

namespace CompanyServiceLayer.Services
{
    public interface IBidService
    {
        /*
         * Повертає список усіх ставок для заданого аукціону.
         */
        List<BidDTO> GetByAuctionId(Guid auctionId);

        /*
         * Асинхронно повертає список усіх ставок для заданого аукціону.
         */
        Task<List<BidDTO>> GetByAuctionIdAsync(Guid auctionId);

        /*
         * Повертає список ставок, поданих конкретним користувачем.
         */
        List<BidDTO> GetByUserId(Guid userId);

        /*
         * Повертає найвищу ставку для заданого аукціону або null, якщо ставок немає.
         */
        BidDTO? GetHighestBid(Guid auctionId);

        /*
         * Подає нову ставку на активний аукціон зі списанням коштів з балансу учасника та поверненням коштів попередньому лідеру.
         */
        BidDTO PlaceBid(Guid auctionId, decimal bidAmount);

        /*
         * Асинхронно подає нову ставку на активний аукціон.
         */
        Task<BidDTO> PlaceBidAsync(Guid auctionId, decimal bidAmount);

        /*
         * Виконує миттєвий викуп лота за фіксованою ціною BuyoutPrice та закриває аукціон.
         */
        BidDTO Buyout(Guid auctionId);

        /*
         * Асинхронно виконує миттєвий викуп лота за ціною BuyoutPrice.
         */
        Task<BidDTO> BuyoutAsync(Guid auctionId);
    }
}
