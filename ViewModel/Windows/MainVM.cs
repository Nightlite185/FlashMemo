using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Services;
using FlashMemo.View;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel.Windows;

public partial class MainVM(IDisplayControl ds, long userId): BaseVM, IViewModel, IDisplayHost
{
    [ObservableProperty]
    //* current User Control being displayed in the main window, bound to this VM
    public partial IViewModel CurrentDisplay { get; set; }

    [RelayCommand]
    public async Task DisplayDecks()
    {
        await display.SwitchToDecks(userId);
    }

    [RelayCommand]
    private async Task ShowBrowse()
    {
        await NavigateTo(new BrowseNavRequest(userId));
    }

    [RelayCommand]
    private async Task ShowUserSettings(long userId)
    {
        await NavigateTo(new UserOptionsNavRequest(userId));
    }

    [RelayCommand]
    private async Task ChangeUser()
    {
        //* closing all windows despite Main (singleton),
        //* to avoid stale state and restart based on new user
        
        foreach (var view in App.Current.Windows)
            if (view is Window win and not MainWindow)
                win.Close();

        await NavigateTo(new UserSelectNavRequest());
    }

    [RelayCommand]
    private async Task ShowCreateCard(DeckNode selectedDeck)
    {
        await NavigateTo(new CreateCardNavRequest(selectedDeck));
    }

    [RelayCommand]
    private async Task ShowOptions()
    {
        await NavigateTo(new UserOptionsNavRequest(userId));
    }

    [RelayCommand]
    private async Task DisplayStats()
    {
        
    }

    #region private things
    private readonly IDisplayControl display = ds;
    private readonly long userId = userId;
    #endregion
}