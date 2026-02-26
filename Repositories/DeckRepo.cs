using System.Collections;
using FlashMemo.Model.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Repositories;
    
public sealed class DeckRepo(IDbContextFactory<AppDbContext> dbFactory) : DbDependentClass(dbFactory), IDeckRepo
{
    public async Task<IDeckMeta?> GetFirstDeckMeta(long userId)
    {
        return await AllDecksQuery(GetDb, userId)
            .Cast<IDeckMeta>()
            .FirstOrDefaultAsync();
    }
    
    public async Task<IDeckMeta> GetDeckMetaById(long deckId)
    {
        return await GetDb.Decks
            .AsNoTracking()
            .Cast<IDeckMeta>()
            .SingleAsync(d => d.Id == deckId);
    }

    public async Task SaveEditedDeck(Deck updated)
    {
        var db = GetDb;

        var tracked = await db.Decks
            .SingleAsync(d => d.Id == updated.Id);
        
        db.Entry(tracked)
            .CurrentValues
            .SetValues(updated);

        await db.SaveChangesAsync();
    }
    public async Task AddNewDeck(Deck deck)
    {
        var db = GetDb;
        
        await db.Decks.AddAsync(deck);
        await db.SaveChangesAsync();
    }
    public async Task RemoveDeck(long deckId)
    {
        await GetDb.Decks
            .Where(d => d.Id == deckId)
            .ExecuteDeleteAsync();
    }
    public async Task<Deck> GetById(long deckId)
    {
        return await GetDb.Decks
            .AsNoTracking()
            .SingleAsync(d => d.Id == deckId);
    }
    public async Task<ILookup<long?, Deck>> ParentIdChildrenLookup(long userId, AppDbContext? db = null)
    {
        return (
            await AllDecksQuery(db ?? GetDb, userId)
            .ToArrayAsync())
            .ToLookup(d => d.ParentDeckId
        );
    }

    public async Task<IEnumerable<long>> GetChildrenIds(long deckId)
    {
        var db = GetDb;

        var userId = await db.Decks
            .Where(d => d.Id == deckId)
            .Select(d => d.UserId)
            .SingleAsync();

        var deckTree = await
            ParentIdChildrenLookup(userId, db);

        List<long> childrenIds = [];

        GetChildrenIds(deckId, deckTree, childrenIds);
        return childrenIds;
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

    private static IQueryable<Deck> AllDecksQuery(AppDbContext db, long userId)
    {
        return db.Decks
            .AsNoTracking()
            .Where(d => d.UserId == userId);
    }
}