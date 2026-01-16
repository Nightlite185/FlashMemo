using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;

namespace FlashMemo.ViewModel
{
    public partial class DeckItemVM : ObservableObject, IViewModel
    {
        public DeckItemVM(Deck deck, CardsCount cc, IEnumerable<DeckItemVM> children)
        {
            Deck = deck;
            DeckId = deck.Id;
            Name = deck.Name;

            Children = [..children];

            this.SetCardCount(cc);
        }
        
        #region public properties
        public long DeckId { get; init; }
        public Deck Deck { get; init; }
        public ObservableCollection<DeckItemVM> Children { get; set; }

        [ObservableProperty]
        public partial string Name { get; set; }

        [ObservableProperty]
        public partial bool IsSelected { get; set; }
        
        [ObservableProperty]
        public partial int LessonsCount { get; set; }

        [ObservableProperty]
        public partial int LearningCount { get; set; }

        [ObservableProperty]
        public partial int ReviewsCount { get; set; }
        #endregion
        
        #region methods
        public void SetCardCount(CardsCount cc)
        {
            LessonsCount = cc.Lessons;
            LearningCount = cc.Learning;
            ReviewsCount = cc.Reviews;   
        }
        #endregion
    }
}