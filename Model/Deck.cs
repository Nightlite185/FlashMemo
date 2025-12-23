using System.Collections;
using Force.DeepCloner;
namespace FlashMemo.Model
{
    public class Deck: IEnumerable<Card>, IEquatable<Deck>
    {
        public Deck(string name, int? parentDeckId = null, params Card[]? cards)
        {   
            this.cards = cards is not null ? [..cards] : [];

            Name = name;
            Scheduler = new Scheduler();
            ParentDeckId = parentDeckId;
        }
        protected List<Card> cards;
        public string Name { get; private set; }
        public int Id { get; set; }
        public int? ParentDeckId { get; set; }
        public int UserId { get; }
        public Scheduler Scheduler { get; init; }
        public void Sort(SortingOptions sortBy = SortingOptions.Created, SortingDirection dir = SortingDirection.Descending)
        {   // TO DO: this should probably be encapsulated in sorting class, not here.
            switch (sortBy)
            {
                case SortingOptions.Created:
                    SortHelper(x => x.Created, dir);
                    break;

                case SortingOptions.Id:
                    SortHelper(x => x.Id, dir);
                    break;

                case SortingOptions.LastModified:
                    SortHelper(x => x.LastModified, dir);
                    break;

                case SortingOptions.LastReviewed:
                    SortHelper(x => x.LastReviewed, dir);
                    break;

                case SortingOptions.Interval:
                    SortHelper(x => x.Interval, dir);
                    break;

                case SortingOptions.NextReview:
                    SortHelper(x => x.NextReview, dir);
                    break;

                case SortingOptions.State:
                    SortHelper(x => x.State, dir);
                    break;

                case SortingOptions.Random:
                    SortHelper(_ => Random.Shared.Next(), dir);
                    break;

                default: throw new ArgumentOutOfRangeException(nameof(sortBy), $"Wrong {nameof(SortingOptions)} enum value, its {sortBy}");
            }
        }
        private void SortHelper<TOut> (Func<Card, TOut> keySelector, SortingDirection dir)
        {
            cards = dir switch
            {
                SortingDirection.Ascending => [.. cards.OrderBy(keySelector)],
                SortingDirection.Descending => [.. cards.OrderByDescending(keySelector)],

                _ => throw new ArgumentOutOfRangeException(nameof(dir), $"SortingDirection wasnt either asc or desc, but {dir}"),
            };
        }
        public void AddCards(params Card[] newCards) // if its temp deck? dont clone, else if normal: deep clone all arg cards 
        {                                           // BUT NOT IN BASE -> IN IMPLEMENTATION
            foreach (var c in newCards) c.DeckId = this.Id;
            cards.AddRange(newCards);
        }
        public Deck Duplicate(int? HighestCopyNum) // this shouldnt be a thing in temp deck, only in normal one.
        {                                      // TO DO: get this out of base -> to the normal deck implementation. 
            if (HighestCopyNum <= 0) throw new ArgumentOutOfRangeException(nameof(HighestCopyNum));
            
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