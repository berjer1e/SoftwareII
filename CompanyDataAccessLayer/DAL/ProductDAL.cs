using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompanyDataAccessLayer.Entities;
using DTO = CompanyDataTransferObject;

namespace CompanyDataAccessLayer.DAL{
    

    public interface IProductDAL : IBaseDAL<DTO.ProductDTO> { }

    public class ProductDAL: BaseDAL<Product, DTO.ProductDTO>, IProductDAL
    {
        public ProductDAL(string connectionString) : base(connectionString)
        {
        }

        protected override DTO.ProductDTO ToDto(Product entity)
        {
            return new DTO.ProductDTO
            {
                Id = entity.ProductId,
                Name = entity.ProductName?? string.Empty,
                Price = (decimal)(entity.BasePrice?? decimal.Zero),
                ProductTypeId = entity.ProductTypeId,
                Quantity = entity.Quantity,
                ManufacturerId = entity.ManufacturerId
            };
        }

        protected override Product ToEntity(DTO.ProductDTO dto)
        {
            return new Product
            {
                ProductId = dto.Id,
                ProductName = dto.Name,
                ProductTypeId = dto.ProductTypeId,
                Quantity = dto.Quantity,
                BasePrice = dto.Price,
                ManufacturerId = dto.ManufacturerId

            };
        }
    }
}