using FlashMemo.Model.Persistence;

namespace FlashMemo.Services
{

    public static class Sorting
    {
        public static IQueryable<CardEntity> SortLessons(this IQueryable<CardEntity> cards, DeckOptions.OrderingOpt options)
        {
            var orderBy = options.LessonsOrder;
            var dir = options.LessonsSortDir;

            return orderBy switch
            {
                LessonOrder.Created => cards.SortHelper(c => c.Created, dir),
                LessonOrder.LastModified => cards.SortHelper(c => c.LastModified, dir),
                LessonOrder.Random => cards.SortHelper(_ => Random.Shared.Next(), dir),
                // IMPORTANT TO DO: random sorting NEEDS to be done in memory.

                _ => throw new ArgumentOutOfRangeException(nameof(options.LessonsOrder), $"Wrong {nameof(LessonOrder)} enum value: {orderBy}")
            };
        }
        public static IQueryable<CardEntity> SortReviews(this IQueryable<CardEntity> cards, DeckOptions.OrderingOpt options)
        {
            var orderBy = options.ReviewsOrder;
            var dir = options.ReviewsSortDir;

            return orderBy switch
            {
                ReviewOrder.Created => cards.SortHelper(c => c.Created, dir),
                ReviewOrder.Due => cards.SortHelper(c => c.Due, dir),
                ReviewOrder.Interval => cards.SortHelper(c => c.Interval, dir),
                ReviewOrder.LastModified => cards.SortHelper(c => c.LastModified, dir),
                ReviewOrder.LastReviewed => cards.SortHelper(c => c.LastReviewed, dir),
                ReviewOrder.Random => cards.SortHelper(_ => Random.Shared.Next(), dir),
                // IMPORTANT TO DO: random sorting NEEDS to be done in memory.

                _ => throw new ArgumentOutOfRangeException(nameof(options.ReviewsOrder), $"Wrong {nameof(ReviewOrder)} enum value: {orderBy}")
            };
        }
        public static IQueryable<CardEntity> SortAny(this IQueryable<CardEntity> cards, CardsOrder orderBy, SortingDirection dir)
        {
            return orderBy switch
            {
                CardsOrder.Created => cards.SortHelper(c => c.Created, dir),
                CardsOrder.Id => cards.SortHelper(c => c.Id, dir),
                CardsOrder.LastModified => cards.SortHelper(c => c.LastModified, dir),
                CardsOrder.Due => cards.SortHelper(c => c.Due, dir),
                CardsOrder.LastReviewed => cards.SortHelper(c => c.LastReviewed, dir),
                CardsOrder.Interval => cards.SortHelper(c => c.Interval, dir),
                CardsOrder.State => cards.SortHelper(c => c.State, dir),
                CardsOrder.Random => cards.SortHelper(_ => Random.Shared.Next(), dir),
                // IMPORTANT TO DO: random sorting NEEDS to be done in memory.

                _ => throw new ArgumentOutOfRangeException(nameof(orderBy), $"Wrong {nameof(CardsOrder)} enum value: {orderBy}")
            };
        }
        private static IQueryable<CardEntity> SortHelper<TOut> (this IQueryable<CardEntity> cards, Func<CardEntity, TOut> keySelector, SortingDirection dir)
        {
            return dir switch
            {
                SortingDirection.Ascending 
                    => cards.OrderBy(keySelector)
                    .AsQueryable(),

                SortingDirection.Descending 
                    => cards.OrderByDescending(keySelector)
                    .AsQueryable(),

                _ => throw new ArgumentOutOfRangeException(
                    nameof(dir), $"SortingDirection wasnt either asc or desc, but {dir}"),
            };
        }
    }
}