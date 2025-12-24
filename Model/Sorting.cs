namespace FlashMemo.Model
{
    public enum SortingOptions
    {
        Random,
        Id,
        Created,
        LastModified,
        NextReview,
        LastReviewed,
        Interval,
        State
    }
    public enum SortingDirection { Ascending, Descending }

    public static class Sorting
    {
        private const SortingOptions DefSortBy = SortingOptions.Created;
        private const SortingDirection DefDir = SortingDirection.Descending;
        public static IEnumerable<Card> Sort(this Deck d, SortingOptions sortBy = DefSortBy, SortingDirection dir = DefDir)
        {
            return sortBy switch
            {
                SortingOptions.Created => d.SortHelper(x => x.Created, dir),
                SortingOptions.LastModified => d.SortHelper(x => x.LastModified, dir),
                SortingOptions.NextReview => d.SortHelper(x => x.NextReview, dir),
                SortingOptions.LastReviewed => d.SortHelper(x => x.LastReviewed, dir),
                SortingOptions.Interval => d.SortHelper(x => x.Interval, dir),
                SortingOptions.State => d.SortHelper(x => x.State, dir),
                SortingOptions.Id => d.SortHelper(x => x.Id, dir),
                SortingOptions.Random => d.SortHelper(_ => Random.Shared.Next(), dir),

                _ => throw new ArgumentOutOfRangeException(nameof(sortBy), $"Wrong {nameof(SortingOptions)} enum value, its {sortBy}")
            };
        }
        private static IEnumerable<Card> SortHelper<TOut> (this Deck d, Func<Card, TOut> keySelector, SortingDirection dir)
        {
            return dir switch
            {
                SortingDirection.Ascending => [.. d.OrderBy(keySelector)],
                SortingDirection.Descending => [.. d.OrderByDescending(keySelector)],

                _ => throw new ArgumentOutOfRangeException(nameof(dir), $"SortingDirection wasnt either asc or desc, but {dir}"),
            };
        }
    }
}