using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Services;

public class CountingService(IDbContextFactory<AppDbContext> factory): DbDependentClass(factory), ICountingService
{
    #region public methods
    public async Task<int> AllCards(long userId)
    {
        var db = GetDb;

        return await db.Cards
            .Where(c => c.Id == userId)
            .AsNoTracking()
            .CountAsync();
    }
    public async Task<int> AllDecks(long userId)
    {
        var db = GetDb;

        return await db.Decks
            .Where(d => d.UserId == userId)
            .AsNoTracking()
            .CountAsync();
    }
    public async Task<int> AllReviewableCards(long userId)
    {
        var db = GetDb;

        var deckIdsQ = db.Decks
            .Where(d => d.UserId == userId)
            .Select(d => d.Id);

        return await db.Cards
            .Where(c => deckIdsQ
            .Contains(c.Id))
            .CountAsync();
    }
    
    public async Task<IDictionary<long, CardsCount>> CardsByState(long userId, bool countOnlyStudyable)
    {
        var db = GetDb;

        var deckIds = await db.Decks
            .Where(d => d.UserId == userId)
            .Select(d => d.Id)
            .ToArrayAsync();

        return await CardsByState(
            deckIds,
            countOnlyStudyable
        );
    }
    public async Task<IDictionary<long, CardsCount>> CardsByState(IEnumerable<long> deckIds, bool countOnlyStudyable)
    {
        var db = GetDb;
        Dictionary<long, CardsCount> result = [];

        foreach(long id in deckIds)
        {
            var allCardsQuery = await AllCardsInDeckQAsync(id, db); // TODO: encapsulate query building methods to a new service.

            if (countOnlyStudyable)
                allCardsQuery = allCardsQuery
                    .Where(c => !c.IsSuspended && !c.IsBuried && c.IsDueNow);
                                                                            
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

    #region private helpers
    private async static Task<CardsCount> CountByStateAsync(CardsByStateQ grouped)
    {
        return new()
        {
            Lessons = await grouped.Lessons.CountAsync(),
            Learning = await grouped.Learning.CountAsync(),
            Reviews = await grouped.Reviews.CountAsync()
        };
    }
    private static CardsByStateQ GroupByStateQ(IQueryable<CardEntity> baseQuery)
    {
        return new CardsByStateQ() // TODO: encapsulate this too into query builder service.
        {
            Lessons = baseQuery
                .Where(c => c.State == CardState.New),

            Learning = baseQuery
                .Where(c => c.State == CardState.Learning),

            Reviews = baseQuery
                .Where(c => c.State == CardState.Review),
        };
    }
    #endregion
}