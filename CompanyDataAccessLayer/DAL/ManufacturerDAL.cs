using CompanyDataTransferObject;
using Ent = CompanyDataAccessLayer.Entities;

namespace CompanyDataAccessLayer.DAL
{
    public interface IManufacturerDAL : IBaseDAL<ManufacturerDTO> { }

    public class ManufacturerDAL : BaseDAL<Ent.Manufacturer, ManufacturerDTO>, IManufacturerDAL
    {
        public ManufacturerDAL(string connectionString) : base(connectionString) { }

        protected override ManufacturerDTO ToDto(Ent.Manufacturer e) => new()
        {
            Id = e.ManufacturerId,
            Name = e.Name ?? string.Empty,
            Country = e.Country
        };

        protected override Ent.Manufacturer ToEntity(ManufacturerDTO d) => new()
        {
            ManufacturerId = d.Id,
            Name = d.Name,
            Country = d.Country
        };
    }
}
