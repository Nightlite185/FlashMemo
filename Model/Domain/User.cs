using System.Security.Cryptography;
using System.Text;

namespace FlashMemo.Model.Domain
{
    public sealed class User: IEquatable<User>
    {
        public User(string name, string password) // genuine creation
        {
            HashedPassword = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            Id = IdGetter.Next();
            Username = name;
            Cfg = new();

            SchedulerPresets = [];
            Decks = [];
            Tags = [];
        }
        public User(long id) => Id = id; // for mapper use only
        
        #region Properties
        public string Username { get; set; } = null!;
        public Settings Cfg { get; private set; } = null!;
        public long Id { get; private init; }
        private byte[] HashedPassword = null!;

        public List<Deck> Decks { get; set; } = null!;
        public List<Scheduler> SchedulerPresets { get; set; } = null!;
        public List<Tag> Tags { get; set; } = null!;
        #endregion
        public User Rehydrate(string username, byte[] hashedPassword, ICollection<Deck> decks, // TO DO: add settings object later
                              ICollection<Scheduler> schedulers, ICollection<Tag> tags)
        {                                                                                                                                                                              
            Username = username;
            HashedPassword = hashedPassword;
            //Cfg = cfg; // not implemented yet

            Decks = [..decks];
            SchedulerPresets = [..schedulers];
            Tags = [..tags];
            
            return this;
        }

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