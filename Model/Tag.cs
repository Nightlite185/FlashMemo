using System.Windows.Media;

namespace FlashMemo.Model
{
    public class Tag(string name): IEquatable<Tag>
    {
        public int Id { get; set; }
        public string Name { get; set; } = name;
        public int UserId { get; set; }
        public ICollection<Card> Cards { get; set; } = [];
        public Color Color { get; set; }

        public override bool Equals(object? obj)
            => obj is Tag other && other.Id == this.Id;

        public bool Equals(Tag? other)
            => other is Tag t && this.Id == t.Id;

        public override int GetHashCode()
            => HashCode.Combine(Id);
    }
}