/*
 * Файл: IProductTypeService.cs
 * Призначення: Інтерфейс сервісу бізнес-логіки для роботи з довідником категорій (типів) товарів.
 */

using System;
using System.Collections.Generic;
using CompanyDataTransferObject;

namespace CompanyServiceLayer.Services
{
    public interface IProductTypeService
    {
        /*
         * Повертає список усіх категорій товарів.
         */
        List<ProductTypeDTO> GetAll();

        /*
         * Повертає категорію товару за її унікальним ідентифікатором.
         */
        ProductTypeDTO? GetById(Guid id);

        /*
         * Створює нову категорію товару в довіднику.
         */
        ProductTypeDTO Create(ProductTypeDTO productType);

        /*
         * Оновлює назву існуючої категорії товару.
         */
        ProductTypeDTO Update(ProductTypeDTO productType);

        /*
         * Видаляє категорію товару за її ідентифікатором.
         */
        bool Delete(Guid id);
    }
}
