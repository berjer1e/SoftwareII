/*
 * Файл: ProductTypeService.cs
 * Призначення: Реалізація сервісу бізнес-логіки для керування довідником категорій (типів) товарів.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using CompanyDataAccessLayer.DAL;
using CompanyDataTransferObject;
using CompanyServiceLayer.Exceptions;

namespace CompanyServiceLayer.Services
{
    public class ProductTypeService : IProductTypeService
    {
        private readonly IProductTypeDAL _productTypeDal;

        /*
         * Ініціалізує сервіс категорій товарів екземпляром IProductTypeDAL.
         */
        public ProductTypeService(IProductTypeDAL productTypeDal)
        {
            _productTypeDal = productTypeDal ?? throw new ArgumentNullException(nameof(productTypeDal));
        }

        /*
         * Повертає список усіх категорій товарів із бази даних.
         */
        public List<ProductTypeDTO> GetAll() => _productTypeDal.GetAll();

        /*
         * Повертає категорію товару за її унікальним ідентифікатором.
         */
        public ProductTypeDTO? GetById(Guid id) => _productTypeDal.GetById(id);

        /*
         * Створює нову категорію товарів після перевірки непорожності та унікальності назви.
         */
        public ProductTypeDTO Create(ProductTypeDTO productType)
        {
            if (productType == null) throw new ArgumentNullException(nameof(productType));
            if (string.IsNullOrWhiteSpace(productType.Name))
            {
                throw new BusinessRuleException("Назва категорії товару не може бути порожньою.");
            }

            productType.Name = productType.Name.Trim();

            bool duplicateExists = _productTypeDal.GetAll()
                .Any(pt => string.Equals(pt.Name?.Trim(), productType.Name, StringComparison.OrdinalIgnoreCase));
            if (duplicateExists)
            {
                throw new BusinessRuleException($"Категорія '{productType.Name}' уже існує в довіднику.");
            }

            if (!_productTypeDal.Add(productType))
            {
                throw new BusinessRuleException("Не вдалося додати категорію товару до бази даних.");
            }

            return _productTypeDal.GetById(productType.Id) ?? productType;
        }

        /*
         * Оновлює назву категорії товару в базі даних.
         */
        public ProductTypeDTO Update(ProductTypeDTO productType)
        {
            if (productType == null) throw new ArgumentNullException(nameof(productType));
            if (string.IsNullOrWhiteSpace(productType.Name))
            {
                throw new BusinessRuleException("Назва категорії товару не може бути порожньою.");
            }

            productType.Name = productType.Name.Trim();

            if (!_productTypeDal.Update(productType))
            {
                throw new BusinessRuleException("Не вдалося оновити категорію товару.");
            }

            return _productTypeDal.GetById(productType.Id) ?? productType;
        }

        /*
         * Видаляє категорію товару за її ідентифікатором.
         */
        public bool Delete(Guid id) => _productTypeDal.Delete(id);
    }
}
