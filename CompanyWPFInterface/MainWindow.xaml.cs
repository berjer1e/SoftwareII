/*
 * Файл: MainWindow.xaml.cs
 * Призначення: Code-behind головного вікна програми WPF.
 * Отримує MainWindowViewModel через конструктор із контейнера впровадження залежностей (DI)
 * і встановлює її як DataContext вікна.
 */

using System;
using System.Windows;
using CompanyWPFInterface.ViewModels;

namespace CompanyWPFInterface
{
    public partial class MainWindow : Window
    {
        /*
         * Ініціалізує головне вікно та прив'язує передану через DI MainWindowViewModel до DataContext.
         */
        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }
    }
}
