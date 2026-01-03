namespace FlashMemo.Model.Persistence
{
    public class TagEntity(): IEntity
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public int Color { get; set; }
        public long? UserId { get; set; }
        public UserEntity User { get; set; } = null!;
        public ICollection<CardEntity> Cards { get; set; } = [];
    }
}