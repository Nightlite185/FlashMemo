using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;

namespace FlashMemo.ViewModel.Wrappers;

public partial class DeckNode : ObservableObject, IViewModel
{
    public DeckNode(Deck deck, IEnumerable<DeckNode> children, CardsCount? cc = null)
    {
        this.deck = deck;
        Name = deck.Name;

        Children = [..children];

        if (cc is CardsCount validCC)
            SetCardCount(validCC);
    }
    
    #region public properties
    public long Id => deck.Id;
    public long UserId => deck.UserId;
    private readonly Deck deck;
    public ObservableCollection<DeckNode> Children { get; set; }

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
    public void SetCardCount(CardsCount? cc)
    {
        if (cc is CardsCount validCC)
        {
            LessonsCount = validCC.Lessons;
            LearningCount = validCC.Learning;
            ReviewsCount = validCC.Reviews;   
        }

        else
        {
            LessonsCount = -1;
            LearningCount = -1;
            ReviewsCount = -1;
        }
    }
    public Deck ToEntity()
    {
        deck.Name = Name;
        return deck;
    }
    #endregion
}
