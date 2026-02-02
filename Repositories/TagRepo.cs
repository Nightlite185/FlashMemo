using FlashMemo.Model.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Repositories;

public class TagRepo(IDbContextFactory<AppDbContext> factory): DbDependentClass(factory), ITagRepo
{
    public async Task<IEnumerable<Tag>> GetFromUser(long userId)
    {
        var db = GetDb;

        return await db.Tags
            .Where(t => t.UserId == userId)
            .AsNoTracking()
            .ToListAsync();
    }
    public async Task<IEnumerable<Tag>> GetFromCard(long cardId)
    {
        var db = GetDb;
        
        // TODO: this can be improved with a separate many<->many separate cardTags table
        var card = await db.Cards
            .AsNoTracking()
            .Include(c => c.Tags)
            .SingleAsync(c => c.Id == cardId);
            
        return card.Tags;
    }
    public async Task AddNew(params IEnumerable<Tag> tags)
    {
        var db = GetDb;

        await db.Tags.AddRangeAsync(tags);
        await db.SaveChangesAsync();
    }
    public async Task Remove(params IEnumerable<Tag> tags)
    {
        var db = GetDb;

        db.Tags.RemoveRange(tags);
        await db.SaveChangesAsync();
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
    public async Task<IEnumerable<Tag>> GetByIds(IEnumerable<long> tagIds)
    {
        var db = GetDb;

        return await db.Tags
            .Where(t => tagIds.Contains(t.Id))
            .AsNoTracking()
            .ToArrayAsync();
    }
}