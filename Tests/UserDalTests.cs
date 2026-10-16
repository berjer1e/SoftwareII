using CompanyDataAccessLayer.DAL;
using System.Transactions;
using DTO = CompanyDataTransferObject;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class UserDalTests
    {
        private UserDAL _userDal;
        private RoleDAL _roleDal;
        private TransactionScope _scope;
        
        private const string ConnectionString = "Server=localhost;Database=softwrDB;Integrated Security=True;TrustServerCertificate=True;";

        [SetUp]
        public void Setup()
        {
            _userDal = new UserDAL(ConnectionString);
            _roleDal = new RoleDAL(ConnectionString);
            _scope = new TransactionScope();
        }

        [TearDown]
        public void TearDown()
        {
            _scope.Dispose();
        }

        [Test]
        public void GetAll_Users_ReturnsListWithItems()
        {
            var users = _userDal.GetAll();
            
            Assert.That(users, Is.Not.Null);
            Assert.That(users.Count, Is.GreaterThan(0));
        }

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

        [Test]
        public void Add_ValidUser_ReturnsTrueAndSetsNewId()
        {
            var role = _roleDal.GetAll().FirstOrDefault(r => r.Name == "Manager");
            var newUser = new DTO.UserDTO
            {
                Name = "Test Manager",
                Email = "test@ex.com",
                RoleId = role!.Id,
                IsActive = true,
                RegDate = DateOnly.FromDateTime(DateTime.Now)
            };

            bool isAdded = _userDal.Add(newUser);

            Assert.Multiple(() =>
            {
                Assert.That(isAdded, Is.True);
                Assert.That(newUser.Id, Is.Not.EqualTo(Guid.Empty));
            });
        }

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

        [Test]
        public void Delete_ExistingUser_ReturnsTrueAndRemovesFromDb()
        {
            var role = _roleDal.GetAll().FirstOrDefault(r => r.Name == "Manager");
            var userToDelete = new DTO.UserDTO
            {
                Id = Guid.NewGuid(),
                Name = "User to Delete",
                Email = "delete@ex.com",
                RoleId = role!.Id,
                IsActive = true,
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
    }
}
//Need some better tests because currently testing the DAL layer is not very useful,
//because it is just testing the EF Core functionality.
//The DAL layer should be tested with a mock database or an in-memory database
//to ensure that the methods are working correctly without relying on the actual database.
//Also, the tests should cover edge cases and error handling, such as trying to add a user
//with a duplicate email, updating a non-existent user, or deleting a user that is referenced
//by other entities.