/*
 * Файл: IManufacturerService.cs
 * Призначення: Інтерфейс сервісу бізнес-логіки для роботи з довідником виробників товарів.
 */

using System;
using System.Collections.Generic;
using CompanyDataTransferObject;

namespace CompanyServiceLayer.Services
{
    public interface IManufacturerService
    {
        /*
         * Повертає список усіх виробників товарів.
         */
        List<ManufacturerDTO> GetAll();

        /*
         * Повертає виробника за його унікальним ідентифікатором.
         */
        ManufacturerDTO? GetById(Guid id);

        /*
         * Додає нового виробника до довідника.
         */
        ManufacturerDTO Create(ManufacturerDTO manufacturer);

        /*
         * Оновлює дані виробника в довіднику.
         */
        ManufacturerDTO Update(ManufacturerDTO manufacturer);

        /*
         * Видаляє виробника з довідника за його ідентифікатором.
         */
        bool Delete(Guid id);
    }
}
