using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.View;
using FlashMemo.ViewModel.Bases;

namespace FlashMemo.ViewModel.Windows;

public partial class MainVM(ILastSessionService lss, IDeckRepo dr, long userId)
: NavBaseVM, IViewModel, IDisplayHost, IDialogClosedHandler
{
    // called by factory only. Need this two-step creation cuz circular dependency
    internal void Initialize(IDisplayControl dsControl)
    {
        display = dsControl;
        display.SwitchToDecks(UserId);
    }

    public async Task OnDialogClosed()
    {
        if (NotifyRefresh is not null)
            await NotifyRefresh.Invoke();
    }

    [ObservableProperty]
    //* current User Control being displayed in the main window, bound to this VM
    public partial object CurrentDisplay { get; set; }
    public long UserId { get; private set; } = userId;
    public event Func<Task>? NotifyRefresh;
    
    #region ICommands
    [RelayCommand]
    public async Task DisplayDecks()
    {
        if (CurrentDisplay is not DecksVM)
            await display.SwitchToDecks(UserId);
    }

    [RelayCommand]
    private async Task ShowBrowse()
    {
        await NavigateTo(new BrowseNavRequest(UserId));
    }

    [RelayCommand]
    private async Task ShowChangeUser()
    {
        //* closing all windows despite Main (singleton),
        //* to avoid stale state and restart based on new user
        
        foreach (var view in App.Current.Windows)
            if (view is Window win and not MainWindow)
                win.Close();

        await NavigateTo(new UserSelectNavRequest(UserId));
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
        else if (await deckRepo.GetFirstDeckMeta(UserId) is IDeckMeta first)
            deck = first;

        else return; // TODO: maybe show some warning messagebox in code-behind like "you have no decks so you cant add cards".
        
        await NavigateTo(new CreateCardNavRequest(deck, this));
    }

    [RelayCommand]
    private async Task ShowUserOptions()
    {
        await NavigateTo(new UserOptionsNavRequest(UserId));
    }

    [RelayCommand]
    private async Task DisplayStats()
    {
        if (CurrentDisplay is not StatsVM)
            await display.SwitchToStats(UserId);
    }
    #endregion

    #region private things
    private IDisplayControl display = null!;
    private readonly IDeckRepo deckRepo = dr;
    private readonly ILastSessionService lastSession = lss;    
    #endregion
}