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
        public IReadOnlyCollection<CardEntity> SelectedCards { get
        {
            throw new NotImplementedException();
        }}
        #endregion
        
        #region methods
        private void ThrowIfNoneSelected(IReadOnlyCollection<CardEntity> selected, bool throwIfNotSingle = false)
        {
            if (throwIfNotSingle && selected.Count != 1)
                throw new InvalidOperationException(
                    $"Cannot execute this context command since exactly one card has to be selected and you had {selected.Count}");


            else if (selected.Count <= 0)
                throw new InvalidOperationException(
                "Cannot execute context command since no cards were selected");
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
        // IMPORTANT: commands that open a window dont get async, 
        // but those that directly call async internal methods and use services should 

        [RelayCommand]
        public void MoveCardsCtx()
        {
            
        }

        [RelayCommand]
        public void RescheduleCardsCtx() // this lets u choose datetime
        {
            
        }

        [RelayCommand]
        public void PostponeCardsCtx() // this moves due date by specified num of days. Choose if keep interval or change to num of days choosen.
        {                                    // also you can choose if postpone by days SINCE today or SINCE card's DUE DATE 
            var selected = SelectedCards;
            ThrowIfNoneSelected(selected);
        }

        [RelayCommand]
        public async Task ForgetCardsCtx()
        {
            var selected = SelectedCards;
            ThrowIfNoneSelected(selected);
        }

        [RelayCommand]
        public async Task ToggleBuryCtx()
        {
            var selected = SelectedCards;
            ThrowIfNoneSelected(selected);
        }

        [RelayCommand]
        public async Task ToggleSuspendedCtx()
        {
            var selected = SelectedCards;
            ThrowIfNoneSelected(selected);
        }

        [RelayCommand]
        public async Task DeleteCardsCtx()
        {
            var selected = SelectedCards;
            ThrowIfNoneSelected(selected);

            await cardRepo.DeleteCard(selected);
        }

        [RelayCommand]
        public async Task ManageTags() // ONLY VISIBLE IF ONE CARD IS SELECTED
        {
            var selected = SelectedCards;
            ThrowIfNoneSelected(selected, throwIfNotSingle: true);

            // Open ManageTagsWindow here
        }
        #endregion

        
        #endregion
    }
}