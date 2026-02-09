using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Services;

public class LastSessionService(IDbContextFactory<AppDbContext> factory): DbDependentClass(factory), ILastSessionService
{
    public LastSessionData Current { get; private set; } = new();

    public async Task LoadAsync()
        => Current = await GetDb.LastSessionData.SingleAsync();

    public async Task SaveStateAsync()
    {
        var db = GetDb;

        db.LastSessionData.Update(Current);
        await db.SaveChangesAsync();
    }
}