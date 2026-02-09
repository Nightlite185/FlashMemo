using FlashMemo.Model.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Repositories;
    
public sealed class DeckRepo(IDbContextFactory<AppDbContext> dbFactory) : DbDependentClass(dbFactory), IDeckRepo
{
    public async Task<IDeckMeta> GetFirstDeckMeta(long userId)
    {
        var db = GetDb;

        return await db.Decks
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .Cast<IDeckMeta>()
            .FirstAsync();
    }
    
    public async Task<IDeckMeta> GetDeckMetaById (long deckId)
    {
        var db = GetDb;

        return await db.Decks
            .AsNoTracking()
            .Cast<IDeckMeta>()
            .SingleAsync(d => d.Id == deckId);
    }

    public async Task SaveEditedDeckAsync(Deck updated)
    {
        var db = GetDb;

        var tracked = await db.Decks
            .SingleAsync(d => d.Id == updated.Id);
        
        db.Entry(tracked)
            .CurrentValues
            .SetValues(updated);

        await db.SaveChangesAsync();
    }
    public async Task AddNewDeckAsync(Deck deck)
    {
        var db = GetDb;
        
        await db.Decks.AddAsync(deck);
        await db.SaveChangesAsync();
    }
    public async Task RemoveDeckAsync(long deckId)
    {
        var db = GetDb;

        await db.Decks
            .Where(d => d.Id == deckId)
            .ExecuteDeleteAsync();
    }
    public async Task<Deck> LoadDeckAsync(long deckId)
    {
        var db = GetDb;

        return await db.Decks
            .AsNoTracking()
            .SingleAsync(d => d.Id == deckId);
    }
    public async Task<ILookup<long?, Deck>> BuildDeckLookupAsync(long userId, AppDbContext? db = null)
    {
        db ??= GetDb;

        var decks = await db.Decks
            .Where(d => d.UserId == userId)
            .AsNoTracking()
            .ToListAsync();

        return decks.ToLookup(
            d => d.ParentDeckId);
    }
    
    // if any duplicates -> change IList to hashset. 99% sure there won't be any tho
    private static void GetChildrenIds(long deckId, ILookup<long?, Deck> deckTree, IList<long> result)
    {
        result.Add(deckId);

        var children = deckTree[deckId];
        
        if (!children.Any())
            return;
        
        foreach (var deck in children)
            GetChildrenIds(deck.Id, deckTree, result);
    }
    public async Task<IEnumerable<long>> GetChildrenIds(long deckId)
    {
        var db = GetDb;

        var userId = await db.Decks
            .Where(d => d.Id == deckId)
            .Select(d => d.UserId)
            .SingleAsync();

        var deckTree = await
            BuildDeckLookupAsync(userId, db);

        List<long> childrenIds = [];

        GetChildrenIds(deckId, deckTree, childrenIds);
        return childrenIds;
    }
}