using AutoMapper;
using CompanyDataAccessLayer.DAL;
using CompanyDataTransferObject;
using CompanyDataTransferObject.Constants;
using CompanyServiceLayer;
using CompanyServiceLayer.Session;
using CompanyWebInterface.Controllers;
using CompanyWebInterface.Mapping;
using CompanyWebInterface.Models;
using CompanyWebInterface.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace Tests;

public class ControllersTests
{
    private FakeUserDal _userDal = null!;
    private FakeAuctionDal _auctionDal = null!;
    private FakeProductDal _productDal = null!;
    private FakeWebUserSession _webSession = null!;
    private IHashService _hashService = null!;
    private IAuthService _authService = null!;
    private IAuctionService _auctionService = null!;
    private IMapper _mapper = null!;

    private UserDTO _manager = null!;
    private UserDTO _buyer = null!;
    private ProductDTO _product = null!;

    [SetUp]
    public void Setup()
    {
        _userDal = new FakeUserDal();
        _auctionDal = new FakeAuctionDal();
        _productDal = new FakeProductDal();
        _webSession = new FakeWebUserSession();
        _hashService = new HashService();
        _authService = new AuthService(_userDal, _webSession, _hashService);
        _auctionService = new AuctionService(_auctionDal, _webSession, _userDal, _productDal);

        var sp = new ServiceCollection()
            .AddLogging()
            .AddAutoMapper(cfg => cfg.AddProfile<DtoToViewModelProfile>())
            .BuildServiceProvider();
        _mapper = sp.GetRequiredService<IMapper>();

        Guid managerId = Guid.NewGuid();
        byte[] managerHash = _hashService.HashPassword("1234", managerId);
        _manager = new UserDTO
        {
            Id = managerId,
            Name = "Sarah Connor",
            Email = "sarah@auction.com",
            IsActive = true,
            RoleId = AppRoles.AuctionManagerRoleId,
            Balance = 100m,
            PasswordHash = $"{managerId:N}"[..16] + ":" + Convert.ToHexString(managerHash).ToLowerInvariant()
        };

        Guid buyerId = Guid.NewGuid();
        byte[] buyerHash = _hashService.HashPassword("1234", buyerId);
        _buyer = new UserDTO
        {
            Id = buyerId,
            Name = "Harry Potter",
            Email = "harry@auction.com",
            IsActive = true,
            RoleId = AppRoles.BuyerRoleId,
            Balance = 2000m,
            PasswordHash = $"{buyerId:N}"[..16] + ":" + Convert.ToHexString(buyerHash).ToLowerInvariant()
        };

        _product = new ProductDTO
        {
            Id = Guid.NewGuid(),
            Name = "Вінтажний годинник",
            Price = 500m,
            Quantity = 1
        };

        _userDal.Add(_manager);
        _userDal.Add(_buyer);
        _productDal.Add(_product);
        _webSession.StartSession(_manager);
    }

    [Test]
    public void AuctionsController_Index_MapsProductAndManagerNamesAndFilters()
    {
        _auctionDal.Add(new AuctionDTO
        {
            Id = Guid.NewGuid(),
            ProductId = _product.Id,
            AuctionManagerId = _manager.Id,
            StartPrice = 500m,
            EndPrice = 750m,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(5),
            IsActive = true
        });

        var controller = CreateAuctionsController();

        var result = controller.Index(search: "годинник", onlyActive: true) as ViewResult;

        Assert.That(result, Is.Not.Null);
        var model = result!.Model as AuctionIndexViewModel;
        Assert.That(model, Is.Not.Null);
        Assert.That(model!.Auctions, Has.Count.EqualTo(1));
        Assert.That(model.Auctions[0].ProductName, Is.EqualTo("Вінтажний годинник"));
        Assert.That(model.Auctions[0].ManagerName, Is.EqualTo("Sarah Connor"));
        Assert.That(model.Auctions[0].CurrentPrice, Is.EqualTo(750m));
    }

    [Test]
    public void AuctionsController_Details_ReturnsNotFoundForMissingId()
    {
        var controller = CreateAuctionsController();

        var result = controller.Details(Guid.NewGuid());

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public void AuctionsController_CreatePost_InvalidModelState_ReturnsViewWithoutSaving()
    {
        var controller = CreateAuctionsController();
        controller.ModelState.AddModelError("StartPrice", "Вкажіть стартову ціну.");

        var formModel = new AuctionCreateViewModel
        {
            ProductId = _product.Id,
            StartPrice = null
        };

        var result = controller.Create(formModel) as ViewResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Model, Is.SameAs(formModel));
        Assert.That(_auctionDal.GetAll(), Is.Empty);
    }

    [Test]
    public void AuctionsController_CreatePost_ValidModel_AddsAuctionAndRedirectsToIndex()
    {
        var controller = CreateAuctionsController();
        var formModel = new AuctionCreateViewModel
        {
            ProductId = _product.Id,
            StartPrice = 600m,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(3)
        };

        var result = controller.Create(formModel) as RedirectToActionResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ActionName, Is.EqualTo(nameof(AuctionsController.Index)));
        Assert.That(_auctionDal.GetAll(), Has.Count.EqualTo(1));
        Assert.That(_auctionDal.GetAll()[0].StartPrice, Is.EqualTo(600m));
        Assert.That(controller.TempData["StatusMessage"], Is.Not.Null);
    }

    [Test]
    public void AuctionsController_Buyout_TransfersFundsWith10PercentCommissionAndRedirects()
    {
        var auction = new AuctionDTO
        {
            Id = Guid.NewGuid(),
            ProductId = _product.Id,
            AuctionManagerId = _manager.Id,
            StartPrice = 1000m,
            EndPrice = 1000m,
            IsActive = true
        };
        _auctionDal.Add(auction);

        var controller = CreateAuctionsController();
        var result = controller.Buyout(new BuyoutViewModel
        {
            AuctionId = auction.Id,
            BuyerId = _buyer.Id
        }) as RedirectToActionResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ActionName, Is.EqualTo(nameof(AuctionsController.Index)));
        Assert.That(_userDal.GetById(_buyer.Id)!.Balance, Is.EqualTo(1000m));
        Assert.That(_auctionDal.GetById(auction.Id)!.IsActive, Is.False);
    }

    [Test]
    public void UsersController_EditBalance_UpdatesUserBalanceAndRedirects()
    {
        var controller = new UsersController(_auctionService, _mapper)
        {
            TempData = CreateTempData()
        };

        var missing = controller.EditBalance(Guid.NewGuid());
        Assert.That(missing, Is.InstanceOf<NotFoundResult>());

        var result = controller.EditBalance(_buyer.Id, new UpdateBalanceViewModel
        {
            UserId = _buyer.Id,
            NewBalance = 3500m
        }) as RedirectToActionResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ActionName, Is.EqualTo(nameof(UsersController.Index)));
        Assert.That(_userDal.GetById(_buyer.Id)!.Balance, Is.EqualTo(3500m));
    }

    [Test]
    public async Task AccountController_Login_HandlesInvalidAndValidCredentials()
    {
        var controller = new AccountController(_authService, _auctionService, _webSession)
        {
            TempData = CreateTempData()
        };

        var badResult = await controller.Login(new LoginViewModel
        {
            Login = "Sarah Connor",
            Password = "wrong"
        }) as ViewResult;

        Assert.That(badResult, Is.Not.Null);
        Assert.That(controller.ModelState.IsValid, Is.False);
        Assert.That(_webSession.SignedInCount, Is.EqualTo(0));

        controller.ModelState.Clear();

        var okResult = await controller.Login(new LoginViewModel
        {
            Login = "Harry Potter",
            Password = "1234"
        }) as RedirectToActionResult;

        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.ControllerName, Is.EqualTo("Auctions"));
        Assert.That(_webSession.SignedInCount, Is.EqualTo(1));
        Assert.That(_webSession.CurrentUser!.Name, Is.EqualTo("Harry Potter"));
        Assert.That(_webSession.CurrentUser.IsManager, Is.False);
    }

    private AuctionsController CreateAuctionsController()
    {
        return new AuctionsController(_auctionService, _mapper)
        {
            TempData = CreateTempData()
        };
    }

    private static TempDataDictionary CreateTempData()
    {
        return new TempDataDictionary(new DefaultHttpContext(), new InMemoryTempDataProvider());
    }

    private sealed class InMemoryTempDataProvider : ITempDataProvider
    {
        private readonly Dictionary<string, object> _data = new();
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>(_data);
        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
            _data.Clear();
            foreach (var kv in values) _data[kv.Key] = kv.Value;
        }
    }

    private sealed class FakeWebUserSession : IWebUserSession
    {
        public UserDTO? CurrentUser { get; private set; }
        public DateTime? LoginDate { get; private set; }
        public bool IsAuthenticated => CurrentUser != null;
        public int SignedInCount { get; private set; }

        public void StartSession(UserDTO user)
        {
            CurrentUser = user;
            LoginDate = DateTime.Now;
        }

        public void ClearSession()
        {
            CurrentUser = null;
            LoginDate = null;
        }

        public Task SignInAsync(UserDTO user, bool isPersistent)
        {
            SignedInCount++;
            StartSession(user);
            return Task.CompletedTask;
        }

        public Task SignOutAsync()
        {
            ClearSession();
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUserDal : IUserDAL
    {
        private readonly List<UserDTO> _items = new();
        public bool Add(UserDTO item) { _items.Add(item); return true; }
        public bool Delete(Guid id) => _items.RemoveAll(x => x.Id == id) > 0;
        public List<UserDTO> GetAll() => _items.ToList();
        public UserDTO? GetById(Guid id) => _items.FirstOrDefault(x => x.Id == id);
        public bool Update(UserDTO item)
        {
            int idx = _items.FindIndex(x => x.Id == item.Id);
            if (idx < 0) return false;
            _items[idx] = item;
            return true;
        }
    }

    private sealed class FakeAuctionDal : IAuctionDAL
    {
        private readonly List<AuctionDTO> _items = new();
        public bool Add(AuctionDTO item) { _items.Add(item); return true; }
        public bool Delete(Guid id) => _items.RemoveAll(x => x.Id == id) > 0;
        public List<AuctionDTO> GetAll() => _items.ToList();
        public List<AuctionDTO> GetActive() => _items.Where(x => x.IsActive).ToList();
        public AuctionDTO? GetById(Guid id) => _items.FirstOrDefault(x => x.Id == id);
        public bool Update(AuctionDTO item)
        {
            int idx = _items.FindIndex(x => x.Id == item.Id);
            if (idx < 0) return false;
            _items[idx] = item;
            return true;
        }
    }

    private sealed class FakeProductDal : IProductDAL
    {
        private readonly List<ProductDTO> _items = new();
        public bool Add(ProductDTO item) { _items.Add(item); return true; }
        public bool Delete(Guid id) => _items.RemoveAll(x => x.Id == id) > 0;
        public List<ProductDTO> GetAll() => _items.ToList();
        public ProductDTO? GetById(Guid id) => _items.FirstOrDefault(x => x.Id == id);
        public bool Update(ProductDTO item)
        {
            int idx = _items.FindIndex(x => x.Id == item.Id);
            if (idx < 0) return false;
            _items[idx] = item;
            return true;
        }
    }
}
