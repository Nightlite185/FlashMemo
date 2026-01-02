namespace FlashMemo.Model.Persistence
{
    public class DeckEntity: IEntity
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTime Created { get; set; }
        public long UserId { get; set; }
        public UserEntity Owner { get; set; } = null!;
        public SchedulerEntity Scheduler { get; set; } = null!;
        public ICollection<CardEntity> Cards { get; set; } = [];
        public bool IsTemporary { get; set; }
    }
}