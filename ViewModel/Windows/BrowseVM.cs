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
                                    : BaseVM(bus, logger, dialogService), IPopupHost, IFiltrable, IClosedHandler, ICtxMenuHost
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

        await ApplyFiltersAsync(
            filtersVM.CachedFilters);

        UpdateVisibleCardsCount();
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
    public async Task ApplyFiltersAsync(Filters? snapshot = null)
    {
        //* take fresh snapshot by default if filters arent given;
        //* this is used in browse xaml bc its a parameterless command
        
        snapshot ??= filtersVM.TakeSnapshot();
        cards.Clear();
        SelectedCount = 0;

        cards.AddRange((await cardQueryS
            .GetCardsWhere(snapshot, SortOrder, SortDir))
            .ToVMs());

        UpdateVisibleCardsCount();
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
    protected override async Task ReloadDomainAsync()
        => await ApplyFiltersAsync(filtersVM.CachedFilters);
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