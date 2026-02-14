using FlashMemo.Helpers;
using FlashMemo.Model.Domain;

namespace FlashMemo.Model.Persistence;

public interface IEntity { long Id { get; set; } }
public class CardEntity: IEntity, ICard
{
    #region Properties
    public long Id { get; set; }
    public string FrontContent { get; set; } = null!;
    public string? BackContent { get; set; }
    public long DeckId { get; set; }
    public ICollection<Tag> Tags { get; set; } = [];
    public ICollection<CardLog> CardLogs { get; set; } = [];
    public Deck Deck { get; set; } = null!;
    public bool IsBuried { get; set; }
    public bool IsSuspended { get; set; }
    public DateTime Created { get; set; }
    public DateTime? LastModified { get; set; }
    public DateTime? Due { get; set; }
    public bool IsDueToday => Due?.Date == DateTime.Today;
    public bool IsDueNow => Due <= DateTime.Now;
    public DateTime? LastReviewed { get; set; }
    public TimeSpan Interval { get; set; }
    public CardState State { get; set; }
    public int? LearningStage { get; set; }
    #endregion

    #region methods
    private void RescheduleHelper(DateTime newDate, bool keepInterval)
    {
        var now = DateTime.Now;
        var timeFromNow = newDate - now;

        if (Due.HasValue)
        {
            Due = newDate;

            if (!keepInterval)
                Interval += timeFromNow;
        }
        else
        {
            //* when postponing a new card, we add interval regardless of user's choice
            //* because it would introduce bugs when a card has a due date,
            //* state is review but interval = null
            Interval += timeFromNow;
        }
    }
    private void PostponeHelper(int moveBy, bool keepInterval)
    {
        var days = TimeSpan.FromDays(moveBy);

        if (Due.HasValue)
        {
            Due = Due.Value.Add(days);

            if (!keepInterval)
                Interval += days;
        }
        else
        {
            Due = DateTime.Today.Add(days);
            
            //* when postponing a new card, we add interval regardless of user's choice
            //* because it would introduce bugs when a card has a due date,
            //* state is review but interval = null
            Interval += days;
        }
    }
    private void ModifyDateHelper()
    {
        State = CardState.Review; // reschedule forces state to review, weird outcomes otherwise.
        LastModified = DateTime.Now;
        LearningStage = null;
    }
    public void FlipBuried() => IsBuried = !IsBuried;
    public void FlipSuspended() => IsSuspended = !IsSuspended;
    public void Reschedule(DateTime newReviewDate, bool keepInterval)
    {
        ModifyDateHelper();
        RescheduleHelper(newReviewDate, keepInterval);
    }
    public void Reschedule(int daysFromNow, bool keepInterval)
    {
        TimeSpan days = TimeSpan.FromDays(daysFromNow);

        ModifyDateHelper();
        RescheduleHelper(DateTime.Now + days, keepInterval);
    }
    public void Postpone(int putOffByDays, bool keepInterval)
    {
        ModifyDateHelper();
        PostponeHelper(putOffByDays, keepInterval);
    }
    public void Forget()
    {
        State = CardState.New;
        Due = DateTime.Now;
        LastModified = DateTime.Now;
        Interval = TimeSpan.MinValue;
    }
    public void MoveToDeck(long newDeckId) => DeckId = newDeckId;
    public void SyncTagsFrom(CardEntity other)
    {
        // Current tracked tag IDs
        var myTagIds = Tags
            .Select(t => t.Id)
            .ToHashSet();

        // Incoming tag IDs
        var otherTagIds = other.Tags
            .Select(t => t.Id)
            .ToHashSet();

        // Remove tags that no longer exist
        var toRemove = Tags
            .Where(t => !otherTagIds.Contains(t.Id))
            .ToList();

        toRemove.ForEach(t => Tags.Remove(t));

        // Add new tags
        foreach (var tag in other.Tags)
        {
            if (!myTagIds.Contains(tag.Id))
                Tags.Add(new Tag(tag.Id));
        }
    }
    public void ReplaceTagsWith(IEnumerable<Tag> newTags)
    {
        Tags.Clear();
        Tags.AddRange(newTags);
    }
    #endregion
    public static CardEntity CreateNew(string frontContent, string? backContent, long deckId, IEnumerable<Tag> tags)
    {
        return new()
        {
            Id = IdGetter.Next(),
            DeckId = deckId,

            Created = DateTime.Now,
            Interval = TimeSpan.Zero,
            State = CardState.New,

            LastReviewed = null,
            LastModified = null,
            LearningStage = null,

            IsBuried = false,
            IsSuspended = false,

            Tags = [..tags],
            
            FrontContent = frontContent,
            BackContent = backContent
        };
    }
}
