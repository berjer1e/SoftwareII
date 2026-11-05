/*
 * Файл: AuthServiceTests.cs
 * Призначення: Модульні тести для сервісу автентифікації AuthService з використанням Moq.
 * Перевіряє вхід за поштою та іменем, захист від атак за часом при невідомому логіні,
 * блокування неактивних користувачів та керування сесією IUserSession.
 */

using System;
using System.Threading.Tasks;
using CompanyDataAccessLayer.DAL;
using CompanyDataAccessLayer.Logging;
using CompanyDataTransferObject;
using CompanyDataTransferObject.Constants;
using CompanyDataTransferObject.Enums;
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
    public class AuthServiceTests
    {
        private Mock<IUserDAL> _userDalMock = null!;
        private Mock<IPasswordHasher> _hasherMock = null!;
        private Mock<IAppLogger> _loggerMock = null!;
        private IUserSession _userSession = null!;
        private AuthService _authService = null!;

        /*
         * Ініціалізує мок-об'єкти та екземпляр AuthService перед кожним тестом.
         */
        [SetUp]
        public void SetUp()
        {
            _userDalMock = new Mock<IUserDAL>();
            _hasherMock = new Mock<IPasswordHasher>();
            _loggerMock = new Mock<IAppLogger>();
            _userSession = new UserSession();
            _authService = new AuthService(_userDalMock.Object, _hasherMock.Object, _userSession, _loggerMock.Object);
        }

        /*
         * Очищує сесію після кожного тесту.
         */
        [TearDown]
        public void TearDown()
        {
            _userSession.SignOut();
        }

        /*
         * Перевіряє успішну автентифікацію активного Менеджера аукціонів та встановлення прапорця CanManageAuctions у сесії.
         */
        [Test]
        [Description("Успішний вхід Менеджера аукціонів за електронною поштою")]
        public void Login_ValidManagerCredentials_SignsInAndSetsManagerSession()
        {
            var manager = new UserDTO
            {
                Id = Guid.NewGuid(),
                Name = "Sarah Connor",
                Email = "sarah@co.uk",
                RoleId = AppRoles.AuctionManagerRoleId,
                IsActive = true,
                Balance = 1000m,
                PasswordHash = "salt:hash"
            };

            _userDalMock.Setup(d => d.GetByEmail("sarah@co.uk")).Returns(manager);
            _hasherMock.Setup(h => h.VerifyPassword("Password123!", "salt:hash")).Returns(true);

            var result = _authService.Login("sarah@co.uk", "Password123!");

            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.EqualTo(manager.Id));
                Assert.That(_userSession.IsAuthenticated, Is.True);
                Assert.That(_userSession.CanManageAuctions, Is.True);
                Assert.That(_userSession.CurrentUser!.RoleType, Is.EqualTo(UserRoleType.AuctionManager));
            });
        }

        /*
         * Перевіряє, що при відсутності користувача в БД все одно викликається VerifyPassword (захист від Timing Attack)
         * і генерується BusinessRuleException.
         */
        [Test]
        public void Login_UnknownUser_ExecutesDummyVerifyForTimingProtectionAndThrows()
        {
            _userDalMock.Setup(d => d.GetByEmail(It.IsAny<string>())).Returns((UserDTO?)null);
            _userDalMock.Setup(d => d.GetByName(It.IsAny<string>())).Returns((UserDTO?)null);

            var ex = Assert.Throws<BusinessRuleException>(() => _authService.Login("ghost@nowhere.com", "Password123!"));

            Assert.Multiple(() =>
            {
                Assert.That(ex!.Message, Does.Contain("Невірний логін або пароль"));
                _hasherMock.Verify(h => h.VerifyPassword("Password123!", It.IsNotNull<string>()), Times.Once);
                Assert.That(_userSession.IsAuthenticated, Is.False);
            });
        }

        /*
         * Перевіряє блокування входу для деактивованого облікового запису користувача.
         */
        [Test]
        public void Login_InactiveUser_ThrowsBusinessRuleException()
        {
            var inactiveUser = new UserDTO
            {
                Id = Guid.NewGuid(),
                Name = "Blocked User",
                Email = "blocked@m.co",
                RoleId = AppRoles.BuyerRoleId,
                IsActive = false,
                PasswordHash = "salt:hash"
            };

            _userDalMock.Setup(d => d.GetByEmail("blocked@m.co")).Returns(inactiveUser);
            _hasherMock.Setup(h => h.VerifyPassword("Password123!", "salt:hash")).Returns(true);

            var ex = Assert.Throws<BusinessRuleException>(() => _authService.Login("blocked@m.co", "Password123!"));
            Assert.That(ex!.Message, Does.Contain("деактивовано"));
        }

        /*
         * Перевіряє асинхронний вхід та коректне очищення сесії під час виклику Logout.
         */
        [Test]
        public async Task LoginAsync_And_Logout_ClearsSession()
        {
            var buyer = new UserDTO
            {
                Id = Guid.NewGuid(),
                Name = "Mia Wallace",
                Email = "mia@m.co",
                RoleId = AppRoles.BuyerRoleId,
                IsActive = true,
                PasswordHash = "salt:hash"
            };

            _userDalMock.Setup(d => d.GetByEmail("mia@m.co")).Returns(buyer);
            _hasherMock.Setup(h => h.VerifyPassword("Password123!", "salt:hash")).Returns(true);

            await _authService.LoginAsync("mia@m.co", "Password123!");
            Assert.That(_userSession.IsAuthenticated, Is.True);
            Assert.That(_userSession.CanManageAuctions, Is.False);

            _authService.Logout();
            Assert.That(_userSession.IsAuthenticated, Is.False);
            Assert.That(_userSession.CurrentUser, Is.Null);
        }
    }
}
