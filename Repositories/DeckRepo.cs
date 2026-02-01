using FlashMemo.Model.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Repositories;
    
public sealed class DeckRepo(IDbContextFactory<AppDbContext> dbFactory) : DbDependentClass(dbFactory), IDeckRepo
{
    ///<summary>ONLY UPDATES SCALARS, does not touch navs.</summary>
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
    public async Task DeleteDeckAsync(long deckId)
    {
        var db = GetDb;

        var deck = await db.Decks.FindAsync(deckId)
            ?? throw new ArgumentException("deck with this id was not found in database.", nameof(deckId));

        db.Decks.Remove(deck);
        await db.SaveChangesAsync();
    }
    public async Task<Deck> LoadDeckAsync(long id)
    {
        var db = GetDb;

        return await db.Decks
            .AsNoTracking()
            .SingleAsync(d => d.Id == id);
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