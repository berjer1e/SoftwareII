/*
 * Файл: AuthService.cs
 * Призначення: Реалізація сервісу автентифікації користувачів із захистом від атак за часом (Timing Attack),
 * перевіркою активності облікового запису, записом подій у журнал та керуванням сесією IUserSession.
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CompanyDataAccessLayer.DAL;
using CompanyDataAccessLayer.Logging;
using CompanyDataTransferObject;
using CompanyServiceLayer.Exceptions;
using CompanyServiceLayer.Security;
using CompanyServiceLayer.Session;

namespace CompanyServiceLayer.Services
{
    public class AuthService : IAuthService
    {
        private const string DummyStoredHash = "0000000000000000:e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

        private readonly IUserDAL _userDal;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUserSession _userSession;
        private readonly IAppLogger _logger;

        /*
         * Ініціалізує сервіс автентифікації необхідними залежностями DAL, хешера паролів, сесії та логера.
         */
        public AuthService(
            IUserDAL userDal,
            IPasswordHasher passwordHasher,
            IUserSession userSession,
            IAppLogger? logger = null)
        {
            _userDal = userDal ?? throw new ArgumentNullException(nameof(userDal));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
            _logger = logger ?? FileAppLogger.Instance;
        }

        /*
         * Перевіряє вхідні облікові дані, виконує криптографічну верифікацію пароля (включно з фіктивною перевіркою,
         * якщо користувача не знайдено), перевіряє статус активності та відкриває сесію користувача.
         */
        public UserDTO Login(string loginOrEmail, string password)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(loginOrEmail))
            {
                errors.Add("Введіть електронну пошту або ім'я користувача.");
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add("Введіть пароль.");
            }
            if (errors.Count > 0)
            {
                throw new BusinessRuleException(errors);
            }

            string normalized = loginOrEmail.Trim();
            UserDTO? user = _userDal.GetByEmail(normalized) ?? _userDal.GetByName(normalized);

            if (user == null || string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                _passwordHasher.VerifyPassword(password, DummyStoredHash);
                _logger.LogWarning($"Failed login attempt for unknown identifier '{normalized}'");
                throw new BusinessRuleException("Невірний логін або пароль.");
            }

            bool isValidPassword = _passwordHasher.VerifyPassword(password, user.PasswordHash);
            if (!isValidPassword)
            {
                _logger.LogWarning($"Failed login attempt (bad password) for user '{user.Email}'");
                throw new BusinessRuleException("Невірний логін або пароль.");
            }

            if (!user.IsActive)
            {
                _logger.LogWarning($"Blocked login attempt for inactive user '{user.Email}'");
                throw new BusinessRuleException("Обліковий запис користувача деактивовано.");
            }

            _userSession.SignIn(user);
            _logger.LogInfo($"User '{user.Name}' ({user.Email}, Role={user.RoleType}) logged in successfully");
            return user;
        }

        /*
         * Виконує автентифікацію користувача у фоновому потоці для забезпечення плавної роботи графічного інтерфейсу.
         */
        public Task<UserDTO> LoginAsync(string loginOrEmail, string password)
        {
            return Task.Run(() => Login(loginOrEmail, password));
        }

        /*
         * Виходить із поточного облікового запису та реєструє подію виходу в журналі.
         */
        public void Logout()
        {
            var current = _userSession.CurrentUser;
            _userSession.SignOut();
            if (current != null)
            {
                _logger.LogInfo($"User '{current.Name}' ({current.Email}) logged out");
            }
        }
    }
}
