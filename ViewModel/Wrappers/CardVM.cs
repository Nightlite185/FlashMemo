using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Helpers;
using FlashMemo.Model;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;

namespace FlashMemo.ViewModel.Wrappers;

public partial class CardVM(CardEntity card): ObservableObject, ICard, IViewModel
{
    [ObservableProperty] public partial bool IsSelected { get; set; } = false;
    [ObservableProperty] public partial bool IsDeleted { get; set; } = false;
    [ObservableProperty] public partial string FrontContent { get; set; } = card.FrontContent;
    [ObservableProperty] public partial string BackContent { get; set; } = card.BackContent ?? "";
    public ObservableCollection<TagVM> Tags { get; init; } = [..card.Tags.ToVMs()];
    public int DayInterval => (int)card.Interval.TotalDays;

    #region ICard implementation
    public long Id => card.Id;
    public long DeckId => card.DeckId;
    public bool IsBuried => card.IsBuried;
    public bool IsSuspended => card.IsSuspended;
    public DateTime Created => card.Created;
    public DateTime? LastModified => card.LastModified;
    public DateTime? Due => card.Due;
    public DateTime? LastReviewed => card.LastReviewed;
    public TimeSpan Interval => card.Interval;
    public CardState State => card.State;
    public int? LearningStage => card.LearningStage;
    #endregion
    
    private readonly CardEntity card = card;
    public void NotifyChanged() => CardVersion++;
    public CardEntity ToEntity()
    {
        card.FrontContent = FrontContent;
        
        card.BackContent = BackContent.Length == 0
            ? null
            : BackContent;

        card.ReplaceTagsWith(Tags.ToEntities());

        return card;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FrontContent), nameof(BackContent), nameof(State))]
    private partial int CardVersion { get; set; }
    // this is incremented every time when card is updated, to notify the UI.
}