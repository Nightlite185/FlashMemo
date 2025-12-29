using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model;

namespace FlashMemo.ViewModel
{
    public partial class DeckVM : ObservableObject
    {
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
