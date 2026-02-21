using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Popups;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel.Windows;

public partial class DecksVM: NavBaseVM, IPopupHost
{
    public DecksVM(IDeckRepo dr, IDeckTreeBuilder dtb, long userId)
    {
        deckTreeBuilder = dtb;
        deckRepo = dr;
        this.userId = userId;

        DeckTree = [];
    }

    #region methods
    private void PopupCancel()
    {
        CurrentPopup = null;
    }
    internal async Task SyncDeckTree()
    {
        DeckTree.Clear();

        // build root-level decks without parents (ParentId == null)
        var deckTree = await
            deckTreeBuilder.BuildCountedAsync(userId);

        DeckTree.AddRange(deckTree);
    }
    private async Task CreateDeck(string name)
    {
        Deck deck = Deck
            .CreateNew(name, userId);
        
        await deckRepo
            .AddNewDeck(deck);
        
        DeckNode node = new(
            deck: deck,
            children: [],
            countByState: new()
        );

        DeckTree.Add(node);
    }
    #endregion

    #region ICommands
    [RelayCommand]
    private void ShowCreateDeck()
    {
        //* show create deck popup, in which you give it a name.
        CurrentPopup = new EnterNameVM(CreateDeck, PopupCancel);
    }

    [RelayCommand]
    private async Task SyncDecks() => await SyncDeckTree();
    
    [RelayCommand]
    private void ShowReview(DeckNode deck)
        => OnReviewNavRequest?.Invoke(deck);

    [RelayCommand]
    private async Task ShowDeckOptions(DeckNode? deck)
    {
        if (deck is not null)
            await NavigateTo(new DeckOptionsNavRequest(deck.Id));
    }

    [RelayCommand]
    private async Task RemoveDeck(DeckNode deck)
    {
        throw new NotImplementedException();
    }

    [RelayCommand]
    private async Task RenameDeck(string newName)
    {
        throw new NotImplementedException();
    }
    #endregion

    #region public properties
    public ObservableCollection<DeckNode> DeckTree { get; init; }
    
    [ObservableProperty]
    public partial DeckNode? SelectedDeck { get; set; }

    [ObservableProperty]
    public partial PopupVMBase? CurrentPopup { get; set; }

    public event Func<IDeckMeta, Task>? OnReviewNavRequest;
    #endregion

    #region private things
    private readonly IDeckRepo deckRepo;
    private readonly IDeckTreeBuilder deckTreeBuilder;
    private long userId;
    #endregion
}
