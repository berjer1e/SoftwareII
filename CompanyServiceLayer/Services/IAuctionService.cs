/*
 * Файл: IAuctionService.cs
 * Призначення: Інтерфейс сервісу бізнес-логіки для керування аукціонами.
 * Визначає операції перегляду, фільтрації, створення, редагування, верифікації, деактивації та подання пропозицій лотів.
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CompanyDataTransferObject;

namespace CompanyServiceLayer.Services
{
    public interface IAuctionService
    {
        /*
         * Повертає список усіх аукціонів у системі.
         */
        List<AuctionDTO> GetAll();

        /*
         * Асинхронно повертає список усіх аукціонів у системі.
         */
        Task<List<AuctionDTO>> GetAllAsync();

        /*
         * Повертає список активних і підтверджених аукціонів.
         */
        List<AuctionDTO> GetActive();

        /*
         * Асинхронно повертає список активних і підтверджених аукціонів.
         */
        Task<List<AuctionDTO>> GetActiveAsync();

        /*
         * Повертає список завершених або деактивованих аукціонів.
         */
        List<AuctionDTO> GetFinished();

        /*
         * Повертає список лотів, запропонованих користувачами, які очікують підтвердження менеджером.
         */
        List<AuctionDTO> GetPendingVerification();

        /*
         * Асинхронно повертає список лотів, які очікують підтвердження менеджером.
         */
        Task<List<AuctionDTO>> GetPendingVerificationAsync();

        /*
         * Шукає та повертає аукціон за його унікальним ідентифікатором.
         */
        AuctionDTO? GetById(Guid id);

        /*
         * Виконує пошук, фільтрацію за станом активності та сортування списку аукціонів.
         */
        List<AuctionDTO> SearchAndSort(string? searchTerm, bool? isActive, string? sortBy, bool ascending = true);

        /*
         * Асинхронно виконує пошук, фільтрацію та сортування списку аукціонів.
         */
        Task<List<AuctionDTO>> SearchAndSortAsync(string? searchTerm, bool? isActive, string? sortBy, bool ascending = true);

        /*
         * Створює новий підтверджений та активний аукціон від імені менеджера аукціонів або адміністратора.
         */
        AuctionDTO CreateAuction(AuctionDTO auction);

        /*
         * Подає пропозицію нового лота від звичайного користувача зі статусом очікування верифікації менеджером.
         */
        AuctionDTO ProposeAuction(AuctionDTO auction);

        /*
         * Оновлює параметри існуючого аукціону (доступно лише менеджеру аукціонів або адміністратору).
         */
        AuctionDTO UpdateAuction(AuctionDTO auction);

        /*
         * Верифікує та активує запропонований користувачем аукціон (доступно лише менеджеру або адміністратору).
         */
        bool VerifyAuction(Guid auctionId);

        /*
         * Деактивує аукціон без фізичного видалення запису з бази даних (доступно лише менеджеру або адміністратору).
         */
        bool DeactivateAuction(Guid auctionId);
    }
}
