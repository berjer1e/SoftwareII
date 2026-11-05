/*
 * Файл: ProductTypeDalTests.cs
 * Призначення: Модульні та інтеграційні тести для класу ProductTypeDAL (реальна БД + InMemory).
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
    public class ProductTypeDalTests
    {
        private ProductTypeDAL _dal = null!;
        private TransactionScope _scope = null!;

        private const string ConnectionString =
            "Server=localhost;Database=softwrDB;Integrated Security=True;TrustServerCertificate=True;";

        /*
         * Ініціалізує ProductTypeDAL та відкриває транзакцію перед кожним тестом.
         */
        [SetUp]
        public void Setup()
        {
            _dal = new ProductTypeDAL(ConnectionString);
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
         * Перевіряє CRUD-операції для категорій товарів у реальній базі даних softwrDB.
         */
        [Test]
        public void Crud_RealDb_WorksCorrectly()
        {
            var type = new ProductTypeDTO { Name = "Retro Terminals" };
            bool added = _dal.Add(type);
            var fetched = _dal.GetById(type.Id);

            type.Name = "CRT Monitors";
            bool updated = _dal.Update(type);
            bool deleted = _dal.Delete(type.Id);

            Assert.Multiple(() =>
            {
                Assert.That(added, Is.True);
                Assert.That(fetched, Is.Not.Null);
                Assert.That(updated, Is.True);
                Assert.That(deleted, Is.True);
                Assert.That(_dal.GetById(type.Id), Is.Null);
            });
        }

        /*
         * Перевіряє роботу ProductTypeDAL у середовищі InMemory.
         */
        [Test]
        public void InMemory_Crud_WorksCorrectly()
        {
            var options = new DbContextOptionsBuilder<SoftwrDbContext>()
                .UseInMemoryDatabase($"ProductTypeDalInMemory_{Guid.NewGuid()}")
                .Options;

            var dal = new ProductTypeDAL(options);
            dal.Add(new ProductTypeDTO { Name = "Bonds" });

            Assert.That(dal.GetAll(), Has.Count.EqualTo(1));
        }
    }
}
