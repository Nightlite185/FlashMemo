using FlashMemo.Model;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Services
{
    public class CardQueryService(IDbContextFactory<AppDbContext> factory, DeckRepo dr): DbDependentClass(factory)
    {
        private readonly DeckRepo deckRepo = dr;

        #region Public methods
        public async Task<IEnumerable<CardEntity>> GetCardsWhere(Filters filters, CardsOrder order, SortingDirection dir)
        {
            var db = GetDb;
            IQueryable<CardEntity> baseQuery = db.Cards;
            
            if (filters.IncludeChildrenDecks && filters.DeckId is not null)
                baseQuery = await AllCardsInDeckQAsync((long)filters.DeckId, db);
                
            var filtersQuery = filters.ToExpression();

            var cards = await baseQuery
                .Where(filtersQuery)
                .SortAnyCards(order, dir)
                .Include(c => c.Deck)
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
                && !c.IsBuried
                && c.IsDueNow);

            var grouped = GroupByStateQ(baseQuery);

            var rootDeck = await db.Decks.FindAsync(deckId)
                ?? throw new ArgumentException(IdNotFoundMsg("Deck"), nameof(deckId));

            var sortOpt = rootDeck.Options.Sorting;

            var cards = await SortAndLimitAsync(rootDeck, grouped);
            

            return sortOpt.CardStateOrder switch
            {
                CardStateOrder.NewThenReviews
                    => cards.Learning
                        .Concat(cards.Lessons)
                        .Concat(cards.Reviews),

                CardStateOrder.ReviewsThenNew
                    => cards.Learning
                        .Concat(cards.Reviews)
                        .Concat(cards.Lessons),

                CardStateOrder.Mix
                    => cards.Learning.Concat(
                            cards.Reviews.Concat(cards.Lessons).Shuffle()),

                _ => throw new ArgumentException(
                    $"Invalid {nameof(CardStateOrder)} enum value: {sortOpt.CardStateOrder}")
            };
        }
        public async Task<IList<CardEntity>> GetAllFromUser(long userId)
        {
            var db = GetDb;

            var deckIds = db.Decks
                .Where(d => d.UserId == userId)
                .Select(d => d.Id);

            return await db.Cards
                .Where(c => deckIds.Contains(c.DeckId))
                .Include(c => c.Deck)
                .ToListAsync();
        }
        public async Task<IList<CardEntity>> GetAllFromDeck(long deckId)
        {
            var db = GetDb;

            var cardsQuery = await 
                AllCardsInDeckQAsync(deckId, db);

            return await cardsQuery.ToListAsync();
        }
        
        ///<returns>an IDictionary, of which keys are deck ids, and values are corresponding CardsCount structs;
        ///containing count of cards grouped by their state.</returns>
        public async Task<IDictionary<long, CardsCount>> CountCardsAsync(IEnumerable<long> deckIds, bool countOnlyStudyable)
        {
            var db = GetDb;
            Dictionary<long, CardsCount> result = [];

            foreach(long id in deckIds)
            {
                var allCardsQuery = await AllCardsInDeckQAsync(id, db);

                if (countOnlyStudyable)
                    allCardsQuery = allCardsQuery
                        .Where(c => !c.IsSuspended && !c.IsBuried && c.IsDueNow); // IMPORTANT PLACE deciding if decks are loaded
                                                                                  // based on due NOW IN THIS SECOND or due TODAY
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
        public async Task<IDictionary<long, CardsCount>> CountCardsAsync(long userId, bool countOnlyStudyable)
        {
            var db = GetDb;

            var deckIds = await db.Decks
                .Where(d => d.UserId == userId)
                .Select(d => d.Id)
                .ToArrayAsync();

            return await CountCardsAsync(
                deckIds,
                countOnlyStudyable
            );
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

            long userId = await db.Decks
                .Where(d => d.Id == deckId)
                .Select(d => d.UserId)
                .SingleAsync();

            var deckTree = await deckRepo
                .BuildDeckLookupAsync(userId, db);

            List<long> deckIds = [];
            GetChildrenIds(deckId, deckTree, deckIds);

            return db.Cards
                .AsNoTracking()
                .Where(c => 
                    deckIds.Contains(c.DeckId));
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
                Reviews = await grouped.Reviews.CountAsync()
            };
        }
        private async static Task<CardsByState> SortAndLimitAsync(Deck rootDeck, CardsByStateQ grouped)
        {
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
        public readonly int Lessons { get; init; }
        public readonly int Learning { get; init; }
        public readonly int Reviews { get; init; }
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