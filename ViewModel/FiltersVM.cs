using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Model;
using FlashMemo.Model.Domain;

namespace FlashMemo.ViewModel
{
    public partial class FiltersVM(Func<Filters, Task> applyFilters): ObservableObject, IViewModel
    {
        #region Public binding properties
        public ObservableCollection<TagVM> Tags { get; set; } = [];
        public ObservableCollection<CardStateVM> States { get; set; } = [];
        [ObservableProperty] public partial bool? IsBuried { get; set; }
        [ObservableProperty] public partial bool? IsSuspended { get; set; }
        [ObservableProperty] public partial bool? IsDue { get; set; }
        [ObservableProperty] public partial DeckNode DeckNode { get; set; }
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

        [RelayCommand]
        private async Task ApplyFilters()
            => await applyFilters(TakeSnapshot());

        private Filters TakeSnapshot()
        {
            return new Filters()
            {
                TagIds = (IReadOnlySet<long>) Tags
                    .Where(t => t.IsSelected)
                    .Select(t => t.Id),

                States = (IReadOnlySet<CardState>) States
                    .Where(vm => vm.IsSelected)
                    .Select(vm => vm.State),

                OverdueByDays = OverdueByDays,
                LastModified = LastModified,
                LastReviewed = LastReviewed,
                IsSuspended = IsSuspended,
                DeckId = DeckNode.Deck.Id,
                IsBuried = IsBuried,
                Interval = Interval,
                Created = Created,
                IsDue = IsDue,
                Due = Due
            };
        }

        private readonly Func<Filters, Task> applyFilters = applyFilters;
    }
}