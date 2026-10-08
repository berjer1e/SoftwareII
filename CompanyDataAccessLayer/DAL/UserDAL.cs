using CompanyDataTransferObject;
using Ent = CompanyDataAccessLayer.Entities;

namespace CompanyDataAccessLayer.DAL
{
    public interface IUserDAL : IBaseDAL<UserDTO> { }

    public class UserDAL : BaseDAL<Ent.User, UserDTO>, IUserDAL
    {
        public UserDAL(string connectionString) : base(connectionString) { }

        protected override UserDTO ToDto(Ent.User e) => new()
        {
            Id = e.UserId,
            Name = e.UserName ?? string.Empty,
            Email = e.ContactEmail ?? string.Empty,
            RegDate = e.RegDate,
            IsActive = e.IsActive,
            RoleId = e.UserRole ?? Guid.Empty
        };

        protected override Ent.User ToEntity(UserDTO d) => new()
        {
            UserId = d.Id,
            UserName = d.Name,
            ContactEmail = d.Email,
            RegDate = d.RegDate,
            IsActive = d.IsActive,
            UserRole = d.RoleId == Guid.Empty ? null : d.RoleId
        };
    }
}
