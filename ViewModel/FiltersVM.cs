using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Model;
using FlashMemo.Model.Domain;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel;
    public sealed partial class FiltersVM(
        IDeckTreeBuilder dtb, ITagRepo tr,
        long userId): ObservableObject, IViewModel
    {
        #region Public binding properties

        [NotifyCanExecuteChangedFor(nameof(ApplyFiltersCommand))]
        [ObservableProperty] public partial bool IsChanged { get; private set; } //? maybe this can be priv?? idk
        public ObservableCollection<TagVM> Tags { get; set; } = [];
        public ObservableCollection<CardStateVM> States { get; set; } = [];
        [ObservableProperty] public partial bool? IsBuried { get; set; }
        [ObservableProperty] public partial bool? IsSuspended { get; set; }
        [ObservableProperty] public partial bool? IsDue { get; set; }
        [ObservableProperty] public partial ObservableCollection<DeckNode> DeckTree { get; set; }
        [ObservableProperty] public partial DeckNode SelectedDeck { get; set; }
        [ObservableProperty] public partial bool IncludeChildrenDecks { get; set; } //* whether to recursively include all children of the chosen deck
        [ObservableProperty] public partial TimeSpan? Interval { get; set; }
        [ObservableProperty] public partial DateTime? Due { get; set; }
        [ObservableProperty] public partial DateTime? LastReviewed { get; set; }
        [ObservableProperty] public partial DateTime? LastModified { get; set; }
        [ObservableProperty] public partial DateTime? Created { get; set; }
        //? everywhere with datetime I can do like int input box with 0 meaning today,
        //? -1 yesterday, and 1 meaning tmrw, Instead of some fancy datetime picker.

        [ObservableProperty] public partial int? OverdueByDays { get; set; }
        //* null => not chosen, 0 => due today, 1 => overdue by 1 day.
        #endregion
        
        #region Icommands
        [RelayCommand(CanExecute = nameof(IsChanged))]
        private async Task ApplyFilters()
        {
            IsChanged = false;
            await filtrable.ApplyFiltersAsync(TakeSnapshot());
        }
        #endregion
        
        #region Methods
        private Filters TakeSnapshot()
        {
            return new Filters()
            {
                TagIds = (ImmutableArray<long>) Tags
                    .Where(t => t.IsSelected)
                    .Select(t => t.Id),

                States = (ImmutableArray<CardState>) States
                    .Where(vm => vm.IsSelected)
                    .Select(vm => vm.State),

                IncludeChildrenDecks = IncludeChildrenDecks,
                DeckId = SelectedDeck.Id,
                OverdueByDays = OverdueByDays,
                LastModified = LastModified,
                LastReviewed = LastReviewed,
                IsSuspended = IsSuspended,
                IsBuried = IsBuried,
                Interval = Interval,
                Created = Created,
                IsDue = IsDue,
                Due = Due
            };
        }
        internal async Task InitializeAsync(IFiltrable applyFilters)
        {
            this.filtrable = applyFilters;

            // TODO: load the initial deck tree, tags, etc. & others needed in UI async. 
        }
        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.PropertyName // todo refactor this
                is not nameof(IsChanged)
                and not nameof(Tags)
                and not nameof(States)
            ) 
                IsChanged = true;
        }
        #endregion
       
        #region private things
        private IFiltrable filtrable = null!;
        private readonly IDeckTreeBuilder deckTB = dtb;
        private readonly ITagRepo tagRepo = tr;
        private long userId = userId;
        #endregion
    }
