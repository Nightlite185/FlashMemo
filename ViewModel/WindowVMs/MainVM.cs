using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Services;

namespace FlashMemo.ViewModel.WindowVMs;

public partial class MainVM(INavigationService ns): ObservableObject, IViewModel
{
    [ObservableProperty]
    public partial IViewModel CurrentVM { get; set; }
    private readonly INavigationService navService = ns;
    private long userId; // TODO: this somehow needs to be filled later, probably factory. NOT Initialize()
}
