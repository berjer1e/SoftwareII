/*
 * Файл: AuctionService.cs
 * Призначення: Реалізація сервісу бізнес-логіки для роботи з аукціонами.
 * Забезпечує перевірку прав ролі Менеджера аукціонів, автоматичний розрахунок ціни викупу за формулою
 * (StartPrice + 10) * 2 та мінімального кроку 0.01, верифікацію користувацьких лотів і безпечну деактивацію.
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
    public class AuctionService : IAuctionService
    {
        private readonly IAuctionDAL _auctionDal;
        private readonly IProductDAL _productDal;
        private readonly IUserSession _userSession;
        private readonly TimeProvider _timeProvider;
        private readonly IAppLogger _logger;

        /*
         * Ініціалізує сервіс аукціонів залежностями DAL, сесії користувача, постачальника часу та логера.
         */
        public AuctionService(
            IAuctionDAL auctionDal,
            IProductDAL productDal,
            IUserSession userSession,
            TimeProvider? timeProvider = null,
            IAppLogger? logger = null)
        {
            _auctionDal = auctionDal ?? throw new ArgumentNullException(nameof(auctionDal));
            _productDal = productDal ?? throw new ArgumentNullException(nameof(productDal));
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
            _timeProvider = timeProvider ?? TimeProvider.System;
            _logger = logger ?? FileAppLogger.Instance;
        }

        /*
         * Повертає повний список усіх аукціонів через рівень DAL.
         */
        public List<AuctionDTO> GetAll() => _auctionDal.GetAll();

        /*
         * Асинхронно завантажує повний список усіх аукціонів.
         */
        public Task<List<AuctionDTO>> GetAllAsync() => Task.Run(GetAll);

        /*
         * Повертає список активних і верифікованих аукціонів.
         */
        public List<AuctionDTO> GetActive() => _auctionDal.GetActive();

        /*
         * Асинхронно завантажує список активних і верифікованих аукціонів.
         */
        public Task<List<AuctionDTO>> GetActiveAsync() => Task.Run(GetActive);

        /*
         * Повертає список завершених та деактивованих аукціонів.
         */
        public List<AuctionDTO> GetFinished() => _auctionDal.GetFinished();

        /*
         * Повертає список аукціонів, які очікують на верифікацію менеджером.
         */
        public List<AuctionDTO> GetPendingVerification() => _auctionDal.GetPendingVerification();

        /*
         * Асинхронно завантажує список аукціонів, які очікують на верифікацію менеджером.
         */
        public Task<List<AuctionDTO>> GetPendingVerificationAsync() => Task.Run(GetPendingVerification);

        /*
         * Отримує аукціон за його ідентифікатором або повертає null.
         */
        public AuctionDTO? GetById(Guid id) => _auctionDal.GetById(id);

        /*
         * Виконує пошук і сортування аукціонів за заданими критеріями.
         */
        public List<AuctionDTO> SearchAndSort(string? searchTerm, bool? isActive, string? sortBy, bool ascending = true)
        {
            return _auctionDal.SearchAndSort(searchTerm, isActive, sortBy, ascending);
        }

        /*
         * Асинхронно виконує пошук і сортування аукціонів у фоновому потоці.
         */
        public Task<List<AuctionDTO>> SearchAndSortAsync(string? searchTerm, bool? isActive, string? sortBy, bool ascending = true)
        {
            return Task.Run(() => SearchAndSort(searchTerm, isActive, sortBy, ascending));
        }

        /*
         * Створює новий активний аукціон від імені менеджера після перевірки прав ролі та валідації цінових і часових полів.
         */
        public AuctionDTO CreateAuction(AuctionDTO auction)
        {
            if (auction == null) throw new ArgumentNullException(nameof(auction));
            EnsureManagerAccess();

            ApplyDefaultPricing(auction);
            ValidateAuctionFields(auction);

            if (auction.AuctionManagerId == Guid.Empty)
            {
                auction.AuctionManagerId = _userSession.CurrentUser!.Id;
            }
            if (auction.ProductOwnerId == null || auction.ProductOwnerId == Guid.Empty)
            {
                auction.ProductOwnerId = _userSession.CurrentUser!.Id;
            }

            auction.IsVerified = true;
            auction.IsActive = true;
            auction.EndPrice = 0m;
            auction.LastBidId = null;

            if (!_auctionDal.Add(auction))
            {
                throw new BusinessRuleException("Не вдалося зберегти новий аукціон у базі даних.");
            }

            _logger.LogInfo($"Manager '{_userSession.CurrentUser!.Name}' created auction {auction.Id} for product {auction.ProductId}");
            return _auctionDal.GetById(auction.Id) ?? auction;
        }

        /*
         * Створює пропозицію аукціону від звичайного користувача зі статусами IsVerified = false та IsActive = false.
         */
        public AuctionDTO ProposeAuction(AuctionDTO auction)
        {
            if (auction == null) throw new ArgumentNullException(nameof(auction));
            EnsureAuthenticated();

            ApplyDefaultPricing(auction);
            ValidateAuctionFields(auction);

            var currentUserId = _userSession.CurrentUser!.Id;
            auction.ProductOwnerId = currentUserId;
            if (auction.AuctionManagerId == Guid.Empty)
            {
                auction.AuctionManagerId = currentUserId;
            }

            auction.IsVerified = false;
            auction.IsActive = false;
            auction.EndPrice = 0m;
            auction.LastBidId = null;

            if (!_auctionDal.Add(auction))
            {
                throw new BusinessRuleException("Не вдалося надіслати пропозицію лота до бази даних.");
            }

            _logger.LogInfo($"User '{_userSession.CurrentUser!.Name}' proposed auction {auction.Id} for product {auction.ProductId}");
            return _auctionDal.GetById(auction.Id) ?? auction;
        }

        /*
         * Оновлює дані існуючого аукціону після перевірки прав менеджера та валідації параметрів лота.
         */
        public AuctionDTO UpdateAuction(AuctionDTO auction)
        {
            if (auction == null) throw new ArgumentNullException(nameof(auction));
            EnsureManagerAccess();

            var existing = _auctionDal.GetById(auction.Id);
            if (existing == null)
            {
                throw new BusinessRuleException("Аукціон для редагування не знайдено.");
            }

            ApplyDefaultPricing(auction);
            ValidateAuctionFields(auction);

            if (auction.AuctionManagerId == Guid.Empty)
            {
                auction.AuctionManagerId = existing.AuctionManagerId != Guid.Empty
                    ? existing.AuctionManagerId
                    : _userSession.CurrentUser!.Id;
            }
            if (auction.ProductOwnerId == null || auction.ProductOwnerId == Guid.Empty)
            {
                auction.ProductOwnerId = existing.ProductOwnerId ?? _userSession.CurrentUser!.Id;
            }
            if (auction.LastBidId == null && existing.LastBidId != null)
            {
                auction.LastBidId = existing.LastBidId;
            }
            if (auction.EndPrice <= 0m && existing.EndPrice > 0m)
            {
                auction.EndPrice = existing.EndPrice;
            }
            if (auction.RowInsertTime == default)
            {
                auction.RowInsertTime = existing.RowInsertTime;
            }
            auction.RowUpdateTime = _timeProvider.GetUtcNow().UtcDateTime;

            if (!_auctionDal.Update(auction))
            {
                throw new BusinessRuleException("Не вдалося оновити параметри аукціону в базі даних.");
            }

            _logger.LogInfo($"Manager '{_userSession.CurrentUser!.Name}' updated auction {auction.Id}");
            return _auctionDal.GetById(auction.Id) ?? auction;
        }

        /*
         * Підтверджує запропонований користувачем лот, закріплює за ним поточного менеджера та переводить у стан активних торгів.
         */
        public bool VerifyAuction(Guid auctionId)
        {
            EnsureManagerAccess();

            var existing = _auctionDal.GetById(auctionId);
            if (existing == null)
            {
                throw new BusinessRuleException("Аукціон для верифікації не знайдено.");
            }

            existing.AuctionManagerId = _userSession.CurrentUser!.Id;
            existing.IsVerified = true;
            existing.IsActive = true;
            existing.RowUpdateTime = _timeProvider.GetUtcNow().UtcDateTime;

            bool updated = _auctionDal.VerifyAuction(auctionId);
            if (!updated)
            {
                throw new BusinessRuleException("Не вдалося підтвердити аукціон у базі даних.");
            }

            _auctionDal.Update(existing);
            _logger.LogInfo($"Manager '{_userSession.CurrentUser!.Name}' verified auction {auctionId}");
            return true;
        }

        /*
         * Переводить аукціон у неактивний стан без видалення запису з бази даних.
         */
        public bool DeactivateAuction(Guid auctionId)
        {
            EnsureManagerAccess();

            var existing = _auctionDal.GetById(auctionId);
            if (existing == null)
            {
                throw new BusinessRuleException("Аукціон для деактивації не знайдено.");
            }

            bool result = _auctionDal.Deactivate(auctionId);
            if (!result)
            {
                throw new BusinessRuleException("Не вдалося деактивувати аукціон.");
            }

            _logger.LogInfo($"Manager '{_userSession.CurrentUser!.Name}' deactivated auction {auctionId}");
            return true;
        }

        /*
         * Застосовує значення за замовчуванням для мінімального кроку ставки (0.01) та ціни миттєвого викупу ((StartPrice + 10) * 2).
         */
        private void ApplyDefaultPricing(AuctionDTO auction)
        {
            if (auction.MinBid <= 0m)
            {
                auction.MinBid = 0.01m;
            }

            if (auction.BuyoutPrice <= 1m && auction.StartPrice > 0m)
            {
                auction.BuyoutPrice = (auction.StartPrice + 10m) * 2m;
            }

            if (auction.StartDate == default)
            {
                auction.StartDate = _timeProvider.GetLocalNow().DateTime;
            }
        }

        /*
         * Перевіряє коректність заповнення всіх обов'язкових полів аукціону та наявність товару в базі даних.
         */
        private void ValidateAuctionFields(AuctionDTO auction)
        {
            var errors = new List<string>();

            if (auction.ProductId == Guid.Empty)
            {
                errors.Add("Необхідно обрати товар для аукціону.");
            }
            else if (_productDal.GetById(auction.ProductId) == null)
            {
                errors.Add("Обраний товар не знайдено в базі даних.");
            }

            if (auction.StartPrice <= 0m)
            {
                errors.Add("Стартова ціна лота повинна бути більшою за 0.");
            }

            if (auction.MinBid <= 0m)
            {
                errors.Add("Мінімальний крок ставки повинен бути більшим за 0.");
            }

            if (auction.BuyoutPrice < auction.StartPrice)
            {
                errors.Add("Ціна миттєвого викупу не може бути меншою за стартову ціну.");
            }

            if (auction.EndDate <= auction.StartDate)
            {
                errors.Add("Дата завершення аукціону повинна бути пізнішою за дату початку.");
            }

            if (errors.Count > 0)
            {
                throw new BusinessRuleException(errors);
            }
        }

        /*
         * Перевіряє, чи користувач увійшов у систему, і генерує виняток бізнес-логіки у разі відсутності авторизації.
         */
        private void EnsureAuthenticated()
        {
            if (!_userSession.IsAuthenticated || _userSession.CurrentUser == null)
            {
                throw new BusinessRuleException("Для виконання цієї дії необхідно увійти в систему.");
            }
        }

        /*
         * Перевіряє наявність у поточного користувача прав ролі Менеджера аукціонів або Адміністратора.
         */
        private void EnsureManagerAccess()
        {
            EnsureAuthenticated();
            if (!_userSession.CanManageAuctions)
            {
                throw new BusinessRuleException("Доступ заборонено: операція дозволена лише для ролі Менеджер аукціонів.");
            }
        }
    }
}
