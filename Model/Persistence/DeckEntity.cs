namespace FlashMemo.Model.Persistence
{
    public class DeckEntity(): IEntity
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTime Created { get; set; }
        public long? UserId { get; set; }
        public long? SchedulerId { get; set; }
        public int DailyReviewLimit { get; set; } // TO DO: later move those limits into a deck options class
        public int DailyNewLimit { get; set; } // this too
        public UserEntity User { get; set; } = null!;
        public SchedulerEntity Scheduler { get; set; } = null!;
        public ICollection<CardEntity> Cards { get; set; } = [];
        public ICollection<DeckEntity> ChildrenDecks { get; set; } = [];
        public DeckEntity ParentDeck { get; set; } = null!;
        public long? ParentDeckId { get; set; }
        public bool IsTemporary { get; set; }
    }
}