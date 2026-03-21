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

    private void CaptureSelected()
    {
        var cards = GetSelectedCards();
        
        capturedCards = cards;
        cardCtxMenu.OpenMenu(cards);
    }
    public async Task OnActionExecuted(CtxMenuAction action)
    {
        switch (action)
        {
            default: throw new NotImplementedException();
        }
    }
    protected override async Task ReloadDomainAsync()
        => await ApplyFiltersAsync(filtersVM.CachedFilters);
    #endregion
    
    #region private things
    private CardCtxMenuVM cardCtxMenu = null!;
    private IReadOnlyCollection<CardVM>? capturedCards;
    #endregion
}
