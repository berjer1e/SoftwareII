/*
 * Файл: LoginMenu.xaml.cs
 * Призначення: View форми входу та реєстрації користувача (MVVM: без бізнес-логіки у code-behind).
 */

using System.Windows.Controls;
using CompanyServiceLayer.Views;
using CompanyWPFInterface.ViewModels;

namespace CompanyWPFInterface.Windows;

public partial class LoginMenu : Page, ILoginMenu
{
    /*
     * Конструктор: LoginMenu
     * Призначення: Ініціалізує XAML-розмітку та прив'язує DataContext до LoginViewModel.
     */
    public LoginMenu(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    /*
     * Метод: Login
     * Призначення: Реалізація контракту ILoginMenu для сумісності з шаром навігації.
     */
    public bool Login() => true;

    /*
     * Метод: Register
     * Призначення: Реалізація контракту ILoginMenu для сумісності з шаром навігації.
     */
    public void Register()
    {
    }
}
