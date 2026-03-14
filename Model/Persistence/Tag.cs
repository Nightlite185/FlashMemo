using FlashMemo.Helpers;
using FlashMemo.Model.Domain;

namespace FlashMemo.Model.Persistence
{
    public class Tag
    {
        public Tag(long id) => Id = id;
        public Tag(){}
        public const int DefaultColor = -1; // placeholder, custom colors not implemented yet.
        public long Id { get; set; }
        public long UserId { get; set; }
        public UserEntity User { get; set; } = null!;
        public ICollection<CardEntity> Cards { get; set; } = [];
        public string Name { get; set; } = null!;
        public int IntColor { get; set; }

        public static Tag CreateNew(string name, int color = DefaultColor)
        {
            return new()
            {
                Id = IdGetter.Next(),
                Name = name,
                IntColor = color
            };
        }
    }
}