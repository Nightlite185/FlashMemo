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

public partial class BrowseVM: ObservableObject, IViewModel
{
    internal BrowseVM(IWindowService ws, ICardRepo cr, ManageTagsVMF mtVMF,
    ICardQueryService cqs, ICardService cs, FiltersVM fvm, long userId)
    {
        manageTagsVMF = mtVMF;
        cardService = cs;
        windowService = ws;
        cardQueryS = cqs;
        cardRepo = cr;
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
    
    public IReadOnlyCollection<CardItemVM> SelectedCardVMs => [..Cards
        .Where(vm => vm.IsSelected)];
    
    [ObservableProperty]
    public partial PopupVMBase? CurrentPopup { get; set; }

    [ObservableProperty]
    public partial CardsOrder SortOrder { get; set; }

    [ObservableProperty]
    public partial SortingDirection SortDir { get; set; }
    #endregion
    
    #region methods
    
    ///<summary> FiltersVM should either call this as delegate whenever current filters change and user presses 'apply'.</summary>
    internal async Task ApplyFiltersAsync(Filters filters)
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
    private async Task ReloadCardsAsync()
    {
        if (cachedFilters is null)
            throw new InvalidOperationException(
            "Cannot reload cards since no filters were cached yet.");

        await ApplyFiltersAsync(cachedFilters);
    }
    private static void ThrowIfInvalidSelected(int selectedCount, bool throwIfNotSingle = false, [CallerMemberName] string? called = null)
    {
        if (selectedCount <= 0)
            throw new InvalidOperationException(
            $"Cannot execute context command '{called}' with no cards selected");

        if (throwIfNotSingle && selectedCount != 1)
            throw new InvalidOperationException(
            $"Cannot execute context command '{called}' since exactly one card has to be selected but you had {selectedCount}");
    }
    private void ThrowIfNoCardsCaptured(string? calledMember)
    {
        if (capturedCards is null || capturedCards.Count == 0)
            throw new InvalidOperationException(
            $"Called {calledMember}, but there are no captured cards yet.");
    }

    private void CaptureSelected(bool throwIfNotSingle = false)
    {
        if (capturedCards is not null)
            throw new InvalidOperationException("Cannot capture cards if there already are some captured. Clean those up first.");

        var cards = SelectedCardVMs;

        ThrowIfInvalidSelected(cards.Count, throwIfNotSingle);

        capturedCards = cards;
    }
    private void PopupCancel()
    {
        capturedCards = null;
        CurrentPopup = null;
    }
    private async Task ModifyCardsHelper(Action<CardEntity> cardModifier, CardAction cardAction, [CallerMemberName] string? caller = null)
    {
        ThrowIfNoCardsCaptured(caller);

        foreach (var vm in capturedCards!)
        {
            cardModifier(vm.ToEntity());
            vm.NotifyUI();
        }

        await cardService.SaveEditedCards(
            capturedCards.ToEntities(),
            cardAction
        );
    }
    private async Task RescheduleCards(DateTime dt, bool keepInterval)
    {
        await ModifyCardsHelper(
            c => c.Reschedule(dt, keepInterval),
            CardAction.Reschedule);
    }
    private async Task PostponeCards(int PostponeBy, bool keepInterval)
    {
        await ModifyCardsHelper(
            c => c.Postpone(PostponeBy, keepInterval),
            CardAction.Reschedule
        );
    }
    private async Task MoveCards(Deck newDeck)
    {
        await ModifyCardsHelper(
            c => c.MoveToDeck(newDeck),
            CardAction.Relocate
        );
    }
    private async Task ChangeTags(IEnumerable<Tag> newTags, bool globalTagsEdited)
    {
        await ModifyCardsHelper(
            c => c.ReplaceTagsWith(newTags),
            CardAction.Modify
        );

        if (globalTagsEdited)
            await ReloadCardsAsync();
    }
    #endregion
    
    #region private things
    private readonly IWindowService windowService;
    private readonly ICardQueryService cardQueryS;
    private readonly ICardService cardService;
    private readonly ICardRepo cardRepo;
    private readonly FiltersVM filtersVM;
    private readonly ManageTagsVMF manageTagsVMF;
    private EditCardVM? editVM;
    private IReadOnlyCollection<CardItemVM>? capturedCards;
    private long loadedUserId;
    private Filters? cachedFilters;
    #endregion

    #region ICommands
    #region context menu commands (Ctx suffix in members stands for context commands)
    /* IMPORTANT: commands that open a window -> dont get async,
    but those that directly call async internal methods and use services -> should

    TODO: Maybe encapsulate this later, along with those private callback methods, 
    since card review VM might be using this context menu as well. */

    [RelayCommand]
    public void MoveCardsCtx()
    {
        CaptureSelected();
        CurrentPopup = new MoveCardsVM(MoveCards, PopupCancel);
    }

    [RelayCommand]
    public void ShowRescheduleCtx() // this lets u choose datetime
    {
        CaptureSelected();
        CurrentPopup = new RescheduleVM(RescheduleCards, PopupCancel);
    }

    [RelayCommand]
    public void ShowPostponeCtx() // this moves due date by specified num of days. Choose if keep interval or change to num of days choosen.
    {                                    // also you can choose if postpone by days SINCE today or SINCE card's DUE DATE 
        CaptureSelected();

        CurrentPopup = new PostponeVM(PostponeCards, PopupCancel);
    }

    [RelayCommand]
    public async Task ForgetCardsCtx()
    {
        CaptureSelected();

        await ModifyCardsHelper(
            c => c.Forget(),
            CardAction.Forget
        );
    }

    [RelayCommand]
    public async Task ToggleBuryCtx()
    {
        CaptureSelected();
        
        await ModifyCardsHelper(
            c => c.FlipBuried(),
            CardAction.Bury
        );
    }

    [RelayCommand]
    public async Task ToggleSuspendedCtx()
    {
        CaptureSelected();
        
        await ModifyCardsHelper(
            c => c.FlipSuspended(),
            CardAction.Suspend
        );
    }

    [RelayCommand]
    public async Task DeleteCardsCtx()
    {
        CaptureSelected();
        
        foreach (var vm in capturedCards!)
        {
            vm.IsDeleted = true;
            vm.NotifyUI();
        }
        
        await cardRepo.DeleteCards(
            capturedCards.Select(vm => vm.ToEntity()));
    }

    [RelayCommand]
    public async Task ManageTags() // ONLY VISIBLE IF ONE CARD IS SELECTED
    {
        CaptureSelected(throwIfNotSingle: true);

        long cardId = capturedCards!
            .First()
            .ToEntity().Id;

        CurrentPopup = await manageTagsVMF.CreateAsync(
            confirm: ChangeTags,
            cancel: PopupCancel,
            cardId, loadedUserId
        );
    }
    
    #endregion

    
    #endregion
}
