using System.Collections;
using Force.DeepCloner;
namespace FlashMemo.Model
{
    public class Deck: IEnumerable<Card>, IEquatable<Deck>
    {
        public Deck(bool temporary, string name, int? parentDeckId = null, params Card[]? newCards)
        {
            IsTemporary = temporary;

            cards = [];

            if (newCards is not null) 
                AddCards(newCards);

            Created = DateTime.Now;
            
            Name = name;
            Scheduler = new Scheduler();
            ParentDeckId = parentDeckId;
        }
        protected List<Card> cards;
        public string Name { get; set; }
        public int Id { get; set; }
        public int? ParentDeckId { get; set; }
        public int UserId { get; set; }
        public DateTime Created { get; }
        public Scheduler Scheduler { get; init; }
        public bool IsTemporary { get; init; }
        
        public void AddCards(params Card[] newCards)
        {                                         
            if (!this.IsTemporary)
                foreach (var c in newCards)
                    c.DeckId = this.Id;

            cards.AddRange(newCards);
        }
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
        public void Clear() => cards.Clear();
        public void SortBy(SortingOptions options, SortingDirection direction)
            => cards = [..this.Sort(options, direction)];
        public int Count => cards.Count;

        public Card this[int index]
        {
            get { return cards[index]; }
            set { cards[index] = value; }
        }
        public IEnumerator<Card> GetEnumerator()
        {
            foreach (var card in cards)
                yield return card;
        }
        IEnumerator IEnumerable.GetEnumerator() 
            => GetEnumerator();

        #region Hashcode and Equals
        public override bool Equals(object? obj)
            => obj is Deck d && this.Id == d.Id;
        public override int GetHashCode()
            => HashCode.Combine(Id);
        public bool Equals(Deck? other)
            => other is Deck d && d.Id == this.Id;
        #endregion
    }
}