/*
 * Файл: BidDalTests.cs
 * Призначення: Модульні та інтеграційні тести для класу BidDAL.
 * Перевіряє збереження ставок, отримання ставок за аукціоном і користувачем та визначення найвищої ставки.
 */

using System;
using System.Linq;
using System.Transactions;
using CompanyDataAccessLayer.DAL;
using CompanyDataAccessLayer.Entities;
using CompanyDataTransferObject;
using CompanyDataTransferObject.Constants;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    [Category("DAL")]
    public class BidDalTests
    {
        private BidDAL _bidDal = null!;
        private AuctionDAL _auctionDal = null!;
        private UserDAL _userDal = null!;
        private TransactionScope _scope = null!;

        private const string ConnectionString =
            "Server=localhost;Database=softwrDB;Integrated Security=True;TrustServerCertificate=True;";

        /*
         * Ініціалізує класи DAL та відкриває транзакцію перед кожним тестом.
         */
        [SetUp]
        public void Setup()
        {
            _bidDal = new BidDAL(ConnectionString);
            _auctionDal = new AuctionDAL(ConnectionString);
            _userDal = new UserDAL(ConnectionString);
            _scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        }

        /*
         * Відкочує транзакцію після кожного тесту.
         */
        [TearDown]
        public void TearDown()
        {
            _scope.Dispose();
        }

        /*
         * Перевіряє додавання ставок у реальну БД softwrDB та отримання найвищої ставки для аукціону.
         */
        [Test]
        public void Add_And_GetHighestBid_RealDb_ReturnsHighestBid()
        {
            var auction = _auctionDal.GetAll().First();
            var user = _userDal.GetAll().First();

            var bid1 = new BidDTO
            {
                AuctionId = auction.Id,
                BidOwner = user.Id,
                Price = auction.StartPrice + 10m,
                IsBuyout = false
            };
            var bid2 = new BidDTO
            {
                AuctionId = auction.Id,
                BidOwner = user.Id,
                Price = auction.StartPrice + 55m,
                IsBuyout = false
            };

            bool added1 = _bidDal.Add(bid1);
            bool added2 = _bidDal.Add(bid2);

            var byAuction = _bidDal.GetByAuctionId(auction.Id);
            var byUser = _bidDal.GetByUserId(user.Id);
            var highest = _bidDal.GetHighestBid(auction.Id);

            Assert.Multiple(() =>
            {
                Assert.That(added1, Is.True);
                Assert.That(added2, Is.True);
                Assert.That(byAuction.Count, Is.GreaterThanOrEqualTo(2));
                Assert.That(byUser.Any(b => b.Id == bid2.Id), Is.True);
                Assert.That(highest, Is.Not.Null);
                Assert.That(highest!.Price, Is.GreaterThanOrEqualTo(bid2.Price));
            });
        }

        /*
         * Перевіряє роботу BidDAL в ізольованій базі даних InMemory.
         */
        [Test]
        public void InMemory_BidCrudAndQueries_WorkCorrectly()
        {
            var options = new DbContextOptionsBuilder<SoftwrDbContext>()
                .UseInMemoryDatabase($"BidDalInMemory_{Guid.NewGuid()}")
                .Options;

            Guid auctionId = Guid.NewGuid();
            Guid userId = Guid.NewGuid();

            using (var db = new SoftwrDbContext(options))
            {
                db.Users.Add(new User { UserId = userId, UserName = "Mia Wallace", ContactEmail = "mia@m.co", UserRole = AppRoles.BuyerRoleId, IsActive = true, Balance = 1000m });
                db.SaveChanges();
            }

            var bidDal = new BidDAL(options);
            var dto = new BidDTO
            {
                AuctionId = auctionId,
                BidOwner = userId,
                Price = 250m,
                IsBuyout = true
            };

            bool added = bidDal.Add(dto);
            var fetched = bidDal.GetById(dto.Id);
            var highest = bidDal.GetHighestBid(auctionId);

            Assert.Multiple(() =>
            {
                Assert.That(added, Is.True);
                Assert.That(fetched, Is.Not.Null);
                Assert.That(fetched!.BidderName, Is.EqualTo("Mia Wallace"));
                Assert.That(highest!.Price, Is.EqualTo(250m));
                Assert.That(highest.IsBuyout, Is.True);
            });
        }
    }
}
