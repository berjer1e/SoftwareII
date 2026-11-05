/*
 * Файл: MainWindowViewModel.cs
 * Призначення: Головна ViewModel вікна-оболонки термінала Wall Street.
 * Керує верхньою інформаційною панеллю сесії (ім'я оператора, роль, поточний баланс),
 * годинником реального часу (RTC) та містить навігатор ViewHandler для відображення активного екрана.
 */

using System;
using System.Windows.Threading;
using CompanyServiceLayer.Services;
using CompanyServiceLayer.Session;
using CompanyWPFInterface.Infrastructure;

namespace CompanyWPFInterface.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly IUserSession _userSession;
        private readonly IAuthService _authService;
        private readonly TimeProvider _timeProvider;
        private readonly DispatcherTimer _rtcTimer;
        private string _rtcClockText = string.Empty;

        public IViewHandler ViewHandler { get; }

        public bool IsAuthenticated => _userSession.IsAuthenticated;

        public string OperatorStatusText => _userSession.CurrentUser != null
            ? $"ОПЕРАТОР: {_userSession.CurrentUser.Name.ToUpperInvariant()} [{_userSession.CurrentUser.RoleName}]"
            : "СТАТУС: ОЧІКУВАННЯ АВТОРИЗАЦІЇ [NO SESSION]";

        public string BalanceStatusText => _userSession.CurrentUser != null
            ? $"БАЛАНС РАХУНКУ: {_userSession.CurrentUser.Balance:F2} USD"
            : "БАЛАНС: ---.-- USD";

        public string RtcClockText
        {
            get => _rtcClockText;
            private set => SetProperty(ref _rtcClockText, value);
        }

        public RelayCommand LogoutCommand { get; }

        /*
         * Ініціалізує головну ViewModel вікна, запускає таймер RTC-годинника, підписується на події сесії та відкриває стартовий екран входу.
         */
        public MainWindowViewModel(
            IViewHandler viewHandler,
            IUserSession userSession,
            IAuthService authService,
            TimeProvider? timeProvider = null)
        {
            ViewHandler = viewHandler ?? throw new ArgumentNullException(nameof(viewHandler));
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _timeProvider = timeProvider ?? TimeProvider.System;

            _userSession.SessionChanged += OnSessionChanged;

            UpdateRtcClock();
            _rtcTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _rtcTimer.Tick += OnRtcTimerTick;
            _rtcTimer.Start();

            LogoutCommand = new RelayCommand(ExecuteLogout, () => IsAuthenticated);
            ViewHandler.NavigateTo<LoginViewModel>();
        }

        /*
         * Оновлює значення рядка RTC-годинника поточним локальним часом системи.
         */
        public void UpdateRtcClock()
        {
            DateTime now = _timeProvider.GetLocalNow().DateTime;
            RtcClockText = $"RTC // {now:dd.MM.yyyy HH:mm:ss}";
        }

        /*
         * Обробляє щосекундний сигнал таймера для оновлення показань RTC-годинника на верхній панелі термінала.
         */
        private void OnRtcTimerTick(object? sender, EventArgs e)
        {
            UpdateRtcClock();
        }

        /*
         * Оновлює індикатори сесії та балансу у верхньому рядку стану після входу, виходу або зміни балансу.
         */
        private void OnSessionChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(IsAuthenticated));
            OnPropertyChanged(nameof(OperatorStatusText));
            OnPropertyChanged(nameof(BalanceStatusText));
            LogoutCommand.RaiseCanExecuteChanged();
        }

        /*
         * Виконує вихід користувача із системи та повертає термінал на екран авторизації.
         */
        private void ExecuteLogout()
        {
            _authService.Logout();
            ViewHandler.NavigateTo<LoginViewModel>();
        }
    }
}
