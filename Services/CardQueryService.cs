using FlashMemo.Model;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Services
{
    public class CardQueryService(IDbContextFactory<AppDbContext> factory): DbDependentClass(factory)
    {
        #region Public methods
        public async Task<IList<CardEntity>> GetCardsWhere(Filters filters, CardsOrder order, SortingDirection dir)
        {
            var db = GetDb;
            var query = filters.ToExpression();

            var cards = await db.Cards
                .Where(query)
                .SortAnyCards(order, dir)
                .ToListAsync();

            cards.ShuffleIf(order == CardsOrder.Random);

            return cards;
        }
        public async Task<IEnumerable<CardEntity>> GetForStudy(long deckId)
        {
            var db = GetDb;
            var baseQuery = await 
                AllCardsInDeckQAsync(deckId, db);
            
            baseQuery = baseQuery.Where(c =>
                !c.IsSuspended 
                && !c.IsBuried);
            
            #region branching into 3 subsets and ordering
            var rootDeck = await db.Decks.FindAsync(deckId)
                ?? throw new ArgumentException(IdNotFoundMsg("Deck"), nameof(deckId));

            var sortOpt = rootDeck.Options.Sorting;
            var limitsOpt = rootDeck.Options.DailyLimits;

            var grouped = GroupByStateQ(baseQuery);

            var learning = await grouped.Learning
                .OrderBy(c => c.Due)
                .ToListAsync();

            var lessons = await grouped.Lessons
                .SortLessons(sortOpt)
                .Take(limitsOpt.DailyLessonsLimit)
                .ToListAsync();
            
            var reviews = await grouped.Reviews
                .SortReviews(sortOpt)
                .Take(limitsOpt.DailyReviewsLimit)
                .ToListAsync();

            lessons.ShuffleIf(sortOpt.LessonsOrder == LessonOrder.Random);
            reviews.ShuffleIf(sortOpt.ReviewsOrder == ReviewOrder.Random);
            #endregion
            
            #region final return
            return sortOpt.CardStateOrder switch
            {
                CardStateOrder.NewThenReviews
                    => learning
                        .Concat(lessons)
                        .Concat(reviews),

                CardStateOrder.ReviewsThenNew
                    => learning
                        .Concat(reviews)
                        .Concat(lessons),

                CardStateOrder.Mix
                    => learning.Concat(
                            reviews.Concat(lessons).Shuffle()),

                _ => throw new ArgumentException(
                    $"Invalid {nameof(CardStateOrder)} enum value: {sortOpt.CardStateOrder}")
            };
            #endregion
        }
        public async Task<IList<CardEntity>> GetAllFromUser(long userId)
        {
            var db = GetDb;

            var deckIds = db.Decks
                .Where(d => d.UserId == userId)
                .Select(d => d.Id);

            return await db.Cards
                .Where(c => deckIds.Contains(c.DeckId))
                .ToListAsync();
        }
        public async Task<IList<CardEntity>> GetAllFromDeck(long deckId)
        {
            var db = GetDb;

            var cardsQuery = await 
                AllCardsInDeckQAsync(deckId, db);

            return await cardsQuery.ToListAsync();
        }
        public async Task<IDictionary<long, CardsCount>> GetCardsCountFor(IEnumerable<long> deckIds)
        {
            var db = GetDb;
            Dictionary<long, CardsCount> result = [];

            foreach(long id in deckIds)
            {
                var allCardsQuery = await AllCardsInDeckQAsync(id, db);
                var grouped = GroupByStateQ(allCardsQuery);
                var counted = await CountByStateAsync(grouped);

                if (!result.TryAdd(id, counted))
                    throw new ArgumentException(
                        $"provided IEnumerable contains duplicate deck ids", 
                        nameof(deckIds)
                    );
            }
            
            return result;
        }
        #endregion
        
        #region Private helpers || Q in member names stands for Query
        ///<summary> if any duplicates -> change IList to hashset. 99% sure there won't be any tho</summary>
        private static void GetChildrenIds(long deckId, ILookup<long?, Deck> deckTree, IList<long> result)
        {
            result.Add(deckId);

            var children = deckTree[deckId];
            
            if (!children.Any())
                return;
            
            foreach (var deck in children)
                GetChildrenIds(deck.Id, deckTree, result);
        }
        private async Task<IQueryable<CardEntity>> AllCardsInDeckQAsync(long deckId, AppDbContext? db = null)
        {
            db ??= GetDb;

            var allDecks = await
                db.Decks.ToListAsync();

            var deckTree = allDecks // cache this somewhere, its dumb to create a new one every time.
                .ToLookup(d => d.ParentDeckId);

            List<long> deckIds = [];
            GetChildrenIds(deckId, deckTree, deckIds);

            var query = db.Cards
                .AsNoTracking();

            return db.Cards
                .AsNoTracking()
                .Where(c => deckIds.Contains(c.DeckId));
        }
        private static CardsByStateQ GroupByStateQ(IQueryable<CardEntity> baseQuery)
        {
            return new CardsByStateQ()
            {
                Lessons = baseQuery
                    .Where(c => c.State == CardState.New),

                Learning = baseQuery
                    .Where(c => c.State == CardState.Learning),

                Reviews = baseQuery
                    .Where(c => c.State == CardState.Review),
            };
        }
        private async static Task<CardsCount> CountByStateAsync(CardsByStateQ grouped)
        {
            return new()
            {
                Lessons = await grouped.Lessons.CountAsync(),
                Learning = await grouped.Learning.CountAsync(),
                Review = await grouped.Reviews.CountAsync()
            };
        }
        private async static Task<CardsByState> SortAndLimit(long deckId, AppDbContext db, CardsByStateQ grouped)
        {
            var rootDeck = await db.Decks.FindAsync(deckId)
                ?? throw new ArgumentException(IdNotFoundMsg("Deck"), nameof(deckId));

            var sortOpt = rootDeck.Options.Sorting;
            var limitsOpt = rootDeck.Options.DailyLimits;

            var learning = await grouped.Learning
                .OrderBy(c => c.Due)
                .ToListAsync();

            var lessons = await grouped.Lessons
                .SortLessons(sortOpt)
                .Take(limitsOpt.DailyLessonsLimit)
                .ToListAsync();
            
            var reviews = await grouped.Reviews
                .SortReviews(sortOpt)
                .Take(limitsOpt.DailyReviewsLimit)
                .ToListAsync();

            lessons.ShuffleIf(sortOpt.LessonsOrder == LessonOrder.Random);
            reviews.ShuffleIf(sortOpt.ReviewsOrder == ReviewOrder.Random);

            return new()
            {
                Learning = learning.AsReadOnly(),
                Lessons = lessons.AsReadOnly(),
                Reviews = reviews.AsReadOnly()
            };
        }
        #endregion
    }
    public readonly struct CardsCount
    {
        public readonly long DeckId { get; init; }
        public readonly int Lessons { get; init; }
        public readonly int Learning { get; init; }
        public readonly int Review { get; init; }
    }
    internal readonly struct CardsByStateQ
    {
        public IQueryable<CardEntity> Lessons { get; init; }
        public IQueryable<CardEntity> Learning { get; init; }
        public IQueryable<CardEntity> Reviews { get; init; }
    }
    public readonly struct CardsByState
    {
        public readonly IReadOnlyList<CardEntity> Lessons { get; init; }
        public readonly IReadOnlyList<CardEntity> Learning { get; init; }
        public readonly IReadOnlyList<CardEntity> Reviews { get; init; }
    }
}