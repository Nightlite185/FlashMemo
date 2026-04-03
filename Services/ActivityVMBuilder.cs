using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.ViewModel.Wrappers;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Services;

public class ActivityVMBuilder(IDbContextFactory<AppDbContext> factory)
    : DbDependentClass(factory), IActivityVMBuilder
{
    public async Task<ICollection<ActivityWeekVM>> BuildWeeks(long userId)
    {
        return (await BuildCells(GetDb, userId))
            .GroupBy(c => StartOfWeekMonday(c.Date))
            .OrderBy(g => g.Key)
            .Select(g => new ActivityWeekVM(
                g.OrderBy(c => c.Date)))
            .ToArray();
    }

    private static DateOnly StartOfWeekMonday(DateOnly date)
    {
        int daysSinceMonday =
            ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;

        return date.AddDays(-daysSinceMonday);
    }

    private async static Task<ICollection<ActivityCellVM>> BuildCells(AppDbContext db, long userId)
    {
        var today = DateTime.Today.ToDateOnly();
        var janFirst = new DateTime(today.Year, 1, 1).ToDateOnly();

        var countByDate = await db.CardLogs
            .AsNoTracking()
            .Where(l => l.UserId == userId
                && l.Action == CardAction.Review)
            .GroupBy(l => DateOnly.FromDateTime(l.TimeStamp))
            .ToDictionaryAsync(
                g => g.Key, 
                g => g.Count());
        
        List<ActivityCellVM> cells = [];

        for (DateOnly date = janFirst; date <= today; date = date.AddDays(1))
        {
            int count = countByDate.GetValueOrDefault(date, 0);

            cells.Add(new()
            {
                Date = date,
                ReviewCount = count
            });
        }

        return cells;
    }
}