using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.ViewModel.Bases;

namespace FlashMemo.ViewModel.Wrappers;

public partial class DeckNode : RenameVMBase, IDeckMeta
{
    private readonly Deck deck;
    public DeckNode(Deck deck, ICollection<DeckNode> children, DeckNode? parent = null, CardsCount? countByState = null)
    {
        this.deck = deck;
        this.Parent = parent;
        Name = deck.Name;

        Children = [..children];

        if (countByState is CardsCount validCC)
            SetCardCount(validCC);
    }
    
    #region public properties
    public long Id => deck.Id;
    public DeckNode? Parent { get; private set; }
    public long UserId => deck.UserId;
    public long OptionsId => deck.OptionsId;
    public ObservableCollection<DeckNode> Children { get; set; }
    [ObservableProperty] public partial bool IsExpanded { get; set; }
    [ObservableProperty] public partial bool IsSelected { get; set; }
    [ObservableProperty] public partial int LessonsCount { get; set; }
    [ObservableProperty] public partial int LearningCount { get; set; }
    [ObservableProperty] public partial int ReviewsCount { get; set; }
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
        deck.ParentDeckId = Parent?.Id;

        return deck;
    }

    public void AddChild(DeckNode newChild)
    {
        //* checking for circular relations
        if (ReferenceEquals(this, newChild) || Id == newChild.Id)
            throw new InvalidOperationException(
            "Circular parent/child DeckNode relation detected.");

        //* checking for duplicates
        if (Children.Any(c => c.Id == newChild.Id))
            throw new InvalidOperationException(
            "This deck already has the child you tried to add.");

        Children.Add(newChild);
        newChild.Parent = this;
    }

    public void RemoveChild(DeckNode deck)
    {
        if (!Children.Remove(deck))
            throw new InvalidOperationException(
            "Tried to remove a child from this deck, but it never had it in the first place.");

        deck.Parent = null;
    }
    
    #endregion
}