/*
 * Файл: LoginViewModel.cs
 * Призначення: ViewModel екрана автентифікації користувача.
 * Забезпечує валідацію полів логіна/пошти та пароля через INotifyDataErrorInfo,
 * виклик сервісу автентифікації та маршрутизацію через ViewHandler відповідно до ролі
 * (повний термінал Менеджера аукціонів або обмежений термінал учасника торгів).
 */

using System;
using System.Threading.Tasks;
using CompanyServiceLayer.Exceptions;
using CompanyServiceLayer.Services;
using CompanyServiceLayer.Session;
using CompanyWPFInterface.Infrastructure;

namespace CompanyWPFInterface.ViewModels
{
    public class LoginViewModel : ValidatableViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly IUserSession _userSession;
        private readonly IViewHandler _viewHandler;

        private string _loginOrEmail = string.Empty;
        private string _password = string.Empty;
        private string _statusMessage = "ВВЕДІТЬ ОБЛІКОВІ ДАНІ ОПЕРАТОРА АБО ОБЕРІТЬ ТЕСТОВИЙ ПРОФІЛЬ.";
        private bool _isError;
        private bool _isBusy;

        public string LoginOrEmail
        {
            get => _loginOrEmail;
            set
            {
                if (SetProperty(ref _loginOrEmail, value))
                {
                    ValidateLoginOrEmail();
                    RaiseCommandStates();
                }
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                {
                    ValidatePassword();
                    RaiseCommandStates();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsError
        {
            get => _isError;
            set => SetProperty(ref _isError, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    RaiseCommandStates();
                }
            }
        }

        public RelayCommand LoginCommand { get; }
        public AsyncRelayCommand LoginAsyncCommand { get; }
        public RelayCommand FillManagerProfileCommand { get; }
        public RelayCommand FillTraderProfileCommand { get; }

        /*
         * Ініціалізує LoginViewModel сервісами автентифікації, сесії та навігації й налаштовує команди входу.
         */
        public LoginViewModel(
            IAuthService authService,
            IUserSession userSession,
            IViewHandler viewHandler)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
            _viewHandler = viewHandler ?? throw new ArgumentNullException(nameof(viewHandler));

            LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin);
            LoginAsyncCommand = new AsyncRelayCommand(ExecuteLoginAsync, CanExecuteLogin);
            FillManagerProfileCommand = new RelayCommand(FillManagerProfile);
            FillTraderProfileCommand = new RelayCommand(FillTraderProfile);
        }

        /*
         * Скидає поля форми та повідомлення про помилки під час повторного відкриття екрана входу.
         */
        public override void Initialize()
        {
            ClearAllErrors();
            Password = string.Empty;
            ClearErrors(nameof(Password));
            IsError = false;
            StatusMessage = "ВВЕДІТЬ ОБЛІКОВІ ДАНІ ОПЕРАТОРА АБО ОБЕРІТЬ ТЕСТОВИЙ ПРОФІЛЬ.";
        }

        /*
         * Визначає, чи можна виконати команду входу (поля непорожні, немає помилок валідації і не виконується запит).
         */
        public bool CanExecuteLogin()
        {
            return !IsBusy
                && !HasErrors
                && !string.IsNullOrWhiteSpace(LoginOrEmail)
                && !string.IsNullOrWhiteSpace(Password);
        }

        /*
         * Виконує синхронну автентифікацію користувача та перенаправляє його у відповідний термінал за роллю.
         */
        public void ExecuteLogin()
        {
            ValidateLoginOrEmail();
            ValidatePassword();
            if (HasErrors)
            {
                IsError = true;
                StatusMessage = FirstErrorMessage;
                return;
            }

            try
            {
                IsBusy = true;
                IsError = false;
                var user = _authService.Login(LoginOrEmail, Password);
                StatusMessage = $"ДОСТУП НАДАНО: {user.Name} ({user.RoleName})";
                RouteAuthenticatedUser();
            }
            catch (BusinessRuleException ex)
            {
                IsError = true;
                StatusMessage = ex.Message;
            }
            catch (Exception ex)
            {
                IsError = true;
                StatusMessage = $"СИСТЕМНА ПОМИЛКА АВТОРИЗАЦІЇ: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /*
         * Виконує асинхронну автентифікацію користувача без блокування графічного потоку та здійснює перехід за роллю.
         */
        public async Task ExecuteLoginAsync()
        {
            ValidateLoginOrEmail();
            ValidatePassword();
            if (HasErrors)
            {
                IsError = true;
                StatusMessage = FirstErrorMessage;
                return;
            }

            try
            {
                IsBusy = true;
                IsError = false;
                StatusMessage = "ПЕРЕВІРКА ОБЛІКОВИХ ДАНИХ У БАЗІ SOFTWRDB...";
                var user = await _authService.LoginAsync(LoginOrEmail, Password);
                StatusMessage = $"ДОСТУП НАДАНО: {user.Name} ({user.RoleName})";
                RouteAuthenticatedUser();
            }
            catch (BusinessRuleException ex)
            {
                IsError = true;
                StatusMessage = ex.Message;
            }
            catch (Exception ex)
            {
                IsError = true;
                StatusMessage = $"СИСТЕМНА ПОМИЛКА АВТОРИЗАЦІЇ: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /*
         * Заповнює поля входу тестовими даними Менеджера аукціонів (Sarah Connor) із бази даних softwrDB.
         */
        private void FillManagerProfile()
        {
            LoginOrEmail = "sarah@co.uk";
            Password = "Password123!";
            IsError = false;
            StatusMessage = "ПРОФІЛЬ МЕНЕДЖЕРА АУКЦІОНІВ (sarah@co.uk) ПІДГОТОВЛЕНО ДО ВХОДУ.";
        }

        /*
         * Заповнює поля входу тестовими даними учасника торгів / покупця (Mia Wallace) із бази даних softwrDB.
         */
        private void FillTraderProfile()
        {
            LoginOrEmail = "mia@m.co";
            Password = "Password123!";
            IsError = false;
            StatusMessage = "ПРОФІЛЬ УЧАСНИКА ТОРГІВ (mia@m.co) ПІДГОТОВЛЕНО ДО ВХОДУ.";
        }

        /*
         * Перевіряє права авторизованого користувача в сесії та перемикає ViewHandler на відповідний екран.
         */
        private void RouteAuthenticatedUser()
        {
            if (_userSession.CanManageAuctions)
            {
                _viewHandler.NavigateTo<AuctionManagerViewModel>();
            }
            else
            {
                _viewHandler.NavigateTo<TraderTerminalViewModel>();
            }
        }

        /*
         * Перевіряє коректність заповнення поля логіна або електронної пошти.
         */
        private void ValidateLoginOrEmail()
        {
            ClearErrors(nameof(LoginOrEmail));
            if (string.IsNullOrWhiteSpace(LoginOrEmail))
            {
                AddError(nameof(LoginOrEmail), "Поле логіна або електронної пошти є обов'язковим.");
            }
            else if (LoginOrEmail.Trim().Length < 3)
            {
                AddError(nameof(LoginOrEmail), "Логін або пошта повинні містити щонайменше 3 символи.");
            }
        }

        /*
         * Перевіряє непорожність і мінімальну довжину пароля користувача.
         */
        private void ValidatePassword()
        {
            ClearErrors(nameof(Password));
            if (string.IsNullOrWhiteSpace(Password))
            {
                AddError(nameof(Password), "Пароль не може бути порожнім.");
            }
            else if (Password.Length < 4)
            {
                AddError(nameof(Password), "Пароль повинен містити щонайменше 4 символи.");
            }
        }

        /*
         * Оновлює стан активності команд входу після зміни полів форми.
         */
        private void RaiseCommandStates()
        {
            LoginCommand.RaiseCanExecuteChanged();
            LoginAsyncCommand.RaiseCanExecuteChanged();
        }
    }
}
