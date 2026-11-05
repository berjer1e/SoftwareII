/*
 * Файл: LoginView.xaml.cs
 * Призначення: Code-behind елемента керування LoginView.
 * Забезпечує двосторонню синхронізацію захищеного елемента PasswordBox із властивістю Password у LoginViewModel.
 */

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using CompanyWPFInterface.ViewModels;

namespace CompanyWPFInterface.Views
{
    public partial class LoginView : UserControl
    {
        private bool _isUpdatingPassword;

        /*
         * Ініціалізує компонент LoginView та підписується на зміну контексту даних DataContext.
         */
        public LoginView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        /*
         * Підключає обробник події PropertyChanged при призначенні нової LoginViewModel.
         */
        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is INotifyPropertyChanged oldVm)
            {
                oldVm.PropertyChanged -= OnViewModelPropertyChanged;
            }

            if (e.NewValue is LoginViewModel newVm)
            {
                newVm.PropertyChanged += OnViewModelPropertyChanged;
                _isUpdatingPassword = true;
                TerminalPasswordBox.Password = newVm.Password;
                _isUpdatingPassword = false;
            }
        }

        /*
         * Оновлює вміст PasswordBox, якщо властивість Password була змінена програмно з ViewModel (наприклад, демо-кнопкою).
         */
        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_isUpdatingPassword)
            {
                return;
            }

            if (e.PropertyName == nameof(LoginViewModel.Password) && DataContext is LoginViewModel vm)
            {
                if (TerminalPasswordBox.Password != vm.Password)
                {
                    _isUpdatingPassword = true;
                    TerminalPasswordBox.Password = vm.Password;
                    _isUpdatingPassword = false;
                }
            }
        }

        /*
         * Передає введений користувачем пароль із елемента PasswordBox у властивість Password об'єкта LoginViewModel.
         */
        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingPassword)
            {
                return;
            }

            if (DataContext is LoginViewModel vm)
            {
                _isUpdatingPassword = true;
                vm.Password = TerminalPasswordBox.Password;
                _isUpdatingPassword = false;
            }
        }
    }
}
