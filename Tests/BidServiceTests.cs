/*
 * Файл: BidServiceTests.cs
 * Призначення: Модульні тести для сервісу ставок і миттєвого викупу BidService з використанням Moq.
 * Перевіряє правила мінімальної ставки, списання коштів з балансу учасника, автоматичне повернення
 * коштів попередньому лідеру аукціону та закриття аукціону під час миттєвого викупу (Buyout).
 */

using System;
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
    public class BidServiceTests
    {
        private Mock<IBidDAL> _bidDalMock = null!;
        private Mock<IAuctionDAL> _auctionDalMock = null!;
        private Mock<IUserDAL> _userDalMock = null!;
        private Mock<IAppLogger> _loggerMock = null!;
        private IUserSession _userSession = null!;
        private BidService _bidService = null!;

        private UserDTO _bidderOne = null!;
        private UserDTO _bidderTwo = null!;
        private AuctionDTO _activeAuction = null!;

        /*
         * Налаштовує мок-об'єкти, тестових учасників та активний аукціон перед кожним тестом.
         */
        [SetUp]
        public void SetUp()
        {
            _bidDalMock = new Mock<IBidDAL>();
            _auctionDalMock = new Mock<IAuctionDAL>();
            _userDalMock = new Mock<IUserDAL>();
            _loggerMock = new Mock<IAppLogger>();
            _userSession = new UserSession();

            _bidderOne = new UserDTO
            {
                Id = Guid.NewGuid(),
                Name = "Mia Wallace",
                Email = "mia@m.co",
                RoleId = AppRoles.BuyerRoleId,
                IsActive = true,
                Balance = 1000m
            };

            _bidderTwo = new UserDTO
            {
                Id = Guid.NewGuid(),
                Name = "Luke Skywalker",
                Email = "luke@m.co",
                RoleId = AppRoles.BuyerRoleId,
                IsActive = true,
                Balance = 1000m
            };

            _activeAuction = new AuctionDTO
            {
                Id = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                AuctionManagerId = Guid.NewGuid(),
                StartDate = DateTime.Now.AddDays(-1),
                EndDate = DateTime.Now.AddDays(5),
                StartPrice = 100m,
                EndPrice = 0m,
                MinBid = 10m,
                BuyoutPrice = 220m,
                IsVerified = true,
                IsActive = true,
                LastBidId = null
            };

            _userDalMock.Setup(u => u.GetById(_bidderOne.Id)).Returns(() => _bidderOne);
            _userDalMock.Setup(u => u.GetById(_bidderTwo.Id)).Returns(() => _bidderTwo);
            _userDalMock.Setup(u => u.UpdateBalance(It.IsAny<Guid>(), It.IsAny<decimal>()))
                .Callback<Guid, decimal>((id, bal) =>
                {
                    if (id == _bidderOne.Id) _bidderOne.Balance = bal;
                    if (id == _bidderTwo.Id) _bidderTwo.Balance = bal;
                })
                .Returns(true);

            _auctionDalMock.Setup(a => a.GetById(_activeAuction.Id)).Returns(() => _activeAuction);
            _auctionDalMock.Setup(a => a.Update(It.IsAny<AuctionDTO>())).Returns(true);
            _bidDalMock.Setup(b => b.Add(It.IsAny<BidDTO>())).Returns(true);

            _bidService = new BidService(
                _bidDalMock.Object,
                _auctionDalMock.Object,
                _userDalMock.Object,
                _userSession,
                TimeProvider.System,
                _loggerMock.Object);
        }

        /*
         * Перевіряє подання першої ставки та наступної ставки іншим учасником із поверненням балансу першому учаснику.
         */
        [Test]
        [Description("Перевірка списання балансу нового лідера та повернення коштів попередньому лідеру")]
        public void PlaceBid_FirstAndSecondBidder_DeductsAndRefundsBalancesCorrectly()
        {
            _userSession.SignIn(_bidderOne);
            var firstBid = _bidService.PlaceBid(_activeAuction.Id, 100m);

            _bidDalMock.Setup(b => b.GetById(firstBid.Id)).Returns(firstBid);

            Assert.Multiple(() =>
            {
                Assert.That(_bidderOne.Balance, Is.EqualTo(900m));
                Assert.That(_activeAuction.EndPrice, Is.EqualTo(100m));
                Assert.That(_activeAuction.LastBidId, Is.EqualTo(firstBid.Id));
            });

            _userSession.SignIn(_bidderTwo);
            var secondBid = _bidService.PlaceBid(_activeAuction.Id, 115m);

            Assert.Multiple(() =>
            {
                Assert.That(_bidderOne.Balance, Is.EqualTo(1000m));
                Assert.That(_bidderTwo.Balance, Is.EqualTo(885m));
                Assert.That(_activeAuction.EndPrice, Is.EqualTo(115m));
                Assert.That(_activeAuction.LastBidId, Is.EqualTo(secondBid.Id));
            });
        }

        /*
         * Перевіряє відхилення ставки, яка менша за мінімальний крок або перевищує баланс користувача.
         */
        [Test]
        public void PlaceBid_TooLowOrInsufficientBalance_ThrowsBusinessRuleException()
        {
            _userSession.SignIn(_bidderOne);

            Assert.Multiple(() =>
            {
                var lowEx = Assert.Throws<BusinessRuleException>(() => _bidService.PlaceBid(_activeAuction.Id, 50m));
                Assert.That(lowEx!.Message, Does.Contain("менша за мінімально допустиму"));

                var balanceEx = Assert.Throws<BusinessRuleException>(() => _bidService.PlaceBid(_activeAuction.Id, 5000m));
                Assert.That(balanceEx!.Message, Does.Contain("Недостатньо коштів"));
            });
        }

        /*
         * Перевіряє миттєвий викуп лота (Buyout): повернення коштів попередньому лідеру, списання ціни викупу та закриття аукціону.
         */
        [Test]
        public void Buyout_ActiveAuctionWithPreviousBid_RefundsLeaderDeductsBuyoutAndClosesAuction()
        {
            _userSession.SignIn(_bidderOne);
            var firstBid = _bidService.PlaceBid(_activeAuction.Id, 120m);
            _bidDalMock.Setup(b => b.GetById(firstBid.Id)).Returns(firstBid);

            _userSession.SignIn(_bidderTwo);
            var buyoutBid = _bidService.Buyout(_activeAuction.Id);

            Assert.Multiple(() =>
            {
                Assert.That(buyoutBid.IsBuyout, Is.True);
                Assert.That(buyoutBid.Price, Is.EqualTo(220m));
                Assert.That(_bidderOne.Balance, Is.EqualTo(1000m));
                Assert.That(_bidderTwo.Balance, Is.EqualTo(780m));
                Assert.That(_activeAuction.IsActive, Is.False);
                Assert.That(_activeAuction.EndPrice, Is.EqualTo(220m));
            });
        }

        /*
         * Перевіряє, що поточний лідер торгів може підвищити власну ставку або виконати викуп,
         * використовуючи суму своєї попередньої заблокованої ставки разом із залишком на балансі.
         */
        [Test]
        public void PlaceBid_And_Buyout_WhenCurrentLeaderRaisesOwnBid_UsesEscrowedSelfRefund()
        {
            _activeAuction.BuyoutPrice = 800m;
            _userSession.SignIn(_bidderOne);

            var firstBid = _bidService.PlaceBid(_activeAuction.Id, 600m);
            _bidDalMock.Setup(b => b.GetById(firstBid.Id)).Returns(firstBid);
            Assert.That(_bidderOne.Balance, Is.EqualTo(400m));

            var raisedBid = _bidService.PlaceBid(_activeAuction.Id, 700m);
            _bidDalMock.Setup(b => b.GetById(raisedBid.Id)).Returns(raisedBid);
            Assert.That(_bidderOne.Balance, Is.EqualTo(300m));

            var buyoutBid = _bidService.Buyout(_activeAuction.Id);
            Assert.Multiple(() =>
            {
                Assert.That(buyoutBid.IsBuyout, Is.True);
                Assert.That(_bidderOne.Balance, Is.EqualTo(200m));
                Assert.That(_activeAuction.IsActive, Is.False);
            });
        }

        /*
         * Перевіряє, що ставка на аукціон, дата початку якого ще не настала (StartDate у майбутньому),
         * відхиляється винятком BusinessRuleException.
         */
        [Test]
        public void PlaceBid_WhenAuctionStartDateInFuture_ThrowsBusinessRuleException()
        {
            _activeAuction.StartDate = DateTime.Now.AddDays(2);
            _userSession.SignIn(_bidderOne);

            var ex = Assert.Throws<BusinessRuleException>(() => _bidService.PlaceBid(_activeAuction.Id, 110m));
            Assert.That(ex!.Message, Does.Contain("ще не розпочалися"));
        }

        /*
         * Перевіряє, що у разі збою оновлення аукціону в DAL виконується компенсуючий відкат:
         * нова ставка видаляється, а баланси поточного учасника і попереднього лідера відновлюються.
         */
        [Test]
        public void PlaceBid_WhenAuctionUpdateFails_RollsBackBalancesAndDeletesOrphanBid()
        {
            _userSession.SignIn(_bidderOne);
            var firstBid = _bidService.PlaceBid(_activeAuction.Id, 100m);
            _bidDalMock.Setup(b => b.GetById(firstBid.Id)).Returns(firstBid);

            _userSession.SignIn(_bidderTwo);
            _auctionDalMock.Setup(a => a.Update(It.IsAny<AuctionDTO>())).Returns(false);

            Assert.Throws<BusinessRuleException>(() => _bidService.PlaceBid(_activeAuction.Id, 120m));

            Assert.Multiple(() =>
            {
                Assert.That(_bidderOne.Balance, Is.EqualTo(900m));
                Assert.That(_bidderTwo.Balance, Is.EqualTo(1000m));
                _bidDalMock.Verify(b => b.Delete(It.IsAny<Guid>()), Times.Once);
            });
        }
    }
}
