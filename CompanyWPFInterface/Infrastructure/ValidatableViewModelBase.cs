/*
 * Файл: ValidatableViewModelBase.cs
 * Призначення: Базовий клас ViewModel із підтримкою інтерфейсу INotifyDataErrorInfo
 * для миттєвої валідації полів введення у WPF та блокування команд у разі некоректних даних.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace CompanyWPFInterface.Infrastructure
{
    public abstract class ValidatableViewModelBase : ViewModelBase, INotifyDataErrorInfo
    {
        private readonly Dictionary<string, List<string>> _errors = new(StringComparer.Ordinal);

        public bool HasErrors => _errors.Count > 0;

        public string FirstErrorMessage =>
            _errors.Values.SelectMany(x => x).FirstOrDefault() ?? string.Empty;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        /*
         * Повертає перелік помилок валідації для вказаної властивості або всі помилки об'єкта.
         */
        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                return _errors.Values.SelectMany(e => e).ToList();
            }

            return _errors.TryGetValue(propertyName, out var list)
                ? list
                : Enumerable.Empty<string>();
        }

        /*
         * Повертає типізований список помилок для конкретного поля.
         */
        public IReadOnlyList<string> GetPropertyErrors(string propertyName)
        {
            if (_errors.TryGetValue(propertyName, out var list))
            {
                return list;
            }

            return Array.Empty<string>();
        }

        /*
         * Додає нове повідомлення про помилку валідації для заданої властивості та сповіщає інтерфейс WPF.
         */
        protected void AddError(string propertyName, string error)
        {
            if (!_errors.TryGetValue(propertyName, out var list))
            {
                list = new List<string>();
                _errors[propertyName] = list;
            }

            if (!list.Contains(error))
            {
                list.Add(error);
                RaiseErrorsChanged(propertyName);
            }
        }

        /*
         * Очищує всі помилки валідації для вказаної властивості та оновлює стан інтерфейсу.
         */
        protected void ClearErrors(string propertyName)
        {
            if (_errors.Remove(propertyName))
            {
                RaiseErrorsChanged(propertyName);
            }
        }

        /*
         * Очищує всі зареєстровані помилки валідації в межах поточного об'єкта ViewModel.
         */
        protected void ClearAllErrors()
        {
            var keys = _errors.Keys.ToList();
            _errors.Clear();
            foreach (var key in keys)
            {
                RaiseErrorsChanged(key);
            }
        }

        /*
         * Генерує подію ErrorsChanged та оновлює залежні властивості HasErrors і FirstErrorMessage.
         */
        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            OnPropertyChanged(nameof(HasErrors));
            OnPropertyChanged(nameof(FirstErrorMessage));
        }
    }
}
