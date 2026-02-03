using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;

namespace FlashMemo.ViewModel.Wrappers;

public partial class CardItemVM(CardEntity card): ObservableObject, IViewModel
{
    public void NotifyUI() => CardVersion++;

    public long Id => card.Id;

    [ObservableProperty]
    public partial bool IsSelected { get; set; } = false;
    
    [ObservableProperty]
    public partial bool IsDeleted { get; set; } = false;
    
    private readonly CardEntity card = card;

    [ObservableProperty]
    public partial string FrontContent { get; set; } = card.FrontContent;
    
    [ObservableProperty]
    public partial string BackContent { get; set; } = card.BackContent ?? "";
    public Deck Deck => card.Deck;
    public CardState CardState => card.State;
    public int DayInterval => (int)card.Interval.TotalDays;

    public CardEntity ToEntity()
    {
        card.FrontContent = FrontContent;
        
        card.BackContent = BackContent.Length == 0
            ? null
            : BackContent;

        return card;
    }

    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FrontContent), nameof(BackContent), nameof(CardState))]
    private partial int CardVersion { get; set; }
    // this is incremented every time when card is updated, to notify the UI.
}