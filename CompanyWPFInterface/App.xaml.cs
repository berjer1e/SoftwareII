/*
 * Файл: App.xaml.cs
 * Призначення: Клас запуску WPF-програми з конфігурацією контейнера Dependency Injection (Microsoft.Extensions.DependencyInjection).
 * Реєструє всі рівні DAL, сервіси бізнес-логіки, сесію користувача, навігатор ViewHandler, ViewModel та MainWindow
 * із обов'язковою перевіркою графа залежностей під час побудови (ValidateOnBuild = true).
 */

using System;
using System.Windows;
using CompanyDataAccessLayer.DAL;
using CompanyDataAccessLayer.Logging;
using CompanyServiceLayer.Security;
using CompanyServiceLayer.Services;
using CompanyServiceLayer.Session;
using CompanyWPFInterface.Infrastructure;
using CompanyWPFInterface.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CompanyWPFInterface
{
    public partial class App : Application
    {
        public const string DefaultConnectionString =
            "Server=localhost;Database=softwrDB;Integrated Security=True;TrustServerCertificate=True;";

        public IServiceProvider? ServiceProvider { get; private set; }

        /*
         * Створює та налаштовує контейнер Dependency Injection з усіма сервісами програми та перевіркою ValidateOnBuild = true.
         */
        public static ServiceProvider ConfigureServices(string connectionString = DefaultConnectionString)
        {
            var services = new ServiceCollection();

            services.AddSingleton<IAppLogger>(_ => FileAppLogger.Instance);
            services.AddSingleton<TimeProvider>(_ => TimeProvider.System);
            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            services.AddSingleton<IUserSession, UserSession>();

            services.AddSingleton<IAuctionDAL>(sp => new AuctionDAL(connectionString, sp.GetRequiredService<IAppLogger>()));
            services.AddSingleton<IBidDAL>(sp => new BidDAL(connectionString, sp.GetRequiredService<IAppLogger>()));
            services.AddSingleton<IProductDAL>(sp => new ProductDAL(connectionString, sp.GetRequiredService<IAppLogger>()));
            services.AddSingleton<IUserDAL>(sp => new UserDAL(connectionString, sp.GetRequiredService<IAppLogger>()));
            services.AddSingleton<IManufacturerDAL>(sp => new ManufacturerDAL(connectionString, sp.GetRequiredService<IAppLogger>()));
            services.AddSingleton<IProductTypeDAL>(sp => new ProductTypeDAL(connectionString, sp.GetRequiredService<IAppLogger>()));

            services.AddSingleton<IAuthService, AuthService>();
            services.AddSingleton<IAuctionService, AuctionService>();
            services.AddSingleton<IBidService, BidService>();
            services.AddSingleton<IProductService, ProductService>();
            services.AddSingleton<IUserService, UserService>();
            services.AddSingleton<IManufacturerService, ManufacturerService>();
            services.AddSingleton<IProductTypeService, ProductTypeService>();

            services.AddSingleton<IViewHandler, ViewHandler>();

            services.AddTransient<LoginViewModel>();
            services.AddTransient<AuctionManagerViewModel>();
            services.AddTransient<TraderTerminalViewModel>();
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<MainWindow>();

            return services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });
        }

        /*
         * Ініціалізує DI-контейнер при старті програми та відкриває головне вікно MainWindow.
         */
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ServiceProvider = ConfigureServices();
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            mainWindow.Show();
        }
    }
}
