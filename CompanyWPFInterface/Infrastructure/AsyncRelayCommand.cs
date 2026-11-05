/*
 * Файл: AsyncRelayCommand.cs
 * Призначення: Реалізація інтерфейсу ICommand для асинхронних операцій із захистом від повторного запуску
 * до завершення поточного завдання.
 */

using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CompanyWPFInterface.Infrastructure
{
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<object?, Task> _executeAsync;
        private readonly Predicate<object?>? _canExecute;
        private bool _isExecuting;

        public event EventHandler? CanExecuteChanged;

        /*
         * Ініціалізує асинхронну команду з делегатом Task без параметрів та умовою доступності.
         */
        public AsyncRelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
        {
            if (executeAsync == null) throw new ArgumentNullException(nameof(executeAsync));
            _executeAsync = _ => executeAsync();
            if (canExecute != null)
            {
                _canExecute = _ => canExecute();
            }
        }

        /*
         * Ініціалізує асинхронну команду з делегатом Task, що приймає параметр, та предикатом перевірки.
         */
        public AsyncRelayCommand(Func<object?, Task> executeAsync, Predicate<object?>? canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        /*
         * Визначає можливість виконання команди, блокуючи її, якщо попередній виклик ще триває.
         */
        public bool CanExecute(object? parameter)
        {
            return !_isExecuting && (_canExecute == null || _canExecute(parameter));
        }

        /*
         * Запускає асинхронне виконання команди.
         */
        public async void Execute(object? parameter)
        {
            await ExecuteAsync(parameter);
        }

        /*
         * Асинхронно виконує делегат команди з автоматичним керуванням прапорцем зайнятості _isExecuting.
         */
        public async Task ExecuteAsync(object? parameter = null)
        {
            if (!CanExecute(parameter))
            {
                return;
            }

            try
            {
                _isExecuting = true;
                RaiseCanExecuteChanged();
                await _executeAsync(parameter);
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        /*
         * Сповіщає інтерфейс користувача про зміну стану доступності асинхронної команди.
         */
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
