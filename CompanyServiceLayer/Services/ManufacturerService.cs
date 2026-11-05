/*
 * Файл: ManufacturerService.cs
 * Призначення: Реалізація сервісу бізнес-логіки для керування довідником виробників товарів.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using CompanyDataAccessLayer.DAL;
using CompanyDataTransferObject;
using CompanyServiceLayer.Exceptions;

namespace CompanyServiceLayer.Services
{
    public class ManufacturerService : IManufacturerService
    {
        private readonly IManufacturerDAL _manufacturerDal;

        /*
         * Ініціалізує сервіс виробників екземпляром IManufacturerDAL.
         */
        public ManufacturerService(IManufacturerDAL manufacturerDal)
        {
            _manufacturerDal = manufacturerDal ?? throw new ArgumentNullException(nameof(manufacturerDal));
        }

        /*
         * Повертає повний перелік виробників із бази даних.
         */
        public List<ManufacturerDTO> GetAll() => _manufacturerDal.GetAll();

        /*
         * Знаходить і повертає виробника за ідентифікатором.
         */
        public ManufacturerDTO? GetById(Guid id) => _manufacturerDal.GetById(id);

        /*
         * Перевіряє заповнення та унікальність назви й країни виробника і створює новий запис у базі даних.
         */
        public ManufacturerDTO Create(ManufacturerDTO manufacturer)
        {
            if (manufacturer == null) throw new ArgumentNullException(nameof(manufacturer));
            Validate(manufacturer);

            manufacturer.Name = manufacturer.Name.Trim();
            manufacturer.Country = manufacturer.Country?.Trim() ?? string.Empty;

            bool duplicateExists = _manufacturerDal.GetAll()
                .Any(m => string.Equals(m.Name?.Trim(), manufacturer.Name, StringComparison.OrdinalIgnoreCase));
            if (duplicateExists)
            {
                throw new BusinessRuleException($"Виробник із назвою '{manufacturer.Name}' уже існує в довіднику.");
            }

            if (!_manufacturerDal.Add(manufacturer))
            {
                throw new BusinessRuleException("Не вдалося додати виробника до бази даних.");
            }

            return _manufacturerDal.GetById(manufacturer.Id) ?? manufacturer;
        }

        /*
         * Оновлює запис виробника після перевірки коректності назви та країни.
         */
        public ManufacturerDTO Update(ManufacturerDTO manufacturer)
        {
            if (manufacturer == null) throw new ArgumentNullException(nameof(manufacturer));
            Validate(manufacturer);

            manufacturer.Name = manufacturer.Name.Trim();
            manufacturer.Country = manufacturer.Country?.Trim() ?? string.Empty;

            if (!_manufacturerDal.Update(manufacturer))
            {
                throw new BusinessRuleException("Не вдалося оновити дані виробника.");
            }

            return _manufacturerDal.GetById(manufacturer.Id) ?? manufacturer;
        }

        /*
         * Видаляє виробника за його ідентифікатором.
         */
        public bool Delete(Guid id) => _manufacturerDal.Delete(id);

        /*
         * Перевіряє непорожність назви та країни походження виробника.
         */
        private static void Validate(ManufacturerDTO manufacturer)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(manufacturer.Name))
            {
                errors.Add("Назва виробника не може бути порожньою.");
            }
            if (string.IsNullOrWhiteSpace(manufacturer.Country))
            {
                errors.Add("Країна виробника не може бути порожньою.");
            }
            if (errors.Count > 0)
            {
                throw new BusinessRuleException(errors);
            }
        }
    }
}
