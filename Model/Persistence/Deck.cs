using System.Collections;
using Force.DeepCloner;

namespace FlashMemo.Model.Persistence
{
    public class Deck(): IEntity, IEnumerable<CardEntity>, IEquatable<Deck>
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTime Created { get; set; }
        public long? UserId { get; set; }
        public long? OptionsId { get; set; }
        public DeckOptions Options { get; set; } = null!;
        public UserEntity User { get; set; } = null!;
        public ICollection<CardEntity> Cards { get; set; } = [];
        public ICollection<Deck> ChildrenDecks { get; set; } = [];
        public Deck ParentDeck { get; set; } = null!;
        public long? ParentDeckId { get; set; }
        public bool IsTemporary { get; set; } // idk if I should go with this or make another class inheriting this one. Theres not that much to add tho, just some diff rules.
        public int Count => Cards.Count;
        
        #region Methods
        [Obsolete]
        public void AddCards(params CardEntity[] newCards)
        {
            if (!this.IsTemporary)
                foreach (var c in newCards)
                {
                    if (c.Deck.Equals(this))
                        throw new InvalidOperationException($"Card with id {c.Id} already belongs to this deck.");
                    
                    c.Deck = this;
                }
        }
        
        [Obsolete()]
        public Deck Duplicate(int? HighestCopyNum)
        {
            if (HighestCopyNum <= 0) throw new ArgumentOutOfRangeException(nameof(HighestCopyNum));
            if (IsTemporary) throw new InvalidOperationException("Cannot duplicate a temporary deck.");

            var copy = this.DeepClone();

            copy.Name = $"{this.Name} - copy" + (
                HighestCopyNum != null
                    ? $"({HighestCopyNum+1})" 
                    : ""
            );

            return copy;
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