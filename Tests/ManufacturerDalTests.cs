/*
 * Файл: ManufacturerDalTests.cs
 * Призначення: Модульні та інтеграційні тести для класу ManufacturerDAL (реальна БД + InMemory).
 */

using System;
using System.Transactions;
using CompanyDataAccessLayer.DAL;
using CompanyDataAccessLayer.Entities;
using CompanyDataTransferObject;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    [Category("DAL")]
    public class ManufacturerDalTests
    {
        private ManufacturerDAL _dal = null!;
        private TransactionScope _scope = null!;

        private const string ConnectionString =
            "Server=localhost;Database=softwrDB;Integrated Security=True;TrustServerCertificate=True;";

        /*
         * Ініціалізує ManufacturerDAL та відкриває транзакцію перед кожним тестом.
         */
        [SetUp]
        public void Setup()
        {
            _dal = new ManufacturerDAL(ConnectionString);
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
         * Перевіряє CRUD-операції для довідника виробників у реальній базі даних softwrDB.
         */
        [Test]
        public void Crud_RealDb_WorksCorrectly()
        {
            var mfg = new ManufacturerDTO { Name = "Commodore Business Machines", Country = "USA" };
            bool added = _dal.Add(mfg);
            var fetched = _dal.GetById(mfg.Id);

            mfg.Country = "United States";
            bool updated = _dal.Update(mfg);
            bool deleted = _dal.Delete(mfg.Id);

            Assert.Multiple(() =>
            {
                Assert.That(added, Is.True);
                Assert.That(fetched, Is.Not.Null);
                Assert.That(updated, Is.True);
                Assert.That(deleted, Is.True);
                Assert.That(_dal.GetById(mfg.Id), Is.Null);
            });
        }

        /*
         * Перевіряє роботу ManufacturerDAL у середовищі InMemory.
         */
        [Test]
        public void InMemory_Crud_WorksCorrectly()
        {
            var options = new DbContextOptionsBuilder<SoftwrDbContext>()
                .UseInMemoryDatabase($"ManufacturerDalInMemory_{Guid.NewGuid()}")
                .Options;

            var dal = new ManufacturerDAL(options);
            var item = new ManufacturerDTO { Name = "Atari Corp", Country = "USA" };
            dal.Add(item);

            Assert.That(dal.GetAll(), Has.Count.EqualTo(1));
        }
    }
}
