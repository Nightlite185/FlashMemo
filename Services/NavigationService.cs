using FlashMemo.ViewModel;
using FlashMemo.ViewModel.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace FlashMemo.Services;
public class NavigationService(IServiceProvider isp, MainVM vm): INavigationService
{
    private readonly IServiceProvider sp = isp;
    private readonly MainVM mainVM = vm;

    public void NavigateTo<TViewModel>() where TViewModel : IViewModel
    {
        mainVM.CurrentVM = sp.GetRequiredService<TViewModel>();
    }
}
