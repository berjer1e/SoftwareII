/*
 * Файл: ViewModelBase.cs
 * Призначення: Базовий клас для презентаційних моделей у шаблоні MVVM (запозичено з проєкту Game).
 * Реалізує інтерфейс INotifyPropertyChanged для реактивного зв'язування даних (Data Binding) з UI.
 */

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CompanyServiceLayer.Navigation;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    /*
     * Подія: PropertyChanged
     * Призначення: Сповіщає інтерфейс користувача (WPF або інший рендерер) про зміну значення властивості.
     */
    public event PropertyChangedEventHandler? PropertyChanged;

    /*
     * Метод: OnPropertyChanged
     * Призначення: Викликає подію PropertyChanged для переданого імені властивості.
     * Параметри:
     *   propertyName - ім'я властивості (підставляється автоматично через [CallerMemberName])
     */
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /*
     * Метод: SetField
     * Призначення: Оновлює значення поля та генерує подію сповіщення, якщо нове значення відрізняється від старого.
     * Параметри:
     *   field - посилання на приватне поле (ref)
     *   value - нове значення
     *   propertyName - ім'я властивості
     * Повертає: true, якщо значення змінилося; інакше false.
     */
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
