using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Services;
using FlashMemo.ViewModel.Factories;

namespace FlashMemo.ViewModel.Windows;

public partial class MainVM(INavigationService ns, BrowseVMF bVMF, long userId): ObservableObject, IViewModel
{
    private readonly INavigationService navService = ns;
    private readonly long userId = userId;
    private readonly BrowseVMF browseVMF = bVMF;
}
