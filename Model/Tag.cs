using System.Windows.Media;

namespace FlashMemo.Model
{
    public class Tag(string name)
    {
        public int Id { get; set; }
        public string Name { get; set; } = name;
        public int UserId { get; set; }
        public ICollection<Card> Cards { get; set; } = [];
        public Color Color { get; set; }
    }
}