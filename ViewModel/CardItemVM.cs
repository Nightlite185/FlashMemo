using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;

namespace FlashMemo.ViewModel
{
    public partial class CardItemVM(CardEntity card): ObservableObject, IViewModel
    {
        public void NotifyUI() => CardVersion++;

        [ObservableProperty]
        public partial bool IsSelected { get; set; } = false;
        
        [ObservableProperty]
        public partial bool IsDeleted { get; set; } = false;
        
        [ObservableProperty]
        public partial CardEntity Card { get; set; } = card;

        public string FrontContent => Card.FrontContent;
        public string BackContent => Card.BackContent ?? "";
        public Deck Deck => Card.Deck;
        public CardState CardState => Card.State;
        public int DayInterval => (int)Card.Interval.TotalDays;

        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FrontContent), nameof(BackContent), nameof(CardState))]
        private partial int CardVersion { get; set; }
        // this is incremented every time when card is updated, to notify the UI.
    }
}