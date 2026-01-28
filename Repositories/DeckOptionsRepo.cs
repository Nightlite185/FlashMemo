using FlashMemo.Model.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Repositories;

public class DeckOptionsRepo(IDbContextFactory<AppDbContext> dbFactory) 
    : DbDependentClass(dbFactory), IDeckOptionsRepo
{
    public async Task<IEnumerable<DeckOptions>> GetAllFromUser(long userId)
    {
        var db = GetDb;

        return await db.DeckOptions
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToArrayAsync();
    }

    public Task Remove(long presetId)
    {
        throw new NotImplementedException();
    }

    public Task AddNew(DeckOptions options)
    {
        throw new NotImplementedException();
    }

    public Task SaveEditedPreset(DeckOptions edited)
    {
        throw new NotImplementedException();
    }
}