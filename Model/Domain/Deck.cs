using System.Collections;
using FlashMemo.Model.Persistence;
using Force.DeepCloner;

namespace FlashMemo.Model.Domain
{
    public class Deck: IEnumerable<Card>, IEquatable<Deck>
    {
        [Obsolete]
        public Deck(bool temporary, string name, params Card[]? newCards)
        {
            IsTemporary = temporary;

            cards = [];

            if (newCards is not null) 
                AddCards(newCards);

            Created = DateTime.Now;
            
            Name = name;
            Scheduler = new Scheduler();
        }
        public Deck(long id) => Id = id; // ctor for mapper only
        public Deck Rehydrate(ICollection<Card> cards, string name, User owner, 
                              DateTime created, Scheduler scheduler, bool isTemporary)
        {
            this.cards = [..cards];
            Name = name;
            Owner = owner;
            Created = created;
            Scheduler = scheduler;
            IsTemporary = isTemporary;

            return this;
        }
        #region Properties
        protected Card[] cards = null!;
        public string Name { get; set; } = null!;
        public long Id { get; private init; }

        public User Owner { get; protected set; } = null!;
        public DateTime Created { get; protected set; }
        public Scheduler Scheduler { get; protected set; } = null!; // only one scheduler per deck, can be shared tho.
        public bool IsTemporary { get; protected set; } // idk if I should go with this or make another class inheriting this one. Theres not that much to add tho, just some diff rules.
        public int Count => cards.Length;
        #endregion
        
        #region Methods
        [Obsolete]
        public void AddCards(params Card[] newCards)
        {
            if (!this.IsTemporary)
                foreach (var c in newCards)
                {
                    if (c.ParentDeck.Equals(this))
                        throw new InvalidOperationException($"Card with id {c.Id} already belongs to this deck.");
                    
                    c.ParentDeck = this;
                }
            
            //cards.AddRange(newCards);
        }
        
        [Obsolete("this does not belong here, should be somewhere else, bc it couples library with persistance and domain")]
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
        public void SortBy(SortingOptions options, SortingDirection direction)
            => cards = [..this.Sort(options, direction)];
        #endregion

        #region Equality, IEnumerable, and indexer boilerplate
        public override bool Equals(object? obj)
            => obj is Deck d && this.Id == d.Id;
        public override int GetHashCode()
            => HashCode.Combine(Id);
        public bool Equals(Deck? other)
            => other is Deck d && d.Id == this.Id;

        public IEnumerator<Card> GetEnumerator()
        {
            foreach (var card in cards)
                yield return card;
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public Card this[int index]
        {
            get { return cards[index]; }
            set { cards[index] = value; }
        }
        #endregion
    }
}