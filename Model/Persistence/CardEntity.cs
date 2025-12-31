namespace FlashMemo.Model.Persistence
{
    public interface IEntity{ }
    public class CardEntity: IEntity
    {
        public int Id { get; set; }
        public string FrontContent { get; set; } = null!;
        public string? BackContent { get; set; }
        public int DeckId { get; set; }
        public ICollection<TagEntity> Tags { get; set; } = [];
        public DeckEntity Deck { get; set; } = null!;
        public bool IsBuried { get; set; }
        public bool IsSuspended { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastModified { get; set; }
        public DateTime NextReview { get; set; }
        public DateTime LastReviewed { get; set; }
        public TimeSpan Interval { get; set; }
        public CardState State { get; set; }
    }
}