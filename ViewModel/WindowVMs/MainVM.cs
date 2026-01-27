using System.Windows.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FlashMemo.ViewModel.WindowVMs;

public partial class MainVM(NavigationService ns): ObservableObject, IViewModel
{
    [ObservableProperty]
    public partial IViewModel CurrentVM { get; set; }
    private readonly NavigationService navService = ns;
}
