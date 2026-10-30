using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Factories;
using FlashMemo.ViewModel.Other;
using FlashMemo.ViewModel.Wrappers;
using Microsoft.Extensions.Logging;

namespace FlashMemo.ViewModel.Windows;

public partial class DecksVM(IDeckRepo deckRepo, IDeckTreeBuilder decksBuilder, HeatmapVM heatmapVM,
                            ILastSessionService lastSession, IUserOptionsService userOptionsService,
                            long userId, IVMEventBus bus, ExportVMF exportVMF, ILogger<DecksVM> log,
                            IDialogService dialogService, CreateDeckVMF createDeckVMF, SafetyNet net)
                            : BaseVM(bus, log, dialogService), IPopupHost
{
    public sealed record DeckReparentRequest(DeckNode Deck, DeckNode? NewParent);

    #region methods
    internal async Task InitAsync()
    {
        eventBus.DomainChanged += OnDomainChanged;
        eventBus.DeckOptionsChanged += OnDeckOptChanged;
        eventBus.UserOptionsChanged += OnUserOptChanged;

        await ReloadDomainAsync();
    }
    private void PopupCancel() => CurrentPopup = null;
    private async Task CreateDeck(string name, DeckNode? parent = null)
    {
        Deck deck = Deck.CreateNew(
            name, userId, parent?.Id);

        deck = await deckRepo.AddNewDeck(deck);
        
        DeckNode node = new(
            deck: deck,
            children: [],
            countByState: new());

        if (parent is not null)
            parent.AddChild(node);

        else DeckTree.Add(node);
    }
    
    protected override async Task ReloadDeckOptAsync() 
        => await ReloadDomainAsync();
    protected override async Task ReloadUserOptAsync()
        => await ReloadDomainAsync();
    public override async Task OnFocusGained()
    {
        await base.OnFocusGained();
        await Heatmap.OnFocusGained();

        // forwarding focus changes to HeatmapVM too
        // bc the focus helper only cares about the DataContext
    }
    public override void OnFocusLost()
    {
        base.OnFocusLost();
        Heatmap.OnFocusLost();
    }
    public override void OnClosed()
    {
        base.OnClosed();
        Heatmap.OnClosed();
    }
    #endregion

    #region ICommands

    [RelayCommand]
    protected override async Task ReloadDomainAsync()
    {
        SelectedDecks.Clear();
        DeckTree.Clear();

        DeckTree.AddRange(await decksBuilder
            .BuildCountedAsync(userId));
    }

    [RelayCommand]
    private void ShowCreateDeck(DeckNode? parent = null)
    {
        //* show create deck popup, in which you give it a name.
        CurrentPopup = createDeckVMF.Create(CreateDeck, PopupCancel, parent);
    }

    [RelayCommand]
    private async Task ShowReview(DeckNode deck)
    {
        if (OnReviewNavRequest is { } handler)
            await net.TryUnsafe(
                () => handler(deck),
                actionTried: "open the review session",
                retry: false,
                operation: nameof(OnReviewNavRequest));
    }

    [RelayCommand]
    private async Task ShowDeckOptions(DeckNode? deck)
    {
        if (deck is not null)
            await net.TryUnsafe(
                () => NavigateTo(new DeckOptionsNavRequest(deck.Id)),
                actionTried: "open the deck options",
                retry: false);
    }

    [RelayCommand]
    private async Task ShowExportMenu()
    {
        if (SelectedDecks.Count == 0)
            throw new InvalidOperationException(
            "Tried to export decks, but none are selected.");

        CurrentPopup = await exportVMF.CreateAsync(
            userId, SelectedDecks.Cast<DeckNode>(), PopupCancel);
    }

    [RelayCommand]
    private async Task RemoveDeck()
    {
        // Snapshot before awaiting: the tree updates selection as nodes are removed.
        DeckNode[] decks = [.. SelectedDecks.Cast<DeckNode>()];

        if (decks.Length == 0)
            return;

        var selectedRoots = decks.GetSelectedRoots();

        var userOptionsResult = await net.TryUnsafe(
            () => userOptionsService.GetFromUser(userId),
            actionTried: "load the deck deletion preferences",
            retry: true);

        if (!userOptionsResult.Success)
            return;

        var userOptions = userOptionsResult.Value;

        if (userOptions.ConfirmDeckDeletion)
        {
            string message = (decks.Length == 1)
                ? $"Are you sure you want to permanently delete '{decks[0].Name}'? "
                    + "This will also delete all of its subdecks and every card contained "
                    + "in this deck or any of those subdecks."
                : $"Are you sure you want to permanently delete {decks.Length} selected decks? "
                    + "This will also delete all of their subdecks and every card contained "
                    + "in the selected decks or any of their subdecks.";

            var answer = dialogs.Show(
                title: decks.Length == 1 ? "Delete deck?" : "Delete decks?",
                message: message
                    + " This action can't be undone."
                    + DialogService.DeleteConfirmationSettingsHint,
                buttons: DialogButtons.YesNo,
                icon: DialogIcons.Warning);

            if (answer is DialogResult.No)
                return;
        }

        foreach (DeckNode deck in selectedRoots)
        {
            var result = await net.TryUnsafe(
                () => deckRepo.RemoveDeck(deck.Id),
                actionTried: "delete the deck and its subdecks",
                retry: false);

            if (!result.Success)
                return;

            var removedDeckIds = result.Value;

            if (lastSession.LastDeckId is long lastDeckId
                && removedDeckIds.Contains(lastDeckId))
            {
                lastSession.LastDeckId = null;
            }

            foreach (DeckNode removedDeck in deck.Flatten())
                SelectedDecks.Remove(removedDeck);

            if (deck.Parent is DeckNode parent)
                parent.RemoveChild(deck);

            else DeckTree.Remove(deck);
        }
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

    private static bool CanReparentDeck(DeckReparentRequest? request)
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

    #region properties & fields
    public HeatmapVM Heatmap { get; } = heatmapVM;
    public ObservableCollection<DeckNode> DeckTree { get; } = [];
    
    // SfTreeView.SelectedItems requires ObservableCollection<object>.
    public ObservableCollection<object> SelectedDecks { get; } = [];

    [ObservableProperty]
    public partial PopupVMBase? CurrentPopup { get; set; }

    public event Func<IDeckMeta, Task>? OnReviewNavRequest;
    #endregion
}
