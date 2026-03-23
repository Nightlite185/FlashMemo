using FlashMemo.Helpers;
using FlashMemo.Model.Domain;
using FlashMemo.ViewModel.Popups;

namespace FlashMemo.Model.Persistence;

public class CardEntity: ICard
{
    #region Properties
    public long Id { get; set; }
    public long UserId { get; set; }
    public Note Note { get; set; } = null!;
    public long NoteId { get; set; }
    public long DeckId { get; set; }
    public ICollection<Tag> Tags { get; set; } = [];
    public ICollection<CardLog> CardLogs { get; set; } = [];
    public Deck Deck { get; set; } = null!;
    public bool IsBuried { get; set; }
    public bool IsSuspended { get; set; }
    public DateTime Created { get; set; }
    public DateTime? LastModified { get; set; }
    public DateTime? Due { get; set; }
    public bool IsDueToday => Due?.Date <= DateTime.Today;
    public bool IsDueNow => Due <= DateTime.Now;
    public DateTime? LastReviewed { get; set; }
    public TimeSpan Interval { get; set; }
    public CardState State { get; set; }
    public LearningStage? LearningStage { get; set; }
    #endregion

    #region methods
    private void ModifyDateHelper()
    {
        State = CardState.Review; // reschedule forces state to review, weird outcomes otherwise.
        LastModified = DateTime.Now;
        LearningStage = null;
    }
    private void MaybeRaiseInterval(bool keepInterval, TimeSpan newInterval)
    {
        //* if this card was new (due == null), we add interval regardless of user's choice
        //* because its illegal for a card to have interval of 0 
        //* together with state == review and a due value

        if (Due is null || !keepInterval)
            Interval = newInterval;
    }
    public void FlipBuried() => IsBuried = !IsBuried;
    public void FlipSuspended() => IsSuspended = !IsSuspended;
    public void Reschedule(RescheduleData data)
    {
        var timeFromNow = 
            data.NewDate - DateTime.Now;

        ModifyDateHelper();

        MaybeRaiseInterval(
            data.KeepInterval, 
            newInterval: timeFromNow);

        Due = data.NewDate;
    }
    public void Postpone(PostponeData data)
    {
        var days = TimeSpan.FromDays(
            data.PostponeByDays);
        
        ModifyDateHelper();

        MaybeRaiseInterval(
            data.KeepInterval, 
            newInterval: days);

        Due = (Due is null || data.SinceToday)
            ? DateTime.Today.Add(days)
            : Due.Value.Add(days);
    }
    public void Forget()
    {
        State = CardState.New;
        LearningStage = null;
        Due = null;
        LastModified = DateTime.Now;
        Interval = TimeSpan.Zero;
    }
    public void MoveToDeck(long newDeckId) => DeckId = newDeckId;
    public void ReplaceTagsWith(IEnumerable<Tag> newTags)
    {
        Tags.Clear();
        Tags.AddRange(newTags);
    }
    #endregion
    public static CardEntity CreateNew(Note note, IDeckMeta deck, IEnumerable<Tag> tags)
    {
        return new()
        {
            Id = IdGetter.Next(),
            DeckId = deck.Id,
            UserId = deck.UserId,

            Created = DateTime.Now,
            Interval = TimeSpan.Zero,
            State = CardState.New,

            LastReviewed = null,
            LastModified = null,
            LearningStage = null,

            IsBuried = false,
            IsSuspended = false,

            Tags = [..tags],
            Note = note
        };
    }
}
