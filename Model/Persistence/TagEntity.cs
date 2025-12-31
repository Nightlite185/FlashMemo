namespace FlashMemo.Model.Persistence
{
    public class TagEntity: IEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public uint Color { get; set; }
        public int UserId { get; set; }
        public UserEntity Owner { get; set; } = null!;
        public ICollection<CardEntity> Cards { get; set; } = [];
    }
}