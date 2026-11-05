/*
 * Файл: ProductAndUserServiceTests.cs
 * Призначення: Модульні тести для сервісів ProductService, UserService, ManufacturerService та ProductTypeService.
 */

using System;
using CompanyDataAccessLayer.DAL;
using CompanyDataTransferObject;
using CompanyDataTransferObject.Constants;
using CompanyServiceLayer.Exceptions;
using CompanyServiceLayer.Security;
using CompanyServiceLayer.Services;
using CompanyServiceLayer.Session;
using Moq;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    [Category("BusinessLogic")]
    public class ProductAndUserServiceTests
    {
        private Mock<IProductDAL> _productDalMock = null!;
        private Mock<IUserDAL> _userDalMock = null!;
        private Mock<IManufacturerDAL> _manufacturerDalMock = null!;
        private Mock<IProductTypeDAL> _productTypeDalMock = null!;
        private IUserSession _userSession = null!;
        private IPasswordHasher _passwordHasher = null!;

        /*
         * Ініціалізує мок-об'єкти DAL та допоміжні сервіси перед кожним тестом.
         */
        [SetUp]
        public void SetUp()
        {
            _productDalMock = new Mock<IProductDAL>();
            _userDalMock = new Mock<IUserDAL>();
            _manufacturerDalMock = new Mock<IManufacturerDAL>();
            _productTypeDalMock = new Mock<IProductTypeDAL>();
            _userSession = new UserSession();
            _passwordHasher = new PasswordHasher();
        }

        /*
         * Перевіряє валідацію та створення товару в ProductService, а також обмеження видалення лише для Менеджера.
         */
        [Test]
        public void ProductService_ValidationAndRoleChecks_WorkCorrectly()
        {
            var service = new ProductService(_productDalMock.Object, _userSession);
            var buyer = new UserDTO { Id = Guid.NewGuid(), Name = "Buyer", RoleId = AppRoles.BuyerRoleId, IsActive = true };
            var manager = new UserDTO { Id = Guid.NewGuid(), Name = "Manager", RoleId = AppRoles.AuctionManagerRoleId, IsActive = true };

            _userSession.SignIn(buyer);
            _productDalMock.Setup(d => d.Add(It.IsAny<ProductDTO>())).Returns(true);

            var validProduct = new ProductDTO
            {
                Name = "Commodore PET",
                Price = 350m,
                Quantity = 2,
                ManufacturerId = Guid.NewGuid(),
                ProductTypeId = Guid.NewGuid()
            };

            var created = service.CreateProduct(validProduct);
            Assert.That(created.Name, Is.EqualTo("Commodore PET"));

            Assert.Throws<BusinessRuleException>(() => service.DeleteProduct(Guid.NewGuid()));

            _userSession.SignIn(manager);
            Assert.Throws<BusinessRuleException>(() => service.CreateProduct(new ProductDTO { Name = "", Price = -5m, Quantity = 0 }));
        }

        /*
         * Перевіряє створення нового користувача з хешуванням пароля та перевіркою унікальності пошти в UserService.
         */
        [Test]
        public void UserService_CreateUser_HashesPasswordAndRejectsDuplicateEmail()
        {
            var service = new UserService(_userDalMock.Object, _passwordHasher, _userSession);
            _userDalMock.Setup(d => d.Add(It.IsAny<UserDTO>())).Returns(true);

            var newUser = new UserDTO
            {
                Name = "Bud Fox",
                Email = "bud@wallstreet.com",
                Balance = 1000m
            };

            var created = service.CreateUser(newUser, "Secret123!");
            Assert.Multiple(() =>
            {
                Assert.That(created.PasswordHash, Does.StartWith("pbkdf2:"));
                Assert.That(_passwordHasher.VerifyPassword("Secret123!", created.PasswordHash), Is.True);
            });

            _userDalMock.Setup(d => d.GetByEmail("bud@wallstreet.com")).Returns(created);
            Assert.Throws<BusinessRuleException>(() => service.CreateUser(newUser, "Secret123!"));
        }

        /*
         * Перевіряє валідацію довідників ManufacturerService та ProductTypeService.
         */
        [Test]
        public void ManufacturerAndProductTypeServices_ValidateEmptyFields()
        {
            var mfgService = new ManufacturerService(_manufacturerDalMock.Object);
            var ptService = new ProductTypeService(_productTypeDalMock.Object);

            Assert.Multiple(() =>
            {
                Assert.Throws<BusinessRuleException>(() => mfgService.Create(new ManufacturerDTO { Name = "", Country = "" }));
                Assert.Throws<BusinessRuleException>(() => ptService.Create(new ProductTypeDTO { Name = "   " }));
            });
        }

        /*
         * Перевіряє, що UpdateUser зберігає існуючий хеш пароля та роль користувача і блокує дублювання пошти іншого користувача.
         */
        [Test]
        public void UserService_UpdateUser_PreservesPasswordHashAndRejectsDuplicateEmail()
        {
            var service = new UserService(_userDalMock.Object, _passwordHasher, _userSession);
            var existingUser = new UserDTO
            {
                Id = Guid.NewGuid(),
                Name = "Gordon Gekko",
                Email = "gekko@ws.com",
                RoleId = AppRoles.AuctionManagerRoleId,
                Balance = 2000m,
                PasswordHash = "pbkdf2:100000:salt:hash"
            };
            var otherUser = new UserDTO
            {
                Id = Guid.NewGuid(),
                Name = "Bud Fox",
                Email = "bud@ws.com"
            };

            _userDalMock.Setup(d => d.GetById(existingUser.Id)).Returns(existingUser);
            _userDalMock.Setup(d => d.GetByEmail("bud@ws.com")).Returns(otherUser);
            _userDalMock.Setup(d => d.Update(It.IsAny<UserDTO>())).Returns(true);

            var duplicateEmailDto = new UserDTO
            {
                Id = existingUser.Id,
                Name = "Gordon Gekko",
                Email = "bud@ws.com",
                Balance = 2000m
            };
            Assert.Throws<BusinessRuleException>(() => service.UpdateUser(duplicateEmailDto));

            var updateDtoWithoutHash = new UserDTO
            {
                Id = existingUser.Id,
                Name = "Gordon Gekko Sr.",
                Email = "gekko@ws.com",
                Balance = 3500m,
                PasswordHash = null
            };
            service.UpdateUser(updateDtoWithoutHash);

            Assert.Multiple(() =>
            {
                Assert.That(updateDtoWithoutHash.PasswordHash, Is.EqualTo("pbkdf2:100000:salt:hash"));
                Assert.That(updateDtoWithoutHash.RoleId, Is.EqualTo(AppRoles.AuctionManagerRoleId));
            });
        }

        /*
         * Перевіряє, що спроба видалити товар із реальної БД softwrDB, на який посилається існуючий аукціон (FK_Auction_Products),
         * коректно перехоплюється та генерує BusinessRuleException.
         */
        [Test]
        public void ProductService_DeleteProductReferencedByAuction_RealDb_ThrowsBusinessRuleException()
        {
            const string connectionString =
                "Server=localhost;Database=softwrDB;Integrated Security=True;TrustServerCertificate=True;";
            using var scope = new System.Transactions.TransactionScope(System.Transactions.TransactionScopeAsyncFlowOption.Enabled);

            var productDal = new ProductDAL(connectionString);
            var auctionDal = new AuctionDAL(connectionString);
            var service = new ProductService(productDal, _userSession);

            _userSession.SignIn(new UserDTO
            {
                Id = Guid.NewGuid(),
                Name = "Sarah Connor",
                RoleId = AppRoles.AuctionManagerRoleId,
                IsActive = true
            });

            var auction = auctionDal.GetAll()[0];
            var ex = Assert.Throws<BusinessRuleException>(() => service.DeleteProduct(auction.ProductId));
            Assert.That(ex!.Message, Does.Contain("використовується в аукціонах"));
        }
    }
}
