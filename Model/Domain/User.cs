using System.Security.Cryptography;
using System.Text;

namespace FlashMemo.Model.Domain
{
    public sealed class User: IEquatable<User>
    {
        public User(string name, string password) // move this to entity
        {
            HashedPassword = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            Id = IdGetter.Next();
            Username = name;
            Options = new();

            DeckOptionsIds = [];
            DeckIds = [];
            TagIds = [];
        }
        public User(long id) => Id = id; // for mapper use only
        
        #region Properties
        public string Username { get; set; } = null!;
        public UserOptions Options { get; private set; } = null!;
        public long Id { get; private init; }
        public byte[] HashedPassword { get; private set; } = [];

        public List<long> DeckIds { get; set; } = [];
        public List<long> DeckOptionsIds { get; set; } = [];
        public List<long> TagIds { get; set; } = [];
        #endregion

        #region Hashcode and Equals
        public override bool Equals(object? obj)
            => obj is User u && this.Id == u.Id;
        public override int GetHashCode()
            => HashCode.Combine(Id);
        public bool Equals(User? other)
            => other is User u && u.Id == this.Id;
        #endregion
    }
}