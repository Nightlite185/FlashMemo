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

public partial class DecksVM: BaseVM, IPopupHost
{
    public DecksVM(IDeckRepo dr, IDeckTreeBuilder dtb, long userId, IVMEventBus bus): base(bus)
    {
        deckTreeBuilder = dtb;
        deckRepo = dr;
        this.userId = userId;

        DeckTree = [];
    }

    #region methods
    internal async Task InitAsync()
    {
        eventBus.DomainChanged += OnDomainChanged;
        await ReloadDomainAsync();
    }
    private void PopupCancel() => CurrentPopup = null;
    
    private async Task CreateDeck(string name, DeckNode? parent = null)
    {
        Deck deck = Deck.CreateNew(
            name, userId, parent?.Id);
        
        DeckNode node = new(
            deck: deck,
            children: [],
            countByState: new());

        if (parent is not null)
            parent.AddChild(node);

        else DeckTree.Add(node);
        
        await deckRepo.AddNewDeck(deck);
    }
    #endregion

    #region ICommands

    [RelayCommand]
    protected override async Task ReloadDomainAsync()
    {
        DeckTree.Clear();

        DeckTree.AddRange(await deckTreeBuilder
            .BuildCountedAsync(userId));
    }

    [RelayCommand]
    private void ShowCreateDeck(DeckNode? parent = null)
    {
        //* show create deck popup, in which you give it a name.
        CurrentPopup = new CreateDeckVM(CreateDeck, PopupCancel, parent);
    }

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
        if (deck.Parent is DeckNode parent)
            parent.RemoveChild(deck);

        else DeckTree.Remove(deck);

        await deckRepo.RemoveDeck(deck.Id);
    }

    [RelayCommand]
    private async Task RenameDeck(DeckNode deck)
    {
        deck.CommitRename();

        await deckRepo.RenameDeck(
            deck.Id, deck.Name);
    }

    [RelayCommand]
    private async Task MoveDeck(DeckNode deck)
    {
        await deckRepo.SaveEditedDeck(deck.ToEntity());
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
