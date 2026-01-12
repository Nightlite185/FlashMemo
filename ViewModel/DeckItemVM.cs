using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Persistence;

namespace FlashMemo.ViewModel
{
    public partial class DeckItemVM : ObservableObject, IViewModel
    {
        [ObservableProperty]
        public partial Deck Deck { get; set; }

        [ObservableProperty]
        public partial bool IsSelected { get; set; }
        
        [ObservableProperty]
        public partial int NewCardsCount { get; set; }

        [ObservableProperty]
        public partial int LearningCardsCount { get; set; }

        [ObservableProperty]
        public partial int ReviewCardsCount { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(NewCardsCount))]
        [NotifyPropertyChangedFor(nameof(LearningCardsCount))]
        [NotifyPropertyChangedFor(nameof(ReviewCardsCount))]
        private partial int CardsVersion { get; set; }
    }
}
