/*
 * Файл: ViewModelBase.cs
 * Призначення: Базовий клас для всіх ViewModel у патерні MVVM.
 * Реалізує інтерфейс INotifyPropertyChanged для автоматичного оновлення елементів керування WPF
 * під час зміни значень властивостей.
 */

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CompanyWPFInterface.Infrastructure
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        /*
         * Викликає подію PropertyChanged для сповіщення системи прив'язки даних WPF про зміну властивості.
         */
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /*
         * Встановлює нове значення поля резервного збереження, якщо воно відрізняється від поточного,
         * і сповіщає інтерфейс про зміну властивості.
         */
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /*
         * Віртуальний метод ініціалізації ViewModel, який викликається навігатором ViewHandler під час переходу на екран.
         */
        public virtual void Initialize()
        {
        }
    }
}
