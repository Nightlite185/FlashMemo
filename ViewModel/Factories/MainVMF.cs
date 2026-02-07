using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.ViewModel.Factories;

public class MainVMF(INavigationService ns, IWindowService ws)
{
    private readonly INavigationService navigationService = ns;
    private readonly IWindowService windowService = ws;

    public MainVM Create(long userId)
    {
        // any logic that runs to initialize mainVM goes here.
        return new(navigationService, windowService, userId);
    }
}