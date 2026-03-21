using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Helpers;
using FlashMemo.Model;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Other;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel.Windows;

public sealed partial class BrowseVM(ICardQueryService cardQueryS, FiltersVM filtersVM, long userId, IVMEventBus bus)
                                        : BaseVM(bus), IPopupHost, IFiltrable, IClosedHandler, ICtxMenuHost
{
    #region Public properties
    [ObservableProperty]
    public partial ObservableCollection<CardVM> Cards { get; set; } = [];

    [ObservableProperty]
    public partial string SearchBar { get; set; } = "";
    
    public IReadOnlyCollection<CardVM> GetSelectedCards()
        => [..Cards.Where(vm => vm.IsSelected)];
    
    [ObservableProperty]
    public partial PopupVMBase? CurrentPopup { get; set; }

    [ObservableProperty]
    public partial CardsOrder SortOrder { get; set; }

    [ObservableProperty]
    public partial SortingDirection SortDir { get; set; }

    [ObservableProperty]
    public partial BrowseColumn ActiveBrowseColumn { get; set; } = BrowseColumn.Due;
    #endregion
    
    #region methods
    internal async Task InitializeAsync(CardCtxMenuVM ccm)
    {
        cardCtxMenu = ccm;
        eventBus.DomainChanged += OnDomainChanged;

        await ApplyFiltersAsync(
            filtersVM.CachedFilters);
    }

    public async Task ApplyFiltersAsync(Filters snapshot)
    {
        Cards.Clear();
        
        Cards.AddRange((await cardQueryS
            .GetCardsWhere(snapshot, SortOrder, SortDir))
            .ToVMs());
    }

    private void OpenCtxMenu()
    {
        var cards = GetSelectedCards();
        
        capturedCards = cards;
        cardCtxMenu.OpenMenu(cards);
    }
    public async Task OnActionExecuted(CtxMenuAction action)
    {
        if (capturedCards is null || capturedCards.Count == 0) 
            throw new InvalidOperationException("capturedCards was empty or null");

        var c = capturedCards.First();
        string[] rescheduleProps =
        [
            nameof(c.State), nameof(c.Due), 
            nameof(c.DayInterval), nameof(c.LearningStage), 
            nameof(c.LastModified)
        ];

        string[]? propNames = action switch
        {
            CtxMenuAction.Relocate => [nameof(c.DeckName)],
            CtxMenuAction.Reschedule => rescheduleProps,
            CtxMenuAction.Forget => rescheduleProps,
            CtxMenuAction.Bury => [nameof(c.IsBuried)],
            CtxMenuAction.Suspend => [nameof(c.IsSuspended)],
            CtxMenuAction.Delete => [nameof(c.IsDeleted)],

            _ => null
        };

        if (propNames is null) return;

        foreach (var card in capturedCards)
        foreach (string name in propNames)
            card.NotifyPropChanged(name);
    }
    protected override async Task ReloadDomainAsync()
        => await ApplyFiltersAsync(filtersVM.CachedFilters);

    public void OnColumnClicked(BrowseColumn column)
    {
        // TODO: Handle browse-column sorting here (re-query or re-order cards) once
        // BrowseWindow-to-VM sorting integration is implemented end-to-end.
    }
    #endregion
    
    #region private things
    private CardCtxMenuVM cardCtxMenu = null!;
    private IReadOnlyCollection<CardVM>? capturedCards;
    #endregion
}

public enum BrowseColumn
{
    NoteFrontContent, NoteBackContent,
    Id, DeckName, Due, DayInterval,
    LastModified, LearningStage,
    State, IsBuried, IsSuspended,
    NoteType, Tags, Created
}