using FlashMemo.Model;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Services;

public class CardQueryService(IDbContextFactory<AppDbContext> factory, ICardQueryBuilder cqb)
    : DbDependentClass(factory), ICardQueryService
{
    private readonly ICardQueryBuilder queryBuilder = cqb;

    #region Public methods
    public async Task<IEnumerable<CardEntity>> GetCardsWhere(Filters filters, CardsOrder order, SortingDirection dir)
    {
        var db = GetDb;
        IQueryable<CardEntity> baseQuery = db.Cards;
        
        if (filters.IncludeChildrenDecks && filters.DeckId is not null)
        {
            baseQuery = await queryBuilder
                .AllCardsInDeckQAsync((long)filters.DeckId, db);
        }
            
        var filtersQuery = filters.ToExpression();

        var cards = await baseQuery
            .Where(filtersQuery)
            .SortAnyCards(order, dir)
            .Include(c => c.Deck)
            .ToListAsync();

        cards.ShuffleIf(order == CardsOrder.Random);

        return cards;
    }
    public async Task<(ICollection<CardEntity>, CardsCount)> GetForStudy(long deckId)
    {
        var db = GetDb;

        var baseQuery = await queryBuilder
            .AllCardsInDeckQAsync(deckId, db);

        var today = DateTime.Today;
        
        baseQuery = CardQueryBuilder
            .ForStudy(baseQuery);

        var grouped = CardQueryBuilder
            .GroupByStateQ(baseQuery);

        var rootDeck = await db.Decks
            .AsNoTracking()
            .Include(d => d.Options)
            .SingleAsync(d => d.Id == deckId);

        var sortOpt = rootDeck.Options.Sorting;

        var cards = await SortAndLimitAsync(
            rootDeck, grouped);
        
        var finalCollection = MergeByState(
            cards, sortOpt.CardStateOrder);

        return (finalCollection, (CardsCount)cards);
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

        var cardsQuery = await queryBuilder
            .AllCardsInDeckQAsync(deckId, db);

        return await cardsQuery
            .ToListAsync();
    }
    #endregion
    
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
    private static ICollection<CardEntity> MergeByState(CardsByState cards, CardStateOrder order)
    {
        return order switch
        {
            CardStateOrder.NewThenReviews
                => [..cards.Learning
                    .Concat(cards.Lessons)
                    .Concat(cards.Reviews)],

            CardStateOrder.ReviewsThenNew
                => [..cards.Learning
                    .Concat(cards.Reviews)
                    .Concat(cards.Lessons)],

            CardStateOrder.Mix
                => [..cards.Learning
                    .Concat(cards.Reviews
                        .Concat(cards.Lessons)
                        .Shuffle())],

            _ => throw new ArgumentException(
                $"Invalid {nameof(CardStateOrder)} enum value: {order}")
        };
    }
}
public readonly struct CardsCount
{
    public static explicit operator CardsCount(CardsByState cbs)
    {
        return new()
        {
            Lessons = cbs.Lessons.Count,
            Learning = cbs.Learning.Count,
            Reviews = cbs.Reviews.Count
        };
    }

    public readonly int Lessons { get; init; }
    public readonly int Learning { get; init; }
    public readonly int Reviews { get; init; }
}
public readonly struct CardsByStateQ
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