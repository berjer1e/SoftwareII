/*
 * Файл: IProductService.cs
 * Призначення: Інтерфейс сервісу бізнес-логіки для керування каталогом товарів аукціону.
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CompanyDataTransferObject;

namespace CompanyServiceLayer.Services
{
    public interface IProductService
    {
        /*
         * Повертає список усіх товарів у каталозі.
         */
        List<ProductDTO> GetAll();

        /*
         * Асинхронно повертає список усіх товарів у каталозі.
         */
        Task<List<ProductDTO>> GetAllAsync();

        /*
         * Шукає та повертає товар за його унікальним ідентифікатором.
         */
        ProductDTO? GetById(Guid id);

        /*
         * Виконує пошук товарів за назвою та сортування за вказаним полем.
         */
        List<ProductDTO> SearchAndSort(string? searchTerm, string? sortBy, bool ascending = true);

        /*
         * Асинхронно виконує пошук і сортування товарів.
         */
        Task<List<ProductDTO>> SearchAndSortAsync(string? searchTerm, string? sortBy, bool ascending = true);

        /*
         * Створює новий товар у каталозі після валідації його параметрів.
         */
        ProductDTO CreateProduct(ProductDTO product);

        /*
         * Оновлює дані існуючого товару в каталозі (доступно менеджеру або адміністратору).
         */
        ProductDTO UpdateProduct(ProductDTO product);

        /*
         * Видаляє товар із каталогу за його ідентифікатором (доступно менеджеру або адміністратору).
         */
        bool DeleteProduct(Guid id);
    }
}
