using System.Data;
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
        var cells = await BuildCells(GetDb, userId);

        // shifting day of every log's date to nearest monday
        // and grouping logs by that property expression.
        // +1 because .NET default first weekday is sunday.

        return cells
            .GroupBy(c => (int)c.Date.DayOfWeek + 1) // TODO: its probably wrong, fix this
            .Select(g => new ActivityWeekVM(g))
            .ToArray();
    }

    private async static Task<ICollection<ActivityCellVM>> BuildCells(AppDbContext db, long userId)
    {
        // TODO: include empty days that dont have any logged activity.

        var logs = await db.CardLogs
            .AsNoTracking()
            .Where(l => l.UserId == userId
                && l.Action == CardAction.Review)
            .ToArrayAsync();

        var logsByDate = logs.GroupBy(l =>
                DateOnly.FromDateTime(l.TimeStamp.Date))
            .ToArray();

        List<ActivityCellVM> cells = [];

        foreach(var group in logsByDate)
        {
            cells.Add(new ActivityCellVM
            {
                Date = group.Key,
                ReviewCount = group.Count()
            });
        }

        return [..cells.OrderBy(c => c.Date)];
    }
}