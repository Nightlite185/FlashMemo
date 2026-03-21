using System.ComponentModel;
using FlashMemo.Model;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Services;

public class CardQueryService(IDbContextFactory<AppDbContext> factory, IUserOptionsService userOptService)
    : DbDependentClass(factory), ICardQueryService
{
    #region Public methods
    public async Task<IEnumerable<CardEntity>> GetCardsWhere(Filters filters, CardsOrder order, SortingDirection dir)
    {
        var db = GetDb;
        IQueryable<CardEntity> baseQuery = db.Cards;
        
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

        var groupedQueries = (await db
            .AllCardsInDeckQAsync(deckId))
            .ForStudy()
            .GroupByStateQ();

        var rootDeck = await db.Decks
            .AsNoTracking()
            .Include(d => d.Options)
            .SingleAsync(d => d.Id == deckId);

        var sortOpt = rootDeck.Options.Sorting;
        ApplySortQuery(groupedQueries, sortOpt);

        var takings = await ApplyLimitsQuery(
            rootDeck.UserId, groupedQueries, 
            rootDeck.Options);

        var materialized = await MaterializeQuery(groupedQueries, takings);

        ShuffleIfRandom(materialized, rootDeck.Options.Sorting);

        var finalCollection = MergeByState(
            materialized, sortOpt);

        return (finalCollection, (CardsCount)materialized);
    }
    public async Task<IList<CardEntity>> GetAllFromUser(long userId)
    {
        return await GetDb.Cards
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .Include(c => c.Deck)
            .ToListAsync();
    }
    public async Task<IList<CardEntity>> GetAllFromDeck(long deckId)
    {
        var db = GetDb;

        var cardsQuery = await db
            .AllCardsInDeckQAsync(deckId);

        return await cardsQuery
            .AsNoTracking()
            .ToListAsync();
    }
    #endregion
    
    #region private methods
    private static void ApplySortQuery(CardsByStateQ cardsQ, DeckOptionsEntity.SortingOpt opt)
    {
        cardsQ.Learning = cardsQ.Learning
            .OrderBy(c => c.Due);

        cardsQ.Lessons = cardsQ.Lessons
            .SortLessons(opt);
        
        cardsQ.Reviews = cardsQ.Reviews
            .SortReviews(opt);
    }
    private static ICollection<CardEntity> MergeByState(CardsByState cards, DeckOptionsEntity.SortingOpt sortOpt)
    {
        return sortOpt.CardStateOrder switch
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

            _ => throw new InvalidEnumArgumentException(
                nameof(sortOpt.CardStateOrder), (int)sortOpt.CardStateOrder, typeof(CardStateOrder))
        };
    }
    private async Task<LessonReviewTake> ApplyLimitsQuery(long userId, CardsByStateQ cardsQ, DeckOptionsEntity deckOpt)
    {
        #region variables
        var stateOrder = deckOpt.Sorting.CardStateOrder;

        int reviewsCap = deckOpt.DailyLimits.Reviews;
        int lessonsCap = deckOpt.DailyLimits.Lessons;

        int reviewsTake = reviewsCap;
        int lessonsTake = lessonsCap;

        bool include = (await userOptService.GetFromUser(userId))
            .IncludeLessonsInReviewLimit;
        #endregion

        if (include)
        {
            if (stateOrder is CardStateOrder.ReviewsThenNew or CardStateOrder.Mix)
            {
                int reviewCount = await cardsQ
                    .Reviews.CountAsync();

                int cappedReviews = Math.Min(reviewCount, reviewsTake);

                lessonsTake = Math.Min(
                    lessonsCap, Math.Max(
                        reviewsTake - cappedReviews, 0));
            }

            else if (stateOrder is CardStateOrder.NewThenReviews)
            {
                int lessonCount = await cardsQ
                    .Lessons.CountAsync();

                int cappedLessons = Math.Min(lessonCount, lessonsTake);

                reviewsTake = Math.Max(
                    reviewsCap - cappedLessons, 0);
            }
        }

        cardsQ.Reviews = cardsQ.Reviews
            .Take(reviewsTake);

        cardsQ.Lessons = cardsQ.Lessons
            .Take(lessonsTake);

        return new(lessonsTake, reviewsTake);
    }
    private static void ShuffleIfRandom(CardsByState cards, DeckOptionsEntity.SortingOpt sortOpt)
    {
        cards.Lessons.ShuffleIf(
            sortOpt.LessonsOrder == LessonOrder.Random);

        cards.Reviews.ShuffleIf(
            sortOpt.ReviewsOrder == ReviewOrder.Random);
    }
    private static async Task<CardsByState> MaterializeQuery(CardsByStateQ query, LessonReviewTake takings)
    {
        return new()
        {
            Learning = await query.Learning
                .AsNoTracking()
                .ToListAsync(),

            Lessons = (takings.LessonsTake != 0) 
                ? await query.Lessons
                    .AsNoTracking()
                    .ToListAsync()
                : [],

            Reviews = (takings.ReviewsTake != 0) 
                ? await query.Reviews
                    .AsNoTracking()
                    .ToListAsync()
                : []
        };
    }
    #endregion
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
public class CardsByStateQ
{
    public required IQueryable<CardEntity> Lessons { get; set; }
    public required IQueryable<CardEntity> Learning { get; set; }
    public required IQueryable<CardEntity> Reviews { get; set; }
}
public readonly struct CardsByState
{
    public readonly List<CardEntity> Lessons { get; init; }
    public readonly List<CardEntity> Learning { get; init; }
    public readonly List<CardEntity> Reviews { get; init; }
}

internal record struct LessonReviewTake(int LessonsTake, int ReviewsTake);