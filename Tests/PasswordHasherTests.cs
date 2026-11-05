/*
 * Файл: PasswordHasherTests.cs
 * Призначення: Модульні тести для криптографічного сервісу PasswordHasher.
 * Перевіряє генерацію та перевірку хешів у форматах PBKDF2-SHA256 і salt:sha256_hex (формат тестових записів БД).
 */

using System;
using CompanyServiceLayer.Security;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    [Category("BusinessLogic")]
    public class PasswordHasherTests
    {
        private PasswordHasher _hasher = null!;

        /*
         * Ініціалізує екземпляр PasswordHasher один раз перед запуском набору тестів.
         */
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _hasher = new PasswordHasher();
        }

        /*
         * Перевіряє, що згенерований хеш PBKDF2 має правильний префікс і успішно проходить верифікацію для правильного пароля.
         */
        [Test]
        [Description("Перевірка циклу хешування та верифікації PBKDF2")]
        public void HashAndVerify_Pbkdf2Format_ReturnsTrueForMatchingPassword()
        {
            string hash = _hasher.HashPassword("Password123!");

            Assert.Multiple(() =>
            {
                Assert.That(hash, Does.StartWith("pbkdf2:100000:"));
                Assert.That(_hasher.VerifyPassword("Password123!", hash), Is.True);
                Assert.That(_hasher.VerifyPassword("WrongPassword!", hash), Is.False);
            });
        }

        /*
         * Перевіряє сумісність із форматом salt:sha256_hex, яким заповнені користувачі в базі даних softwrDB.
         */
        [Test]
        [TestCase("cb91fb50d51d4eaf:eaccd56d6f58557276c3ee5e3c4010f34a78f742f42ec51257ec146083a681e1", "Password123!", true)]
        [TestCase("36efa61319aa44d3:1d86f9f66af8a2ccc87402662e33b1732b60820b69ee83c324fc976649894dcd", "Password123!", true)]
        [TestCase("36efa61319aa44d3:1d86f9f66af8a2ccc87402662e33b1732b60820b69ee83c324fc976649894dcd", "BadPass", false)]
        [TestCase("invalid_format_without_colon", "Password123!", false)]
        [TestCase("", "Password123!", false)]
        public void VerifyPassword_SeededSaltSha256HexAndEdgeCases_ReturnsExpectedResult(
            string storedHash,
            string password,
            bool expected)
        {
            bool actual = _hasher.VerifyPassword(password, storedHash);
            Assert.That(actual, Is.EqualTo(expected));
        }

        /*
         * Перевіряє, що спроба захешувати порожній пароль викликає ArgumentException.
         */
        [Test]
        public void HashPassword_EmptyPassword_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _hasher.HashPassword("   "));
        }
    }
}
