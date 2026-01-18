using FlashMemo.ViewModel;
using Microsoft.Extensions.DependencyInjection;

namespace FlashMemo.Services
{
    public class NavigationService(IServiceProvider isp, MainVM vm)
    {
        private readonly IServiceProvider sp = isp;
        private readonly MainVM mainVM = vm;

        public void NavigateTo<TViewModel>() where TViewModel : IViewModel
        {
            var vm = sp.GetRequiredService<TViewModel>();
            mainVM.CurrentVM = vm;
        }
    }
}