/*
 * Файл: AuctionDalTests.cs
 * Призначення: Модульні та інтеграційні тести для класу AuctionDAL.
 * Перевіряє CRUD-операції, деактивацію, верифікацію лотів, вибірку активних/завершених/непідтверджених
 * аукціонів та пошук і сортування як на реальній БД softwrDB, так і в режимі InMemory.
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
    public class AuctionDalTests
    {
        private AuctionDAL _auctionDal = null!;
        private ProductDAL _productDal = null!;
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
            _auctionDal = new AuctionDAL(ConnectionString);
            _productDal = new ProductDAL(ConnectionString);
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
         * Перевіряє отримання всіх аукціонів із реальної бази даних softwrDB.
         */
        [Test]
        public void GetAll_RealDb_ReturnsSeededAuctions()
        {
            var all = _auctionDal.GetAll();
            Assert.That(all.Count, Is.GreaterThan(0));
        }

        /*
         * Перевіряє створення, верифікацію та деактивацію аукціону в реальній базі даних.
         */
        [Test]
        public void Add_Verify_And_Deactivate_RealDb_WorkCorrectly()
        {
            var product = _productDal.GetAll().First();
            var user = _userDal.GetAll().First();

            var dto = new AuctionDTO
            {
                ProductId = product.Id,
                AuctionManagerId = user.Id,
                ProductOwnerId = user.Id,
                StartDate = DateTime.Now.AddHours(-1),
                EndDate = DateTime.Now.AddDays(10),
                StartPrice = 200m,
                EndPrice = 0m,
                MinBid = 0.50m,
                BuyoutPrice = 420m,
                IsVerified = false,
                IsActive = false
            };

            bool added = _auctionDal.Add(dto);
            var pending = _auctionDal.GetPendingVerification();
            bool verified = _auctionDal.VerifyAuction(dto.Id);
            var afterVerify = _auctionDal.GetById(dto.Id);
            bool deactivated = _auctionDal.Deactivate(dto.Id);
            var afterDeactivate = _auctionDal.GetById(dto.Id);

            Assert.Multiple(() =>
            {
                Assert.That(added, Is.True);
                Assert.That(pending.Any(a => a.Id == dto.Id), Is.True);
                Assert.That(verified, Is.True);
                Assert.That(afterVerify!.IsVerified, Is.True);
                Assert.That(afterVerify.IsActive, Is.True);
                Assert.That(deactivated, Is.True);
                Assert.That(afterDeactivate!.IsActive, Is.False);
            });
        }

        /*
         * Перевіряє фільтрацію та сортування аукціонів за різними полями в ізольованій базі даних InMemory.
         */
        [Test]
        public void InMemory_SearchAndSort_HandlesActiveFinishedAndSorting()
        {
            var options = new DbContextOptionsBuilder<SoftwrDbContext>()
                .UseInMemoryDatabase($"AuctionDalInMemory_{Guid.NewGuid()}")
                .Options;

            Guid mfgId = Guid.NewGuid();
            Guid typeId = Guid.NewGuid();
            Guid prod1Id = Guid.NewGuid();
            Guid prod2Id = Guid.NewGuid();
            Guid managerId = Guid.NewGuid();

            using (var db = new SoftwrDbContext(options))
            {
                db.Manufacturers.Add(new Manufacturer { ManufacturerId = mfgId, Name = "DEC", Country = "USA" });
                db.ProductTypes.Add(new ProductType { TypeId = typeId, TypeName = "Mainframe" });
                db.Products.Add(new Product { ProductId = prod1Id, ProductName = "Alpha Server", BasePrice = 1000m, Quantity = 1, ManufacturerId = mfgId, ProductTypeId = typeId });
                db.Products.Add(new Product { ProductId = prod2Id, ProductName = "VAX Station", BasePrice = 500m, Quantity = 1, ManufacturerId = mfgId, ProductTypeId = typeId });
                db.Users.Add(new User { UserId = managerId, UserName = "Sarah Connor", ContactEmail = "sarah@co.uk", UserRole = AppRoles.AuctionManagerRoleId, IsActive = true, Balance = 1000m });
                db.SaveChanges();
            }

            var dal = new AuctionDAL(options);
            dal.Add(new AuctionDTO
            {
                ProductId = prod1Id,
                AuctionManagerId = managerId,
                StartDate = DateTime.Now.AddDays(-1),
                EndDate = DateTime.Now.AddDays(5),
                StartPrice = 800m,
                EndPrice = 900m,
                MinBid = 10m,
                BuyoutPrice = 1800m,
                IsVerified = true,
                IsActive = true
            });
            dal.Add(new AuctionDTO
            {
                ProductId = prod2Id,
                AuctionManagerId = managerId,
                StartDate = DateTime.Now.AddDays(-10),
                EndDate = DateTime.Now.AddDays(-2),
                StartPrice = 300m,
                EndPrice = 350m,
                MinBid = 5m,
                BuyoutPrice = 700m,
                IsVerified = true,
                IsActive = false
            });

            var pendingLot = new AuctionDTO
            {
                ProductId = prod2Id,
                AuctionManagerId = managerId,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(7),
                StartPrice = 200m,
                EndPrice = 0m,
                MinBid = 1m,
                BuyoutPrice = 420m,
                IsVerified = false,
                IsActive = false
            };
            dal.Add(pendingLot);

            var active = dal.GetActive();
            var finished = dal.GetFinished();
            var pendingBeforeDeactivate = dal.GetPendingVerification();
            var searchAlpha = dal.SearchAndSort("Alpha", null, "startprice", ascending: false);

            dal.Deactivate(pendingLot.Id);
            var pendingAfterDeactivate = dal.GetPendingVerification();
            var finishedAfterDeactivate = dal.GetFinished();

            Assert.Multiple(() =>
            {
                Assert.That(active, Has.Count.EqualTo(1));
                Assert.That(active[0].ProductName, Is.EqualTo("Alpha Server"));
                Assert.That(finished, Has.Count.EqualTo(1));
                Assert.That(finished[0].ProductName, Is.EqualTo("VAX Station"));
                Assert.That(pendingBeforeDeactivate, Has.Count.EqualTo(1));
                Assert.That(pendingAfterDeactivate, Is.Empty);
                Assert.That(finishedAfterDeactivate, Has.Count.EqualTo(2));
                Assert.That(searchAlpha, Has.Count.EqualTo(1));
            });
        }
    }
}
