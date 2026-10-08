using CompanyDataTransferObject;
using Ent = CompanyDataAccessLayer.Entities;

namespace CompanyDataAccessLayer.DAL
{
    public interface IRoleDAL : IBaseDAL<RoleDTO> { }

    public class RoleDAL : BaseDAL<Ent.Role, RoleDTO>, IRoleDAL
    {
        public RoleDAL(string connectionString) : base(connectionString) { }

        protected override RoleDTO ToDto(Ent.Role e) => new()
        {
            Id = e.RoleId,
            Name = e.RoleName.Trim()
        };

        protected override Ent.Role ToEntity(RoleDTO d) => new()
        {
            RoleId = d.Id,
            RoleName = d.Name
        };
    }
}
