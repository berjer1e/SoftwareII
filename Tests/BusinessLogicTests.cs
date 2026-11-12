/*
 * Файл: BusinessLogicTests.cs
 * Призначення: Модульні тести для рівня бізнес-логіки (HashService, AuthService, AuctionService).
 */

using CompanyDataAccessLayer.DAL;
using CompanyDataTransferObject;
using CompanyDataTransferObject.Constants;
using CompanyServiceLayer;
using CompanyServiceLayer.Session;

namespace Tests;

public class BusinessLogicTests
{
    private InMemoryUserDal _userDal = null!;
    private InMemoryAuctionDal _auctionDal = null!;
    private IUserSession _session = null!;
    private IHashService _hashService = null!;
    private AuthService _authService = null!;
    private AuctionService _auctionService = null!;

    [SetUp]
    public void Setup()
    {
        _userDal = new InMemoryUserDal();
        _auctionDal = new InMemoryAuctionDal();
        _session = new UserSession();
        _hashService = new HashService();
        _authService = new AuthService(_userDal, _session, _hashService);
        _auctionService = new AuctionService(_auctionDal, _session, _userDal);
    }

    [Test]
    public void HashService_VerifyHashedPassword_ReturnsTrueForValidPasswordAndFalseForInvalid()
    {
        Guid salt = Guid.NewGuid();
        byte[] hash = _hashService.HashPassword("1234", salt);

        Assert.That(_hashService.VerifyHashedPassword("1234", salt, hash), Is.True);
        Assert.That(_hashService.VerifyHashedPassword("wrong", salt, hash), Is.False);
        Assert.That(_hashService.VerifyHashedPassword("1234", Guid.NewGuid(), hash), Is.False);
    }

    [Test]
    public void AuthService_RegisterAndLogin_SucceedsWithValidPasswordAndFailsWithWrongPassword()
    {
        bool registered = _authService.Register("manager1", "1234");
        Assert.That(registered, Is.True);

        UserDTO? storedUser = _userDal.GetAll().FirstOrDefault(u => u.Name == "manager1");
        Assert.That(storedUser, Is.Not.Null);
        Assert.That(storedUser!.PasswordHash, Is.Not.Null.And.Not.Empty);

        Assert.That(_authService.Login("manager1", "wrong"), Is.False);
        Assert.That(_session.IsAuthenticated, Is.False);

        Assert.That(_authService.Login("manager1", "1234"), Is.True);
        Assert.That(_session.IsAuthenticated, Is.True);
        Assert.That(_authService.CanManageAuctions, Is.True);
    }

    [Test]
    public void AuthService_RegisterDuplicateUsername_ReturnsFalse()
    {
        Assert.That(_authService.Register("userA", "1234"), Is.True);
        Assert.That(_authService.Register("userA", "5678"), Is.False);
    }

    [Test]
    public void AuthService_CanManageAuctions_ReturnsFalseForBuyerRole()
    {
        Guid buyerId = Guid.NewGuid();
        byte[] hash = _hashService.HashPassword("1234", buyerId);
        _userDal.Add(new UserDTO
        {
            Id = buyerId,
            Name = "buyer1",
            IsActive = true,
            RoleId = AppRoles.BuyerRoleId,
            PasswordHash = $"{buyerId:N}"[..16] + ":" + Convert.ToHexString(hash).ToLowerInvariant()
        });

        Assert.That(_authService.Login("buyer1", "1234"), Is.True);
        Assert.That(_authService.CanManageAuctions, Is.False);
    }

    [Test]
    public void AuctionService_BuyoutAuction_TransfersAmountAnd10PercentCommissionToManager()
    {
        Guid managerId = Guid.NewGuid();
        var manager = new UserDTO
        {
            Id = managerId,
            Name = "manager",
            IsActive = true,
            RoleId = AppRoles.AuctionManagerRoleId,
            Balance = 0m
        };
        var buyer = new UserDTO
        {
            Id = Guid.NewGuid(),
            Name = "buyer",
            IsActive = true,
            RoleId = AppRoles.BuyerRoleId,
            Balance = 1500m
        };
        var seller = new UserDTO
        {
            Id = Guid.NewGuid(),
            Name = "seller",
            IsActive = true,
            RoleId = AppRoles.SellerRoleId,
            Balance = 0m
        };
        _userDal.Add(manager);
        _userDal.Add(buyer);
        _userDal.Add(seller);
        _session.StartSession(manager);

        var auction = new AuctionDTO
        {
            Id = Guid.NewGuid(),
            StartPrice = 500m,
            EndPrice = 1000m,
            IsActive = true,
            AuctionManagerId = seller.Id
        };
        _auctionDal.Add(auction);

        bool success = _auctionService.BuyoutAuction(auction.Id, buyer.Id, 0.10m);

        Assert.That(success, Is.True);
        Assert.That(_userDal.GetById(buyer.Id)!.Balance, Is.EqualTo(500m));
        Assert.That(_userDal.GetById(seller.Id)!.Balance, Is.EqualTo(900m));
        Assert.That(_userDal.GetById(managerId)!.Balance, Is.EqualTo(100m));
        Assert.That(_auctionDal.GetById(auction.Id)!.IsActive, Is.False);
    }

    [Test]
    public void AuctionService_BuyoutAuction_FailsWhenBuyerHasInsufficientFunds()
    {
        var manager = new UserDTO { Id = Guid.NewGuid(), Name = "m", IsActive = true, RoleId = AppRoles.AuctionManagerRoleId };
        var buyer = new UserDTO { Id = Guid.NewGuid(), Name = "b", IsActive = true, RoleId = AppRoles.BuyerRoleId, Balance = 100m };
        _userDal.Add(manager);
        _userDal.Add(buyer);
        _session.StartSession(manager);

        var auction = new AuctionDTO { Id = Guid.NewGuid(), StartPrice = 500m, EndPrice = 500m, IsActive = true };
        _auctionDal.Add(auction);

        bool success = _auctionService.BuyoutAuction(auction.Id, buyer.Id, 0.10m);

        Assert.That(success, Is.False);
        Assert.That(_auctionDal.GetById(auction.Id)!.IsActive, Is.True);
    }

    private sealed class InMemoryUserDal : IUserDAL
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

    private sealed class InMemoryAuctionDal : IAuctionDAL
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
}
