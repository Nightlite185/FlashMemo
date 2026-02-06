using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Services;

public class SessionDataService(IDbContextFactory<AppDbContext> factory): DbDependentClass(factory), ISessionDataService
{
    public LastSessionData Current { get; private set; } = new();

    public async Task LoadAsync()
        => Current = await GetDb.SessionData.SingleAsync();

    public async Task SaveStateAsync()
    {
        var db = GetDb;

        db.SessionData.Update(Current);
        await db.SaveChangesAsync();
    }
}