/*
 * Файл: MainViewModel.cs
 * Призначення: ViewModel головної панелі аукціонів із командами (ICommand), валідацією (IDataErrorInfo) та рольовим доступом.
 */

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using CompanyDataTransferObject;
using CompanyServiceLayer;
using CompanyServiceLayer.Navigation;
using CompanyServiceLayer.Session;
using CompanyWPFInterface.Commands;

namespace CompanyWPFInterface.ViewModels;

public class MainViewModel : ViewModelBase, IDataErrorInfo
{
    private readonly IAuthService _authService;
    private readonly IUserSession _userSession;
    private readonly IAuctionService _auctionService;

    private string _headerUserText = "Користувач: —";
    private string _headerSessionText = "Сесія: — | Баланс: 0.00 грн";
    private bool _isManager;
    private AuctionDTO? _selectedAuction;
    private UserDTO? _selectedBuyer;
    private UserDTO? _selectedBalanceUser;
    private string _newBalanceText = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _isStatusError;

    /*
     * Подія: LogoutRequested
     * Призначення: Сповіщає вікно про вихід користувача для повернення на форму авторизації.
     */
    public event Action? LogoutRequested;

    /*
     * Конструктор: MainViewModel
     * Призначення: Ініціалізує ViewModel сервісами бізнес-логіки та командами взаємодії.
     */
    public MainViewModel(IAuthService authService, IUserSession userSession, IAuctionService auctionService)
    {
        _authService = authService;
        _userSession = userSession;
        _auctionService = auctionService;

        RefreshCommand = new RelayCommand(_ => LoadData());
        LogoutCommand = new RelayCommand(_ => ExecuteLogout());
        BuyoutCommand = new RelayCommand(_ => ExecuteBuyout(), _ => CanExecuteBuyout());
        UpdateBalanceCommand = new RelayCommand(_ => ExecuteUpdateBalance(), _ => CanExecuteUpdateBalance());
    }

    /*
     * Властивість: HeaderUserText
     * Призначення: Ім'я та роль поточного авторизованого користувача.
     */
    public string HeaderUserText
    {
        get => _headerUserText;
        private set => SetField(ref _headerUserText, value);
    }

    /*
     * Властивість: HeaderSessionText
     * Призначення: Дата початку сесії та поточний баланс користувача.
     */
    public string HeaderSessionText
    {
        get => _headerSessionText;
        private set => SetField(ref _headerSessionText, value);
    }

    /*
     * Властивість: IsManager
     * Призначення: Визначає, чи має поточний користувач права керування аукціонами та балансами.
     */
    public bool IsManager
    {
        get => _isManager;
        private set
        {
            if (SetField(ref _isManager, value))
            {
                OnPropertyChanged(nameof(AccessBannerText));
            }
        }
    }

    /*
     * Властивість: AccessBannerText
     * Призначення: Попередження про обмежений режим для користувачів без ролі менеджера чи адміністратора.
     */
    public string AccessBannerText => IsManager
        ? string.Empty
        : "Обмежений режим: ваша роль не має прав менеджера (доступний лише перегляд списку аукціонів).";

    /*
     * Властивість: Auctions
     * Призначення: Колекція аукціонів для відображення в таблиці.
     */
    public ObservableCollection<AuctionDTO> Auctions { get; } = new();

    /*
     * Властивість: Buyers
     * Призначення: Колекція покупців для вибору при викупі лота.
     */
    public ObservableCollection<UserDTO> Buyers { get; } = new();

    /*
     * Властивість: UsersForBalance
     * Призначення: Колекція всіх користувачів для керування балансами.
     */
    public ObservableCollection<UserDTO> UsersForBalance { get; } = new();

    /*
     * Властивість: SelectedAuction
     * Призначення: Поточний вибраний аукціон у таблиці.
     */
    public AuctionDTO? SelectedAuction
    {
        get => _selectedAuction;
        set => SetField(ref _selectedAuction, value);
    }

    /*
     * Властивість: SelectedBuyer
     * Призначення: Вибраний покупець для викупу лота.
     */
    public UserDTO? SelectedBuyer
    {
        get => _selectedBuyer;
        set => SetField(ref _selectedBuyer, value);
    }

    /*
     * Властивість: SelectedBalanceUser
     * Призначення: Вибраний користувач для редагування балансу.
     */
    public UserDTO? SelectedBalanceUser
    {
        get => _selectedBalanceUser;
        set
        {
            if (SetField(ref _selectedBalanceUser, value) && value != null)
            {
                NewBalanceText = value.Balance.ToString("F2", CultureInfo.InvariantCulture);
            }
        }
    }

    /*
     * Властивість: NewBalanceText
     * Призначення: Текстове поле введення нового балансу користувача.
     */
    public string NewBalanceText
    {
        get => _newBalanceText;
        set => SetField(ref _newBalanceText, value);
    }

    /*
     * Властивість: StatusMessage
     * Призначення: Повідомлення про результат останньої операції.
     */
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    /*
     * Властивість: IsStatusError
     * Призначення: Вказує, чи є поточне статус-повідомлення помилкою.
     */
    public bool IsStatusError
    {
        get => _isStatusError;
        private set => SetField(ref _isStatusError, value);
    }

    /*
     * Команда: RefreshCommand
     * Призначення: Оновлює списки аукціонів та користувачів із бази даних.
     */
    public ICommand RefreshCommand { get; }

    /*
     * Команда: LogoutCommand
     * Призначення: Завершує сесію користувача та повертає на екран входу.
     */
    public ICommand LogoutCommand { get; }

    /*
     * Команда: BuyoutCommand
     * Призначення: Виконує викуп вибраного активного лота з нарахуванням 10% комісії менеджеру.
     */
    public ICommand BuyoutCommand { get; }

    /*
     * Команда: UpdateBalanceCommand
     * Призначення: Зберігає новий баланс вибраного користувача.
     */
    public ICommand UpdateBalanceCommand { get; }

    /*
     * Властивість: Error
     * Призначення: Загальна помилка моделі для інтерфейсу IDataErrorInfo.
     */
    public string Error => string.Empty;

    /*
     * Індексатор: this[columnName]
     * Призначення: Валідація введеного значення балансу.
     */
    public string this[string columnName]
    {
        get
        {
            if (columnName == nameof(NewBalanceText))
            {
                if (string.IsNullOrWhiteSpace(NewBalanceText))
                {
                    return "Введіть суму балансу.";
                }

                if (!TryParseBalance(NewBalanceText, out decimal balance) || balance < 0)
                {
                    return "Баланс має бути невід'ємним числом.";
                }
            }

            return string.Empty;
        }
    }

    /*
     * Метод: LoadData
     * Призначення: Завантажує інформацію про поточну сесію, аукціони та користувачів.
     */
    public void LoadData()
    {
        IsManager = _authService.CanManageAuctions;

        var currentUser = _userSession.CurrentUser;
        if (currentUser != null)
        {
            var freshUser = _auctionService.GetAllUsers().FirstOrDefault(u => u.Id == currentUser.Id) ?? currentUser;
            HeaderUserText = $"Користувач: {freshUser.Name} ({freshUser.RoleType})";
            HeaderSessionText = $"Сесія з: {_userSession.LoginDate:dd.MM.yyyy HH:mm:ss} | Баланс: {freshUser.Balance:N2} грн";
        }

        Auctions.Clear();
        foreach (var auction in _auctionService.GetAllAuctions())
        {
            Auctions.Add(auction);
        }

        Buyers.Clear();
        UsersForBalance.Clear();
        foreach (var user in _auctionService.GetAllUsers())
        {
            UsersForBalance.Add(user);
            if (user.IsActive)
            {
                Buyers.Add(user);
            }
        }

        SelectedAuction = Auctions.FirstOrDefault(a => a.IsActive) ?? Auctions.FirstOrDefault();
        SelectedBuyer = Buyers.FirstOrDefault();
        SelectedBalanceUser = UsersForBalance.FirstOrDefault();
        SetStatus(string.Empty, isError: false);
    }

    private void ExecuteLogout()
    {
        _authService.Logout();
        LogoutRequested?.Invoke();
    }

    private bool CanExecuteBuyout()
    {
        return IsManager && SelectedAuction != null && SelectedAuction.IsActive && SelectedBuyer != null;
    }

    private void ExecuteBuyout()
    {
        if (SelectedAuction == null || SelectedBuyer == null)
        {
            SetStatus("Оберіть лот і покупця.", isError: true);
            return;
        }

        bool success = _auctionService.BuyoutAuction(SelectedAuction.Id, SelectedBuyer.Id, 0.10m);
        if (success)
        {
            LoadData();
            SetStatus("Лот успішно викуплено з комісією 10%.", isError: false);
        }
        else
        {
            SetStatus("Не вдалося викупити лот (перевірте баланс покупця та активність лота).", isError: true);
        }
    }

    private bool CanExecuteUpdateBalance()
    {
        return IsManager && SelectedBalanceUser != null && string.IsNullOrEmpty(this[nameof(NewBalanceText)]);
    }

    private void ExecuteUpdateBalance()
    {
        if (SelectedBalanceUser == null || !TryParseBalance(NewBalanceText, out decimal newBalance) || newBalance < 0)
        {
            SetStatus("Введіть коректний невід'ємний баланс.", isError: true);
            return;
        }

        bool updated = _auctionService.UpdateBalance(SelectedBalanceUser.Id, newBalance);
        if (updated)
        {
            string updatedName = SelectedBalanceUser.Name;
            LoadData();
            SetStatus($"Баланс '{updatedName}' оновлено до {newBalance:N2} грн.", isError: false);
        }
        else
        {
            SetStatus("Не вдалося оновити баланс.", isError: true);
        }
    }

    private void SetStatus(string message, bool isError)
    {
        StatusMessage = message;
        IsStatusError = isError;
    }

    private static bool TryParseBalance(string input, out decimal balance)
    {
        return decimal.TryParse(input.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out balance);
    }
}
