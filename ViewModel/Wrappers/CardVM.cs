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
        Note = NoteVM.Create(card.Note);

        Tags.Clear();
        Tags.AddRange(card.Tags.ToVMs());
    }

    #region Properties
    [ObservableProperty] public partial bool IsSelected { get; set; } = false;
    [ObservableProperty] public partial bool IsDeleted { get; set; } = false;
    [ObservableProperty] public partial NoteVM Note { get; set; } = null!;
    public ObservableCollection<TagVM> Tags { get; init; }
    public int DayInterval => (int)card.Interval.TotalDays;
    public bool IsSuspended => card.IsSuspended;
    public bool IsBuried => card.IsBuried;

    public long Id => card.Id;
    public TimeSpan Interval => card.Interval;
    public CardState State => card.State;
    public DateTime? Due => card.Due;
    public int? LearningStage => card.LearningStage;
    #endregion
    
    private CardEntity card;
}