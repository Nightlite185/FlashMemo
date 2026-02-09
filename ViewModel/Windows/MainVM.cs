using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.View;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel.Windows;

public partial class MainVM(IDisplayControl ds, ILastSessionService lss, IDeckRepo dr, long userId): BaseVM, IViewModel, IDisplayHost
{
    [ObservableProperty]
    //* current User Control being displayed in the main window, bound to this VM
    public partial IViewModel CurrentDisplay { get; set; }

    #region ICommands
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
    private async Task ShowCreateCard()
    {
        IDeckMeta deck; // declare deck variable
        
        // first we try getting deck from DecksVM if its active
        if (CurrentDisplay is DecksVM vm && vm.SelectedDeck is IDeckMeta meta)
            deck = meta;

        // else: try from last used cached deck
        else if (lastSession.Current.LastUsedDeckId is long id)
            deck = await deckRepo.GetDeckMetaById(id);

        // finally, just get first (oldest) deck from db with current user's id
        else deck = await deckRepo.GetFirstDeckMeta(userId);
        
        await NavigateTo(new CreateCardNavRequest(deck));
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
    #endregion

    #region private things
    private readonly IDisplayControl display = ds;
    private readonly IDeckRepo deckRepo = dr;
    private readonly ILastSessionService lastSession = lss;
    private readonly long userId = userId;
    #endregion
}