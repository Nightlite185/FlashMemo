using FlashMemo.Helpers;

namespace FlashMemo.Model.Persistence
{
    public class Tag
    {
        public Tag(long id) => Id = id;
        public Tag(){}
        public long Id { get; set; }
        public long UserId { get; set; }
        public UserEntity User { get; set; } = null!;
        public ICollection<CardEntity> Cards { get; set; } = [];
        public string Name { get; set; } = null!;
        
        public static Tag CreateNew(string name, long userId)
        {
            return new()
            {
                Id = IdGetter.Next(),
                UserId = userId,
                Name = name,
            };
        }
    }
}