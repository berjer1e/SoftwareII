/*
 * Файл: TraderTerminalViewModel.cs
 * Призначення: Обмежена за роллю ViewModel термінала учасника торгів (Покупець / Продавець).
 * Надає доступ лише до перегляду активних верифікованих лотів, пошуку й сортування,
 * подання ставок, миттєвого викупу в межах балансу та подання пропозиції власного лота
 * на розгляд та верифікацію Менеджеру аукціонів.
 */

using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using CompanyDataTransferObject;
using CompanyServiceLayer.Exceptions;
using CompanyServiceLayer.Services;
using CompanyServiceLayer.Session;
using CompanyWPFInterface.Infrastructure;

namespace CompanyWPFInterface.ViewModels
{
    public class TraderTerminalViewModel : ValidatableViewModelBase
    {
        private readonly IAuctionService _auctionService;
        private readonly IBidService _bidService;
        private readonly IProductService _productService;
        private readonly IManufacturerService _manufacturerService;
        private readonly IProductTypeService _productTypeService;
        private readonly IAuthService _authService;
        private readonly IUserSession _userSession;
        private readonly IViewHandler _viewHandler;

        private AuctionDTO? _selectedAuction;
        private ManufacturerDTO? _selectedManufacturer;
        private ProductTypeDTO? _selectedProductType;

        private string _searchTerm = string.Empty;
        private string _sortBy = "enddate";
        private bool _sortAscending = true;

        private string _bidAmountInput = string.Empty;

        private string _proposalProductName = string.Empty;
        private string _proposalStartPriceInput = "150.00";
        private string _proposalMinBidInput = "0.01";
        private string _proposalBuyoutPriceInput = "320.00";
        private string _proposalDurationDaysInput = "14";

        private string _newManufacturerNameInput = string.Empty;
        private string _newManufacturerCountryInput = "USA";
        private string _newProductTypeNameInput = string.Empty;

        private string _statusMessage = "ТОРГОВИЙ ТЕРМІНАЛ УЧАСНИКА АКТИВОВАНО (ОБМЕЖЕНИЙ РЕЖИМ РОЛІ).";
        private bool _isErrorStatus;

        public ObservableCollection<AuctionDTO> ActiveAuctions { get; } = new();
        public ObservableCollection<BidDTO> SelectedAuctionBids { get; } = new();
        public ObservableCollection<ManufacturerDTO> Manufacturers { get; } = new();
        public ObservableCollection<ProductTypeDTO> ProductTypes { get; } = new();

        public ObservableCollection<string> SortFieldOptions { get; } = new()
        {
            "enddate",
            "startdate",
            "productname",
            "startprice",
            "endprice"
        };

        public string TraderHeader => _userSession.CurrentUser != null
            ? $"{_userSession.CurrentUser.Name} | {_userSession.CurrentUser.RoleName} [ОБМЕЖЕНИЙ ДОСТУП]"
            : "ГОСТЬОВИЙ ТЕРМІНАЛ";

        public decimal CurrentBalance => _userSession.CurrentUser?.Balance ?? 0m;

        public AuctionDTO? SelectedAuction
        {
            get => _selectedAuction;
            set
            {
                if (SetProperty(ref _selectedAuction, value))
                {
                    if (value != null)
                    {
                        BidAmountInput = value.MinNextBid.ToString("F2", CultureInfo.InvariantCulture);
                    }
                    LoadBidsForSelectedAuction(value);
                    RaiseCommandStates();
                }
            }
        }

        public ManufacturerDTO? SelectedManufacturer
        {
            get => _selectedManufacturer;
            set
            {
                if (SetProperty(ref _selectedManufacturer, value))
                {
                    RaiseCommandStates();
                }
            }
        }

        public ProductTypeDTO? SelectedProductType
        {
            get => _selectedProductType;
            set
            {
                if (SetProperty(ref _selectedProductType, value))
                {
                    RaiseCommandStates();
                }
            }
        }

        public string SearchTerm
        {
            get => _searchTerm;
            set => SetProperty(ref _searchTerm, value);
        }

        public string SortBy
        {
            get => _sortBy;
            set
            {
                if (SetProperty(ref _sortBy, value))
                {
                    ApplyFilterAndSort();
                }
            }
        }

        public bool SortAscending
        {
            get => _sortAscending;
            set
            {
                if (SetProperty(ref _sortAscending, value))
                {
                    ApplyFilterAndSort();
                }
            }
        }

        public string BidAmountInput
        {
            get => _bidAmountInput;
            set
            {
                if (SetProperty(ref _bidAmountInput, value))
                {
                    ValidateBidInput();
                    RaiseCommandStates();
                }
            }
        }

        public string ProposalProductName
        {
            get => _proposalProductName;
            set
            {
                if (SetProperty(ref _proposalProductName, value))
                {
                    ValidateProposalInputs();
                    RaiseCommandStates();
                }
            }
        }

        public string ProposalStartPriceInput
        {
            get => _proposalStartPriceInput;
            set
            {
                if (SetProperty(ref _proposalStartPriceInput, value))
                {
                    if (TryParseDecimal(value, out decimal sp) && sp > 0m)
                    {
                        ProposalBuyoutPriceInput = ((sp + 10m) * 2m).ToString("F2", CultureInfo.InvariantCulture);
                    }
                    ValidateProposalInputs();
                    RaiseCommandStates();
                }
            }
        }

        public string ProposalMinBidInput
        {
            get => _proposalMinBidInput;
            set
            {
                if (SetProperty(ref _proposalMinBidInput, value))
                {
                    ValidateProposalInputs();
                    RaiseCommandStates();
                }
            }
        }

        public string ProposalBuyoutPriceInput
        {
            get => _proposalBuyoutPriceInput;
            set
            {
                if (SetProperty(ref _proposalBuyoutPriceInput, value))
                {
                    ValidateProposalInputs();
                    RaiseCommandStates();
                }
            }
        }

        public string ProposalDurationDaysInput
        {
            get => _proposalDurationDaysInput;
            set
            {
                if (SetProperty(ref _proposalDurationDaysInput, value))
                {
                    ValidateProposalInputs();
                    RaiseCommandStates();
                }
            }
        }

        public string NewManufacturerNameInput
        {
            get => _newManufacturerNameInput;
            set
            {
                if (SetProperty(ref _newManufacturerNameInput, value))
                {
                    RaiseCommandStates();
                }
            }
        }

        public string NewManufacturerCountryInput
        {
            get => _newManufacturerCountryInput;
            set
            {
                if (SetProperty(ref _newManufacturerCountryInput, value))
                {
                    RaiseCommandStates();
                }
            }
        }

        public string NewProductTypeNameInput
        {
            get => _newProductTypeNameInput;
            set
            {
                if (SetProperty(ref _newProductTypeNameInput, value))
                {
                    RaiseCommandStates();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsErrorStatus
        {
            get => _isErrorStatus;
            set => SetProperty(ref _isErrorStatus, value);
        }

        public RelayCommand RefreshCommand { get; }
        public RelayCommand ApplyFilterCommand { get; }
        public RelayCommand PlaceBidCommand { get; }
        public RelayCommand BuyoutCommand { get; }
        public RelayCommand ProposeAuctionCommand { get; }
        public RelayCommand CreateManufacturerCommand { get; }
        public RelayCommand CreateProductTypeCommand { get; }
        public RelayCommand LogoutCommand { get; }

        /*
         * Ініціалізує ViewModel торгового термінала учасника залежностями сервісів бізнес-логіки та навігації.
         */
        public TraderTerminalViewModel(
            IAuctionService auctionService,
            IBidService bidService,
            IProductService productService,
            IManufacturerService manufacturerService,
            IProductTypeService productTypeService,
            IAuthService authService,
            IUserSession userSession,
            IViewHandler viewHandler)
        {
            _auctionService = auctionService ?? throw new ArgumentNullException(nameof(auctionService));
            _bidService = bidService ?? throw new ArgumentNullException(nameof(bidService));
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _manufacturerService = manufacturerService ?? throw new ArgumentNullException(nameof(manufacturerService));
            _productTypeService = productTypeService ?? throw new ArgumentNullException(nameof(productTypeService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
            _viewHandler = viewHandler ?? throw new ArgumentNullException(nameof(viewHandler));

            RefreshCommand = new RelayCommand(LoadAllData);
            ApplyFilterCommand = new RelayCommand(ApplyFilterAndSort);
            PlaceBidCommand = new RelayCommand(ExecutePlaceBid, CanExecutePlaceBid);
            BuyoutCommand = new RelayCommand(ExecuteBuyout, CanExecuteBuyout);
            ProposeAuctionCommand = new RelayCommand(ExecuteProposeAuction, CanExecuteProposeAuction);
            CreateManufacturerCommand = new RelayCommand(ExecuteCreateManufacturer, () => !string.IsNullOrWhiteSpace(NewManufacturerNameInput) && !string.IsNullOrWhiteSpace(NewManufacturerCountryInput));
            CreateProductTypeCommand = new RelayCommand(ExecuteCreateProductType, () => !string.IsNullOrWhiteSpace(NewProductTypeNameInput));
            LogoutCommand = new RelayCommand(ExecuteLogout);
        }

        /*
         * Перевіряє авторизацію користувача та завантажує список активних лотів і довідників.
         */
        public override void Initialize()
        {
            if (!_userSession.IsAuthenticated)
            {
                _viewHandler.NavigateTo<LoginViewModel>();
                return;
            }

            ClearAllErrors();
            LoadAllData();
        }

        /*
         * Завантажує довідники виробників і категорій та оновлює перелік активних аукціонів.
         */
        public void LoadAllData()
        {
            try
            {
                Manufacturers.Clear();
                foreach (var m in _manufacturerService.GetAll())
                {
                    Manufacturers.Add(m);
                }

                ProductTypes.Clear();
                foreach (var pt in _productTypeService.GetAll())
                {
                    ProductTypes.Add(pt);
                }

                SelectedManufacturer ??= Manufacturers.FirstOrDefault();
                SelectedProductType ??= ProductTypes.FirstOrDefault();

                ApplyFilterAndSort();
                OnPropertyChanged(nameof(TraderHeader));
                OnPropertyChanged(nameof(CurrentBalance));
                SetStatus($"ДОСТУПНО АКТИВНИХ ЛОТІВ: {ActiveAuctions.Count}. БАЛАНС: {CurrentBalance:F2} $.", isError: false);
            }
            catch (Exception ex)
            {
                SetStatus($"ПОМИЛКА ОНОВЛЕННЯ ТЕРМІНАЛА: {ex.Message}", isError: true);
            }
        }

        /*
         * Фільтрує та сортує виключно активні та верифіковані аукціони.
         */
        public void ApplyFilterAndSort()
        {
            try
            {
                var active = _auctionService.SearchAndSort(SearchTerm, true, SortBy, SortAscending);
                ActiveAuctions.Clear();
                foreach (var a in active)
                {
                    ActiveAuctions.Add(a);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"ПОМИЛКА ПОШУКУ ЛОТІВ: {ex.Message}", isError: true);
            }
        }

        /*
         * Подає ставку від імені поточного учасника на вибраний активний лот.
         */
        public void ExecutePlaceBid()
        {
            if (SelectedAuction == null)
            {
                return;
            }

            ValidateBidInput();
            if (GetPropertyErrors(nameof(BidAmountInput)).Count > 0 || !TryParseDecimal(BidAmountInput, out decimal amount))
            {
                SetStatus("ВКАЖІТЬ КОРЕКТНУ СУМУ СТАВКИ.", isError: true);
                return;
            }

            try
            {
                Guid auctionId = SelectedAuction.Id;
                _bidService.PlaceBid(auctionId, amount);
                ApplyFilterAndSort();
                SelectedAuction = ActiveAuctions.FirstOrDefault(a => a.Id == auctionId);
                OnPropertyChanged(nameof(CurrentBalance));
                SetStatus($"ВАШУ СТАВКУ {amount:F2} $ ПРИЙНЯТО! ПОТОЧНИЙ БАЛАНС: {CurrentBalance:F2} $.", isError: false);
            }
            catch (BusinessRuleException ex)
            {
                SetStatus(ex.Message, isError: true);
            }
        }

        /*
         * Виконує миттєвий викуп вибраного лота за ціною BuyoutPrice.
         */
        public void ExecuteBuyout()
        {
            if (SelectedAuction == null)
            {
                return;
            }

            try
            {
                string productName = SelectedAuction.ProductName ?? "Лот";
                var bid = _bidService.Buyout(SelectedAuction.Id);
                ApplyFilterAndSort();
                SelectedAuction = null;
                OnPropertyChanged(nameof(CurrentBalance));
                SetStatus($"ВИ ВИКУПИЛИ ЛОТ [{productName}] ЗА {bid.Price:F2} $! ЗАЛИШОК: {CurrentBalance:F2} $.", isError: false);
            }
            catch (BusinessRuleException ex)
            {
                SetStatus(ex.Message, isError: true);
            }
        }

        /*
         * Створює новий товар і подає заявку на виставлення лота, яка очікуватиме на підтвердження Менеджером аукціонів.
         */
        public void ExecuteProposeAuction()
        {
            ValidateProposalInputs();
            if (HasProposalErrors() || SelectedManufacturer == null || SelectedProductType == null)
            {
                SetStatus("ПЕРЕВІРТЕ ПОЛЯ ЗАЯВКИ НА ВИСТАВЛЕННЯ ЛОТА.", isError: true);
                return;
            }

            try
            {
                TryParseDecimal(ProposalStartPriceInput, out decimal startPrice);
                TryParseDecimal(ProposalMinBidInput, out decimal minBid);
                TryParseDecimal(ProposalBuyoutPriceInput, out decimal buyoutPrice);
                int.TryParse(ProposalDurationDaysInput.Trim(), out int days);

                var createdProduct = _productService.CreateProduct(new ProductDTO
                {
                    Name = ProposalProductName.Trim(),
                    Price = startPrice,
                    Quantity = 1,
                    ManufacturerId = SelectedManufacturer.Id,
                    ProductTypeId = SelectedProductType.Id
                });

                var proposedAuction = _auctionService.ProposeAuction(new AuctionDTO
                {
                    ProductId = createdProduct.Id,
                    StartPrice = startPrice,
                    MinBid = minBid > 0m ? minBid : 0.01m,
                    BuyoutPrice = buyoutPrice > 1m ? buyoutPrice : (startPrice + 10m) * 2m,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(days > 0 ? days : 14)
                });

                ProposalProductName = string.Empty;
                ClearErrors(nameof(ProposalProductName));
                SetStatus($"ЗАЯВКУ НА ЛОТ [{createdProduct.Name}] НАДІСЛАНО МЕНЕДЖЕРУ НА ВЕРИФІКАЦІЮ (ID {proposedAuction.Id.ToString()[..8]}).", isError: false);
            }
            catch (BusinessRuleException ex)
            {
                SetStatus(ex.Message, isError: true);
            }
        }

        /*
         * Додає нового виробника до довідника під час підготовки заявки на лот.
         */
        public void ExecuteCreateManufacturer()
        {
            if (string.IsNullOrWhiteSpace(NewManufacturerNameInput) || string.IsNullOrWhiteSpace(NewManufacturerCountryInput))
            {
                SetStatus("ВКАЖІТЬ НАЗВУ ТА КРАЇНУ НОВОГО ВИРОБНИКА.", isError: true);
                return;
            }

            try
            {
                var created = _manufacturerService.Create(new ManufacturerDTO
                {
                    Name = NewManufacturerNameInput.Trim(),
                    Country = NewManufacturerCountryInput.Trim()
                });

                NewManufacturerNameInput = string.Empty;
                Manufacturers.Add(created);
                SelectedManufacturer = created;
                SetStatus($"ДОДАНО НОВОГО ВИРОБНИКА: [{created.Name} ({created.Country})].", isError: false);
            }
            catch (BusinessRuleException ex)
            {
                SetStatus(ex.Message, isError: true);
            }
        }

        /*
         * Додає нову категорію товарів до довідника під час підготовки заявки на лот.
         */
        public void ExecuteCreateProductType()
        {
            if (string.IsNullOrWhiteSpace(NewProductTypeNameInput))
            {
                SetStatus("ВКАЖІТЬ НАЗВУ НОВОЇ КАТЕГОРІЇ.", isError: true);
                return;
            }

            try
            {
                var created = _productTypeService.Create(new ProductTypeDTO
                {
                    Name = NewProductTypeNameInput.Trim()
                });

                NewProductTypeNameInput = string.Empty;
                ProductTypes.Add(created);
                SelectedProductType = created;
                SetStatus($"ДОДАНО НОВУ КАТЕГОРІЮ: [{created.Name}].", isError: false);
            }
            catch (BusinessRuleException ex)
            {
                SetStatus(ex.Message, isError: true);
            }
        }

        /*
         * Завершує сесію учасника торгів та повертає на екран входу.
         */
        public void ExecuteLogout()
        {
            _authService.Logout();
            _viewHandler.NavigateTo<LoginViewModel>();
        }

        /*
         * Завантажує історію ставок для вибраного активного аукціону.
         */
        private void LoadBidsForSelectedAuction(AuctionDTO? auction)
        {
            SelectedAuctionBids.Clear();
            if (auction == null)
            {
                return;
            }

            foreach (var bid in _bidService.GetByAuctionId(auction.Id))
            {
                SelectedAuctionBids.Add(bid);
            }
        }

        /*
         * Перевіряє валідність суми ставки учасника через INotifyDataErrorInfo.
         */
        private void ValidateBidInput()
        {
            ClearErrors(nameof(BidAmountInput));
            if (string.IsNullOrWhiteSpace(BidAmountInput))
            {
                return;
            }

            if (!TryParseDecimal(BidAmountInput, out decimal amount) || amount <= 0m)
            {
                AddError(nameof(BidAmountInput), "Сума ставки повинна бути додатним числом.");
                return;
            }

            if (SelectedAuction != null && amount < SelectedAuction.MinNextBid)
            {
                AddError(nameof(BidAmountInput), $"Мінімальна ставка для цього лота: {SelectedAuction.MinNextBid:F2} $.");
            }
        }

        /*
         * Перевіряє валідність полів форми подання пропозиції нового лота.
         */
        private void ValidateProposalInputs()
        {
            ClearErrors(nameof(ProposalProductName));
            ClearErrors(nameof(ProposalStartPriceInput));
            ClearErrors(nameof(ProposalMinBidInput));
            ClearErrors(nameof(ProposalBuyoutPriceInput));
            ClearErrors(nameof(ProposalDurationDaysInput));

            if (string.IsNullOrWhiteSpace(ProposalProductName))
            {
                AddError(nameof(ProposalProductName), "Вкажіть назву товару для пропозиції лота.");
            }
            else if (ProposalProductName.Trim().Length > 50)
            {
                AddError(nameof(ProposalProductName), "Назва товару не може перевищувати 50 символів.");
            }

            bool validStart = TryParseDecimal(ProposalStartPriceInput, out decimal startPrice) && startPrice > 0m;
            if (!validStart)
            {
                AddError(nameof(ProposalStartPriceInput), "Стартова ціна повинна бути більшою за 0.");
            }

            if (!TryParseDecimal(ProposalMinBidInput, out decimal minBid) || minBid <= 0m)
            {
                AddError(nameof(ProposalMinBidInput), "Мінімальний крок повинен бути більшим за 0.");
            }

            if (!string.IsNullOrWhiteSpace(ProposalBuyoutPriceInput))
            {
                if (!TryParseDecimal(ProposalBuyoutPriceInput, out decimal buyout) || buyout <= 0m)
                {
                    AddError(nameof(ProposalBuyoutPriceInput), "Ціна викупу повинна бути додатним числом.");
                }
                else if (validStart && buyout > 1m && buyout < startPrice)
                {
                    AddError(nameof(ProposalBuyoutPriceInput), "Ціна викупу не може бути меншою за стартову.");
                }
            }

            if (!int.TryParse(ProposalDurationDaysInput?.Trim(), out int days) || days < 1 || days > 365)
            {
                AddError(nameof(ProposalDurationDaysInput), "Тривалість лота повинна бути від 1 до 365 днів.");
            }
        }

        /*
         * Перевіряє наявність помилок валідації у полях пропозиції нового лота.
         */
        private bool HasProposalErrors()
        {
            return GetPropertyErrors(nameof(ProposalProductName)).Count > 0
                || GetPropertyErrors(nameof(ProposalStartPriceInput)).Count > 0
                || GetPropertyErrors(nameof(ProposalMinBidInput)).Count > 0
                || GetPropertyErrors(nameof(ProposalBuyoutPriceInput)).Count > 0
                || GetPropertyErrors(nameof(ProposalDurationDaysInput)).Count > 0;
        }

        /*
         * Визначає, чи доступна команда подання ставки.
         */
        private bool CanExecutePlaceBid()
        {
            return SelectedAuction != null
                && !string.IsNullOrWhiteSpace(BidAmountInput)
                && GetPropertyErrors(nameof(BidAmountInput)).Count == 0;
        }

        /*
         * Визначає, чи доступна команда миттєвого викупу.
         */
        private bool CanExecuteBuyout()
        {
            return SelectedAuction != null && SelectedAuction.BuyoutPrice > 0m;
        }

        /*
         * Визначає, чи доступна команда надсилання пропозиції лота на верифікацію.
         */
        private bool CanExecuteProposeAuction()
        {
            return !string.IsNullOrWhiteSpace(ProposalProductName)
                && SelectedManufacturer != null
                && SelectedProductType != null
                && !HasProposalErrors();
        }

        /*
         * Оновлює стан активності команд торгового термінала.
         */
        private void RaiseCommandStates()
        {
            PlaceBidCommand.RaiseCanExecuteChanged();
            BuyoutCommand.RaiseCanExecuteChanged();
            ProposeAuctionCommand.RaiseCanExecuteChanged();
            CreateManufacturerCommand.RaiseCanExecuteChanged();
            CreateProductTypeCommand.RaiseCanExecuteChanged();
        }

        /*
         * Оновлює текст повідомлення у рядку стану термінала.
         */
        private void SetStatus(string message, bool isError)
        {
            IsErrorStatus = isError;
            StatusMessage = message;
        }

        /*
         * Парсить десяткове число з рядка незалежно від використаного роздільника (крапка або кома).
         */
        private static bool TryParseDecimal(string? input, out decimal result)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                result = 0m;
                return false;
            }

            string normalized = input.Trim().Replace(',', '.');
            return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
        }
    }
}
