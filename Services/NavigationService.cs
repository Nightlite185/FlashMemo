using FlashMemo.ViewModel;
using FlashMemo.ViewModel.WindowVMs;
using Microsoft.Extensions.DependencyInjection;

namespace FlashMemo.Services;
public class NavigationService(IServiceProvider isp, MainVM vm): INavigationService
{
    private readonly IServiceProvider sp = isp;
    private readonly MainVM mainVM = vm;

    public void NavigateTo<TViewModel>() where TViewModel : IViewModel
    {
        var vm = sp.GetRequiredService<TViewModel>();
        mainVM.CurrentVM = vm;
    }
}
