/*
 * Файл: UserSession.cs
 * Призначення: Реалізація сховища сесії поточного користувача. Визначає статус автентифікації,
 * перевіряє наявність ролі менеджера аукціонів або адміністратора та сповіщає підписників про зміну сесії.
 */

using System;
using CompanyDataTransferObject;
using CompanyDataTransferObject.Enums;

namespace CompanyServiceLayer.Session
{
    public class UserSession : IUserSession
    {
        public UserDTO? CurrentUser { get; private set; }

        public bool IsAuthenticated => CurrentUser != null && CurrentUser.IsActive;

        public bool CanManageAuctions =>
            IsAuthenticated &&
            (CurrentUser!.RoleType == UserRoleType.AuctionManager || CurrentUser.RoleType == UserRoleType.Admin);

        public event EventHandler? SessionChanged;

        /*
         * Зберігає переданий об'єкт користувача як активного учасника сесії та викликає подію SessionChanged.
         */
        public void SignIn(UserDTO user)
        {
            CurrentUser = user ?? throw new ArgumentNullException(nameof(user));
            SessionChanged?.Invoke(this, EventArgs.Empty);
        }

        /*
         * Очищує поточного користувача сесії та сповіщає підписників про вихід із системи.
         */
        public void SignOut()
        {
            CurrentUser = null;
            SessionChanged?.Invoke(this, EventArgs.Empty);
        }

        /*
         * Оновлює баланс поточного користувача в пам'яті сесії та ініціює подію оновлення інтерфейсу.
         */
        public void UpdateBalance(decimal newBalance)
        {
            if (CurrentUser != null)
            {
                CurrentUser.Balance = newBalance;
                SessionChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
