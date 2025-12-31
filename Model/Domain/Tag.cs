namespace FlashMemo.Model.Domain
{
    public class Tag(string name): IEquatable<Tag>
    {
        public int Id { get; set; }
        public User Owner { get; set; } = null!;
        public string Name { get; set; } = name;
        public uint Color { get; set; }
        
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