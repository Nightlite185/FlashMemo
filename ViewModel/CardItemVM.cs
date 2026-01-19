using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;

namespace FlashMemo.ViewModel
{
    public partial class CardItemVM: ObservableObject, IViewModel
    {
        [ObservableProperty]
        public partial bool IsSelected { get; set; }
        
        [ObservableProperty]
        public partial CardEntity Card { get; set; }

        public string FrontContent => Card.FrontContent;
        public string BackContent => Card.BackContent ?? "";
        public Deck Deck => Card.Deck;
        public CardState CardState => Card.State;
        public int DayInterval => (int)Card.Interval.TotalDays;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FrontContent), nameof(BackContent), nameof(CardState))]
        // TO DO: etc etc add more notifications in the future
        public partial int CardVersion { get; set; }
    }
}