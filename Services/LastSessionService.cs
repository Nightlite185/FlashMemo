using FlashMemo.Model;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Services;

public class LastSessionService(IDbContextFactory<AppDbContext> factory): DbDependentClass(factory), ILastSessionService
{
    private LastSessionData data = new();

    public Filters? LastFilters {
        get => data.LastFilters;
        set => data.LastFilters = value;
    }
    public long? LastUserId { 
        get => data.LastLoadedUserId; 
        set => data.LastLoadedUserId = value; 
    }
    public long? LastDeckId { 
        get => data.LastUsedDeckId; 
        set => data.LastUsedDeckId = value; 
    }

    public async Task LoadAsync()
        => data = await GetDb.LastSessionData.SingleAsync();

    public async Task SaveStateAsync()
    {
        var db = GetDb;

        db.LastSessionData.Update(data);
        await db.SaveChangesAsync();
    }
}