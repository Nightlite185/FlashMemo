namespace FlashMemo.Model.Persistence
{
    public class UserEntity(): IEntity
    {
        public long Id { get; set; }
        public string Username { get; set; } = null!;
        public byte[] HashedPassword { get; set; } = null!;
        public UserOptions Options { get; set; } = null!;
        public ICollection<DeckEntity> Decks { get; set; } = [];
        public ICollection<Tag> Tags { get; set; } = [];
        public ICollection<CardLogEntity> Logs { get; set; } = [];
        public ICollection<DeckOptions> DeckOptions { get; set; } = [];
    }
}