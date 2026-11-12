/*
 * Файл: IHashService.cs
 * Призначення: Контракт криптографічного сервісу хешування та перевірки паролів з сіллю.
 */

namespace CompanyServiceLayer;

public interface IHashService
{
    /*
     * Метод: HashPassword
     * Призначення: Обчислює криптографічний хеш відкритого пароля з використанням унікальної солі.
     * Параметри:
     *   password - пароль у відкритому вигляді
     *   salt - сіль користувача (Guid)
     * Повертає: Масив байтів згенерованого хешу.
     */
    byte[] HashPassword(string password, Guid salt);

    /*
     * Метод: VerifyHashedPassword
     * Призначення: Звіряє введений пароль з еталонним хешем за фіксований час (захист від timing attack).
     * Параметри:
     *   password - введений пароль
     *   salt - сіль користувача
     *   expectedHash - очікуваний масив байтів хешу
     * Повертає: true, якщо хеші співпадають, інакше false.
     */
    bool VerifyHashedPassword(string password, Guid salt, byte[] expectedHash);
}
