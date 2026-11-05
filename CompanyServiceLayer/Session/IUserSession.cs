/*
 * Файл: IUserSession.cs
 * Призначення: Інтерфейс керування поточною сесією автентифікованого користувача в межах програми.
 */

using System;
using CompanyDataTransferObject;

namespace CompanyServiceLayer.Session
{
    public interface IUserSession
    {
        UserDTO? CurrentUser { get; }
        bool IsAuthenticated { get; }
        bool CanManageAuctions { get; }
        event EventHandler? SessionChanged;

        /*
         * Фіксує вхід користувача в систему та зберігає його обліковий запис у поточній сесії.
         */
        void SignIn(UserDTO user);

        /*
         * Завершує поточну сесію користувача та очищує дані авторизації.
         */
        void SignOut();

        /*
         * Оновлює значення грошового балансу поточного авторизованого користувача в сесії.
         */
        void UpdateBalance(decimal newBalance);
    }
}
