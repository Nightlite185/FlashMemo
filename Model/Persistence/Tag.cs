using FlashMemo.Helpers;

namespace FlashMemo.Model.Persistence
{
    public class Tag: IEquatable<Tag>, IEntity
    {
        public Tag(string name, int color) // genuine creation
        {
            Id = IdGetter.Next();
            
            IntColor = color;
            Name = name;
        }
        public Tag(long id) => Id = id; // ctor for EF
        public long Id { get; set; }
        public long UserId { get; set; }
        public UserEntity User { get; set; } = null!;
        public ICollection<CardEntity> Cards { get; set; } = [];
        public string Name { get; set; } = null!;
        public int IntColor { get; set; }
        
        #region equality
        public override bool Equals(object? obj)
            => obj is Tag other && other.Id == this.Id;

        public bool Equals(Tag? other)
            => other is Tag t && this.Id == t.Id;

        public override int GetHashCode()
            => HashCode.Combine(Id);
        #endregion
    }
}