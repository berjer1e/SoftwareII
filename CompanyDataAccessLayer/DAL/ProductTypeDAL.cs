using CompanyDataTransferObject;
using Ent = CompanyDataAccessLayer.Entities;

namespace CompanyDataAccessLayer.DAL
{
    public interface IProductTypeDAL : IBaseDAL<ProductTypeDTO> { }

    public class ProductTypeDAL : BaseDAL<Ent.ProductType, ProductTypeDTO>, IProductTypeDAL
    {
        public ProductTypeDAL(string connectionString) : base(connectionString) { }

        protected override ProductTypeDTO ToDto(Ent.ProductType e) => new()
        {
            Id = e.TypeId,
            Name = e.TypeName ?? string.Empty
        };

        protected override Ent.ProductType ToEntity(ProductTypeDTO d) => new()
        {
            TypeId = d.Id,
            TypeName = d.Name
        };
    }
}
