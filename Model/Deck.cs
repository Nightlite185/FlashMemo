namespace FlashMemo.Model
{
    public class Deck
    {
        public Deck(string name, params Card[]? cards)
        {   
            if (cards is not null)
                Cards = [..cards];

            else Cards = [];

            Name = name;
            Options = Settings.DeckSettings.GetDefault();
        }

        private List<Card> Cards { get; set; }
        public string Name { get; private set; }
        public Settings.DeckSettings Options { get; init; }
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

                case SortingOptions.Random:
                    SortHelper(_ => Random.Shared.Next(), dir);
                    break;

                default: throw new ArgumentOutOfRangeException(nameof(sortBy), $"Wrong {nameof(SortingOptions)} enum value, its {sortBy}");
            }
        }
        private void SortHelper<TOut> (Func<Card, TOut> keySelector, SortingDirection dir)
        {
            Cards = dir switch
            {
                SortingDirection.Ascending => [.. Cards.OrderBy(keySelector)],
                SortingDirection.Descending => [.. Cards.OrderByDescending(keySelector)],

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
            Cards.AddRange(cards);
        }
        public Deck Clone()
        {
            throw new NotImplementedException();
            // return this.DeepClone();
            // think ab what to do with the Ids bc they should be only assigned by the db
        }
    }
}