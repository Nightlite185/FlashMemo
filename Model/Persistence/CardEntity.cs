using FlashMemo.Helpers;
using FlashMemo.Model.Domain;

namespace FlashMemo.Model.Persistence;
    public interface IEntity { long Id { get; set; } }
    public class CardEntity(): IEntity
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
        public DateTime LastModified { get; set; }
        public DateTime Due { get; set; }
        public bool IsDueNow => Due <= DateTime.Now;
        public DateTime LastReviewed { get; set; }
        public TimeSpan Interval { get; set; }
        public CardState State { get; set; }
        public int? LearningStage { get; set; }
        #endregion

        #region methods
        public void FlipBuried() => IsBuried = !IsBuried;
        public void FlipSuspended() => IsSuspended = !IsSuspended;
        public void Reschedule(DateTime newReviewDate, bool keepInterval)
        {
            State = CardState.Review; // reschedule forces state to review, weird outcomes otherwise.
            Due = newReviewDate;
            LastModified = DateTime.Now;

            if (!keepInterval)
                Interval += newReviewDate - DateTime.Now;
        }
        public void Reschedule(TimeSpan timeFromNow, bool keepInterval)
        {
            var now = DateTime.Now;

            Due = now.Add(timeFromNow);
            LastModified = now;
            State = CardState.Review;

            if (!keepInterval) 
                Interval += timeFromNow;
        }
        public void Postpone(int putOffByDays, bool keepInterval)
        {
            var interval = TimeSpan.FromDays(putOffByDays);

            Due = Due.Add(interval);
            LastModified = DateTime.Now;
            State = CardState.Review;

            if (!keepInterval) 
                Interval += interval;
        }
        public void Forget()
        {
            State = CardState.New;
            Due = DateTime.Now;
            LastModified = DateTime.Now;
            Interval = TimeSpan.MinValue;
        }
        public void MoveToDeck(Deck newDeck) => DeckId = newDeck.Id;
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
    }
