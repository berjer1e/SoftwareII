/*
 * Файл: AuctionManagerViewModel.cs
 * Призначення: Головна робоча ViewModel ролі «Менеджер аукціонів» (Варіант 21).
 * Забезпечує повне керування життєвим циклом аукціонів (створення, редагування, верифікація запропонованих лотів,
 * деактивація без видалення з БД, пошук і сортування), керування каталогом товарів,
 * перегляд історії ставок та виконання торгових операцій із валідацією INotifyDataErrorInfo.
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
    public class AuctionManagerViewModel : ValidatableViewModelBase
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
        private AuctionDTO? _selectedPendingAuction;
        private ProductDTO? _selectedProduct;
        private ProductDTO? _selectedAuctionProduct;
        private ManufacturerDTO? _selectedManufacturer;
        private ProductTypeDTO? _selectedProductType;

        private string _searchTerm = string.Empty;
        private string _statusFilter = "ALL";
        private string _sortBy = "startdate";
        private bool _sortAscending;

        private string _startPriceInput = "100.00";
        private string _minBidInput = "0.01";
        private string _buyoutPriceInput = "220.00";
        private DateTime _startDateInput = DateTime.Now;
        private DateTime _endDateInput = DateTime.Now.AddDays(14);

        private string _productNameInput = string.Empty;
        private string _productPriceInput = "100.00";
        private string _productQuantityInput = "1";

        private string _newManufacturerNameInput = string.Empty;
        private string _newManufacturerCountryInput = "USA";
        private string _newProductTypeNameInput = string.Empty;

        private string _bidAmountInput = string.Empty;
        private string _statusMessage = "ТЕРМІНАЛ МЕНЕДЖЕРА АУКЦІОНІВ ГОТОВИЙ ДО РОБОТИ.";
        private bool _isErrorStatus;
        private bool _isPopulating;

        public ObservableCollection<AuctionDTO> Auctions { get; } = new();
        public ObservableCollection<AuctionDTO> PendingAuctions { get; } = new();
        public ObservableCollection<BidDTO> SelectedAuctionBids { get; } = new();
        public ObservableCollection<ProductDTO> Products { get; } = new();
        public ObservableCollection<ManufacturerDTO> Manufacturers { get; } = new();
        public ObservableCollection<ProductTypeDTO> ProductTypes { get; } = new();

        public ObservableCollection<string> StatusFilterOptions { get; } = new()
        {
            "ALL",
            "ACTIVE",
            "FINISHED",
            "PENDING"
        };

        public ObservableCollection<string> SortFieldOptions { get; } = new()
        {
            "startdate",
            "enddate",
            "productname",
            "startprice",
            "endprice"
        };

        public string OperatorHeader => _userSession.CurrentUser != null
            ? $"{_userSession.CurrentUser.Name} | {_userSession.CurrentUser.RoleName}"
            : "НЕАВТОРИЗОВАНИЙ ОПЕРАТОР";

        public decimal CurrentBalance => _userSession.CurrentUser?.Balance ?? 0m;

        public AuctionDTO? SelectedAuction
        {
            get => _selectedAuction;
            set
            {
                if (SetProperty(ref _selectedAuction, value))
                {
                    if (value != null && _selectedPendingAuction != null)
                    {
                        _selectedPendingAuction = null;
                        OnPropertyChanged(nameof(SelectedPendingAuction));
                    }
                    PopulateFromSelectedAuction(value);
                    LoadBidsForSelectedAuction(value);
                    RaiseAllCommandStates();
                }
            }
        }

        public AuctionDTO? SelectedPendingAuction
        {
            get => _selectedPendingAuction;
            set
            {
                if (SetProperty(ref _selectedPendingAuction, value))
                {
                    if (value != null && _selectedAuction != null)
                    {
                        _selectedAuction = null;
                        OnPropertyChanged(nameof(SelectedAuction));
                    }
                    if (value != null)
                    {
                        PopulateFromSelectedAuction(value);
                        LoadBidsForSelectedAuction(value);
                    }
                    RaiseAllCommandStates();
                }
            }
        }

        public ProductDTO? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (SetProperty(ref _selectedProduct, value))
                {
                    PopulateFromSelectedProduct(value);
                    RaiseAllCommandStates();
                }
            }
        }

        public ProductDTO? SelectedAuctionProduct
        {
            get => _selectedAuctionProduct;
            set
            {
                if (SetProperty(ref _selectedAuctionProduct, value))
                {
                    ValidateAuctionInputs();
                    RaiseAllCommandStates();
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
                    ValidateProductInputs();
                    RaiseAllCommandStates();
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
                    ValidateProductInputs();
                    RaiseAllCommandStates();
                }
            }
        }

        public string SearchTerm
        {
            get => _searchTerm;
            set => SetProperty(ref _searchTerm, value);
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set
            {
                if (SetProperty(ref _statusFilter, value))
                {
                    ApplyFilterAndSort();
                }
            }
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

        public string StartPriceInput
        {
            get => _startPriceInput;
            set
            {
                if (SetProperty(ref _startPriceInput, value))
                {
                    if (!_isPopulating && TryParseDecimal(value, out decimal startPrice) && startPrice > 0m)
                    {
                        BuyoutPriceInput = ((startPrice + 10m) * 2m).ToString("F2", CultureInfo.InvariantCulture);
                    }
                    if (!_isPopulating)
                    {
                        ValidateAuctionInputs();
                        RaiseAllCommandStates();
                    }
                }
            }
        }

        public string MinBidInput
        {
            get => _minBidInput;
            set
            {
                if (SetProperty(ref _minBidInput, value))
                {
                    ValidateAuctionInputs();
                    RaiseAllCommandStates();
                }
            }
        }

        public string BuyoutPriceInput
        {
            get => _buyoutPriceInput;
            set
            {
                if (SetProperty(ref _buyoutPriceInput, value))
                {
                    ValidateAuctionInputs();
                    RaiseAllCommandStates();
                }
            }
        }

        public DateTime StartDateInput
        {
            get => _startDateInput;
            set
            {
                if (SetProperty(ref _startDateInput, value))
                {
                    ValidateAuctionInputs();
                    RaiseAllCommandStates();
                }
            }
        }

        public DateTime EndDateInput
        {
            get => _endDateInput;
            set
            {
                if (SetProperty(ref _endDateInput, value))
                {
                    ValidateAuctionInputs();
                    RaiseAllCommandStates();
                }
            }
        }

        public string ProductNameInput
        {
            get => _productNameInput;
            set
            {
                if (SetProperty(ref _productNameInput, value))
                {
                    ValidateProductInputs();
                    RaiseAllCommandStates();
                }
            }
        }

        public string ProductPriceInput
        {
            get => _productPriceInput;
            set
            {
                if (SetProperty(ref _productPriceInput, value))
                {
                    ValidateProductInputs();
                    RaiseAllCommandStates();
                }
            }
        }

        public string ProductQuantityInput
        {
            get => _productQuantityInput;
            set
            {
                if (SetProperty(ref _productQuantityInput, value))
                {
                    ValidateProductInputs();
                    RaiseAllCommandStates();
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
                    ValidateNewManufacturerInputs();
                    RaiseAllCommandStates();
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
                    ValidateNewManufacturerInputs();
                    RaiseAllCommandStates();
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
                    ValidateNewProductTypeInput();
                    RaiseAllCommandStates();
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
                    RaiseAllCommandStates();
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
        public RelayCommand CreateAuctionCommand { get; }
        public RelayCommand UpdateAuctionCommand { get; }
        public RelayCommand DeactivateAuctionCommand { get; }
        public RelayCommand VerifyAuctionCommand { get; }
        public RelayCommand CreateProductCommand { get; }
        public RelayCommand UpdateProductCommand { get; }
        public RelayCommand DeleteProductCommand { get; }
        public RelayCommand CreateManufacturerCommand { get; }
        public RelayCommand DeleteManufacturerCommand { get; }
        public RelayCommand CreateProductTypeCommand { get; }
        public RelayCommand DeleteProductTypeCommand { get; }
        public RelayCommand PlaceBidCommand { get; }
        public RelayCommand BuyoutCommand { get; }
        public RelayCommand ResetFormCommand { get; }
        public RelayCommand LogoutCommand { get; }

        /*
         * Ініціалізує ViewModel Менеджера аукціонів усіма сервісами бізнес-логіки та налаштовує команди керування.
         */
        public AuctionManagerViewModel(
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
            CreateAuctionCommand = new RelayCommand(ExecuteCreateAuction, CanExecuteCreateAuction);
            UpdateAuctionCommand = new RelayCommand(ExecuteUpdateAuction, CanExecuteUpdateAuction);
            DeactivateAuctionCommand = new RelayCommand(ExecuteDeactivateAuction, CanExecuteDeactivateAuction);
            VerifyAuctionCommand = new RelayCommand(ExecuteVerifyAuction, CanExecuteVerifyAuction);
            CreateProductCommand = new RelayCommand(ExecuteCreateProduct, CanExecuteCreateProduct);
            UpdateProductCommand = new RelayCommand(ExecuteUpdateProduct, CanExecuteUpdateProduct);
            DeleteProductCommand = new RelayCommand(ExecuteDeleteProduct, () => SelectedProduct != null);
            CreateManufacturerCommand = new RelayCommand(ExecuteCreateManufacturer, CanExecuteCreateManufacturer);
            DeleteManufacturerCommand = new RelayCommand(ExecuteDeleteManufacturer, () => SelectedManufacturer != null);
            CreateProductTypeCommand = new RelayCommand(ExecuteCreateProductType, CanExecuteCreateProductType);
            DeleteProductTypeCommand = new RelayCommand(ExecuteDeleteProductType, () => SelectedProductType != null);
            PlaceBidCommand = new RelayCommand(ExecutePlaceBid, CanExecutePlaceBid);
            BuyoutCommand = new RelayCommand(ExecuteBuyout, CanExecuteBuyout);
            ResetFormCommand = new RelayCommand(ResetForms);
            LogoutCommand = new RelayCommand(ExecuteLogout);
        }

        /*
         * Перевіряє права доступу ролі Менеджера аукціонів та завантажує всі довідники, лоти й заявки на верифікацію.
         */
        public override void Initialize()
        {
            if (!_userSession.CanManageAuctions)
            {
                SetStatus("ВІДМОВА У ДОСТУПІ: ПОТРІБНА РОЛЬ МЕНЕДЖЕРА АУКЦІОНІВ.", isError: true);
                _viewHandler.NavigateTo<LoginViewModel>();
                return;
            }

            ClearAllErrors();
            LoadAllData();
        }

        /*
         * Завантажує актуальні списки товарів, виробників, категорій, аукціонів та непідтверджених лотів.
         */
        public void LoadAllData()
        {
            try
            {
                ReloadCatalogData();
                ApplyFilterAndSort();
                ReloadPendingAuctions();
                OnPropertyChanged(nameof(OperatorHeader));
                OnPropertyChanged(nameof(CurrentBalance));
                SetStatus($"ДАНІ ОНОВЛЕНО: {Auctions.Count} ЛОТІВ, {PendingAuctions.Count} ОЧІКУЮТЬ ВЕРИФІКАЦІЇ.", isError: false);
            }
            catch (Exception ex)
            {
                SetStatus($"ПОМИЛКА ЗАВАНТАЖЕННЯ ДАНИХ: {ex.Message}", isError: true);
            }
        }

        /*
         * Застосовує поточні параметри пошуку, фільтрації за статусом та сортування до списку аукціонів.
         */
        public void ApplyFilterAndSort()
        {
            try
            {
                var items = _StatusFilterSwitch();
                Auctions.Clear();
                foreach (var item in items)
                {
                    Auctions.Add(item);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"ПОМИЛКА ФІЛЬТРАЦІЇ ЛОТІВ: {ex.Message}", isError: true);
            }
        }

        /*
         * Створює новий активний і верифікований аукціон на основі введених у форму параметрів.
         */
        public void ExecuteCreateAuction()
        {
            ValidateAuctionInputs();
            if (HasAuctionInputErrors() || SelectedAuctionProduct == null)
            {
                SetStatus("ПЕРЕВІРТЕ ПОЛЯ СТВОРЕННЯ АУКЦІОНУ.", isError: true);
                return;
            }

            try
            {
                TryParseDecimal(StartPriceInput, out decimal startPrice);
                TryParseDecimal(MinBidInput, out decimal minBid);
                TryParseDecimal(BuyoutPriceInput, out decimal buyoutPrice);

                var dto = new AuctionDTO
                {
                    ProductId = SelectedAuctionProduct.Id,
                    StartPrice = startPrice,
                    MinBid = minBid > 0m ? minBid : 0.01m,
                    BuyoutPrice = buyoutPrice > 1m ? buyoutPrice : (startPrice + 10m) * 2m,
                    StartDate = StartDateInput,
                    EndDate = EndDateInput
                };

                var created = _auctionService.CreateAuction(dto);
                ApplyFilterAndSort();
                SelectedAuction = Auctions.FirstOrDefault(a => a.Id == created.Id);
                SetStatus($"СТУДІЯ ТОРГІВ: СТВОРЕНО АУКЦІОН [{created.ProductName}] СТАРТ {created.StartPrice:F2} $.", isError: false);
            }
            catch (BusinessRuleException ex)
            {
                SetStatus(ex.Message, isError: true);
            }
        }

        /*
         * Зберігає зміни у вибраному аукціоні через сервіс бізнес-логіки.
         */
        public void ExecuteUpdateAuction()
        {
            var target = SelectedAuction ?? SelectedPendingAuction;
            if (target == null)
            {
                return;
            }

            ValidateAuctionInputs();
            if (HasAuctionInputErrors() || SelectedAuctionProduct == null)
            {
                SetStatus("ПЕРЕВІРТЕ КОРЕКТНІСТЬ ПОЛІВ РЕДАГУВАННЯ АУКЦІОНУ.", isError: true);
                return;
            }

            try
            {
                TryParseDecimal(StartPriceInput, out decimal startPrice);
                TryParseDecimal(MinBidInput, out decimal minBid);
                TryParseDecimal(BuyoutPriceInput, out decimal buyoutPrice);

                target.ProductId = SelectedAuctionProduct.Id;
                target.StartPrice = startPrice;
                target.MinBid = minBid;
                target.BuyoutPrice = buyoutPrice;
                target.StartDate = StartDateInput;
                target.EndDate = EndDateInput;

                var updated = _auctionService.UpdateAuction(target);
                ApplyFilterAndSort();
                ReloadPendingAuctions();
                SelectedAuction = Auctions.FirstOrDefault(a => a.Id == updated.Id);
                SetStatus($"ОНОВЛЕНО ПАРАМЕТРИ АУКЦІОНУ [{updated.Id.ToString()[..8]}].", isError: false);
            }
            catch (BusinessRuleException ex)
            {
                SetStatus(ex.Message, isError: true);
            }
        }

        /*
         * Деактивує вибраний аукціон без видалення запису з бази даних.
         */
        public void ExecuteDeactivateAuction()
        {
            var target = SelectedPendingAuction ?? SelectedAuction;
            if (target == null)
            {
                return;
            }

            try
            {
                _auctionService.DeactivateAuction(target.Id);
                ApplyFilterAndSort();
                ReloadPendingAuctions();
                SetStatus($"АУКЦІОН [{target.Id.ToString()[..8]}] ДЕАКТИВОВАНО (БЕЗ ВИДАЛЕННЯ З БД).", isError: false);
            }
            catch (BusinessRuleException ex)
            {
                SetStatus(ex.Message, isError: true);
            }
        }

        /*
         * Підтверджує запропонований користувачем лот і переводить його в активні торги.
         */
        public void ExecuteVerifyAuction()
        {
            var target = SelectedPendingAuction ?? SelectedAuction;
            if (target == null)
            {
                return;
            }

            try
            {
                _auctionService.VerifyAuction(target.Id);
                ReloadPendingAuctions();
                ApplyFilterAndSort();
                SetStatus($"ЛОТ [{target.ProductName}] УСПІШНО ПІДТВЕРДЖЕНО ТА АКТИВОВАНО ДЛЯ ТОРГІВ.", isError: false);
            }
            catch (BusinessRuleException ex)
            {
                SetStatus(ex.Message, isError: true);
            }
        }

        /*
         * Створює новий товар у каталозі після валідації назви, ціни, кількості, виробника та категорії.
         */
        public void ExecuteCreateProduct()
        {
            ValidateProductInputs();
            if (HasProductInputErrors() || SelectedManufacturer == null || SelectedProductType == null)
            {
                SetStatus("ПЕРЕВІРТЕ ПОЛЯ ФОРМИ ТОВАРУ ПЕРЕД ДОДАВАННЯМ.", isError: true);
                return;
            }

            try
            {
                TryParseDecimal(ProductPriceInput, out decimal price);
                int.TryParse(ProductQuantityInput.Trim(), out int qty);

                var dto = new ProductDTO
                {
                    Name = ProductNameInput.Trim(),
                    Price = price,
                    Quantity = qty,
                    ManufacturerId = SelectedManufacturer.Id,
                    ProductTypeId = SelectedProductType.Id
                };

                var created = _productService.CreateProduct(dto);
                ReloadCatalogData();
                SelectedProduct = Products.FirstOrDefault(p => p.Id == created.Id);
                SelectedAuctionProduct = SelectedProduct;
                SetStatus($"ДОДАНО НОВИЙ ТОВАР У КАТАЛОГ: [{created.Name}].", isError: false);
            }
            catch (BusinessRuleException ex)
            {
                SetStatus(ex.Message, isError: true);
            }
        }

        /*
         * Оновлює вибраний товар у каталозі.
         */
        public void ExecuteUpdateProduct()
        {
            if (SelectedProduct == null)
            {
                return;
            }

            ValidateProductInputs();
            if (HasProductInputErrors() || SelectedManufacturer == null || SelectedProductType == null)
            {
                SetStatus("ПЕРЕВІРТЕ ПОЛЯ ФОРМИ ТОВАРУ ПЕРЕД ОНОВЛЕННЯМ.", isError: true);
                return;
            }

            try
            {
                TryParseDecimal(ProductPriceInput, out decimal price);
                int.TryParse(ProductQuantityInput.Trim(), out int qty);

                SelectedProduct.Name = ProductNameInput.Trim();
                SelectedProduct.Price = price;
                SelectedProduct.Quantity = qty;
                SelectedProduct.ManufacturerId = SelectedManufacturer.Id;
                SelectedProduct.ProductTypeId = SelectedProductType.Id;

                var updated = _productService.UpdateProduct(SelectedProduct);
                ReloadCatalogData();
                ApplyFilterAndSort();
                SetStatus($"ОНОВЛЕНО ТОВАР КАТАЛОГУ: [{updated.Name}].", isError: false);
            }
            catch (BusinessRuleException ex)
            {
                SetStatus(ex.Message, isError: true);
            }
        }

        /*
         * Видаляє вибраний товар із каталогу, якщо він не задіяний в існуючих аукціонах.
         */
        public void ExecuteDeleteProduct()
        {
            if (SelectedProduct == null)
            {
                return;
            }

            try
            {
                string deletedName = SelectedProduct.Name;
                _productService.DeleteProduct(SelectedProduct.Id);
                ReloadCatalogData();
                SetStatus($"ВИДАЛЕНО ТОВАР ІЗ КАТАЛОГУ: [{deletedName}].", isError: false);
            }
            catch (BusinessRuleException ex)
            {
                SetStatus(ex.Message, isError: true);
            }
        }

        /*
         * Додає нового виробника до довідника після перевірки заповнення назви та країни походження.
         */
        public void ExecuteCreateManufacturer()
        {
            ValidateNewManufacturerInputs();
            if (GetPropertyErrors(nameof(NewManufacturerNameInput)).Count > 0
                || GetPropertyErrors(nameof(NewManufacturerCountryInput)).Count > 0)
            {
                SetStatus("ВКАЖІТЬ КОРЕКТНУ НАЗВУ ТА КРАЇНУ ВИРОБНИКА.", isError: true);
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
                ClearErrors(nameof(NewManufacturerNameInput));
                ReloadCatalogData();
                SelectedManufacturer = Manufacturers.FirstOrDefault(m => m.Id == created.Id);
                SetStatus($"ДОДАНО НОВОГО ВИРОБНИКА: [{created.Name} ({created.Country})].", isError: false);
            }
            catch (BusinessRuleException ex)
            {
                SetStatus(ex.Message, isError: true);
            }
        }

        /*
         * Видаляє вибраного виробника з довідника, якщо він не використовується в існуючих товарах.
         */
        public void ExecuteDeleteManufacturer()
        {
            if (SelectedManufacturer == null)
            {
                return;
            }

            string name = SelectedManufacturer.Name;
            if (_manufacturerService.Delete(SelectedManufacturer.Id))
            {
                SelectedManufacturer = null;
                ReloadCatalogData();
                SetStatus($"ВИДАЛЕНО ВИРОБНИКА З ДОВІДНИКА: [{name}].", isError: false);
            }
            else
            {
                SetStatus($"НЕМОЖЛИВО ВИДАЛИТИ ВИРОБНИКА [{name}]: ВІН ПРИВ'ЯЗАНИЙ ДО ТОВАРІВ.", isError: true);
            }
        }

        /*
         * Додає нову категорію (тип) товарів до довідника після перевірки назви.
         */
        public void ExecuteCreateProductType()
        {
            ValidateNewProductTypeInput();
            if (GetPropertyErrors(nameof(NewProductTypeNameInput)).Count > 0)
            {
                SetStatus("ВКАЖІТЬ КОРЕКТНУ НАЗВУ НОВОЇ КАТЕГОРІЇ.", isError: true);
                return;
            }

            try
            {
                var created = _productTypeService.Create(new ProductTypeDTO
                {
                    Name = NewProductTypeNameInput.Trim()
                });

                NewProductTypeNameInput = string.Empty;
                ClearErrors(nameof(NewProductTypeNameInput));
                ReloadCatalogData();
                SelectedProductType = ProductTypes.FirstOrDefault(pt => pt.Id == created.Id);
                SetStatus($"ДОДАНО НОВУ КАТЕГОРІЮ ТОВАРІВ: [{created.Name}].", isError: false);
            }
            catch (BusinessRuleException ex)
            {
                SetStatus(ex.Message, isError: true);
            }
        }

        /*
         * Видаляє вибрану категорію з довідника, якщо вона не прив'язана до жодного товару.
         */
        public void ExecuteDeleteProductType()
        {
            if (SelectedProductType == null)
            {
                return;
            }

            string name = SelectedProductType.Name;
            if (_productTypeService.Delete(SelectedProductType.Id))
            {
                SelectedProductType = null;
                ReloadCatalogData();
                SetStatus($"ВИДАЛЕНО КАТЕГОРІЮ З ДОВІДНИКА: [{name}].", isError: false);
            }
            else
            {
                SetStatus($"НЕМОЖЛИВО ВИДАЛИТИ КАТЕГОРІЮ [{name}]: ВОНА ВИКОРИСТОВУЄТЬСЯ В ТОВАРАХ.", isError: true);
            }
        }

        /*
         * Подає нову ставку на вибраний активний аукціон та оновлює баланс і таблицю ставок.
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
                SelectedAuction = Auctions.FirstOrDefault(a => a.Id == auctionId);
                OnPropertyChanged(nameof(CurrentBalance));
                SetStatus($"СТАВКУ {amount:F2} $ УСПІШНО ЗАРЕЄСТРОВАНО.", isError: false);
            }
            catch (BusinessRuleException ex)
            {
                SetStatus(ex.Message, isError: true);
            }
        }

        /*
         * Виконує миттєвий викуп вибраного активного лота за ціною BuyoutPrice.
         */
        public void ExecuteBuyout()
        {
            if (SelectedAuction == null)
            {
                return;
            }

            try
            {
                Guid auctionId = SelectedAuction.Id;
                var bid = _bidService.Buyout(auctionId);
                ApplyFilterAndSort();
                SelectedAuction = Auctions.FirstOrDefault(a => a.Id == auctionId);
                OnPropertyChanged(nameof(CurrentBalance));
                SetStatus($"МИТТЄВИЙ ВИКУП ВИКОНАНО ЗА {bid.Price:F2} $! АУКЦІОН ЗАКРИТО.", isError: false);
            }
            catch (BusinessRuleException ex)
            {
                SetStatus(ex.Message, isError: true);
            }
        }

        /*
         * Скидає поля форм створення аукціону та товару до початкових значень.
         */
        public void ResetForms()
        {
            SelectedAuction = null;
            SelectedPendingAuction = null;
            SelectedProduct = null;
            StartPriceInput = "100.00";
            MinBidInput = "0.01";
            BuyoutPriceInput = "220.00";
            StartDateInput = DateTime.Now;
            EndDateInput = DateTime.Now.AddDays(14);
            ProductNameInput = string.Empty;
            ProductPriceInput = "100.00";
            ProductQuantityInput = "1";
            NewManufacturerNameInput = string.Empty;
            NewManufacturerCountryInput = "USA";
            NewProductTypeNameInput = string.Empty;
            BidAmountInput = string.Empty;
            ClearAllErrors();
            SetStatus("ФОРМИ ОЧИЩЕНО ТА ПІДГОТОВЛЕНО ДЛЯ НОВОГО ВВЕДЕННЯ.", isError: false);
        }

        /*
         * Завершує сесію менеджера та повертає програму на екран авторизації.
         */
        public void ExecuteLogout()
        {
            _authService.Logout();
            _viewHandler.NavigateTo<LoginViewModel>();
        }

        /*
         * Повертає відфільтрований і відсортований список аукціонів відповідно до обраного режиму StatusFilter.
         */
        private System.Collections.Generic.List<AuctionDTO> _StatusFilterSwitch()
        {
            if (StatusFilter == "PENDING")
            {
                var pending = _auctionService.GetPendingVerification();
                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    string term = SearchTerm.Trim();
                    pending = pending
                        .Where(a => a.ProductName != null && a.ProductName.Contains(term, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
                return pending;
            }

            return StatusFilter switch
            {
                "ACTIVE" => _auctionService.SearchAndSort(SearchTerm, true, SortBy, SortAscending),
                "FINISHED" => _auctionService.SearchAndSort(SearchTerm, false, SortBy, SortAscending),
                _ => _auctionService.SearchAndSort(SearchTerm, null, SortBy, SortAscending)
            };
        }

        /*
         * Оновлює колекції каталогу товарів, виробників і типів товарів із сервісів бізнес-логіки.
         */
        private void ReloadCatalogData()
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

            Products.Clear();
            foreach (var p in _productService.GetAll())
            {
                Products.Add(p);
            }

            SelectedManufacturer ??= Manufacturers.FirstOrDefault();
            SelectedProductType ??= ProductTypes.FirstOrDefault();
            SelectedAuctionProduct ??= Products.FirstOrDefault();
        }

        /*
         * Перезавантажує чергу запропонованих користувачами лотів, які очікують верифікації менеджером.
         */
        private void ReloadPendingAuctions()
        {
            PendingAuctions.Clear();
            foreach (var pending in _auctionService.GetPendingVerification())
            {
                PendingAuctions.Add(pending);
            }
        }

        /*
         * Завантажує історію ставок для вибраного в таблиці аукціону.
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
         * Заповнює поля редагування аукціону та рекомендованої ставки даними вибраного лота.
         */
        private void PopulateFromSelectedAuction(AuctionDTO? auction)
        {
            if (auction == null)
            {
                return;
            }

            try
            {
                _isPopulating = true;
                SelectedAuctionProduct = Products.FirstOrDefault(p => p.Id == auction.ProductId) ?? SelectedAuctionProduct;
                StartPriceInput = auction.StartPrice.ToString("F2", CultureInfo.InvariantCulture);
                MinBidInput = auction.MinBid.ToString("F2", CultureInfo.InvariantCulture);
                BuyoutPriceInput = auction.BuyoutPrice.ToString("F2", CultureInfo.InvariantCulture);
                StartDateInput = auction.StartDate;
                EndDateInput = auction.EndDate;
                BidAmountInput = auction.MinNextBid.ToString("F2", CultureInfo.InvariantCulture);
            }
            finally
            {
                _isPopulating = false;
            }

            ValidateAuctionInputs();
            ValidateBidInput();
        }

        /*
         * Заповнює поля редагування товару даними вибраного запису з каталогу товарів.
         */
        private void PopulateFromSelectedProduct(ProductDTO? product)
        {
            if (product == null)
            {
                return;
            }

            ProductNameInput = product.Name;
            ProductPriceInput = product.Price.ToString("F2", CultureInfo.InvariantCulture);
            ProductQuantityInput = product.Quantity.ToString(CultureInfo.InvariantCulture);
            SelectedManufacturer = Manufacturers.FirstOrDefault(m => m.Id == product.ManufacturerId);
            SelectedProductType = ProductTypes.FirstOrDefault(pt => pt.Id == product.ProductTypeId);
        }

        /*
         * Перевіряє валідність полів форми створення або редагування аукціону через INotifyDataErrorInfo.
         */
        private void ValidateAuctionInputs()
        {
            ClearErrors(nameof(StartPriceInput));
            ClearErrors(nameof(MinBidInput));
            ClearErrors(nameof(BuyoutPriceInput));
            ClearErrors(nameof(EndDateInput));

            bool hasValidStartPrice = TryParseDecimal(StartPriceInput, out decimal startPrice) && startPrice > 0m;
            if (!hasValidStartPrice)
            {
                AddError(nameof(StartPriceInput), "Стартова ціна повинна бути додатним числом.");
            }

            if (!TryParseDecimal(MinBidInput, out decimal minBid) || minBid <= 0m)
            {
                AddError(nameof(MinBidInput), "Мінімальний крок ставки повинен бути додатним числом (наприклад, 0.01).");
            }

            if (!string.IsNullOrWhiteSpace(BuyoutPriceInput))
            {
                if (!TryParseDecimal(BuyoutPriceInput, out decimal buyout) || buyout <= 0m)
                {
                    AddError(nameof(BuyoutPriceInput), "Ціна миттєвого викупу повинна бути додатним числом.");
                }
                else if (hasValidStartPrice && buyout > 1m && buyout < startPrice)
                {
                    AddError(nameof(BuyoutPriceInput), "Ціна викупу не може бути меншою за стартову ціну.");
                }
            }

            if (EndDateInput <= StartDateInput)
            {
                AddError(nameof(EndDateInput), "Дата завершення повинна бути пізнішою за дату початку.");
            }
        }

        /*
         * Перевіряє валідність полів форми створення або редагування товару через INotifyDataErrorInfo.
         */
        private void ValidateProductInputs()
        {
            ClearErrors(nameof(ProductNameInput));
            ClearErrors(nameof(ProductPriceInput));
            ClearErrors(nameof(ProductQuantityInput));

            if (string.IsNullOrWhiteSpace(ProductNameInput))
            {
                AddError(nameof(ProductNameInput), "Назва товару не може бути порожньою.");
            }
            else if (ProductNameInput.Trim().Length > 50)
            {
                AddError(nameof(ProductNameInput), "Назва товару не повинна перевищувати 50 символів.");
            }

            if (!TryParseDecimal(ProductPriceInput, out decimal price) || price <= 0m)
            {
                AddError(nameof(ProductPriceInput), "Базова ціна товару повинна бути більшою за 0.");
            }

            if (!int.TryParse(ProductQuantityInput?.Trim(), out int quantity) || quantity <= 0)
            {
                AddError(nameof(ProductQuantityInput), "Кількість товару повинна бути цілим додатним числом.");
            }
        }

        /*
         * Перевіряє коректність введеної суми ставки відносно мінімального кроку вибраного аукціону.
         */
        private void ValidateBidInput()
        {
            ClearErrors(nameof(BidAmountInput));
            if (string.IsNullOrWhiteSpace(BidAmountInput))
            {
                return;
            }

            if (!TryParseDecimal(BidAmountInput, out decimal bidAmount) || bidAmount <= 0m)
            {
                AddError(nameof(BidAmountInput), "Сума ставки повинна бути додатним числом.");
                return;
            }

            if (SelectedAuction != null && bidAmount < SelectedAuction.MinNextBid)
            {
                AddError(nameof(BidAmountInput), $"Мінімальна допустима ставка: {SelectedAuction.MinNextBid:F2} $.");
            }
        }

        /*
         * Перевіряє коректність назви та країни для створення нового виробника.
         */
        private void ValidateNewManufacturerInputs()
        {
            ClearErrors(nameof(NewManufacturerNameInput));
            ClearErrors(nameof(NewManufacturerCountryInput));

            if (string.IsNullOrWhiteSpace(NewManufacturerNameInput))
            {
                AddError(nameof(NewManufacturerNameInput), "Назва нового виробника не може бути порожньою.");
            }
            else if (NewManufacturerNameInput.Trim().Length > 50)
            {
                AddError(nameof(NewManufacturerNameInput), "Назва виробника не може перевищувати 50 символів.");
            }

            if (string.IsNullOrWhiteSpace(NewManufacturerCountryInput))
            {
                AddError(nameof(NewManufacturerCountryInput), "Країна виробника є обов'язковою.");
            }
        }

        /*
         * Перевіряє коректність назви для створення нової категорії товарів.
         */
        private void ValidateNewProductTypeInput()
        {
            ClearErrors(nameof(NewProductTypeNameInput));

            if (string.IsNullOrWhiteSpace(NewProductTypeNameInput))
            {
                AddError(nameof(NewProductTypeNameInput), "Назва нової категорії не може бути порожньою.");
            }
            else if (NewProductTypeNameInput.Trim().Length > 50)
            {
                AddError(nameof(NewProductTypeNameInput), "Назва категорії не може перевищувати 50 символів.");
            }
        }

        /*
         * Перевіряє, чи є помилки валідації у полях форми аукціону.
         */
        private bool HasAuctionInputErrors()
        {
            return GetPropertyErrors(nameof(StartPriceInput)).Count > 0
                || GetPropertyErrors(nameof(MinBidInput)).Count > 0
                || GetPropertyErrors(nameof(BuyoutPriceInput)).Count > 0
                || GetPropertyErrors(nameof(EndDateInput)).Count > 0;
        }

        /*
         * Перевіряє, чи є помилки валідації у полях форми товару.
         */
        private bool HasProductInputErrors()
        {
            return GetPropertyErrors(nameof(ProductNameInput)).Count > 0
                || GetPropertyErrors(nameof(ProductPriceInput)).Count > 0
                || GetPropertyErrors(nameof(ProductQuantityInput)).Count > 0;
        }

        /*
         * Визначає доступність команди створення нового аукціону.
         */
        private bool CanExecuteCreateAuction()
        {
            return SelectedAuctionProduct != null && !HasAuctionInputErrors();
        }

        /*
         * Визначає доступність команди оновлення існуючого аукціону.
         */
        private bool CanExecuteUpdateAuction()
        {
            var target = SelectedAuction ?? SelectedPendingAuction;
            return target != null && SelectedAuctionProduct != null && !HasAuctionInputErrors();
        }

        /*
         * Визначає доступність команди деактивації вибраного аукціону або відхилення заявки на верифікацію.
         */
        private bool CanExecuteDeactivateAuction()
        {
            var target = SelectedPendingAuction ?? SelectedAuction;
            return target != null && (target.IsActive || !target.IsVerified);
        }

        /*
         * Визначає доступність команди підтвердження (верифікації) запропонованого лота.
         */
        private bool CanExecuteVerifyAuction()
        {
            var target = SelectedPendingAuction ?? SelectedAuction;
            return target != null && !target.IsVerified;
        }

        /*
         * Визначає доступність команди створення нового товару.
         */
        private bool CanExecuteCreateProduct()
        {
            return !string.IsNullOrWhiteSpace(ProductNameInput)
                && SelectedManufacturer != null
                && SelectedProductType != null
                && !HasProductInputErrors();
        }

        /*
         * Визначає доступність команди редагування вибраного товару.
         */
        private bool CanExecuteUpdateProduct()
        {
            return SelectedProduct != null && CanExecuteCreateProduct();
        }

        /*
         * Визначає доступність команди додавання нового виробника до довідника.
         */
        private bool CanExecuteCreateManufacturer()
        {
            return !string.IsNullOrWhiteSpace(NewManufacturerNameInput)
                && !string.IsNullOrWhiteSpace(NewManufacturerCountryInput)
                && GetPropertyErrors(nameof(NewManufacturerNameInput)).Count == 0
                && GetPropertyErrors(nameof(NewManufacturerCountryInput)).Count == 0;
        }

        /*
         * Визначає доступність команди додавання нової категорії товарів до довідника.
         */
        private bool CanExecuteCreateProductType()
        {
            return !string.IsNullOrWhiteSpace(NewProductTypeNameInput)
                && GetPropertyErrors(nameof(NewProductTypeNameInput)).Count == 0;
        }

        /*
         * Визначає доступність команди подання ставки на вибраний активний лот.
         */
        private bool CanExecutePlaceBid()
        {
            return SelectedAuction != null
                && SelectedAuction.IsActive
                && SelectedAuction.IsVerified
                && !string.IsNullOrWhiteSpace(BidAmountInput)
                && GetPropertyErrors(nameof(BidAmountInput)).Count == 0;
        }

        /*
         * Визначає доступність команди миттєвого викупу вибраного активного лота.
         */
        private bool CanExecuteBuyout()
        {
            return SelectedAuction != null
                && SelectedAuction.IsActive
                && SelectedAuction.IsVerified
                && SelectedAuction.BuyoutPrice > 0m;
        }

        /*
         * Оновлює стан активності всіх команд панелі Менеджера аукціонів.
         */
        private void RaiseAllCommandStates()
        {
            CreateAuctionCommand.RaiseCanExecuteChanged();
            UpdateAuctionCommand.RaiseCanExecuteChanged();
            DeactivateAuctionCommand.RaiseCanExecuteChanged();
            VerifyAuctionCommand.RaiseCanExecuteChanged();
            CreateProductCommand.RaiseCanExecuteChanged();
            UpdateProductCommand.RaiseCanExecuteChanged();
            DeleteProductCommand.RaiseCanExecuteChanged();
            CreateManufacturerCommand.RaiseCanExecuteChanged();
            DeleteManufacturerCommand.RaiseCanExecuteChanged();
            CreateProductTypeCommand.RaiseCanExecuteChanged();
            DeleteProductTypeCommand.RaiseCanExecuteChanged();
            PlaceBidCommand.RaiseCanExecuteChanged();
            BuyoutCommand.RaiseCanExecuteChanged();
        }

        /*
         * Встановлює текст рядка стану термінала та прапорець помилки.
         */
        private void SetStatus(string message, bool isError)
        {
            IsErrorStatus = isError;
            StatusMessage = message;
        }

        /*
         * Розпізнає десяткове число з рядка з підтримкою як крапки, так і коми як роздільника.
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
