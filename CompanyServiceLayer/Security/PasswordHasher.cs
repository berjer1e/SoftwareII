/*
 * Файл: PasswordHasher.cs
 * Призначення: Реалізація криптографічного хешування та перевірки паролів.
 * Підтримує сучасний формат PBKDF2-SHA256 для нових паролів і формат salt:sha256_hex
 * для зворотної сумісності з початковими тестовими записами бази даних softwrDB,
 * використовуючи порівняння за сталий час (FixedTimeEquals) для захисту від атак за часом.
 */

using System;
using System.Security.Cryptography;
using System.Text;

namespace CompanyServiceLayer.Security
{
    public class PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int DefaultIterations = 100_000;

        /*
         * Генерує криптографічний хеш пароля за алгоритмом PBKDF2-SHA256 із 16-байтною випадковою сіллю.
         */
        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Пароль не може бути порожнім.", nameof(password));
            }

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] key = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                DefaultIterations,
                HashAlgorithmName.SHA256,
                KeySize);

            return $"pbkdf2:{DefaultIterations}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(key)}";
        }

        /*
         * Перевіряє пароль за збереженим рядком хешу, підтримуючи формати PBKDF2 та salt:sha256_hex із захистом FixedTimeEquals.
         */
        public bool VerifyPassword(string password, string? storedHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(storedHash))
            {
                return false;
            }

            string[] parts = storedHash.Split(':');

            if (parts.Length == 4 && string.Equals(parts[0], "pbkdf2", StringComparison.OrdinalIgnoreCase))
            {
                return VerifyPbkdf2(password, parts[1], parts[2], parts[3]);
            }

            if (parts.Length == 2)
            {
                return VerifySaltSha256Hex(password, parts[0], parts[1]);
            }

            return false;
        }

        /*
         * Виконує перевірку пароля для формату PBKDF2 шляхом повторного обчислення ключа та порівняння за сталий час.
         */
        private static bool VerifyPbkdf2(string password, string iterationsPart, string saltBase64, string hashBase64)
        {
            try
            {
                if (!int.TryParse(iterationsPart, out int iterations) || iterations <= 0)
                {
                    return false;
                }

                byte[] salt = Convert.FromBase64String(saltBase64);
                byte[] expectedKey = Convert.FromBase64String(hashBase64);
                byte[] actualKey = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    iterations,
                    HashAlgorithmName.SHA256,
                    expectedKey.Length);

                return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
            }
            catch
            {
                return false;
            }
        }

        /*
         * Виконує перевірку пароля для формату salt:sha256_hex (SHA-256 від конкатенації солі та пароля) за сталий час.
         */
        private static bool VerifySaltSha256Hex(string password, string salt, string expectedHex)
        {
            try
            {
                byte[] expectedBytes = Convert.FromHexString(expectedHex.Trim());
                byte[] inputBytes = Encoding.UTF8.GetBytes(salt + password);
                byte[] actualBytes = SHA256.HashData(inputBytes);

                return CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
            }
            catch
            {
                return false;
            }
        }
    }
}
