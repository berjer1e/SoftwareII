/*
 * Файл: ViewModelAndNavigationTests.cs
 * Призначення: Модульні тести для рівня WPF MVVM: навігатора ViewHandler, LoginViewModel,
 * AuctionManagerViewModel, TraderTerminalViewModel (включно з валідацією INotifyDataErrorInfo та командами ICommand),
 * а також перевірки графа Dependency Injection (App.ConfigureServices з ValidateOnBuild = true).
 */

using System;
using System.Collections.Generic;
using CompanyDataTransferObject;
using CompanyDataTransferObject.Constants;
using CompanyServiceLayer.Services;
using CompanyServiceLayer.Session;
using CompanyWPFInterface;
using CompanyWPFInterface.Infrastructure;
using CompanyWPFInterface.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    [Category("MVVM")]
    public class ViewModelAndNavigationTests
    {
        private Mock<IAuthService> _authServiceMock = null!;
        private Mock<IAuctionService> _auctionServiceMock = null!;
        private Mock<IBidService> _bidServiceMock = null!;
        private Mock<IProductService> _productServiceMock = null!;
        private Mock<IManufacturerService> _manufacturerServiceMock = null!;
        private Mock<IProductTypeService> _productTypeServiceMock = null!;
        private Mock<IViewHandler> _viewHandlerMock = null!;
        private IUserSession _userSession = null!;

        /*
         * Ініціалізує мок-об'єкти всіх сервісів перед кожним тестом ViewModel.
         */
        [SetUp]
        public void SetUp()
        {
            _authServiceMock = new Mock<IAuthService>();
            _auctionServiceMock = new Mock<IAuctionService>();
            _bidServiceMock = new Mock<IBidService>();
            _productServiceMock = new Mock<IProductService>();
            _manufacturerServiceMock = new Mock<IManufacturerService>();
            _productTypeServiceMock = new Mock<IProductTypeService>();
            _viewHandlerMock = new Mock<IViewHandler>();
            _userSession = new UserSession();

            _auctionServiceMock.Setup(s => s.SearchAndSort(It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<bool>()))
                .Returns(new List<AuctionDTO>());
            _auctionServiceMock.Setup(s => s.GetPendingVerification())
                .Returns(new List<AuctionDTO>());
            _productServiceMock.Setup(s => s.GetAll())
                .Returns(new List<ProductDTO> { new() { Id = Guid.NewGuid(), Name = "Test Lot", Price = 100m, Quantity = 1 } });
            _manufacturerServiceMock.Setup(s => s.GetAll())
                .Returns(new List<ManufacturerDTO> { new() { Id = Guid.NewGuid(), Name = "IBM", Country = "USA" } });
            _productTypeServiceMock.Setup(s => s.GetAll())
                .Returns(new List<ProductTypeDTO> { new() { Id = Guid.NewGuid(), Name = "Terminal" } });
        }

        /*
         * Перевіряє, що DI-контейнер WPF-програми успішно збирає та валідує всі залежності з ValidateOnBuild = true.
         */
        [Test]
        [Description("Перевірка побудови DI-контейнера App.ConfigureServices із ValidateOnBuild = true")]
        public void DependencyInjection_ConfigureServices_ValidatesAllRegistrationsOnBuild()
        {
            using ServiceProvider provider = App.ConfigureServices();

            var viewHandler = provider.GetRequiredService<IViewHandler>();
            var loginVm = provider.GetRequiredService<LoginViewModel>();
            var managerVm = provider.GetRequiredService<AuctionManagerViewModel>();
            var traderVm = provider.GetRequiredService<TraderTerminalViewModel>();

            Assert.Multiple(() =>
            {
                Assert.That(viewHandler, Is.Not.Null);
                Assert.That(loginVm, Is.Not.Null);
                Assert.That(managerVm, Is.Not.Null);
                Assert.That(traderVm, Is.Not.Null);
            });
        }

        /*
         * Перевіряє валідацію INotifyDataErrorInfo у LoginViewModel та маршрутизацію через ViewHandler
         * на AuctionManagerViewModel для Менеджера і на TraderTerminalViewModel для Покупця.
         */
        [Test]
        public void LoginViewModel_ValidationAndRoleRouting_NavigatesToCorrectTerminal()
        {
            var vm = new LoginViewModel(_authServiceMock.Object, _userSession, _viewHandlerMock.Object);

            vm.LoginOrEmail = "";
            vm.Password = "12";

            Assert.Multiple(() =>
            {
                Assert.That(vm.HasErrors, Is.True);
                Assert.That(vm.LoginCommand.CanExecute(null), Is.False);
            });

            var manager = new UserDTO
            {
                Id = Guid.NewGuid(),
                Name = "Sarah Connor",
                Email = "sarah@co.uk",
                RoleId = AppRoles.AuctionManagerRoleId,
                IsActive = true
            };

            _authServiceMock.Setup(a => a.Login("sarah@co.uk", "Password123!"))
                .Callback(() => _userSession.SignIn(manager))
                .Returns(manager);

            vm.FillManagerProfileCommand.Execute(null);
            Assert.That(vm.HasErrors, Is.False);
            Assert.That(vm.LoginCommand.CanExecute(null), Is.True);

            vm.LoginCommand.Execute(null);
            _viewHandlerMock.Verify(v => v.NavigateTo<AuctionManagerViewModel>(), Times.Once);

            var buyer = new UserDTO
            {
                Id = Guid.NewGuid(),
                Name = "Mia Wallace",
                Email = "mia@m.co",
                RoleId = AppRoles.BuyerRoleId,
                IsActive = true
            };

            _authServiceMock.Setup(a => a.Login("mia@m.co", "Password123!"))
                .Callback(() => _userSession.SignIn(buyer))
                .Returns(buyer);

            vm.FillTraderProfileCommand.Execute(null);
            vm.LoginCommand.Execute(null);
            _viewHandlerMock.Verify(v => v.NavigateTo<TraderTerminalViewModel>(), Times.Once);
        }

        /*
         * Перевіряє валідацію числових і текстових полів у AuctionManagerViewModel через INotifyDataErrorInfo
         * та блокування команд при некоректному введенні.
         */
        [Test]
        public void AuctionManagerViewModel_InvalidInputs_TriggerValidationErrorsAndDisableCommands()
        {
            _userSession.SignIn(new UserDTO
            {
                Id = Guid.NewGuid(),
                Name = "Sarah Connor",
                RoleId = AppRoles.AuctionManagerRoleId,
                IsActive = true
            });

            var vm = new AuctionManagerViewModel(
                _auctionServiceMock.Object,
                _bidServiceMock.Object,
                _productServiceMock.Object,
                _manufacturerServiceMock.Object,
                _productTypeServiceMock.Object,
                _authServiceMock.Object,
                _userSession,
                _viewHandlerMock.Object);

            vm.Initialize();

            vm.StartPriceInput = "not_a_number";
            vm.MinBidInput = "-5";
            vm.ProductNameInput = "";
            vm.ProductQuantityInput = "0";

            Assert.Multiple(() =>
            {
                Assert.That(vm.HasErrors, Is.True);
                Assert.That(vm.GetPropertyErrors(nameof(vm.StartPriceInput)), Is.Not.Empty);
                Assert.That(vm.GetPropertyErrors(nameof(vm.MinBidInput)), Is.Not.Empty);
                Assert.That(vm.GetPropertyErrors(nameof(vm.ProductNameInput)), Is.Not.Empty);
                Assert.That(vm.GetPropertyErrors(nameof(vm.ProductQuantityInput)), Is.Not.Empty);
                Assert.That(vm.CreateAuctionCommand.CanExecute(null), Is.False);
                Assert.That(vm.CreateProductCommand.CanExecute(null), Is.False);
            });

            vm.StartPriceInput = "250.00";
            vm.MinBidInput = "0.50";
            vm.ProductNameInput = "IBM 5150 PC";
            vm.ProductPriceInput = "400.00";
            vm.ProductQuantityInput = "3";

            Assert.Multiple(() =>
            {
                Assert.That(vm.HasErrors, Is.False);
                Assert.That(vm.BuyoutPriceInput, Is.EqualTo("520.00"));
                Assert.That(vm.CreateAuctionCommand.CanExecute(null), Is.True);
                Assert.That(vm.CreateProductCommand.CanExecute(null), Is.True);
            });
        }

        /*
         * Перевіряє роботу TraderTerminalViewModel: валідацію форми пропозиції лота та подання заявки на верифікацію.
         */
        [Test]
        public void TraderTerminalViewModel_ProposeLot_ValidatesAndSubmitsProposal()
        {
            _userSession.SignIn(new UserDTO
            {
                Id = Guid.NewGuid(),
                Name = "Mia Wallace",
                RoleId = AppRoles.BuyerRoleId,
                IsActive = true,
                Balance = 1000m
            });

            _productServiceMock.Setup(p => p.CreateProduct(It.IsAny<ProductDTO>()))
                .Returns((ProductDTO d) =>
                {
                    d.Id = Guid.NewGuid();
                    return d;
                });

            _auctionServiceMock.Setup(a => a.ProposeAuction(It.IsAny<AuctionDTO>()))
                .Returns((AuctionDTO a) =>
                {
                    a.Id = Guid.NewGuid();
                    a.IsVerified = false;
                    a.IsActive = false;
                    return a;
                });

            var vm = new TraderTerminalViewModel(
                _auctionServiceMock.Object,
                _bidServiceMock.Object,
                _productServiceMock.Object,
                _manufacturerServiceMock.Object,
                _productTypeServiceMock.Object,
                _authServiceMock.Object,
                _userSession,
                _viewHandlerMock.Object);

            vm.Initialize();

            vm.ProposalProductName = "Vintage Wall Street Ticker Tape";
            vm.ProposalStartPriceInput = "120.00";
            vm.ProposalMinBidInput = "1.00";
            vm.ProposalDurationDaysInput = "10";

            Assert.Multiple(() =>
            {
                Assert.That(vm.ProposalBuyoutPriceInput, Is.EqualTo("260.00"));
                Assert.That(vm.ProposeAuctionCommand.CanExecute(null), Is.True);
            });

            vm.ProposeAuctionCommand.Execute(null);
            _auctionServiceMock.Verify(a => a.ProposeAuction(It.IsAny<AuctionDTO>()), Times.Once);
        }

        /*
         * Виконує наскрізну інтеграційну перевірку реального DI-контейнера та бази даних softwrDB (у межах TransactionScope):
         * вхід Покупця mia@m.co, подання заявки лота, вхід Менеджера sarah@co.uk, верифікація лота, ставка та миттєвий викуп.
         */
        [Test]
        public void EndToEnd_RealDbWithDiContainer_LoginProposeVerifyBidAndBuyout_WorksSeamlessly()
        {
            using var scope = new System.Transactions.TransactionScope(System.Transactions.TransactionScopeAsyncFlowOption.Enabled);
            using ServiceProvider provider = App.ConfigureServices();

            var authService = provider.GetRequiredService<IAuthService>();
            var auctionService = provider.GetRequiredService<IAuctionService>();
            var bidService = provider.GetRequiredService<IBidService>();
            var productService = provider.GetRequiredService<IProductService>();
            var session = provider.GetRequiredService<IUserSession>();

            var buyer = authService.Login("mia@m.co", "Password123!");
            Assert.That(session.CanManageAuctions, Is.False);

            var existingProduct = productService.GetAll()[0];
            var proposed = auctionService.ProposeAuction(new AuctionDTO
            {
                ProductId = existingProduct.Id,
                StartPrice = 50m,
                StartDate = DateTime.Now.AddMinutes(-5),
                EndDate = DateTime.Now.AddDays(7)
            });

            Assert.Multiple(() =>
            {
                Assert.That(proposed.IsVerified, Is.False);
                Assert.That(proposed.IsActive, Is.False);
            });

            var manager = authService.Login("sarah@co.uk", "Password123!");
            Assert.That(session.CanManageAuctions, Is.True);

            bool verified = auctionService.VerifyAuction(proposed.Id);
            Assert.That(verified, Is.True);

            var placedBid = bidService.PlaceBid(proposed.Id, 60m);
            Assert.That(placedBid.Price, Is.EqualTo(60m));

            authService.Login("mia@m.co", "Password123!");
            var buyoutBid = bidService.Buyout(proposed.Id);
            var closedAuction = auctionService.GetById(proposed.Id);

            Assert.Multiple(() =>
            {
                Assert.That(buyoutBid.IsBuyout, Is.True);
                Assert.That(buyoutBid.Price, Is.EqualTo(120m));
                Assert.That(closedAuction!.IsActive, Is.False);
                Assert.That(closedAuction.EndPrice, Is.EqualTo(120m));
            });
        }

        /*
         * Перевіряє, що вибір заявки в таблиці PendingAuctions у AuctionManagerViewModel очищує SelectedAuction,
         * зберігає індивідуальну ціну викупу без перезапису формулою та дозволяє деактивувати непідтверджений лот.
         */
        [Test]
        public void AuctionManagerViewModel_SelectPendingAuction_PreservesCustomBuyoutAndAllowsDeactivation()
        {
            _userSession.SignIn(new UserDTO
            {
                Id = Guid.NewGuid(),
                Name = "Sarah Connor",
                RoleId = AppRoles.AuctionManagerRoleId,
                IsActive = true
            });

            var activeLot = new AuctionDTO
            {
                Id = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                ProductName = "Active Lot",
                StartPrice = 100m,
                BuyoutPrice = 220m,
                MinBid = 1m,
                StartDate = DateTime.Now.AddDays(-1),
                EndDate = DateTime.Now.AddDays(5),
                IsVerified = true,
                IsActive = true
            };

            var pendingLot = new AuctionDTO
            {
                Id = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                ProductName = "Pending Lot",
                StartPrice = 300m,
                BuyoutPrice = 950m,
                MinBid = 5m,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(10),
                IsVerified = false,
                IsActive = false
            };

            _auctionServiceMock.Setup(s => s.GetPendingVerification()).Returns(new List<AuctionDTO> { pendingLot });
            _auctionServiceMock.Setup(s => s.DeactivateAuction(pendingLot.Id)).Returns(true);
            _bidServiceMock.Setup(b => b.GetByAuctionId(It.IsAny<Guid>())).Returns(new List<BidDTO>());

            var vm = new AuctionManagerViewModel(
                _auctionServiceMock.Object,
                _bidServiceMock.Object,
                _productServiceMock.Object,
                _manufacturerServiceMock.Object,
                _productTypeServiceMock.Object,
                _authServiceMock.Object,
                _userSession,
                _viewHandlerMock.Object);

            vm.Initialize();
            vm.SelectedAuction = activeLot;
            vm.SelectedPendingAuction = pendingLot;

            Assert.Multiple(() =>
            {
                Assert.That(vm.SelectedAuction, Is.Null);
                Assert.That(vm.StartPriceInput, Is.EqualTo("300.00"));
                Assert.That(vm.BuyoutPriceInput, Is.EqualTo("950.00"));
                Assert.That(vm.DeactivateAuctionCommand.CanExecute(null), Is.True);
            });

            vm.DeactivateAuctionCommand.Execute(null);
            _auctionServiceMock.Verify(s => s.DeactivateAuction(pendingLot.Id), Times.Once);
        }

        /*
         * Перевіряє інстанціювання всіх XAML-подань (LoginView, AuctionManagerView, TraderTerminalView)
         * та словника ресурсів Styles.xaml у потоці STA з прив'язкою до ViewModel.
         */
        [Test]
        [Apartment(System.Threading.ApartmentState.STA)]
        public void WpfViewsAndStyles_OnStaThread_InstantiateAndBindWithoutXamlErrors()
        {
            if (System.Windows.Application.Current == null)
            {
                var app = new App();
                app.InitializeComponent();
            }

            var resources = System.Windows.Application.Current!.Resources;
            using ServiceProvider provider = App.ConfigureServices();

            var loginView = new CompanyWPFInterface.Views.LoginView
            {
                DataContext = provider.GetRequiredService<LoginViewModel>()
            };
            var managerView = new CompanyWPFInterface.Views.AuctionManagerView
            {
                DataContext = provider.GetRequiredService<AuctionManagerViewModel>()
            };
            var traderView = new CompanyWPFInterface.Views.TraderTerminalView
            {
                DataContext = provider.GetRequiredService<TraderTerminalViewModel>()
            };

            loginView.Measure(new System.Windows.Size(1360, 840));
            managerView.Measure(new System.Windows.Size(1360, 840));
            traderView.Measure(new System.Windows.Size(1360, 840));

            Assert.Multiple(() =>
            {
                Assert.That(resources["TerminalValidationErrorTemplate"], Is.Not.Null);
                Assert.That(resources["TerminalStatusTextStyle"], Is.Not.Null);
                Assert.That(resources["LoginStatusTextStyle"], Is.Not.Null);
                Assert.That(loginView.DesiredSize.Width, Is.GreaterThan(0));
                Assert.That(managerView.DesiredSize.Width, Is.GreaterThan(0));
                Assert.That(traderView.DesiredSize.Width, Is.GreaterThan(0));
            });
        }

        /*
         * Перевіряє роботу RTC-годинника у MainWindowViewModel та додавання нових виробників і категорій у AuctionManagerViewModel.
         */
        [Test]
        public void MainWindowRtcClock_And_CatalogCreationCommands_WorkCorrectly()
        {
            var mainVm = new MainWindowViewModel(_viewHandlerMock.Object, _userSession, _authServiceMock.Object);
            Assert.That(mainVm.RtcClockText, Does.StartWith("RTC // "));

            _userSession.SignIn(new UserDTO
            {
                Id = Guid.NewGuid(),
                Name = "Sarah Connor",
                RoleId = AppRoles.AuctionManagerRoleId,
                IsActive = true
            });

            var createdMfg = new ManufacturerDTO { Id = Guid.NewGuid(), Name = "DEC", Country = "USA" };
            var createdType = new ProductTypeDTO { Id = Guid.NewGuid(), Name = "Mainframes" };

            _manufacturerServiceMock.Setup(m => m.Create(It.IsAny<ManufacturerDTO>())).Returns(createdMfg);
            _productTypeServiceMock.Setup(pt => pt.Create(It.IsAny<ProductTypeDTO>())).Returns(createdType);

            var vm = new AuctionManagerViewModel(
                _auctionServiceMock.Object,
                _bidServiceMock.Object,
                _productServiceMock.Object,
                _manufacturerServiceMock.Object,
                _productTypeServiceMock.Object,
                _authServiceMock.Object,
                _userSession,
                _viewHandlerMock.Object);

            vm.Initialize();

            Assert.Multiple(() =>
            {
                Assert.That(vm.CreateManufacturerCommand.CanExecute(null), Is.False);
                Assert.That(vm.CreateProductTypeCommand.CanExecute(null), Is.False);
            });

            vm.NewManufacturerNameInput = "DEC";
            vm.NewManufacturerCountryInput = "USA";
            vm.NewProductTypeNameInput = "Mainframes";

            Assert.Multiple(() =>
            {
                Assert.That(vm.CreateManufacturerCommand.CanExecute(null), Is.True);
                Assert.That(vm.CreateProductTypeCommand.CanExecute(null), Is.True);
            });

            vm.CreateManufacturerCommand.Execute(null);
            vm.CreateProductTypeCommand.Execute(null);

            _manufacturerServiceMock.Verify(m => m.Create(It.Is<ManufacturerDTO>(d => d.Name == "DEC" && d.Country == "USA")), Times.Once);
            _productTypeServiceMock.Verify(pt => pt.Create(It.Is<ProductTypeDTO>(d => d.Name == "Mainframes")), Times.Once);
        }
    }
}
