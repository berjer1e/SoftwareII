/*
 * Файл: IAuthService.cs
 * Призначення: Інтерфейс сервісу автентифікації для входу користувача за логіном/поштою та паролем і виходу із сесії.
 */

using System.Threading.Tasks;
using CompanyDataTransferObject;

namespace CompanyServiceLayer.Services
{
    public interface IAuthService
    {
        /*
         * Виконує синхронну перевірку облікових даних користувача, авторизує його в сесії та повертає UserDTO.
         */
        UserDTO Login(string loginOrEmail, string password);

        /*
         * Виконує асинхронну автентифікацію користувача без блокування потоку інтерфейсу WPF.
         */
        Task<UserDTO> LoginAsync(string loginOrEmail, string password);

        /*
         * Завершує активну сесію поточного користувача.
         */
        void Logout();
    }
}
