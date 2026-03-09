using FlashMemo.Helpers;

namespace FlashMemo.Model.Persistence
{
    public class UserEntity
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTime Created { get; set; }
        public UserOptions Options { get; set; } = null!;
        
        public static UserEntity Create(string name)
        {
            return new()
            {
                Id = IdGetter.Next(),
                Created = DateTime.Now,
                Name = name,
                Options = UserOptions.CreateDefault() // Create default here
            };
        }
    }
}