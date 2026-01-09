using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;

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
        private static IEnumerable<CardEntity> SortDispatcher(this IEnumerable<CardEntity> cards, SortingOptions sortBy = DefSortBy, SortingDirection dir = DefDir)
        {
            return sortBy switch
            {
                SortingOptions.Created => cards.SortHelper(x => x.Created, dir),
                SortingOptions.LastModified => cards.SortHelper(x => x.LastModified, dir),
                SortingOptions.NextReview => cards.SortHelper(x => x.NextReview, dir),
                SortingOptions.LastReviewed => cards.SortHelper(x => x.LastReviewed, dir),
                SortingOptions.Interval => cards.SortHelper(x => x.Interval, dir),
                SortingOptions.State => cards.SortHelper(x => x.State, dir),
                SortingOptions.Id => cards.SortHelper(x => x.Id, dir),
                SortingOptions.Random => cards.SortHelper(_ => Random.Shared.Next(), dir),

                _ => throw new ArgumentOutOfRangeException(nameof(sortBy), $"Wrong {nameof(SortingOptions)} enum value, its {sortBy}")
            };
        }
        public static IOrderedEnumerable<CardEntity> OrderCards(this IEnumerable<CardEntity> cards, DeckOptions.OrderingOpt options)
        {
            throw new NotImplementedException(); // TO DO: IMPLEMENT THISSSS
        }
        private static IOrderedEnumerable<CardEntity> SortHelper<TOut> (this IEnumerable<CardEntity> cards, Func<CardEntity, TOut> keySelector, SortingDirection dir)
        {
            return dir switch
            {
                SortingDirection.Ascending => cards.OrderBy(keySelector),
                SortingDirection.Descending => cards.OrderByDescending(keySelector),

                _ => throw new ArgumentOutOfRangeException(nameof(dir), $"SortingDirection wasnt either asc or desc, but {dir}"),
            };
        }
    }
}