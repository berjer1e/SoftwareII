/*
 * Файл: IViewHandler.cs
 * Призначення: Інтерфейс навігаційного обробника подань (ViewHandler) для перемикання активного екрана
 * у головному вікні без жорсткої прив'язки між ViewModel та елементами вікна.
 */

using System.ComponentModel;

namespace CompanyWPFInterface.Infrastructure
{
    public interface IViewHandler : INotifyPropertyChanged
    {
        ViewModelBase? CurrentView { get; }

        /*
         * Створює екземпляр вказаного типу ViewModel через DI-контейнер, ініціалізує його та встановлює як поточний екран.
         */
        void NavigateTo<TViewModel>() where TViewModel : ViewModelBase;

        /*
         * Встановлює переданий екземпляр ViewModel як поточний активний екран та викликає його ініціалізацію.
         */
        void NavigateTo(ViewModelBase viewModel);
    }
}
