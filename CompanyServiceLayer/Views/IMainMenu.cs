/*
 * Файл: IMainMenu.cs
 * Призначення: Контракт головного вікна/меню додатку (управління аукціонами).
 */

namespace CompanyServiceLayer.Views;

public interface IMainMenu : IMenu
{
    /*
     * Метод: Logout
     * Призначення: Завершує активну сесію та повертає користувача до екрана входу.
     */
    void Logout();
    void AuctionManagement();
}
