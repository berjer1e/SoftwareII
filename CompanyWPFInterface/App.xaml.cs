/*
 * Файл: App.xaml.cs
 * Призначення: Точка входу WPF-додатку.
 * Зчитує конфігурацію appsettings.json, реєструє сервіси в DI-контейнері та ініціалізує програму.
 */

using System;
using System.IO;
using System.Windows;
using CompanyDataAccessLayer.DAL;
using CompanyServiceLayer;
using CompanyServiceLayer.Navigation;
using CompanyServiceLayer.Session;
using CompanyWPFInterface.ViewModels;
using CompanyWPFInterface.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CompanyWPFInterface;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    /*
     * Метод: OnStartup
     * Призначення: Завантажує конфігурацію, ініціалізує DI та запускає MainWindow.
     */
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Рядок підключення 'DefaultConnection' не знайдено в appsettings.json.");

        var services = new ServiceCollection();

        // Data Access Layer
        services.AddScoped<IUserDAL>(_ => new UserDAL(connectionString));
        services.AddScoped<IAuctionDAL>(_ => new AuctionDAL(connectionString));

        // Бізнес-сервіси
        services.AddSingleton<IUserSession, UserSession>();
        services.AddSingleton<IHashService, HashService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuctionService, AuctionService>();

        // Навігація
        services.AddSingleton<IViewHandler, ViewHandler>();

        // ViewModels
        services.AddTransient<LoginViewModel>();
        services.AddTransient<MainViewModel>();

        // Вікна та сторінки
        services.AddTransient<MainWindow>();
        services.AddTransient<LoginMenu>();

        Services = services.BuildServiceProvider();

        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
}
