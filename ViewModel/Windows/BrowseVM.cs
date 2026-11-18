using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Helpers;
using FlashMemo.Model;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Other;
using FlashMemo.ViewModel.Wrappers;
using Microsoft.Extensions.Logging;

namespace FlashMemo.ViewModel.Windows;

public sealed partial class BrowseVM(ICardQueryService cardQueryS, FiltersVM filtersVM, long userId,
                                    IVMEventBus bus, ILogger<BrowseVM> logger, IDialogService dialogService, SafetyNet net)
                                    : BaseVM(bus, logger, dialogService, net), IPopupHost, IFiltrable, IClosedHandler, ICtxMenuHost
{
    #region Public properties
    public CollectionView CardsView { get; private set; } = null!;
    [ObservableProperty] public partial int SelectedCount { get; set; }
    [ObservableProperty] public partial int VisibleCardsCount { get; private set; }
    public string SearchBar
    {
        get;
        set
        {
            if (!SetProperty(ref field, value, nameof(SearchBar))) 
                return;

            CardsView.Refresh();
            UpdateVisibleCardsCount();
        }
    } = "";
    
    public IReadOnlyCollection<CardVM> GetSelectedCards()
    {
        var selected = cards
            .Where(vm => vm.IsSelected)
            .ToImmutableList();

        capturedCards = selected;
        return selected;
    }
    
    [ObservableProperty]
    public partial PopupVMBase? CurrentPopup { get; set; }

    [ObservableProperty]
    public partial CardsOrder SortOrder { get; set; }

    [ObservableProperty]
    public partial SortingDirection SortDir { get; set; }
    public FiltersVM FiltersVM => filtersVM;
    public CardCtxMenuVM CtxMenuVM { get; private set; } = null!;
    #endregion

    #region methods
    public override void OnClosed()
    {
        base.OnClosed();
        filtersVM.OnClosed();
    }

    internal async Task InitializeAsync(CardCtxMenuVM ccm)
    {
        CtxMenuVM = ccm;
        eventBus.DomainChanged += OnDomainChanged;

        CardsView = (CollectionView)new CollectionViewSource()
            { Source = cards }.View;
        
        CardsView.Filter += OnFilter;

        // Initialization propagates failure to the factory so it can release this VM.
        var snapshot = filtersVM.CachedFilters;
        var loadedCards = await LoadCardsAsync(snapshot, SortOrder, SortDir);
        CommitCards(loadedCards, snapshot);
    }

    private bool OnFilter(object obj)
    {
        if (obj is not CardVM card)
            throw new InvalidOperationException(
            "obj from the view collection has wrong type");

        var note = card.Note as StandardNoteVM
            ?? throw new InvalidOperationException(
            "Only standard notes allowed for now.");

        return note.FrontContent.Contains(SearchBar, StringComparison.OrdinalIgnoreCase)
            || note.BackContent .Contains(SearchBar, StringComparison.OrdinalIgnoreCase);
    }

    [RelayCommand]
    public async Task TryApplyFiltersAsync(Filters? snapshot = null)
        => await net.TryUnsafe(
            () => ApplyFiltersAsync(snapshot),
            "apply the card filters", retry: false);

    private async Task<bool> ApplyFiltersAsync(Filters? snapshot)
    {
        snapshot ??= filtersVM.TakeSnapshot();
        var order = SortOrder;
        var direction = SortDir;

        var queryAttempt = await net.TryUnsafe(
            action: () => LoadCardsAsync(snapshot, order, direction),
            actionTried: "load cards with applied filters. If you don't wish to retry, we'll attempt loading all cards as fallback.",
            retry: true);

        if (!queryAttempt.Success)
        {
            queryAttempt = await net.TryUnsafe(
                action: async () => (await cardQueryS.GetAllFromUser(userId)).ToVMs().ToArray(),
                actionTried: "load all current user's cards. The previous results will be kept if this fails.",
                retry: true);

            if (!queryAttempt.Success)
                return false;

            // Keep the draft available for another attempt, but remember what actually loaded.
            snapshot = Filters.GetEmpty(userId);
        }

        CommitCards(queryAttempt.Value, snapshot);
        return true;
    }

    private async Task<CardVM[]> LoadCardsAsync(Filters snapshot, CardsOrder order, SortingDirection direction)
        => [..(await cardQueryS.GetCardsWhere(snapshot, order, direction)).ToVMs()];

    private void CommitCards(CardVM[] loadedCards, Filters snapshot)
    {
        cards.Clear();
        cards.AddRange(loadedCards);
        capturedCards = null;
        SelectedCount = 0;
        UpdateVisibleCardsCount();
        filtersVM.RememberAppliedFilters(snapshot);
    }

    public void OpenCtxMenu()
    {
        var selected = (capturedCards ?? GetSelectedCards())
            .Select(card => card.SavedEntity)
            .ToArray();

        CtxMenuVM.OpenMenu(selected);
    }
    public Task OnActionExecuted(CtxMenuAction action, IReadOnlyCollection<CardEntity> affectedCards)
    {
        var affectedById = affectedCards.ToDictionary(card => card.Id);
        CardVM[] affectedVMs = [..cards.Where(card => affectedById.ContainsKey(card.Id))];

        if (action == CtxMenuAction.Delete)
        {
            cards.RemoveRange(affectedVMs);
            SelectedCount = cards.Count(card => card.IsSelected);
            UpdateVisibleCardsCount();
            return Task.CompletedTask;
        }

        foreach (var card in affectedVMs)
            card.Refresh(affectedById[card.Id]);

        return Task.CompletedTask;
    }
    protected override Task<bool> ReloadDomainAsync()
        => ApplyFiltersAsync(filtersVM.CachedFilters);
    #endregion
    
    [RelayCommand]
    private async Task ShowCardEditor(CardVM card)
    {
        await net.TryUnsafe(
            () => NavigateTo(new EditCardNavRequest(card.Id, userId)),
            actionTried: "open the card editor",
            retry: false);
    }

    #region private things
    private readonly ObservableCollection<CardVM> cards = [];
    private IReadOnlyCollection<CardVM>? capturedCards;
    private void UpdateVisibleCardsCount()
    {
        if (CardsView.Count >= 0)
            VisibleCardsCount = CardsView.Count;

        else VisibleCardsCount = cards.Count;
    }
    #endregion
}