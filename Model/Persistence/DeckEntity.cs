namespace FlashMemo.Model.Persistence
{
    public class DeckEntity(): IEntity
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTime Created { get; set; }
        public long? UserId { get; set; }
        public long? OptionsId { get; set; }
        public DeckOptions Options { get; set; } = null!;
        public UserEntity User { get; set; } = null!;
        public ICollection<CardEntity> Cards { get; set; } = [];
        public ICollection<DeckEntity> ChildrenDecks { get; set; } = [];
        public DeckEntity ParentDeck { get; set; } = null!;
        public long? ParentDeckId { get; set; }
        public bool IsTemporary { get; set; }
    }
}