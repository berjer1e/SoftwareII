/*
 * Файл: ViewHandler.cs
 * Призначення: Реалізація централізованого навігатора подань на основі DI-контейнера IServiceProvider.
 * Забезпечує динамічну зміну активної ViewModel у ContentControl головного вікна термінала.
 */

using System;
using Microsoft.Extensions.DependencyInjection;

namespace CompanyWPFInterface.Infrastructure
{
    public class ViewHandler : ViewModelBase, IViewHandler
    {
        private readonly IServiceProvider _serviceProvider;
        private ViewModelBase? _currentView;

        public ViewModelBase? CurrentView
        {
            get => _currentView;
            private set => SetProperty(ref _currentView, value);
        }

        /*
         * Ініціалізує навігатор подань посиланням на контейнер залежностей IServiceProvider.
         */
        public ViewHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /*
         * Отримує потрібну ViewModel із контейнера залежностей, викликає її метод Initialize і відображає у вікні.
         */
        public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
        {
            var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
            NavigateTo(viewModel);
        }

        /*
         * Перемикає активне подання на переданий екземпляр ViewModel після виклику його методу Initialize.
         */
        public void NavigateTo(ViewModelBase viewModel)
        {
            if (viewModel == null) throw new ArgumentNullException(nameof(viewModel));
            viewModel.Initialize();
            CurrentView = viewModel;
        }
    }
}
