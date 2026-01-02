using System.Collections;
using Force.DeepCloner;
using FlashMemo.Model;

namespace FlashMemo.Model.Domain
{
    public class Deck: IEnumerable<Card>, IEquatable<Deck>
    {
        public Deck(bool temporary, string name, Deck? parentDeck = null, params Card[]? newCards)
        {
            Id = IdGetter.Next();
            IsTemporary = temporary;

            cards = [];

            if (newCards is not null) 
                AddCards(newCards);

            Created = DateTime.Now;
            
            Name = name;
            Scheduler = new Scheduler();
            ParentDeck = parentDeck;
        }
        
        #region Properties
        protected List<Card> cards;
        public string Name { get; set; }
        public int Id { get; init; }
        public Deck? ParentDeck { get; set; } // this stays or not: depending on if parent deck's options affect the children's, or the hierarchy is purely for visual UI organizing.
        public User Owner { get; init; } = null!;
        public DateTime Created { get; init; }
        public Scheduler Scheduler { get; protected set; } // only one scheduler per deck, can be shared tho.
        public bool IsTemporary { get; init; } // idk if I should go with this or make another class inheriting this one. Theres not that much to add tho, just some diff rules.
        public int Count => cards.Count;
        #endregion
        
        #region Methods
        public void AddCards(params Card[] newCards)
        {
            if (!this.IsTemporary)
                foreach (var c in newCards)
                {
                    if (c.ParentDeck.Equals(this))
                        throw new InvalidOperationException($"Card with id {c.Id} already belongs to this deck.");
                    
                    c.ParentDeck = this;
                }

            cards.AddRange(newCards);
        }
        
        [Obsolete("this does not belong here, should be somewhere else, bc it couples library with persistance and domain")]
        public Deck Duplicate(int? HighestCopyNum)
        {
            if (HighestCopyNum <= 0) throw new ArgumentOutOfRangeException(nameof(HighestCopyNum));
            if (IsTemporary) throw new InvalidOperationException("Cannot duplicate a temporary deck.");

            var copy = this.DeepClone();
            
            copy.cards.ForEach(c => c.Id = 0);

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