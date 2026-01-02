namespace FlashMemo.Model.Persistence
{
    public class UserEntity: IEntity
    {
        public long Id { get; set; }
        public string Username { get; set; } = null!;
        public byte[] HashedPassword { get; set; } = null!;
        public ICollection<DeckEntity> Decks { get; set; } = [];
        public ICollection<SchedulerEntity> SchedulerPresets { get; set; } = [];
        public ICollection<TagEntity> Tags { get; set; } = [];
        public ICollection<CardLogEntity> Logs { get; set; } = [];
        // TO DO: add settings entity in the future, when its implemented.
    }
}