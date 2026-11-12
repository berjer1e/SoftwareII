/*
 * Файл: IUserSession.cs
 * Призначення: Контракт для зберігання даних про активну сесію поточного користувача в пам'яті застосунку.
 */

using CompanyDataTransferObject;

namespace CompanyServiceLayer;

public interface IUserSession
{
    /*
     * Властивість: CurrentUser
     * Призначення: Дані поточного авторизованого користувача (або null, якщо входу не було).
     */
    UserDTO? CurrentUser { get; }

    /*
     * Властивість: LoginDate
     * Призначення: Точний час і дата, коли користувач увійшов у систему.
     */
    DateTime? LoginDate { get; }

    /*
     * Властивість: IsAuthenticated
     * Призначення: Прапорець, що вказує, чи є активна авторизована сесія.
     */
    bool IsAuthenticated { get; }

    /*
     * Метод: StartSession
     * Призначення: Зберігає дані користувача та фіксує поточний час як початок сесії.
     */
    void StartSession(UserDTO user);

    /*
     * Метод: ClearSession
     * Призначення: Очищає сесію при виході користувача з системи (Logout).
     */
    void ClearSession();
}