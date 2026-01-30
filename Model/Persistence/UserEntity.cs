namespace FlashMemo.Model.Persistence
{
    public class UserEntity(): IEntity
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public byte[] HashedPassword { get; set; } = null!;
        public DateTime Created { get; set; }
        public UserOptions Options { get; set; } = null!;
        public ICollection<Deck> Decks { get; set; } = [];
        public ICollection<Tag> Tags { get; set; } = [];
        public ICollection<CardLog> Logs { get; set; } = [];
        public ICollection<DeckOptionsEntity> DeckOptions { get; set; } = [];
    }
}