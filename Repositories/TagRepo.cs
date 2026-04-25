using FlashMemo.Model.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Repositories;

public class TagRepo(IDbContextFactory<AppDbContext> factory): DbDependentClass(factory), ITagRepo
{
    public async Task<ICollection<Tag>> GetFromUser(long userId)
    {
        return await GetDb.Tags
            .Where(t => t.UserId == userId)
            .AsNoTracking()
            .ToArrayAsync();
    }
    public async Task<ICollection<Tag>> GetFromCard(long cardId)
    {
        return await GetDb.Cards
            .AsNoTracking()
            .Include(c => c.Tags)
            .Where(c => c.Id == cardId)
            .Select(c => c.Tags)
            .SingleAsync();
    }
    public async Task CreateNew(Tag newTag)
    {
        var db = GetDb;

        await db.Tags.AddAsync(newTag);
        await db.SaveChangesAsync();
    }
    public async Task Remove(long tagId)
    {
        await GetDb.Tags
            .Where(t => t.Id == tagId)
            .ExecuteDeleteAsync();
    }
    public async Task SaveEdited(Tag updated)
    {
        var db = GetDb;

        var tracked = await db.Tags
            .SingleAsync(t => t.Id == updated.Id);
        
        db.Entry(tracked)
            .CurrentValues
            .SetValues(updated);
    
        await db.SaveChangesAsync();
    }
    public async Task<Tag> GetById(long tagId)
    {
        return await GetDb.Tags
            .AsNoTracking()
            .SingleAsync(t => t.Id == tagId);
    }
}