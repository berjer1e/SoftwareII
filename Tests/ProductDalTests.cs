using CompanyDataAccessLayer.DAL;
using System.Transactions;
using DTO = CompanyDataTransferObject;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class ProductDalTests
    {
        private ProductDAL _productDal;
        private TransactionScope _scope;
        
        private const string ConnectionString = "Server=localhost;Database=softwrDB;Integrated Security=True;TrustServerCertificate=True;";

        [SetUp]
        public void Setup()
        {
            _productDal = new ProductDAL(ConnectionString);
            _scope = new TransactionScope();
        }

        [TearDown]
        public void TearDown()
        {
            _scope.Dispose();
        }

        [Test]
        public void GetAll_Products_ReturnsListWithItems()
        {
            var products = _productDal.GetAll();
            
            Assert.That(products, Is.Not.Null);
            Assert.That(products.Count, Is.GreaterThan(0));
        }

        [Test]
        public void GetById_ExistingProductId_ReturnsProduct()
        {
            var existingProduct = _productDal.GetAll().First();
            var result = _productDal.GetById(existingProduct.Id);
            
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result!.Id, Is.EqualTo(existingProduct.Id));
            });
        }

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
    }
}
