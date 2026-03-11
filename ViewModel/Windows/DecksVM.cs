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
    public sealed record DeckReparentRequest(DeckNode Deck, DeckNode? NewParent);

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

    [RelayCommand(CanExecute = nameof(CanReparentDeck))]
    private async Task ReparentDeck(DeckReparentRequest request)
    {
        DeckNode deck = request.Deck;
        DeckNode? oldParent = deck.Parent;
        DeckNode? newParent = request.NewParent;

        if (newParent is null)
        {
            if (oldParent is null)
                return;

            oldParent.RemoveChild(deck);
            DeckTree.Add(deck);
        }

        else
        {
            if (oldParent is null)
                DeckTree.Remove(deck);

            deck.Reparent(newParent);
            newParent.IsExpanded = true;
        }

        await deckRepo.SaveEditedDeck(deck.ToEntity());
    }

    private bool CanReparentDeck(DeckReparentRequest? request)
    {
        if (request?.Deck is not DeckNode deck)
            return false;

        DeckNode? newParent = request.NewParent;

        if (newParent is null)
            return deck.Parent is not null;

        if (deck.Id == newParent.Id)
            return false;

        if (deck.Parent?.Id == newParent.Id)
            return false;

        return !IsDeckWithinSubtree(newParent, deck);
    }

    private static bool IsDeckWithinSubtree(DeckNode node, DeckNode potentialAncestor)
    {
        DeckNode? current = node;

        while (current is not null)
        {
            if (current.Id == potentialAncestor.Id)
                return true;

            current = current.Parent;
        }

        return false;
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
