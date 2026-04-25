using System.ComponentModel;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.ViewModel.Other;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Services;

public class StatsQueryService(IDbContextFactory<AppDbContext> factory)
                : DbDependentClass(factory), IStatsQueryService
{
    public async Task<int> GetAnswerRatio(Answers answer, TimePeriod lastPeriod, long userId)
    {
        var today = DateTime.Today;

        // inclusive oldest date where logs still apply.
        var oldestDate = lastPeriod switch
        {
            TimePeriod.Day => today,
            TimePeriod.Week => today.AddDays(-7),
            TimePeriod.Month => today.AddMonths(-1),
            TimePeriod.Year => today.AddYears(-1),

            _ => throw new InvalidEnumArgumentException()
        };

        var baseQuery = GetBaseQuery(
            GetDb, userId, oldestDate);

        int allAnsCount = await baseQuery.CountAsync();

        int chosenAnsCount = await baseQuery
            .Where(l => l.Answer == answer)
            .CountAsync();

        if (chosenAnsCount <= 0 || allAnsCount <= 0)
            return 0;

        return (int)Math.Round((double)chosenAnsCount / allAnsCount * 100);
    }
    public async Task<DayOfWeek> DayWithMostReviewsInLastMonth(long userId)
    {
        return await GetBaseQuery(GetDb, userId, MonthAgo)
            .GroupBy(l => l.TimeStamp.DayOfWeek)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefaultAsync();
    }
    public async Task<TimeSpan> AvgAnswerTimeInLastMonth(long userId)
    {
        var query = GetBaseQuery(GetDb, userId, MonthAgo)
            .Where(l => l.AnswerTimeSeconds.HasValue)
            .Select(l => l.AnswerTimeSeconds);

        if (!await query.AnyAsync())
            return TimeSpan.Zero;

        double secondsAVG = await query
            .AverageAsync() ?? 0;

        return TimeSpan.FromSeconds(secondsAVG);
    }
    public async Task<int> MostReviewedHourOfDayInLastMonth(long userId)
    {
        return await GetBaseQuery(GetDb, userId, MonthAgo)
            .GroupBy(l => l.TimeStamp.Hour)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefaultAsync();
    }
    public async Task<int> TotalReviewsEver(long userId)
    {
        return await GetBaseQuery(GetDb, userId)
            .CountAsync();
    }
    public async Task<int> GetLongestStreak(long userId)
    {
        var reviewDays = await GetBaseQuery(GetDb, userId)
            .Select(l => l.TimeStamp.Date)
            .Distinct().Order()
            .ToArrayAsync();

        if (reviewDays.Length is 0 or 1)
            return reviewDays.Length;

        var previous = reviewDays[0];
        int count = 1, longestCount = 1;

        foreach(var day in reviewDays.Skip(1))
        {
            if (day.Equals(previous.AddDays(1)))
                count++;

            else
            {
                longestCount = count;
                count = 1;
            }

            previous = day;
        }

        return Math.Max(count, longestCount);
    }
    public async Task<int> GetCurrentStreak(long userId)
    {
        var reviewDays = await GetBaseQuery(GetDb, userId)
            .Select(l => l.TimeStamp.Date)
            .Distinct().OrderDescending()
            .ToArrayAsync();

        if (reviewDays.Length == 0) 
            return 0;

        DateTime today = DateTime.Today,
        yesterday = today.AddDays(-1),
        lastReview = reviewDays[0];

        if (lastReview != today
        &&  lastReview != yesterday)
            return 0;

        var latter = lastReview;
        int count = 1;

        foreach(var day in reviewDays.Skip(1))
        {
            if (day.Equals(latter.AddDays(-1)))
                count++;

            else break; 
            latter = day;
        }

        return count;
    }

    private static IQueryable<CardLog> GetBaseQuery(AppDbContext db, long userId, DateTime? oldest = null)
    {
        var q = db.CardLogs.Where(
            l => l.Action == CardAction.Review
            && l.UserId == userId)
        .AsNoTracking();

        return (oldest is not null)
            ? q.Where(l => l.TimeStamp >= oldest)
            : q;
    }
    private static DateTime MonthAgo => DateTime.Today.AddMonths(-1);
}