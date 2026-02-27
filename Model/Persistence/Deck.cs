using System.Collections;
using FlashMemo.Helpers;

namespace FlashMemo.Model.Persistence
{
    public class Deck(): IEntity, IEnumerable<CardEntity>, IEquatable<Deck>, IDeckMeta
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTime Created { get; set; }
        public long UserId { get; set; }
        public long OptionsId { get; set; }
        public DeckOptionsEntity Options { get; set; } = null!;
        public UserEntity User { get; set; } = null!;
        public ICollection<CardEntity> Cards { get; set; } = [];
        public ICollection<Deck> ChildrenDecks { get; set; } = [];
        public Deck? ParentDeck { get; set; }
        public long? ParentDeckId { get; set; }
        public bool IsTemporary { get; set; } //? idk if I should go with this or make another class inheriting this one. Theres not that much to add tho, just some diff rules.

        #region methods

        public static Deck CreateNew(string name, long userId, long? parentId)
        {
            return new()
            {
                Id = IdGetter.Next(),
                Name = name,
                Created = DateTime.Now,
                UserId = userId,
                OptionsId = -1, // default preset
                ParentDeckId = parentId,
                IsTemporary = false,
            };
        }
        #endregion

        #region Equality, IEnumerable, and indexer boilerplate
        public override bool Equals(object? obj)
            => obj is Deck d && this.Id == d.Id;
        public override int GetHashCode()
            => HashCode.Combine(Id);
        public bool Equals(Deck? other)
            => other is Deck d && d.Id == this.Id;

        public IEnumerator<CardEntity> GetEnumerator()
            => Cards.Concat(ChildrenDecks.SelectMany(d => d.AsEnumerable()))
              .GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion
    }
}