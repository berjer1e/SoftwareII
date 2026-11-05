/*
 * Файл: ProductDalTests.cs
 * Призначення: Модульні та інтеграційні тести для класу ProductDAL.
 * Перевіряє CRUD-операції, пошук та сортування як на реальній базі даних softwrDB (із відкатом через TransactionScope),
 * так і в ізольованому середовищі Entity Framework Core InMemory.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using CompanyDataAccessLayer.DAL;
using CompanyDataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using DTO = CompanyDataTransferObject;

namespace Tests
{
    [TestFixture]
    [Category("DAL")]
    public class ProductDalTests
    {
        private ProductDAL _productDal = null!;
        private TransactionScope _scope = null!;

        private const string ConnectionString =
            "Server=localhost;Database=softwrDB;Integrated Security=True;TrustServerCertificate=True;";

        /*
         * Ініціалізує екземпляр ProductDAL та відкриває транзакцію TransactionScope перед кожним тестом.
         */
        [SetUp]
        public void Setup()
        {
            _productDal = new ProductDAL(ConnectionString);
            _scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        }

        /*
         * Відкочує зміни транзакції після завершення кожного тесту, зберігаючи цілісність реальної БД.
         */
        [TearDown]
        public void TearDown()
        {
            _scope.Dispose();
        }

        /*
         * Перевіряє, що метод GetAll повертає непорожній список товарів із бази даних softwrDB.
         */
        [Test]
        [Description("Перевірка отримання всіх товарів із реальної бази даних")]
        public void GetAll_Products_ReturnsListWithItems()
        {
            var products = _productDal.GetAll();

            Assert.Multiple(() =>
            {
                Assert.That(products, Is.Not.Null);
                Assert.That(products.Count, Is.GreaterThan(0));
            });
        }

        /*
         * Перевіряє пошук існуючого товару за його унікальним ідентифікатором.
         */
        [Test]
        public void GetById_ExistingProductId_ReturnsProduct()
        {
            var existingProduct = _productDal.GetAll().First();
            var result = _productDal.GetById(existingProduct.Id);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result!.Id, Is.EqualTo(existingProduct.Id));
                Assert.That(result.Name, Is.EqualTo(existingProduct.Name));
            });
        }

        /*
         * Перевіряє, що при запиті неіснуючого ідентифікатора повертається значення null.
         */
        [Test]
        public void GetById_NonExistentId_ReturnsNull()
        {
            var result = _productDal.GetById(Guid.NewGuid());
            Assert.That(result, Is.Null);
        }

        /*
         * Перевіряє додавання нового товару в реальну БД та автоматичну генерацію його ідентифікатора.
         */
        [Test]
        public void Add_ValidProduct_ReturnsTrueAndSetsNewId()
        {
            var existingProduct = _productDal.GetAll().First();
            var newProduct = new DTO.ProductDTO
            {
                Name = "Тестовий товар",
                Price = 1200m,
                Quantity = 5,
                ProductTypeId = existingProduct.ProductTypeId,
                ManufacturerId = existingProduct.ManufacturerId
            };

            bool isAdded = _productDal.Add(newProduct);

            Assert.Multiple(() =>
            {
                Assert.That(isAdded, Is.True);
                Assert.That(newProduct.Id, Is.Not.EqualTo(Guid.Empty));
            });
        }

        /*
         * Перевіряє оновлення назви та базової ціни існуючого товару в реальній БД.
         */
        [Test]
        public void Update_ExistingProduct_ReturnsTrueAndSavesChanges()
        {
            var productToUpdate = _productDal.GetAll().First();
            productToUpdate.Name = "Оновлена назва";
            productToUpdate.Price = 999m;

            bool isUpdated = _productDal.Update(productToUpdate);
            var updatedProduct = _productDal.GetById(productToUpdate.Id);

            Assert.Multiple(() =>
            {
                Assert.That(isUpdated, Is.True);
                Assert.That(updatedProduct, Is.Not.Null);
                Assert.That(updatedProduct!.Name, Is.EqualTo("Оновлена назва"));
                Assert.That(updatedProduct.Price, Is.EqualTo(999m));
            });
        }

        /*
         * Перевіряє видалення новоствореного товару з бази даних.
         */
        [Test]
        public void Delete_ExistingProduct_ReturnsTrueAndRemovesFromDb()
        {
            var existingProduct = _productDal.GetAll().First();
            var productToDelete = new DTO.ProductDTO
            {
                Id = Guid.NewGuid(),
                Name = "Товар для видалення",
                Price = 100m,
                Quantity = 1,
                ProductTypeId = existingProduct.ProductTypeId,
                ManufacturerId = existingProduct.ManufacturerId
            };
            _productDal.Add(productToDelete);

            bool isDeleted = _productDal.Delete(productToDelete.Id);
            var checkProduct = _productDal.GetById(productToDelete.Id);

            Assert.Multiple(() =>
            {
                Assert.That(isDeleted, Is.True);
                Assert.That(checkProduct, Is.Null);
            });
        }

        /*
         * Перевіряє роботу методів SearchAndSort і CRUD-операцій ProductDAL на ізольованій InMemory базі даних.
         */
        [Test]
        public void InMemory_SearchAndSort_FiltersAndOrdersCorrectly()
        {
            var options = new DbContextOptionsBuilder<SoftwrDbContext>()
                .UseInMemoryDatabase($"ProductDalInMemory_{Guid.NewGuid()}")
                .Options;

            Guid mfgId = Guid.NewGuid();
            Guid typeId = Guid.NewGuid();

            using (var db = new SoftwrDbContext(options))
            {
                db.Manufacturers.Add(new Manufacturer { ManufacturerId = mfgId, Name = "IBM", Country = "USA" });
                db.ProductTypes.Add(new ProductType { TypeId = typeId, TypeName = "Hardware" });
                db.SaveChanges();
            }

            var inMemoryDal = new ProductDAL(options);
            inMemoryDal.Add(new DTO.ProductDTO { Name = "Quotron Terminal", Price = 500m, Quantity = 2, ManufacturerId = mfgId, ProductTypeId = typeId });
            inMemoryDal.Add(new DTO.ProductDTO { Name = "Bloomberg Keyboard", Price = 150m, Quantity = 10, ManufacturerId = mfgId, ProductTypeId = typeId });

            var filtered = inMemoryDal.SearchAndSort("Quotron", "price", ascending: true);
            var sortedByPriceDesc = inMemoryDal.SearchAndSort(null, "price", ascending: false);

            Assert.Multiple(() =>
            {
                Assert.That(filtered, Has.Count.EqualTo(1));
                Assert.That(filtered[0].Name, Is.EqualTo("Quotron Terminal"));
                Assert.That(filtered[0].ManufacturerName, Is.EqualTo("IBM"));
                Assert.That(sortedByPriceDesc[0].Price, Is.EqualTo(500m));
                Assert.That(sortedByPriceDesc[1].Price, Is.EqualTo(150m));
            });
        }
    }
}
