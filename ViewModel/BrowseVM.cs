using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;

namespace FlashMemo.ViewModel
{
    public partial class BrowseVM: ObservableObject, IViewModel
    {
        public BrowseVM(WindowService ws, CardRepo cr, CardQueryService cqs, CardService cs, FiltersVM fVM, EditCardVM eVM)
        {
            cardService = cs;
            windowService = ws;
            cardQueryS = cqs;
            cardRepo = cr;
            filtersVM = fVM;
            Cards = [];
            editVM = eVM;
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
        public void LoadUser(long userId)
        {
            if (loadedUserId is not null)
                throw new InvalidOperationException(
                "Cannot load the user second time through this VM's lifetime.");

            loadedUserId = userId;
        }
        
        ///<summary> FiltersVM should either fire event or call this as delegate whenever current filters change and user presses 'apply'.
        /// Cached filters MUST be replaced with new ones as well.</summary>
        private async Task LoadCards(Filters filters)
        {
            Cards.Clear();
            
            var newCards = await cardQueryS
                .GetCardsWhere(filters, SortOrder, SortDir);

            Cards.AddRange(newCards.TransformToVMs());
        }

        ///<summary>Call this only after loading cards from FiltersVM at least once before,
        ///e.g. when you want to reload cards, but know that filters haven't changed.</summary>
        private async Task ReloadCards()
        {
            if (cachedFilters is null)
                throw new InvalidOperationException(
                "Cannot reload cards since no filters were cached yet.");

            await LoadCards(cachedFilters);
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
        private void ThrowIfUserNotLoaded([CallerMemberName] string? caller = null)
        {
            if (loadedUserId is null) 
                throw new InvalidOperationException(
                $"Called {caller} without loading the user first.");
        }
        private void CaptureSelected(bool throwIfNotSingle = false)
        {
            if (capturedCards is not null)
                throw new InvalidOperationException("Cannot capture cards if there already are some captured. Clean those up first.");

            var cards = SelectedCardVMs;

            ThrowIfInvalidSelected(cards.Count, throwIfNotSingle);

            capturedCards = cards;
        }
        private async Task ModifyCardsHelper(Action<CardEntity> cardModifier, CardAction cardAction, [CallerMemberName] string? caller = null)
        {
            ThrowIfNoCardsCaptured(caller);

            foreach (var vm in capturedCards!)
            {
                cardModifier(vm.Card);
                vm.NotifyUI();
            }

            await cardService.SaveEditedCards(
                capturedCards.Select(vm => vm.Card),
                cardAction
            );
        }
        #endregion
        
        #region private things
        private readonly WindowService windowService;
        private readonly CardQueryService cardQueryS;
        private readonly CardService cardService;
        private readonly CardRepo cardRepo;
        private readonly FiltersVM filtersVM = null!;
        private EditCardVM? editVM;
        private IReadOnlyCollection<CardItemVM>? capturedCards;
        #endregion
    
        #region ICommands
        #region context menu commands (Ctx suffix in members stands for context commands)
        /* IMPORTANT: commands that open a window -> dont get async,
        but those that directly call async internal methods and use services -> should

        TO DO: Maybe encapsulate this later, along with those private callback methods, 
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
                capturedCards.Select(vm => vm.Card));
        }

        [RelayCommand]
        public async Task ManageTags() // ONLY VISIBLE IF ONE CARD IS SELECTED
        {
            ThrowIfUserNotLoaded();
            CaptureSelected(throwIfNotSingle: true);

            long cardId = capturedCards!.First().Card.Id;

            CurrentPopup = new ManageTagsVM(
                confirm: ChangeTags,
                cancel: PopupCancel,
                tagRepo, cardId,
                (long)loadedUserId!
            );
        }
        
        #endregion

        
        #endregion
    }
}