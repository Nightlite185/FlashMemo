using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Services;

public class CountingService(IDbContextFactory<AppDbContext> factory)
: DbDependentClass(factory), ICountingService
{
    #region public methods
    public async Task<int> AllCards(long userId)
    {
        return await GetDb.Cards
            .Where(c => c.UserId == userId)
            .AsNoTracking()
            .CountAsync();
    }
    public async Task<int> AllDecks(long userId)
    {
        return await GetDb.Decks
            .Where(d => d.UserId == userId)
            .AsNoTracking()
            .CountAsync();
    }
    public async Task<int> AllReviewableCards(long userId)
    {
        return await GetDb.Cards
            .Where(c => c.UserId == userId)
            .ForStudy()
            .CountAsync();
    }
    
    public async Task<IDictionary<long, CardsCount>> CardsByState(long userId, bool onlyForStudy)
    {
        var db = GetDb;

        var deckIds = await db.Decks
            .Where(d => d.UserId == userId)
            .Select(d => d.Id)
            .ToArrayAsync();

        return await CardsByState(
            deckIds,
            onlyForStudy
        );
    }
    public async Task<IDictionary<long, CardsCount>> CardsByState(IEnumerable<long> deckIds, bool onlyForStudy)
    {
        var db = GetDb;
        Dictionary<long, CardsCount> result = [];

        foreach(long id in deckIds)
        {
            var allCardsQuery = await db
                .AllCardsInDeckQAsync(id);

            if (onlyForStudy)
            {
                allCardsQuery = allCardsQuery
                    .ForStudy();
            }
                                                                            
            var grouped = allCardsQuery.GroupByStateQ();
            var counted = await CountByStateAsync(grouped);

            if (!result.TryAdd(id, counted))
                throw new ArgumentException(
                    "provided IEnumerable contains duplicate deck ids", 
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
    #endregion
}