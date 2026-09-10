using FlashMemo.Model.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Repositories;

public class TagRepo(IDbContextFactory<AppDbContext> factory): DbDependentClass(factory), ITagRepo
{
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
    public async Task<Tag> CreateNew(Tag newTag)
    {
        await using var db = GetDb;

        await db.Tags.AddAsync(newTag);
        await db.SaveChangesAsync();
        return newTag;
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
