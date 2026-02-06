using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.ViewModel.Factories;

public class MainVMF(INavigationService ns, BrowseVMF bVMF)
{
    private readonly INavigationService navigationService = ns;
    private readonly BrowseVMF browseVMF = bVMF;

    public MainVM Create(long userId)
    {
        // any logic that runs to initialize mainVM goes here.
        return new(navigationService, browseVMF, userId);
    }
}