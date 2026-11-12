/*
 * Файл: IViewHandler.cs
 * Призначення: Контракт диспетчера навігації та перемикання екранів додатку.
 * Зберігає посилання на поточне меню (CurrentView) та сповіщає про переходи через подію OnViewChanged.
 */

using System;
using CompanyServiceLayer.Views;

namespace CompanyServiceLayer.Navigation;

public interface IViewHandler
{
    /*
     * Властивість: CurrentView
     * Призначення: Поточне активне меню/екран із шильдиком IMenu.
     */
    IMenu? CurrentView { get; set; }

    /*
     * Подія: OnViewChanged
     * Призначення: Викликається щоразу при зміні активного меню.
     */
    event Action<IMenu>? OnViewChanged;

    /*
     * Метод: SwitchTo
     * Призначення: Здійснює перехід на нове меню, допущене шильдиком IMenu.
     * Параметри:
     *   newView - нове меню/екран
     */
    void SwitchTo(IMenu newView);
}
