using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Services;

public class UserOptionsService(IDbContextFactory<AppDbContext> factory): DbDependentClass(factory), IUserOptionsService
{
    public async Task Update(long userId, UserOptions updated)
    {
        if (updated.DayStartTime > new TimeOnly(12, 0))
            throw new InvalidOperationException(
            "Can't start the day after 12pm, come on!! Get up a bit earlier would you?");

        var db = GetDb;

        var user = await db.Users
            .SingleAsync(u => u.Id == userId);

        user.Options = updated;

        await db.SaveChangesAsync();
    }

    public async Task<UserOptions> GetFromUser(long userId)
        => await GetDb.Users
            .Where(u => u.Id == userId)
            .Select(u => u.Options)
            .SingleAsync();
}