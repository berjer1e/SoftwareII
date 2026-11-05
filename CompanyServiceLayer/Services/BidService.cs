/*
 * Файл: BidService.cs
 * Призначення: Реалізація сервісу бізнес-логіки для ставок і миттєвого викупу.
 * Контролює стан і терміни дії аукціону, перевіряє мінімальний крок ставки та платоспроможність учасника,
 * повертає заблоковані кошти попередньому лідеру торгів та оновлює поточну ціну і статус лота.
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CompanyDataAccessLayer.DAL;
using CompanyDataAccessLayer.Logging;
using CompanyDataTransferObject;
using CompanyServiceLayer.Exceptions;
using CompanyServiceLayer.Session;

namespace CompanyServiceLayer.Services
{
    public class BidService : IBidService
    {
        private readonly IBidDAL _bidDal;
        private readonly IAuctionDAL _auctionDal;
        private readonly IUserDAL _userDal;
        private readonly IUserSession _userSession;
        private readonly TimeProvider _timeProvider;
        private readonly IAppLogger _logger;

        /*
         * Ініціалізує сервіс ставок залежностями DAL, сесії користувача, постачальника часу та логера.
         */
        public BidService(
            IBidDAL bidDal,
            IAuctionDAL auctionDal,
            IUserDAL userDal,
            IUserSession userSession,
            TimeProvider? timeProvider = null,
            IAppLogger? logger = null)
        {
            _bidDal = bidDal ?? throw new ArgumentNullException(nameof(bidDal));
            _auctionDal = auctionDal ?? throw new ArgumentNullException(nameof(auctionDal));
            _userDal = userDal ?? throw new ArgumentNullException(nameof(userDal));
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
            _timeProvider = timeProvider ?? TimeProvider.System;
            _logger = logger ?? FileAppLogger.Instance;
        }

        /*
         * Повертає перелік ставок для вказаного аукціону.
         */
        public List<BidDTO> GetByAuctionId(Guid auctionId) => _bidDal.GetByAuctionId(auctionId);

        /*
         * Асинхронно завантажує перелік ставок для вказаного аукціону.
         */
        public Task<List<BidDTO>> GetByAuctionIdAsync(Guid auctionId) => Task.Run(() => GetByAuctionId(auctionId));

        /*
         * Повертає перелік ставок конкретного користувача.
         */
        public List<BidDTO> GetByUserId(Guid userId) => _bidDal.GetByUserId(userId);

        /*
         * Повертає найвищу ставку для вказаного аукціону.
         */
        public BidDTO? GetHighestBid(Guid auctionId) => _bidDal.GetHighestBid(auctionId);

        /*
         * Перевіряє умови участі в торгах, списує суму ставки з балансу учасника, повертає кошти попередньому лідеру,
         * зберігає нову ставку та оновлює поточну ціну і лідера аукціону.
         */
        public BidDTO PlaceBid(Guid auctionId, decimal bidAmount)
        {
            var currentUser = GetAuthenticatedUserFromDb();
            var auction = GetValidActiveAuction(auctionId);

            decimal minBidStep = auction.MinBid > 0m ? auction.MinBid : 0.01m;
            bool isFirstBid = auction.LastBidId == null && auction.EndPrice <= 0m;
            decimal minRequiredAmount = isFirstBid
                ? auction.StartPrice
                : Math.Max(auction.StartPrice, auction.EndPrice + minBidStep);

            if (bidAmount < minRequiredAmount)
            {
                throw new BusinessRuleException(
                    $"Сума ставки ({bidAmount:F2} $) менша за мінімально допустиму ({minRequiredAmount:F2} $).");
            }

            var previousBid = GetPreviousLeaderBid(auction);
            decimal selfRefund = previousBid != null && previousBid.BidOwner == currentUser.Id
                ? previousBid.Price
                : 0m;
            decimal availableBalance = currentUser.Balance + selfRefund;

            if (availableBalance < bidAmount)
            {
                throw new BusinessRuleException(
                    $"Недостатньо коштів на балансі. Доступно: {availableBalance:F2} $, потрібно: {bidAmount:F2} $.");
            }

            decimal originalBidderBalance = currentUser.Balance;
            decimal newBidderBalance = availableBalance - bidAmount;
            var refundedLeader = RefundOtherLeaderIfAny(previousBid, currentUser.Id);

            if (!_userDal.UpdateBalance(currentUser.Id, newBidderBalance))
            {
                RollbackPreviousLeaderRefund(refundedLeader);
                throw new BusinessRuleException("Не вдалося оновити баланс користувача під час подання ставки.");
            }

            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var newBid = new BidDTO
            {
                Id = Guid.NewGuid(),
                AuctionId = auction.Id,
                BidOwner = currentUser.Id,
                Price = bidAmount,
                TimeStamp = nowUtc,
                IsBuyout = false,
                RowInsertTime = nowUtc,
                RowUpdateTime = nowUtc,
                BidderName = currentUser.Name,
                BidderEmail = currentUser.Email
            };

            if (!_bidDal.Add(newBid))
            {
                _userDal.UpdateBalance(currentUser.Id, originalBidderBalance);
                RollbackPreviousLeaderRefund(refundedLeader);
                throw new BusinessRuleException("Не вдалося зберегти ставку в базі даних.");
            }

            decimal prevEndPrice = auction.EndPrice;
            Guid? prevLastBidId = auction.LastBidId;
            string? prevLastBidderName = auction.LastBidderName;
            DateTime? prevRowUpdateTime = auction.RowUpdateTime;

            auction.EndPrice = bidAmount;
            auction.LastBidId = newBid.Id;
            auction.LastBidderName = currentUser.Name;
            auction.RowUpdateTime = nowUtc;

            if (!_auctionDal.Update(auction))
            {
                auction.EndPrice = prevEndPrice;
                auction.LastBidId = prevLastBidId;
                auction.LastBidderName = prevLastBidderName;
                auction.RowUpdateTime = prevRowUpdateTime;
                _bidDal.Delete(newBid.Id);
                _userDal.UpdateBalance(currentUser.Id, originalBidderBalance);
                RollbackPreviousLeaderRefund(refundedLeader);
                throw new BusinessRuleException("Не вдалося оновити поточну ціну аукціону.");
            }

            _userSession.UpdateBalance(newBidderBalance);
            _logger.LogInfo($"User '{currentUser.Name}' placed bid {bidAmount:F2} on auction {auction.Id}");
            return newBid;
        }

        /*
         * Асинхронно виконує подання ставки у фоновому потоці.
         */
        public Task<BidDTO> PlaceBidAsync(Guid auctionId, decimal bidAmount)
        {
            return Task.Run(() => PlaceBid(auctionId, bidAmount));
        }

        /*
         * Виконує миттєвий викуп лота за ціною BuyoutPrice: повертає кошти попередньому лідеру,
         * списує BuyoutPrice з покупця, фіксує ставку викупу та переводить аукціон у статус IsActive = false.
         */
        public BidDTO Buyout(Guid auctionId)
        {
            var currentUser = GetAuthenticatedUserFromDb();
            var auction = GetValidActiveAuction(auctionId);

            if (auction.BuyoutPrice <= 0m)
            {
                throw new BusinessRuleException("Для цього лота не передбачено миттєвий викуп.");
            }

            decimal buyoutPrice = auction.BuyoutPrice;
            var previousBid = GetPreviousLeaderBid(auction);
            decimal selfRefund = previousBid != null && previousBid.BidOwner == currentUser.Id
                ? previousBid.Price
                : 0m;
            decimal availableBalance = currentUser.Balance + selfRefund;

            if (availableBalance < buyoutPrice)
            {
                throw new BusinessRuleException(
                    $"Недостатньо коштів для викупу. Доступно: {availableBalance:F2} $, ціна викупу: {buyoutPrice:F2} $.");
            }

            decimal originalBuyerBalance = currentUser.Balance;
            decimal newBuyerBalance = availableBalance - buyoutPrice;
            var refundedLeader = RefundOtherLeaderIfAny(previousBid, currentUser.Id);

            if (!_userDal.UpdateBalance(currentUser.Id, newBuyerBalance))
            {
                RollbackPreviousLeaderRefund(refundedLeader);
                throw new BusinessRuleException("Не вдалося списати кошти для миттєвого викупу.");
            }

            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var buyoutBid = new BidDTO
            {
                Id = Guid.NewGuid(),
                AuctionId = auction.Id,
                BidOwner = currentUser.Id,
                Price = buyoutPrice,
                TimeStamp = nowUtc,
                IsBuyout = true,
                RowInsertTime = nowUtc,
                RowUpdateTime = nowUtc,
                BidderName = currentUser.Name,
                BidderEmail = currentUser.Email
            };

            if (!_bidDal.Add(buyoutBid))
            {
                _userDal.UpdateBalance(currentUser.Id, originalBuyerBalance);
                RollbackPreviousLeaderRefund(refundedLeader);
                throw new BusinessRuleException("Не вдалося зареєструвати транзакцію викупу.");
            }

            decimal prevEndPrice = auction.EndPrice;
            Guid? prevLastBidId = auction.LastBidId;
            string? prevLastBidderName = auction.LastBidderName;
            bool prevIsActive = auction.IsActive;
            DateTime? prevRowUpdateTime = auction.RowUpdateTime;

            auction.EndPrice = buyoutPrice;
            auction.LastBidId = buyoutBid.Id;
            auction.LastBidderName = currentUser.Name;
            auction.IsActive = false;
            auction.RowUpdateTime = nowUtc;

            if (!_auctionDal.Update(auction))
            {
                auction.EndPrice = prevEndPrice;
                auction.LastBidId = prevLastBidId;
                auction.LastBidderName = prevLastBidderName;
                auction.IsActive = prevIsActive;
                auction.RowUpdateTime = prevRowUpdateTime;
                _bidDal.Delete(buyoutBid.Id);
                _userDal.UpdateBalance(currentUser.Id, originalBuyerBalance);
                RollbackPreviousLeaderRefund(refundedLeader);
                throw new BusinessRuleException("Не вдалося закрити аукціон після викупу.");
            }

            _userSession.UpdateBalance(newBuyerBalance);
            _logger.LogInfo($"User '{currentUser.Name}' executed instant buyout ({buyoutPrice:F2}) on auction {auction.Id}");
            return buyoutBid;
        }

        /*
         * Асинхронно виконує миттєвий викуп лота у фоновому потоці.
         */
        public Task<BidDTO> BuyoutAsync(Guid auctionId)
        {
            return Task.Run(() => Buyout(auctionId));
        }

        /*
         * Перевіряє сесію поточного користувача та завантажує його актуальний запис із бази даних разом із балансом.
         */
        private UserDTO GetAuthenticatedUserFromDb()
        {
            if (!_userSession.IsAuthenticated || _userSession.CurrentUser == null)
            {
                throw new BusinessRuleException("Для участі в торгах необхідно увійти в систему.");
            }

            var user = _userDal.GetById(_userSession.CurrentUser.Id);
            if (user == null || !user.IsActive)
            {
                throw new BusinessRuleException("Обліковий запис користувача не знайдено або деактивовано.");
            }

            return user;
        }

        /*
         * Завантажує аукціон за ідентифікатором і перевіряє, що він існує, підтверджений менеджером,
         * активний та перебуває в межах дат проведення торгів (StartDate <= Now <= EndDate).
         */
        private AuctionDTO GetValidActiveAuction(Guid auctionId)
        {
            if (auctionId == Guid.Empty)
            {
                throw new BusinessRuleException("Не обрано лот для виконання операції.");
            }

            var auction = _auctionDal.GetById(auctionId);
            if (auction == null)
            {
                throw new BusinessRuleException("Аукціон не знайдено в базі даних.");
            }

            if (!auction.IsVerified)
            {
                throw new BusinessRuleException("Цей лот ще не підтверджений менеджером аукціонів.");
            }

            if (!auction.IsActive)
            {
                throw new BusinessRuleException("Цей аукціон уже деактивовано або завершено.");
            }

            var nowLocal = _timeProvider.GetLocalNow().DateTime;
            if (auction.StartDate > nowLocal)
            {
                throw new BusinessRuleException("Торги за цим аукціоном ще не розпочалися.");
            }

            if (auction.EndDate < nowLocal)
            {
                throw new BusinessRuleException("Час проведення цього аукціону вже вичерпано.");
            }

            return auction;
        }

        /*
         * Отримує запис попередньої лідируючої ставки аукціону або повертає null, якщо ставок ще не було.
         */
        private BidDTO? GetPreviousLeaderBid(AuctionDTO auction)
        {
            if (!auction.LastBidId.HasValue || auction.LastBidId.Value == Guid.Empty)
            {
                return null;
            }

            var previousBid = _bidDal.GetById(auction.LastBidId.Value);
            if (previousBid == null || previousBid.Price <= 0m)
            {
                return null;
            }

            return previousBid;
        }

        /*
         * Повертає кошти попередньому лідеру аукціону (якщо це інший користувач) і повертає його початковий баланс для можливого відкату.
         */
        private (Guid UserId, decimal OriginalBalance)? RefundOtherLeaderIfAny(BidDTO? previousBid, Guid currentBidderId)
        {
            if (previousBid == null || previousBid.BidOwner == currentBidderId)
            {
                return null;
            }

            var previousOwner = _userDal.GetById(previousBid.BidOwner);
            if (previousOwner == null)
            {
                return null;
            }

            decimal originalBalance = previousOwner.Balance;
            if (!_userDal.UpdateBalance(previousOwner.Id, originalBalance + previousBid.Price))
            {
                throw new BusinessRuleException("Не вдалося повернути кошти попередньому лідеру аукціону.");
            }

            return (previousOwner.Id, originalBalance);
        }

        /*
         * Відновлює баланс попереднього лідера у разі збою збереження нової ставки або оновлення аукціону.
         */
        private void RollbackPreviousLeaderRefund((Guid UserId, decimal OriginalBalance)? refundedLeader)
        {
            if (refundedLeader.HasValue)
            {
                _userDal.UpdateBalance(refundedLeader.Value.UserId, refundedLeader.Value.OriginalBalance);
            }
        }
    }
}
