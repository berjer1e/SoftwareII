/*
 * Файл: ILoginMenu.cs
 * Призначення: Контракт вікна авторизації та реєстрації користувача.
 */

namespace CompanyServiceLayer.Views;

public interface ILoginMenu : IMenu
{
    /*
     * Метод: Login
     * Призначення: Виконує автентифікацію користувача за логіном та паролем.
     * Повертає: true, якщо вхід успішний; інакше false.
     */
    bool Login();

    /*
     * Метод: Register
     * Призначення: Реєструє нового користувача з перевіркою збігу паролів.
     */
    void Register();
}
