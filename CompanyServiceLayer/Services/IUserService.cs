/*
 * Файл: IUserService.cs
 * Призначення: Інтерфейс сервісу бізнес-логіки для керування користувачами системи та їхніми балансами.
 */

using System;
using System.Collections.Generic;
using CompanyDataTransferObject;

namespace CompanyServiceLayer.Services
{
    public interface IUserService
    {
        /*
         * Повертає список усіх користувачів системи.
         */
        List<UserDTO> GetAll();

        /*
         * Повертає користувача за його унікальним ідентифікатором.
         */
        UserDTO? GetById(Guid id);

        /*
         * Повертає користувача за електронною поштою.
         */
        UserDTO? GetByEmail(string email);

        /*
         * Створює нового користувача з хешуванням його пароля.
         */
        UserDTO CreateUser(UserDTO user, string rawPassword);

        /*
         * Оновлює дані існуючого користувача.
         */
        UserDTO UpdateUser(UserDTO user);

        /*
         * Оновлює грошовий баланс користувача за його ідентифікатором.
         */
        bool UpdateBalance(Guid userId, decimal newBalance);
    }
}
