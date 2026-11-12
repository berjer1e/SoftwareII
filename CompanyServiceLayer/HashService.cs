/*
 * Файл: HashService.cs
 * Призначення: Реалізація алгоритму хешування паролів PBKDF2 (SHA-512) згідно з вимогами методички.
 */

using System.Security.Cryptography;

namespace CompanyServiceLayer;

public class HashService : IHashService
{
    /*
     * Константа: Iterations
     * Призначення: Кількість раундів PBKDF2 (210 000 ітерацій згідно з методичкою та рекомендаціями OWASP).
     */
    public const int Iterations = 210_000;

    /*
     * Константа: HashSize
     * Призначення: Розмір вихідного хешу в байтах (32 байти).
     */
    public const int HashSize = 32;

    /*
     * Метод: HashPassword
     * Призначення: Обчислює криптографічний PBKDF2-хеш пароля за допомогою системного класу Rfc2898DeriveBytes.
     * Параметри:
     *   password - відкритий пароль
     *   salt - сіль користувача (Guid)
     * Повертає: Масив байтів згенерованого хешу.
     */
    public byte[] HashPassword(string password, Guid salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt.ToByteArray(),
            Iterations,
            HashAlgorithmName.SHA512,
            HashSize);
    }

    /*
     * Метод: VerifyHashedPassword
     * Призначення: Звіряє хеш пароля з еталоном за фіксований час через CryptographicOperations.FixedTimeEquals.
     * Параметри:
     *   password - відкритий пароль для перевірки
     *   salt - сіль користувача
     *   expectedHash - очікуваний масив байтів хешу
     * Повертає: true, якщо хеші збігаються, інакше false.
     */
    public bool VerifyHashedPassword(string password, Guid salt, byte[] expectedHash)
    {
        return CryptographicOperations.FixedTimeEquals(HashPassword(password, salt), expectedHash);
    }
}
