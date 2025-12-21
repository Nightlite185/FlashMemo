using System.Collections;
using Force.DeepCloner;
namespace FlashMemo.Model
{
    public class Deck: IEnumerable<Card>
    {
        public Deck(string name, params Card[]? cards)
        {   
            if (cards is not null)
                this.cards = [..cards];

            else this.cards = [];

            Name = name;
            Scheduler = new Scheduler();
        }

        private List<Card> cards;
        public string Name { get; private set; }
        public int Id { get; set; }
        public Scheduler Scheduler { get; init; }
        public void Sort(SortingOptions sortBy = SortingOptions.Created, SortingDirection dir = SortingDirection.Descending)
        {
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
        public void Rename(string newName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(newName, nameof(newName));
            this.Name = newName;
        }
        public void AddCards(params Card[] cards)
        {
            this.cards.AddRange(cards);
        }
        public Deck Clone()
        {
            throw new NotImplementedException();
            // return this.DeepClone();
            // think ab what to do with the Ids bc they should be only assigned by the db
        }
    }
}