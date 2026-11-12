/*
 * Файл: MainWindow.xaml.cs
 * Призначення: Головне вікно програми (MVVM: відповідає лише за навігацію між екранами без бізнес-логіки).
 */

using System.Windows;
using CompanyServiceLayer.Navigation;
using CompanyServiceLayer.Views;
using CompanyWPFInterface.ViewModels;
using CompanyWPFInterface.Windows;

namespace CompanyWPFInterface;

public partial class MainWindow : Window, IMainMenu
{
    private readonly IViewHandler _viewHandler;
    private readonly MainViewModel _mainViewModel;
    private readonly LoginViewModel _loginViewModel;
    private readonly object _dashboardContent;

    /*
     * Конструктор: MainWindow
     * Призначення: Ініціалізує вікно, підключає ViewModels та налаштовує перемикання екранів через IViewHandler.
     */
    public MainWindow(IViewHandler viewHandler, MainViewModel mainViewModel, LoginViewModel loginViewModel)
    {
        InitializeComponent();
        _dashboardContent = Content;

        _viewHandler = viewHandler;
        _mainViewModel = mainViewModel;
        _loginViewModel = loginViewModel;

        DataContext = _mainViewModel;

        _viewHandler.OnViewChanged += menu => Content = menu;
        _loginViewModel.LoginSucceeded += AuctionManagement;
        _mainViewModel.LogoutRequested += Logout;

        ShowLoginScreen();
    }

    /*
     * Метод: Logout
     * Призначення: Перемикає вміст вікна на сторінку авторизації.
     */
    public void Logout()
    {
        ShowLoginScreen();
    }

    /*
     * Метод: AuctionManagement
     * Призначення: Відображає панель керування аукціонами та ініціює завантаження даних у ViewModel.
     */
    public void AuctionManagement()
    {
        Content = _dashboardContent;
        _mainViewModel.LoadData();
    }

    private void ShowLoginScreen()
    {
        _viewHandler.SwitchTo(new LoginMenu(_loginViewModel));
    }
}
