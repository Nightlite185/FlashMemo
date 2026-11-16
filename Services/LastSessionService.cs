using FlashMemo.Model;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FlashMemo.Services;

public class LastSessionService(IDbContextFactory<AppDbContext> factory, ILogger<LastSessionService> logger): DbDependentClass(factory), ILastSessionService
{
    #region variables & properties

    // === private ===
    private AppSessionData appSession = null!;
    private UserSessionCache? userCache;

    // === public ===
    public Filters? LastFilters {
        get => GetUserCache().LastFilters;
        set => GetUserCache().LastFilters = value;
    }
    public long? LastUserId => appSession.LastLoadedUserId;
    public long? LastDeckId {
        get => GetUserCache().LastUsedDeckId;
        set => GetUserCache().LastUsedDeckId = value;
    }

    #endregion

    #region public methods
    public async Task LoadAppSessionAsync()
    {
        await using var db = GetDb;
        appSession = await db.AppSessionData.SingleAsync();
    }

    //* Loading only changes in-memory state; SaveStateAsync persists it later.
    //* this methods handles its own failures.
    public async Task LoadUserCacheAsync(long userId)
    {
        UserSessionCache? loadedCache = null;
        try
        {
            await using var db = GetDb;
            loadedCache = await db.UserSessionCaches
                .AsNoTracking()
                .SingleOrDefaultAsync(cache => cache.UserId == userId);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to load the session cache for user {UserId}; using defaults", userId);
        }

        this.userCache = loadedCache ?? new UserSessionCache
        {
            UserId = userId,
            LastFilters = Filters.GetEmpty(userId)
        };
    }

    public Task SetLastUserAsync(long userId) => SaveLastUserAsync(userId);

    public Task ClearLastUserAsync() => SaveLastUserAsync(null);

    public async Task SaveStateAsync()
    {
        var savingCache = userCache;
        await using var db = GetDb;

        db.AppSessionData.Update(appSession);

        if (savingCache is not null)
        {
            bool exists = await db.UserSessionCaches
                .AnyAsync(cache => cache.UserId == savingCache.UserId);

            if (exists)
                db.UserSessionCaches.Update(savingCache);

            else db.UserSessionCaches.Add(savingCache);
        }

        await db.SaveChangesAsync();
    }

    #endregion

    #region private methods

    private UserSessionCache GetUserCache()
        => userCache ?? throw new InvalidOperationException(
            "A user session cache must be loaded before it can be accessed.");

    private async Task SaveLastUserAsync(long? userId)
    {
        var saving = new AppSessionData
        {
            Id = appSession.Id,
            LastLoadedUserId = userId
        };

        await using var db = GetDb;
        
        db.AppSessionData.Update(saving);
        await db.SaveChangesAsync();

        appSession = saving;
    }

    #endregion
}