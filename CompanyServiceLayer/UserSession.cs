/*
 * Файл: UserSession.cs
 * Призначення: Реалізація сесії поточного користувача, що зберігає стан в оперативній пам'яті.
 */

using CompanyDataTransferObject;

namespace CompanyServiceLayer;

public class UserSession : IUserSession
{
    /*
     * Властивість: CurrentUser
     * Призначення: Зберігає DTO-модель поточного користувача.
     */
    public UserDTO? CurrentUser { get; private set; }

    /*
     * Властивість: LoginDate
     * Призначення: Зберігає часову мітку входу користувача.
     */
    public DateTime? LoginDate { get; private set; }

    /*
     * Властивість: IsAuthenticated
     * Призначення: Повертає true, якщо користувач авторизований (CurrentUser не null).
     */
    public bool IsAuthenticated => CurrentUser != null;

    /*
     * Метод: StartSession
     * Призначення: Ініціалізує сесію отриманим користувачем і фіксує поточний час (DateTime.Now).
     */
    public void StartSession(UserDTO user)
    {
        CurrentUser = user;
        LoginDate = DateTime.Now;
    }

    /*
     * Метод: ClearSession
     * Призначення: Очищає посилання на користувача та скидає дату сесії.
     */
    public void ClearSession()
    {
        CurrentUser = null;
        LoginDate = null;
    }
}
