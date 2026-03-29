using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Services;

public class CountingService(IDbContextFactory<AppDbContext> factory, IDeckOptionsService deckOptService, 
                            IUserOptionsService userOptService): DbDependentClass(factory), ICountingService
{
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
        byte offset = await userOptService
            .GetDayStartOffset(userId);

        return await GetDb.Cards
            .Where(c => c.UserId == userId)
            .ForStudy(offset)
            .CountAsync();
    }
    public async Task<LessonReviewTake> CalculateTakings(
        long userId, CardsByStateQ cardsQ, DeckOptionsEntity deckOpt)
    {
        #region variables
        var stateOrder = deckOpt.Sorting.CardStateOrder;

        int reviewsCap = deckOpt.DailyLimits.Reviews;
        int lessonsCap = deckOpt.DailyLimits.Lessons;

        int reviewCount = await cardsQ.Reviews.CountAsync();
        int lessonCount = await cardsQ.Lessons.CountAsync();

        int cappedReviews = Math.Min(reviewCount, reviewsCap);
        int cappedLessons = Math.Min(lessonCount, lessonsCap);

        bool include = (await userOptService.GetFromUser(userId))
            .IncludeLessonsInReviewLimit;
        #endregion

        if (include)
        {
            if (stateOrder is CardStateOrder.ReviewsThenNew or CardStateOrder.Mix)
            {
                cappedLessons = Math.Min(
                    lessonsCap, Math.Max(
                        reviewsCap - cappedReviews, 0));
            }

            else if (stateOrder is CardStateOrder.NewThenReviews)
            {
                cappedReviews = Math.Max(
                    reviewsCap - cappedLessons, 0);
            }
        }

        return new(cappedLessons, cappedReviews);
    }
    public async Task<IDictionary<long, CardsCount>> StudyableCards(long userId)
    {
        var db = GetDb;

        byte offset = await userOptService
            .GetDayStartOffset(userId);

        var deckIdsToOpt = await deckOptService
            .MappedByDeckId(userId);

        Dictionary<long, CardsCount> result = [];

        foreach(var kvp in deckIdsToOpt)
        {
            var grouped = (await db
                .AllCardsInDeckQAsync(kvp.Key))
                .ForStudy(offset)
                .GroupByStateQ();

            var counted = await CountByStateAsync(
                grouped, userId, kvp.Value);

            if (!result.TryAdd(kvp.Key, counted))
                throw new InvalidOperationException(
                "Dictionary contains duplicate deck ids");
        }
        
        return result;
    }
    
    private async Task<CardsCount> CountByStateAsync(CardsByStateQ queries, long userId, DeckOptionsEntity deckOpt)
    {
        var takings = await CalculateTakings(
            userId, queries, deckOpt);

        return new()
        {
            Lessons = takings.Lessons,
            Reviews = takings.Reviews,
            Learning = await queries.Learning.CountAsync(),
        };
    }
}