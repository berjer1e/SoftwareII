/*
 * Файл: AuctionServiceTests.cs
 * Призначення: Модульні тести для сервісу бізнес-логіки AuctionService з використанням Moq та NUnit 4.
 * Перевіряє розмежування прав доступу між Менеджером аукціонів і Покупцем, автоматичний розрахунок
 * ціни викупу (StartPrice + 10) * 2 та кроку 0.01, верифікацію запропонованих лотів і деактивацію без видалення.
 */

using System;
using System.Collections.Generic;
using CompanyDataAccessLayer.DAL;
using CompanyDataAccessLayer.Logging;
using CompanyDataTransferObject;
using CompanyDataTransferObject.Constants;
using CompanyServiceLayer.Exceptions;
using CompanyServiceLayer.Services;
using CompanyServiceLayer.Session;
using Moq;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    [Category("BusinessLogic")]
    public class AuctionServiceTests
    {
        private Mock<IAuctionDAL> _auctionDalMock = null!;
        private Mock<IProductDAL> _productDalMock = null!;
        private Mock<IAppLogger> _loggerMock = null!;
        private IUserSession _userSession = null!;
        private AuctionService _auctionService = null!;

        private UserDTO _managerUser = null!;
        private UserDTO _buyerUser = null!;
        private ProductDTO _sampleProduct = null!;

        /*
         * Одноразово створює тестові профілі Менеджера, Покупця та зразкового товару.
         */
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _managerUser = new UserDTO
            {
                Id = Guid.NewGuid(),
                Name = "Sarah Connor",
                Email = "sarah@co.uk",
                RoleId = AppRoles.AuctionManagerRoleId,
                IsActive = true,
                Balance = 5000m
            };

            _buyerUser = new UserDTO
            {
                Id = Guid.NewGuid(),
                Name = "Mia Wallace",
                Email = "mia@m.co",
                RoleId = AppRoles.BuyerRoleId,
                IsActive = true,
                Balance = 1000m
            };

            _sampleProduct = new ProductDTO
            {
                Id = Guid.NewGuid(),
                Name = "Quotron 800 Terminal",
                Price = 200m,
                Quantity = 1,
                ManufacturerId = Guid.NewGuid(),
                ProductTypeId = Guid.NewGuid()
            };
        }

        /*
         * Ініціалізує мок-об'єкти та сервіс AuctionService перед кожним тестом.
         */
        [SetUp]
        public void SetUp()
        {
            _auctionDalMock = new Mock<IAuctionDAL>();
            _productDalMock = new Mock<IProductDAL>();
            _loggerMock = new Mock<IAppLogger>();
            _userSession = new UserSession();

            _productDalMock.Setup(p => p.GetById(_sampleProduct.Id)).Returns(_sampleProduct);

            _auctionService = new AuctionService(
                _auctionDalMock.Object,
                _productDalMock.Object,
                _userSession,
                TimeProvider.System,
                _loggerMock.Object);
        }

        /*
         * Завершує сесію після кожного тесту.
         */
        [TearDown]
        public void TearDown()
        {
            _userSession.SignOut();
        }

        /*
         * Джерело тестових випадків для перевірки автоматичного обчислення ціни викупу за формулою (StartPrice + 10) * 2.
         */
        public static IEnumerable<TestCaseData> DefaultBuyoutTestCases()
        {
            yield return new TestCaseData(40m, 0m, 100m, 0.01m).SetName("CreateAuction_Start40_DefaultBuyout100");
            yield return new TestCaseData(100m, 1m, 220m, 0.01m).SetName("CreateAuction_Start100_DefaultBuyout220");
            yield return new TestCaseData(250m, 600m, 600m, 0.01m).SetName("CreateAuction_Start250_ExplicitBuyout600");
        }

        /*
         * Перевіряє, що Менеджер аукціонів створює активний і верифікований лот із правильними значеннями за замовчуванням.
         */
        [Test]
        [TestCaseSource(nameof(DefaultBuyoutTestCases))]
        public void CreateAuction_ByManager_AppliesDefaultMinBidAndBuyoutFormula(
            decimal startPrice,
            decimal inputBuyout,
            decimal expectedBuyout,
            decimal expectedMinBid)
        {
            _userSession.SignIn(_managerUser);
            _auctionDalMock.Setup(d => d.Add(It.IsAny<AuctionDTO>())).Returns(true);

            var dto = new AuctionDTO
            {
                ProductId = _sampleProduct.Id,
                StartPrice = startPrice,
                BuyoutPrice = inputBuyout,
                MinBid = 0m,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(7)
            };

            var result = _auctionService.CreateAuction(dto);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsVerified, Is.True);
                Assert.That(result.IsActive, Is.True);
                Assert.That(result.MinBid, Is.EqualTo(expectedMinBid));
                Assert.That(result.BuyoutPrice, Is.EqualTo(expectedBuyout));
                Assert.That(result.AuctionManagerId, Is.EqualTo(_managerUser.Id));
            });
        }

        /*
         * Перевіряє, що звичайний Покупець не має права напряму створювати, редагувати, верифікувати або деактивовувати аукціони.
         */
        [Test]
        public void ManagerOnlyOperations_WhenCalledByBuyer_ThrowBusinessRuleException()
        {
            _userSession.SignIn(_buyerUser);

            var auction = new AuctionDTO
            {
                Id = Guid.NewGuid(),
                ProductId = _sampleProduct.Id,
                StartPrice = 100m,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(5)
            };

            Assert.Multiple(() =>
            {
                Assert.Throws<BusinessRuleException>(() => _auctionService.CreateAuction(auction));
                Assert.Throws<BusinessRuleException>(() => _auctionService.UpdateAuction(auction));
                Assert.Throws<BusinessRuleException>(() => _auctionService.VerifyAuction(auction.Id));
                Assert.Throws<BusinessRuleException>(() => _auctionService.DeactivateAuction(auction.Id));
            });
        }

        /*
         * Перевіряє, що Покупець може запропонувати лот (ProposeAuction), який отримує статус IsVerified = false та IsActive = false,
         * а Менеджер після цього може його верифікувати (VerifyAuction).
         */
        [Test]
        public void ProposeAuction_ByBuyer_ThenVerifyByManager_ActivatesAuction()
        {
            _userSession.SignIn(_buyerUser);
            _auctionDalMock.Setup(d => d.Add(It.IsAny<AuctionDTO>()))
                .Callback<AuctionDTO>(a => a.Id = Guid.NewGuid())
                .Returns(true);

            var proposal = new AuctionDTO
            {
                ProductId = _sampleProduct.Id,
                StartPrice = 150m,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(10)
            };

            var proposed = _auctionService.ProposeAuction(proposal);

            Assert.Multiple(() =>
            {
                Assert.That(proposed.IsVerified, Is.False);
                Assert.That(proposed.IsActive, Is.False);
                Assert.That(proposed.ProductOwnerId, Is.EqualTo(_buyerUser.Id));
                Assert.That(proposed.BuyoutPrice, Is.EqualTo(320m));
            });

            _userSession.SignIn(_managerUser);
            _auctionDalMock.Setup(d => d.GetById(proposed.Id)).Returns(proposed);
            _auctionDalMock.Setup(d => d.VerifyAuction(proposed.Id)).Returns(true);
            _auctionDalMock.Setup(d => d.Update(It.IsAny<AuctionDTO>())).Returns(true);

            bool verified = _auctionService.VerifyAuction(proposed.Id);

            Assert.Multiple(() =>
            {
                Assert.That(verified, Is.True);
                Assert.That(proposed.IsVerified, Is.True);
                Assert.That(proposed.IsActive, Is.True);
                Assert.That(proposed.AuctionManagerId, Is.EqualTo(_managerUser.Id));
            });
        }

        /*
         * Перевіряє, що некоректні параметри лота (від'ємна ціна або дата завершення раніше за дату початку)
         * викликають BusinessRuleException із переліком усіх помилок.
         */
        [Test]
        public void CreateAuction_InvalidDatesAndPrice_ThrowsBusinessRuleExceptionWithMultipleErrors()
        {
            _userSession.SignIn(_managerUser);

            var invalidDto = new AuctionDTO
            {
                ProductId = Guid.Empty,
                StartPrice = -10m,
                StartDate = DateTime.Now.AddDays(5),
                EndDate = DateTime.Now
            };

            var ex = Assert.Throws<BusinessRuleException>(() => _auctionService.CreateAuction(invalidDto));
            Assert.That(ex!.Errors.Count, Is.GreaterThanOrEqualTo(3));
        }
    }
}
