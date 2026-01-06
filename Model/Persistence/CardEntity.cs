using FlashMemo.Model.Domain;

namespace FlashMemo.Model.Persistence
{
    public interface IEntity { long Id { get; set; } }
    public class CardEntity(): IEntity
    {
        public long Id { get; set; }
        public string FrontContent { get; set; } = null!;
        public string? BackContent { get; set; }
        public long DeckId { get; set; }
        public ICollection<TagEntity> Tags { get; set; } = [];
        public ICollection<CardLogEntity> CardLogs { get; set; } = [];
        public DeckEntity Deck { get; set; } = null!;
        public bool IsBuried { get; set; }
        public bool IsSuspended { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastModified { get; set; }
        public DateTime NextReview { get; set; }
        public DateTime LastReviewed { get; set; }
        public TimeSpan Interval { get; set; }
        public CardState State { get; set; }
        public int? LearningStage { get; set; }

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
                    Tags.Add(new TagEntity { Id = tag.Id });
            }
        }
    }
}