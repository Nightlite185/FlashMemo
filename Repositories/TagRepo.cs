using FlashMemo.Model.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Repositories
{
    public class TagRepo(IDbContextFactory<AppDbContext> factory): DbDependentClass(factory)
    {
        public async Task<IEnumerable<Tag>> GetAllFromUserAsync(long userId)
        {
            var db = GetDb;

            return await db.Tags
                .Where(t => t.UserId == userId)
                .AsNoTracking()
                .ToListAsync();
        }
        public async Task AddNewAsync(params IEnumerable<Tag> tags)
        {
            var db = GetDb;

            await db.Tags.AddRangeAsync(tags);
            await db.SaveChangesAsync();
        }
        public async Task RemoveAsync(params IEnumerable<Tag> tags)
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
    }
}