using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.View;
using FlashMemo.ViewModel.Bases;

namespace FlashMemo.ViewModel.Windows;

public partial class MainVM(ILastSessionService lss, IDeckRepo dr, IVMEventBus bus, long userId)
: BaseVM(bus), IViewModel, IDisplayHost
{
    // called by factory only. Need this two-step creation cuz circular dependency
    internal async Task Initialize(IDisplayControl dsControl)
    {
        display = dsControl;
        await display.SwitchToDecks(UserId);
    }

    [ObservableProperty]
    //* current User Control being displayed in the main window, bound to this VM
    public partial object CurrentDisplay { get; set; }
    public long UserId { get; private set; } = userId;
    
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
        IDeckMeta? deck = null;

        // First try the active deck selection. Creating a card has one target,
        // so an ambiguous multi-selection must be resolved by the user.
        if (CurrentDisplay is DecksVM vm)
        {
            int count = vm.SelectedDecks.Count;

            if (count > 1)
            {
                DialogService.Show(
                title: "Choose one deck",
                message: "Select exactly one deck before creating a card.",
                buttons: DialogButtons.OK,
                icon: DialogIcons.Information);

                return;
            }

            else if (count == 1)
                deck = (IDeckMeta)vm.SelectedDecks[0];
        }

        // Otherwise, try the last used cached deck.
        if (deck is null && lastSession.LastDeckId is long id)
            deck = await deckRepo.GetDeckMetaById(id);

        // Finally, get the first (oldest) deck for the current user.
        deck ??= await deckRepo.GetFirst(UserId);

        if (deck is null)
        {
            DialogService.Show(
                title: "No decks yet",
                message: "You don't have any decks to create new cards in. Add a new deck first!",
                buttons: DialogButtons.OK,
                icon: DialogIcons.Information);

            return;
        }
        
        await NavigateTo(new CreateCardNavRequest(deck));
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
