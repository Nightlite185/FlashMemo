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
        #endregion
        
        #region methods
        private static void ThrowIfInvalidSelected(int selectedCount, bool throwIfNotSingle = false)
        {
            if (selectedCount <= 0)
                throw new InvalidOperationException(
                "Cannot execute context command with no cards selected");

            if (throwIfNotSingle && selectedCount != 1)
                throw new InvalidOperationException(
                $"Cannot execute this context command since exactly one card has to be selected but you had {selectedCount}");
        }
        private IReadOnlyCollection<CardItemVM> ValidSelectedVMs(bool throwIfNotSingle = false)
        {
            var cards = SelectedCardVMs;

            ThrowIfInvalidSelected(cards.Count, throwIfNotSingle);

            return [..cards];
        }
        #endregion
        
        #region private things
        private readonly WindowService windowService;
        private readonly CardQueryService cardQueryS;
        private readonly CardService cardService;
        private readonly CardRepo cardRepo;
        private readonly FiltersVM filtersVM = null!;
        private EditCardVM? editVM;
        #endregion
    
        #region ICommands
        #region context menu commands (Ctx suffix in members stands for context commands)
        // IMPORTANT: commands that open a window -> dont get async, 
        // but those that directly call async internal methods and use services -> should

        // TO DO: Maybe encapsulate this later, since card review VM might be using this context menu as well.

        [RelayCommand]
        public void MoveCardsCtx()
        {
            var selected = ValidSelectedVMs();

            // open a new window/grid here
        }

        [RelayCommand]
        public void ShowRescheduleCtx() // this lets u choose datetime
        {
            var selected = ValidSelectedVMs();

            // open new window/grid here
        }

        [RelayCommand]
        public void ShowPostponeCtx() // this moves due date by specified num of days. Choose if keep interval or change to num of days choosen.
        {                                    // also you can choose if postpone by days SINCE today or SINCE card's DUE DATE 
            var selected = ValidSelectedVMs();

            // open new window/grid here
        }

        [RelayCommand]
        public async Task ForgetCardsCtx()
        {
            var cardVMs = ValidSelectedVMs();

            foreach (var vm in cardVMs)
            {
                vm.Card.Forget();
                vm.NotifyUI();
            }

            await cardService.SaveEditedCards(
                cardVMs.Select(vm => vm.Card),
                CardAction.Reschedule);
        }

        [RelayCommand]
        public async Task ToggleBuryCtx()
        {
            var cardVMs = ValidSelectedVMs();
            
            foreach (var vm in cardVMs)
            {
                vm.Card.FlipBuried();
                vm.NotifyUI();
            }

            await cardService.SaveEditedCards(
                cardVMs.Select(vm => vm.Card),
                CardAction.Bury
            );
        }

        [RelayCommand]
        public async Task ToggleSuspendedCtx()
        {
            var cardVMs = ValidSelectedVMs();
            
            foreach (var vm in cardVMs)
            {
                vm.Card.FlipSuspended();
                vm.NotifyUI();
            }

            await cardService.SaveEditedCards(
                cardVMs.Select(vm => vm.Card),
                CardAction.Suspend
            );
        }

        [RelayCommand]
        public async Task DeleteCardsCtx()
        {
            var cardVMs = ValidSelectedVMs();
            
            foreach (var vm in cardVMs)
            {
                vm.IsDeleted = true;
                vm.NotifyUI();
            }
            
            await cardRepo.DeleteCards(
                cardVMs.Select(vm => vm.Card));
        }

        [RelayCommand]
        public async Task ManageTags() // ONLY VISIBLE IF ONE CARD IS SELECTED
        {
            var cardVMs = ValidSelectedVMs(throwIfNotSingle: true);

            // Open ManageTagsWindow here
        }
        #endregion

        
        #endregion
    }
}