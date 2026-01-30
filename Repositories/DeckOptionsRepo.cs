using FlashMemo.Model.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Repositories;

public class DeckOptionsRepo(IDbContextFactory<AppDbContext> dbFactory) 
    : DbDependentClass(dbFactory), IDeckOptionsRepo
{
    public async Task<IEnumerable<DeckOptionsEntity>> GetAllFromUser(long userId)
    {
        var db = GetDb;

        return await db.DeckOptions
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToArrayAsync();
    }

    public async Task Remove(long presetId)
    {
        // id -1 here means that its default preset
        if (presetId == -1) throw new InvalidOperationException(
            "Cannot delete default deck options preset.");

        var db = GetDb;

        await db.DeckOptions
            .Where(x => x.Id == presetId)
            .ExecuteDeleteAsync();

        await db.Decks
            .Where(d => d.OptionsId == presetId)
            .ExecuteUpdateAsync(async s =>
                s.SetProperty(d => d.OptionsId, -1));
    }

    public async Task AddNew(DeckOptionsEntity options)
    {
        throw new NotImplementedException();
    }

    public async Task SaveEditedPreset(DeckOptionsEntity edited)
    {
        throw new NotImplementedException();
    }
}