/*
 * Файл: ViewHandler.cs
 * Призначення: Реалізація диспетчера навігації (запозичено з проєкту Game).
 * Зберігає поточне активне меню із шильдиком IMenu та сповіщає WPF-вікно про зміну через подію OnViewChanged.
 */

using System;
using CompanyServiceLayer.Views;

namespace CompanyServiceLayer.Navigation;

public class ViewHandler : IViewHandler
{
    private IMenu? _currentView;

    /*
     * Подія: OnViewChanged
     * Призначення: Сповіщає підписників (головне вікно WPF) про зміну активного меню.
     */
    public event Action<IMenu>? OnViewChanged;

    /*
     * Властивість: CurrentView
     * Призначення: Дозволяє отримувати та встановлювати поточне активне меню, генеруючи подію OnViewChanged.
     */
    public IMenu? CurrentView
    {
        get => _currentView;
        set
        {
            if (_currentView == value)
            {
                return;
            }

            _currentView = value;
            if (_currentView != null)
            {
                OnViewChanged?.Invoke(_currentView);
            }
        }
    }

    /*
     * Метод: SwitchTo
     * Призначення: Здійснює перехід на нове меню.
     * Параметри:
     *   newView - нове меню/екран, допущене шильдиком IMenu
     */
    public void SwitchTo(IMenu newView)
    {
        CurrentView = newView;
    }
}
