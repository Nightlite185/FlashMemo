using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Helpers;
using FlashMemo.Model;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;

namespace FlashMemo.ViewModel.Wrappers;

public partial class CardVM: ObservableObject, IScheduleInfoCard, ILearningPoolCard, IViewModel, ICardVM
{
    public CardVM(CardEntity card)
    {
        Note = NoteVM.Create(card.Note);
        Tags = [..card.Tags.ToVMs()];

        this.card = card;
    }
    
    public CardEntity ToEntity()
    {
        card.Note = this.Note.ToEntity();

        card.ReplaceTagsWith(Tags.ToEntities());

        return card;
    }
    public void Refresh(CardEntity card)
    {
        this.card = card;
        Note.Refresh(card.Note);

        Tags.Clear();
        Tags.AddRange(card.Tags.ToVMs());
    }
    public void NotifyPropChanged(string name)
        => OnPropertyChanged(name);
    #region Properties
    [ObservableProperty] public partial bool IsSelected { get; set; } = false;
    [ObservableProperty] public partial bool IsInvalid { get; set; } = false;
    [ObservableProperty] public partial NoteVM Note { get; set; } = null!;
    public ObservableCollection<TagVM> Tags { get; init; }
    public string TagsDisplay => string.Join(", ", Tags.Select(tag => tag.Name));
    public int DayInterval => (int)card.Interval.TotalDays;
    public bool IsSuspended => card.IsSuspended;
    public bool IsBuried => card.IsBuried;

    public long Id => card.Id;
    public string DeckName => card.Deck.Name;
    public DateTime? LastModified => card.LastModified;
    public DateTime? LastReviewed => card.LastReviewed;
    public DateTime? Due => card.Due;
    public DateTime Created => card.Created;
    public CardState State => card.State;
    public TimeSpan Interval => card.Interval;
    public LearningStage? LearningStage => card.LearningStage;
    #endregion
    
    private CardEntity card;
}