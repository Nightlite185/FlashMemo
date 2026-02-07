using System.Runtime.CompilerServices;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Services;
using FlashMemo.View;
using FlashMemo.ViewModel.Factories;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel.Windows;

public partial class MainVM(INavigationService ns, IWindowService ws, long userId): ObservableObject, IViewModel
{
    [ObservableProperty]
    //* current User Control being displayed in the main window, bound to this VM
    public partial IViewModel CurrentVM { get; set; }

    [RelayCommand]
    public async Task NavToDecks()
    {
        navigator.NavigateTo<DecksVM>();
    }

    [RelayCommand]
    private async Task ShowBrowse()
    {
        await windowService.ShowBrowse(userId);
    }

    [RelayCommand]
    private async Task ShowUserSettings(long userId)
    {
        await windowService.ShowUserSettings(userId);
    }

    [RelayCommand]
    private async Task ChangeUser()
    {
        //* closing all windows despite Main (singleton),
        //* to avoid stale state and restart based on new user
        
        foreach (var view in App.Current.Windows)
            if (view is Window win and not MainWindow)
                win.Close();

        await windowService.ShowUserSelect();
    }

    [RelayCommand]
    private async Task ShowCreateCard(DeckNode selectedDeck)
    {
        windowService.ShowCreateCard(
            selectedDeck);
    }

    [RelayCommand]
    private async Task ShowOptions()
    {
        
    }

    [RelayCommand]
    private async Task NavToStats()
    {
        
    }

    #region private things
    private readonly INavigationService navigator = ns;
    private readonly IWindowService windowService = ws;
    private readonly long userId = userId;
    #endregion
}
