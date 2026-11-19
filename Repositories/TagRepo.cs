using FlashMemo.Model.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace FlashMemo.Repositories;

public class TagRepo(IDbContextFactory<AppDbContext> factory): DbDependentClass(factory), ITagRepo
{
    private const int SqliteUniqueConstraint = 2067;

    public async Task<ICollection<Tag>> GetFromUser(long userId)
    {
        await using var db = GetDb;
        return await db.Tags
            .Where(t => t.UserId == userId)
            .AsNoTracking()
            .ToArrayAsync();
    }
    public async Task<ICollection<Tag>> GetFromCard(long cardId)
    {
        await using var db = GetDb;
        return await db.Cards
            .AsNoTracking()
            .Include(c => c.Tags)
            .Where(c => c.Id == cardId)
            .Select(c => c.Tags)
            .SingleAsync();
    }
    public async Task<Tag> GetOrCreate(Tag newTag)
    {
        await using var db = GetDb;

        // Query db's own name collation, rather than trusting a window's cached suggestions.
        Task<Tag?> findExisting() => db.Tags.AsNoTracking()
            .SingleOrDefaultAsync(t => t.UserId == newTag.UserId && t.Name == newTag.Name);

        if (await findExisting() is Tag existing)
            return existing;

        await db.Tags.AddAsync(newTag);

        try
        {
            await db.SaveChangesAsync();
            return newTag;
        }

        catch (DbUpdateException e) when (e.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteUniqueConstraint })
        {
            // Another caller may have created this same user/name after our lookup.
            // Other persistence failures still propagate to the caller's SafetyNet.
            if (await findExisting() is Tag concurrent)
                return concurrent;

            throw;
        }
    }
    public async Task Remove(long tagId)
    {
        await using var db = GetDb;
        await db.Tags
            .Where(t => t.Id == tagId)
            .ExecuteDeleteAsync();
    }
    public async Task SaveEdited(Tag updated)
    {
        await using var db = GetDb;

        var tracked = await db.Tags
            .SingleAsync(t => t.Id == updated.Id);

        db.Entry(tracked)
            .CurrentValues
            .SetValues(updated);

        await db.SaveChangesAsync();
    }
    public async Task<Tag> GetById(long tagId)
    {
        await using var db = GetDb;
        return await db.Tags
            .AsNoTracking()
            .SingleAsync(t => t.Id == tagId);
    }
}