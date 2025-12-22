using System.Security.Cryptography;
using System.Text;

namespace FlashMemo.Model
{
    public class User: IEquatable<User>
    {
        public User(string name, string password)
        {
            HashedPassword = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            Username = name;
        }
        public string Username { get; set; }
        public int Id { get; set; }
        private byte[] HashedPassword;

        public List<Deck> Collection { get; set; } = [];
        public List<Scheduler> SchedulerPresets { get; set; } = [];
        
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