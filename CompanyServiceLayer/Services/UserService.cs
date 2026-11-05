/*
 * Файл: UserService.cs
 * Призначення: Реалізація сервісу бізнес-логіки для роботи з обліковими записами користувачів та їхнім балансом.
 */

using System;
using System.Collections.Generic;
using CompanyDataAccessLayer.DAL;
using CompanyDataTransferObject;
using CompanyDataTransferObject.Constants;
using CompanyServiceLayer.Exceptions;
using CompanyServiceLayer.Security;
using CompanyServiceLayer.Session;

namespace CompanyServiceLayer.Services
{
    public class UserService : IUserService
    {
        private readonly IUserDAL _userDal;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUserSession _userSession;

        /*
         * Ініціалізує сервіс користувачів залежностями UserDAL, хешера паролів та сесії.
         */
        public UserService(IUserDAL userDal, IPasswordHasher passwordHasher, IUserSession userSession)
        {
            _userDal = userDal ?? throw new ArgumentNullException(nameof(userDal));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
        }

        /*
         * Повертає перелік усіх користувачів із бази даних.
         */
        public List<UserDTO> GetAll() => _userDal.GetAll();

        /*
         * Шукає та повертає користувача за його унікальним ідентифікатором.
         */
        public UserDTO? GetById(Guid id) => _userDal.GetById(id);

        /*
         * Шукає та повертає користувача за електронною поштою.
         */
        public UserDTO? GetByEmail(string email) => _userDal.GetByEmail(email);

        /*
         * Створює нового користувача після перевірки унікальності пошти, валідації полів і криптографічного хешування пароля.
         */
        public UserDTO CreateUser(UserDTO user, string rawPassword)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(user.Name))
            {
                errors.Add("Ім'я користувача не може бути порожнім.");
            }
            if (string.IsNullOrWhiteSpace(user.Email) || !user.Email.Contains('@'))
            {
                errors.Add("Вкажіть коректну адресу електронної пошти.");
            }
            if (string.IsNullOrWhiteSpace(rawPassword) || rawPassword.Length < 6)
            {
                errors.Add("Пароль повинен містити щонайменше 6 символів.");
            }
            if (user.Balance < 0m)
            {
                errors.Add("Початковий баланс не може бути від'ємним.");
            }
            if (errors.Count > 0)
            {
                throw new BusinessRuleException(errors);
            }

            if (_userDal.GetByEmail(user.Email) != null)
            {
                throw new BusinessRuleException("Користувач із такою електронною поштою вже існує.");
            }

            if (user.RoleId == Guid.Empty)
            {
                user.RoleId = AppRoles.BuyerRoleId;
            }

            user.PasswordHash = _passwordHasher.HashPassword(rawPassword);

            if (!_userDal.Add(user))
            {
                throw new BusinessRuleException("Не вдалося створити користувача в базі даних.");
            }

            return _userDal.GetById(user.Id) ?? user;
        }

        /*
         * Оновлює профіль існуючого користувача після перевірки коректності імені, пошти, унікальності пошти та балансу,
         * зберігаючи існуючий хеш пароля та роль, якщо вони не були передані.
         */
        public UserDTO UpdateUser(UserDTO user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(user.Name))
            {
                errors.Add("Ім'я користувача не може бути порожнім.");
            }
            if (string.IsNullOrWhiteSpace(user.Email) || !user.Email.Contains('@'))
            {
                errors.Add("Вкажіть коректну адресу електронної пошти.");
            }
            if (user.Balance < 0m)
            {
                errors.Add("Баланс користувача не може бути від'ємним.");
            }
            if (errors.Count > 0)
            {
                throw new BusinessRuleException(errors);
            }

            var existing = _userDal.GetById(user.Id);
            if (existing == null)
            {
                throw new BusinessRuleException("Користувача для оновлення не знайдено.");
            }

            var byEmail = _userDal.GetByEmail(user.Email);
            if (byEmail != null && byEmail.Id != user.Id)
            {
                throw new BusinessRuleException("Користувач із такою електронною поштою вже існує.");
            }

            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                user.PasswordHash = existing.PasswordHash;
            }
            if (user.RoleId == Guid.Empty)
            {
                user.RoleId = existing.RoleId;
            }

            if (!_userDal.Update(user))
            {
                throw new BusinessRuleException("Не вдалося оновити дані користувача.");
            }

            if (_userSession.CurrentUser != null && _userSession.CurrentUser.Id == user.Id)
            {
                _userSession.UpdateBalance(user.Balance);
            }

            return _userDal.GetById(user.Id) ?? user;
        }

        /*
         * Встановлює нове невід'ємне значення балансу користувача в базі даних і синхронізує поточну сесію.
         */
        public bool UpdateBalance(Guid userId, decimal newBalance)
        {
            if (newBalance < 0m)
            {
                throw new BusinessRuleException("Баланс користувача не може бути від'ємним.");
            }

            bool updated = _userDal.UpdateBalance(userId, newBalance);
            if (!updated)
            {
                throw new BusinessRuleException("Не вдалося оновити баланс користувача.");
            }

            if (_userSession.CurrentUser != null && _userSession.CurrentUser.Id == userId)
            {
                _userSession.UpdateBalance(newBalance);
            }

            return true;
        }
    }
}
