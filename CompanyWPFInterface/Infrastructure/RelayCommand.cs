/*
 * Файл: RelayCommand.cs
 * Призначення: Реалізація інтерфейсу ICommand для прив'язки синхронних дій ViewModel до кнопок
 * та елементів керування у розмітці XAML.
 */

using System;
using System.Windows.Input;

namespace CompanyWPFInterface.Infrastructure
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public event EventHandler? CanExecuteChanged;

        /*
         * Ініціалізує команду з делегатом виконання без параметрів та необов'язковою умовою доступності.
         */
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            if (execute == null) throw new ArgumentNullException(nameof(execute));
            _execute = _ => execute();
            if (canExecute != null)
            {
                _canExecute = _ => canExecute();
            }
        }

        /*
         * Ініціалізує команду з делегатом виконання, що приймає параметр, та предикатом перевірки доступності.
         */
        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /*
         * Перевіряє, чи може команда бути виконана в поточному стані ViewModel.
         */
        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        /*
         * Виконує логіку команди, якщо перевірка CanExecute повертає true.
         */
        public void Execute(object? parameter)
        {
            if (CanExecute(parameter))
            {
                _execute(parameter);
            }
        }

        /*
         * Примусово сповіщає елементи керування про необхідність повторної перевірки стану активності команди.
         */
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
