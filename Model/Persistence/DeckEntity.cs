namespace FlashMemo.Model.Persistence
{
    public class DeckEntity: IEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTime Created { get; set; }
        public int? ParentDeckId { get; set; }
        public DeckEntity? ParentDeck { get; set; }
        public int UserId { get; set; }
        public UserEntity User { get; set; } = null!;
        public SchedulerEntity Scheduler { get; set; } = null!;
        public ICollection<CardEntity> Cards { get; set; } = [];
        public bool IsTemporary { get; set; }
    }
}