using FlashMemo.Helpers;
namespace FlashMemo.Model.Domain
{
    public class Tag: IEquatable<Tag>
    {
        public Tag(string name, int color) // genuine creation
        {
            Id = IdGetter.Next();
            
            Color = color;
            Name = name;
        }
        public Tag(long id) => Id = id; // ctor for mapper only
        public long Id { get; private init; }
        public User Owner { get; set; } = null!;
        public string Name { get; set; } = null!;
        public int Color { get; set; }
        
        public Tag Rehydrate(string name, int color, User owner)
        {
            Name = name;
            Color = color;
            Owner = owner;

            return this;
        }
        
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