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
        if (card.Note is not StandardNote sn)
            throw new NotSupportedException(
            "Only standard note supported for now.");
        
        Note = new StandardNoteVM(sn);
        Tags = [..card.Tags.ToVMs()];

        this.card = card;
    }

    [ObservableProperty] public partial bool IsSelected { get; set; } = false;
    [ObservableProperty] public partial bool IsDeleted { get; set; } = false;
    [ObservableProperty] public partial NoteVM Note { get; set; } = null!;
    public ObservableCollection<TagVM> Tags { get; init; }
    public int DayInterval => (int)card.Interval.TotalDays;

    #region interface
    public long Id => card.Id;
    public TimeSpan Interval => card.Interval;
    public CardState State => card.State;
    public DateTime? Due => card.Due;
    public int? LearningStage => card.LearningStage;
    #endregion
    
    private readonly CardEntity card;
    public CardEntity ToEntity()
    {
        card.Note = this.Note.ToDomain();

        card.ReplaceTagsWith(Tags.ToEntities());

        return card;
    }
}