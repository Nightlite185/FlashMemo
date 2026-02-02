using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Helpers;
using FlashMemo.Model;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.BaseVMs;
using FlashMemo.ViewModel.PopupVMs;
using FlashMemo.ViewModel.VMFactories;
using FlashMemo.ViewModel.WrapperVMs;

namespace FlashMemo.ViewModel.WindowVMs;

public sealed partial class BrowseVM: ObservableObject, IViewModel, IPopupHost, IReloadHandler, IFiltrable
{
    internal BrowseVM(IWindowService ws, ICardQueryService cqs, FiltersVM fvm, long userId)
    {
        windowService = ws;
        cardQueryS = cqs;
        filtersVM = fvm;
        loadedUserId = userId;
        Cards = [];
        SearchBar = "";
    }

    #region Public properties
    [ObservableProperty]
    public partial ObservableCollection<CardItemVM> Cards { get; set; }

    [ObservableProperty]
    public partial string SearchBar { get; set; }
    
    public IReadOnlyCollection<CardItemVM> GetSelectedCards() => [..Cards
        .Where(vm => vm.IsSelected)];
    
    [ObservableProperty]
    public partial PopupVMBase? CurrentPopup { get; set; }

    [ObservableProperty]
    public partial CardsOrder SortOrder { get; set; }

    [ObservableProperty]
    public partial SortingDirection SortDir { get; set; }
    #endregion
    
    #region methods
    internal void Initialize(CardCtxMenuVM ccm)
    {
        this.cardCtxMenu = ccm;
    }
    ///<summary> FiltersVM should either call this as delegate whenever current filters change and user presses 'apply'.</summary>
    public async Task ApplyFiltersAsync(Filters filters)
    {
        cachedFilters = filters;
        Cards.Clear();
        
        var newCards = await cardQueryS
            .GetCardsWhere(filters, SortOrder, SortDir);

        Cards.AddRange(
            newCards.ToVMs());
    }

    ///<summary>Call this only after loading cards from FiltersVM at least once before,
    ///e.g. when you want to reload cards, but know that filters haven't changed.</summary>
    public async Task ReloadCardsAsync()
    {
        if (cachedFilters is null)
            throw new InvalidOperationException(
            "Cannot reload cards since no filters were cached yet.");

        await ApplyFiltersAsync(cachedFilters);
    }
    public async Task ReloadAsync(ReloadTypes rt)
    {
        //TODO: for each of the flags in rt, gotta reload respectively

        if (rt.HasFlag(ReloadTypes.Cards))
            await ReloadCardsAsync();

        throw new NotImplementedException();
    }
    private static void ThrowIfInvalidSelected(int selectedCount, [CallerMemberName] string? called = null)
    {
        if (selectedCount <= 0)
            throw new InvalidOperationException(
            $"Cannot execute context command '{called}' with no cards selected");
    }
    private void CaptureSelected()
    {
        var cards = GetSelectedCards();
        ThrowIfInvalidSelected(cards.Count);
        
        capturedCards = cards;
        cardCtxMenu.OpenMenu(cards);
    }
    #endregion
    
    #region private things
    private readonly IWindowService windowService;
    private CardCtxMenuVM cardCtxMenu = null!;
    private readonly ICardQueryService cardQueryS;
    private readonly FiltersVM filtersVM;
    private EditCardVM? editVM;
    private IReadOnlyCollection<CardItemVM>? capturedCards;
    private long loadedUserId;
    private Filters? cachedFilters;
    #endregion
}
