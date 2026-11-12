/*
 * Файл: RelayCommand.cs
 * Призначення: Універсальна реалізація інтерфейсу ICommand для зв'язування дій користувача у патерні MVVM.
 */

using System;
using System.Windows.Input;

namespace CompanyWPFInterface.Commands;

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    /*
     * Конструктор: RelayCommand
     * Призначення: Ініціалізує команду делегатами виконання та перевірки доступності.
     */
    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /*
     * Подія: CanExecuteChanged
     * Призначення: Повідомляє WPF про необхідність повторно перевірити доступність команди.
     */
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /*
     * Метод: CanExecute
     * Призначення: Визначає, чи може команда бути виконана в поточному стані.
     */
    public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);

    /*
     * Метод: Execute
     * Призначення: Виконує дію команди.
     */
    public void Execute(object? parameter) => _execute(parameter);
}
