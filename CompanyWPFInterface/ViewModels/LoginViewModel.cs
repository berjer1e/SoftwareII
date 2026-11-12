/*
 * Файл: LoginViewModel.cs
 * Призначення: ViewModel для форми входу та реєстрації користувача з валідацією (IDataErrorInfo) і командами (ICommand).
 */

using System;
using System.ComponentModel;
using System.Windows.Input;
using CompanyServiceLayer;
using CompanyServiceLayer.Navigation;
using CompanyWPFInterface.Commands;

namespace CompanyWPFInterface.ViewModels;

public class LoginViewModel : ViewModelBase, IDataErrorInfo
{
    private readonly IAuthService _authService;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _repeatedPassword = string.Empty;
    private bool _isRegisterMode;
    private string _statusMessage = string.Empty;
    private bool _isError;

    /*
     * Подія: LoginSucceeded
     * Призначення: Сповіщає оболонку вікна про успішну автентифікацію для переходу до головної панелі.
     */
    public event Action? LoginSucceeded;

    /*
     * Конструктор: LoginViewModel
     * Призначення: Ініціалізує ViewModel сервісом автентифікації та командами інтерфейсу.
     */
    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
        ActionCommand = new RelayCommand(_ => ExecuteAction(), _ => CanExecuteAction());
        ToggleModeCommand = new RelayCommand(_ => ExecuteToggleMode());
    }

    /*
     * Властивість: Username
     * Призначення: Логін користувача, введений у формі.
     */
    public string Username
    {
        get => _username;
        set
        {
            if (SetField(ref _username, value))
            {
                ClearStatus();
            }
        }
    }

    /*
     * Властивість: Password
     * Призначення: Пароль користувача, введений у формі.
     */
    public string Password
    {
        get => _password;
        set
        {
            if (SetField(ref _password, value))
            {
                OnPropertyChanged(nameof(RepeatedPassword));
                ClearStatus();
            }
        }
    }

    /*
     * Властивість: RepeatedPassword
     * Призначення: Повторний пароль для перевірки збігу під час реєстрації.
     */
    public string RepeatedPassword
    {
        get => _repeatedPassword;
        set
        {
            if (SetField(ref _repeatedPassword, value))
            {
                ClearStatus();
            }
        }
    }

    /*
     * Властивість: IsRegisterMode
     * Призначення: Визначає, чи активний режим реєстрації замість режиму входу.
     */
    public bool IsRegisterMode
    {
        get => _isRegisterMode;
        set
        {
            if (SetField(ref _isRegisterMode, value))
            {
                OnPropertyChanged(nameof(TitleText));
                OnPropertyChanged(nameof(ActionButtonText));
                OnPropertyChanged(nameof(ToggleModeButtonText));
                OnPropertyChanged(nameof(RepeatedPassword));
            }
        }
    }

    /*
     * Властивість: StatusMessage
     * Призначення: Текст повідомлення про помилку або успіх операції.
     */
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    /*
     * Властивість: IsError
     * Призначення: Вказує, чи є поточне повідомлення помилкою.
     */
    public bool IsError
    {
        get => _isError;
        set => SetField(ref _isError, value);
    }

    /*
     * Властивість: TitleText
     * Призначення: Заголовок форми залежно від активного режиму.
     */
    public string TitleText => IsRegisterMode ? "Реєстрація" : "Вхід до системи";

    /*
     * Властивість: ActionButtonText
     * Призначення: Текст головної кнопки дії залежно від активного режиму.
     */
    public string ActionButtonText => IsRegisterMode ? "Зареєструватися" : "Увійти";

    /*
     * Властивість: ToggleModeButtonText
     * Призначення: Текст кнопки перемикання між входом і реєстрацією.
     */
    public string ToggleModeButtonText => IsRegisterMode
        ? "Вже є акаунт? Увійти"
        : "Немає акаунту? Зареєструватися";

    /*
     * Команда: ActionCommand
     * Призначення: Виконує вхід або реєстрацію користувача.
     */
    public ICommand ActionCommand { get; }

    /*
     * Команда: ToggleModeCommand
     * Призначення: Перемикає режим форми між входом і реєстрацією.
     */
    public ICommand ToggleModeCommand { get; }

    /*
     * Властивість: Error
     * Призначення: Загальна помилка моделі для інтерфейсу IDataErrorInfo.
     */
    public string Error => string.Empty;

    /*
     * Індексатор: this[columnName]
     * Призначення: Виконує валідацію окремих полів форми входу та реєстрації.
     */
    public string this[string columnName]
    {
        get
        {
            return columnName switch
            {
                nameof(Username) when string.IsNullOrWhiteSpace(Username) || Username.Trim().Length < 2
                    => "Логін має містити щонайменше 2 символи.",
                nameof(Password) when string.IsNullOrEmpty(Password) || Password.Length < 4
                    => "Пароль має містити щонайменше 4 символи.",
                nameof(RepeatedPassword) when IsRegisterMode && RepeatedPassword != Password
                    => "Паролі не співпадають.",
                _ => string.Empty
            };
        }
    }

    private bool CanExecuteAction()
    {
        return string.IsNullOrEmpty(this[nameof(Username)])
            && string.IsNullOrEmpty(this[nameof(Password)])
            && string.IsNullOrEmpty(this[nameof(RepeatedPassword)]);
    }

    private void ExecuteAction()
    {
        if (IsRegisterMode)
        {
            bool isRegistered = _authService.Register(Username.Trim(), Password);
            if (!isRegistered)
            {
                SetStatus("Не вдалося створити акаунт (такий логін уже існує).", isError: true);
                return;
            }

            SetStatus("Акаунт створено! Тепер ви можете увійти.", isError: false);
            IsRegisterMode = false;
            Password = string.Empty;
            RepeatedPassword = string.Empty;
            return;
        }

        bool isAuthenticated = _authService.Login(Username.Trim(), Password);
        if (!isAuthenticated)
        {
            SetStatus("Невірний логін або пароль (або акаунт неактивний).", isError: true);
            return;
        }

        ClearStatus();
        LoginSucceeded?.Invoke();
    }

    private void ExecuteToggleMode()
    {
        IsRegisterMode = !IsRegisterMode;
        RepeatedPassword = string.Empty;
        ClearStatus();
    }

    private void SetStatus(string message, bool isError)
    {
        StatusMessage = message;
        IsError = isError;
    }

    private void ClearStatus()
    {
        StatusMessage = string.Empty;
        IsError = false;
    }
}
