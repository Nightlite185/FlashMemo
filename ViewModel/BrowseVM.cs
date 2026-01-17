using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Services;

namespace FlashMemo.ViewModel
{
    public partial class BrowseVM: ObservableObject, IViewModel
    {
        public BrowseVM(WindowService ws, CardQueryService cqs, CardService cs, FiltersVM fVM, EditCardVM eVM)
        {
            cardService = cs;
            windowService = ws;
            cardQueryS = cqs;
            filtersVM = fVM;
            Cards = [];
            editVM = eVM;
        }

        #region Public properties 
        [ObservableProperty]
        public partial ObservableCollection<CardItemVM> Cards { get; set; }
        #endregion
        #region methods
        
        #endregion
        #region private things
        private readonly WindowService windowService;
        private readonly CardQueryService cardQueryS;
        private readonly CardService cardService;
        private readonly FiltersVM filtersVM = null!;
        private EditCardVM? editVM;
        #endregion
    }
}