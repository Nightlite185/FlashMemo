using FlashMemo.Model;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Services;

public class LastSessionService(IDbContextFactory<AppDbContext> factory): DbDependentClass(factory), ILastSessionService
{
    private AppSessionData appSession = null!;
    private UserSessionCache? userCache;

    public Filters? LastFilters {
        get => GetUserCache().LastFilters;
        set => GetUserCache().LastFilters = value;
    }
    public long? LastUserId => appSession.LastLoadedUserId;
    public long? LastDeckId {
        get => GetUserCache().LastUsedDeckId;
        set => GetUserCache().LastUsedDeckId = value;
    }

    public async Task LoadAppSessionAsync()
        => appSession = await GetDb.AppSessionData.SingleAsync();

    public async Task LoadUserCacheAsync(long userId)
    {
        var db = GetDb;

        userCache = await db.UserSessionCaches
            .SingleOrDefaultAsync(cache => cache.UserId == userId);

        if (userCache is not null)
            return;

        userCache = new UserSessionCache
        {
            UserId = userId,
            LastFilters = Filters.GetEmpty(userId)
        };

        await db.UserSessionCaches.AddAsync(userCache);
        await db.SaveChangesAsync();
    }

    public async Task SetLastUserAsync(long userId)
    {
        appSession.LastLoadedUserId = userId;
        await SaveAppSessionAsync();
    }

    public async Task ClearLastUserAsync()
    {
        appSession.LastLoadedUserId = null;
        await SaveAppSessionAsync();
    }

    public async Task SaveStateAsync()
    {
        var db = GetDb;

        db.AppSessionData.Update(appSession);

        if (userCache is not null)
            db.UserSessionCaches.Update(userCache);

        await db.SaveChangesAsync();
    }

    private UserSessionCache GetUserCache()
        => userCache ?? throw new InvalidOperationException(
            "A user session cache must be loaded before it can be accessed.");

    private async Task SaveAppSessionAsync()
    {
        var db = GetDb;
        db.AppSessionData.Update(appSession);
        await db.SaveChangesAsync();
    }
}
