/*
 * Файл: UserDalTests.cs
 * Призначення: Модульні та інтеграційні тести для класу UserDAL.
 * Перевіряє CRUD-операції, пошук за електронною поштою та іменем (GetByEmail, GetByName)
 * і оновлення балансу користувача (UpdateBalance) на реальній БД та в режимі InMemory.
 */

using System;
using System.Linq;
using System.Transactions;
using CompanyDataAccessLayer.DAL;
using CompanyDataAccessLayer.Entities;
using CompanyDataTransferObject.Constants;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using DTO = CompanyDataTransferObject;

namespace Tests
{
    [TestFixture]
    [Category("DAL")]
    public class UserDalTests
    {
        private UserDAL _userDal = null!;
        private TransactionScope _scope = null!;

        private const string ConnectionString =
            "Server=localhost;Database=softwrDB;Integrated Security=True;TrustServerCertificate=True;";

        /*
         * Ініціалізує UserDAL та відкриває транзакцію перед кожним тестом.
         */
        [SetUp]
        public void Setup()
        {
            _userDal = new UserDAL(ConnectionString);
            _scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        }

        /*
         * Скасовує транзакцію після кожного тесту для збереження стану БД.
         */
        [TearDown]
        public void TearDown()
        {
            _scope.Dispose();
        }

        /*
         * Перевіряє отримання повного списку користувачів із бази даних softwrDB.
         */
        [Test]
        public void GetAll_Users_ReturnsListWithItems()
        {
            var users = _userDal.GetAll();

            Assert.Multiple(() =>
            {
                Assert.That(users, Is.Not.Null);
                Assert.That(users.Count, Is.GreaterThanOrEqualTo(20));
            });
        }

        /*
         * Перевіряє отримання користувача за його унікальним ідентифікатором.
         */
        [Test]
        public void GetById_ExistingUserId_ReturnsUser()
        {
            var existingUser = _userDal.GetAll().First();
            var result = _userDal.GetById(existingUser.Id);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result!.Id, Is.EqualTo(existingUser.Id));
            });
        }

        /*
         * Перевіряє додавання нового користувача з роллю Менеджера аукціонів та балансом.
         */
        [Test]
        public void Add_ValidUser_ReturnsTrueAndSetsNewId()
        {
            var newUser = new DTO.UserDTO
            {
                Name = "Test Manager",
                Email = "test@ex.com",
                RoleId = AppRoles.AuctionManagerRoleId,
                IsActive = true,
                Balance = 1000m,
                RegDate = DateOnly.FromDateTime(DateTime.Now)
            };

            bool isAdded = _userDal.Add(newUser);

            Assert.Multiple(() =>
            {
                Assert.That(isAdded, Is.True);
                Assert.That(newUser.Id, Is.Not.EqualTo(Guid.Empty));
            });
        }

        /*
         * Перевіряє оновлення імені користувача в базі даних.
         */
        [Test]
        public void Update_ExistingUser_ReturnsTrueAndSavesChanges()
        {
            var userToUpdate = _userDal.GetAll().First();
            userToUpdate.Name = "Updated Name";

            bool isUpdated = _userDal.Update(userToUpdate);
            var updatedUser = _userDal.GetById(userToUpdate.Id);

            Assert.Multiple(() =>
            {
                Assert.That(isUpdated, Is.True);
                Assert.That(updatedUser, Is.Not.Null);
                Assert.That(updatedUser!.Name, Is.EqualTo("Updated Name"));
            });
        }

        /*
         * Перевіряє видалення новоствореного користувача з бази даних.
         */
        [Test]
        public void Delete_ExistingUser_ReturnsTrueAndRemovesFromDb()
        {
            var userToDelete = new DTO.UserDTO
            {
                Id = Guid.NewGuid(),
                Name = "User to Delete",
                Email = "delete@ex.com",
                RoleId = AppRoles.AuctionManagerRoleId,
                IsActive = true,
                Balance = 500m,
                RegDate = DateOnly.FromDateTime(DateTime.Now)
            };
            _userDal.Add(userToDelete);

            bool isDeleted = _userDal.Delete(userToDelete.Id);
            var checkUser = _userDal.GetById(userToDelete.Id);

            Assert.Multiple(() =>
            {
                Assert.That(isDeleted, Is.True);
                Assert.That(checkUser, Is.Null);
            });
        }

        /*
         * Перевіряє роботу методів GetByEmail, GetByName та UpdateBalance в ізольованій базі даних InMemory.
         */
        [Test]
        public void InMemory_GetByEmail_GetByName_And_UpdateBalance_WorkCorrectly()
        {
            var options = new DbContextOptionsBuilder<SoftwrDbContext>()
                .UseInMemoryDatabase($"UserDalInMemory_{Guid.NewGuid()}")
                .Options;

            var inMemoryUserDal = new UserDAL(options);
            var user = new DTO.UserDTO
            {
                Name = "Gordon Gekko",
                Email = "gekko@wallstreet.com",
                RoleId = AppRoles.AuctionManagerRoleId,
                Balance = 2500m,
                IsActive = true,
                PasswordHash = "salt:hash"
            };
            inMemoryUserDal.Add(user);

            var byEmail = inMemoryUserDal.GetByEmail("  GEKKO@WALLSTREET.COM ");
            var byName = inMemoryUserDal.GetByName("gordon gekko");
            bool balanceUpdated = inMemoryUserDal.UpdateBalance(user.Id, 4200.50m);
            var reloaded = inMemoryUserDal.GetById(user.Id);

            Assert.Multiple(() =>
            {
                Assert.That(byEmail, Is.Not.Null);
                Assert.That(byName, Is.Not.Null);
                Assert.That(balanceUpdated, Is.True);
                Assert.That(reloaded!.Balance, Is.EqualTo(4200.50m));
                Assert.That(inMemoryUserDal.GetByEmail(""), Is.Null);
                Assert.That(inMemoryUserDal.UpdateBalance(Guid.NewGuid(), 100m), Is.False);
            });
        }
    }
}
