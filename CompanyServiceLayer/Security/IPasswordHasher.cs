/*
 * Файл: IPasswordHasher.cs
 * Призначення: Інтерфейс криптографічного сервісу для безпечного хешування паролів користувачів
 * та перевірки відповідності введеного пароля збереженому хешу.
 */

namespace CompanyServiceLayer.Security
{
    public interface IPasswordHasher
    {
        /*
         * Створює криптографічно стійкий хеш пароля з випадковою сіллю.
         */
        string HashPassword(string password);

        /*
         * Перевіряє, чи відповідає введений відкритий пароль збереженому хешу з бази даних.
         */
        bool VerifyPassword(string password, string? storedHash);
    }
}
