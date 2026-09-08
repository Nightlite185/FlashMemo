using FlashMemo.Model.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Repositories;
    
public sealed class DeckRepo(IDbContextFactory<AppDbContext> dbFactory) : DbDependentClass(dbFactory), IDeckRepo
{
    public async Task<Deck?> GetFirst(long userId)
    {
        await using var db = GetDb;
        return await AllDecksQuery(db, userId)
            .FirstOrDefaultAsync();
    }
    
    public async Task<IDeckMeta> GetDeckMetaById(long deckId)
    {
        await using var db = GetDb;
        return await db.Decks
            .AsNoTracking()
            .Cast<IDeckMeta>()
            .SingleAsync(d => d.Id == deckId);
    }

    public async Task RenameDeck(long id, string name)
    {
        await using var db = GetDb;
        await db.Decks
            .Where(d => d.Id == id)
            .ExecuteUpdateAsync(opt => 
                opt.SetProperty(d => d.Name, name));
    }

    public async Task SaveEditedDeck(Deck updated)
    {
        await using var db = GetDb;

        var tracked = await db.Decks
            .SingleAsync(d => d.Id == updated.Id);
        
        db.Entry(tracked)
            .CurrentValues
            .SetValues(updated);

        await db.SaveChangesAsync();
    }
    public async Task AddNewDeck(Deck deck)
    {
        await using var db = GetDb;
        
        await db.Decks.AddAsync(deck);
        await db.SaveChangesAsync();
    }
    public async Task<IReadOnlySet<long>> RemoveDeck(long deckId)
    {
        await using var db = GetDb;
        var removedDeckIds = (await GetChildrenIds(deckId, db))
            .ToHashSet();

        await using var transaction = await db.Database
            .BeginTransactionAsync();

        await db.UserSessionCaches
            .Where(cache => cache.LastUsedDeckId.HasValue
                && removedDeckIds.Contains(cache.LastUsedDeckId.Value))
            .ExecuteUpdateAsync(update => update
                .SetProperty(cache => cache.LastUsedDeckId, (long?)null));

        await db.Decks
            .Where(d => d.Id == deckId)
            .ExecuteDeleteAsync();

        await transaction.CommitAsync();
        return removedDeckIds;
    }
    public async Task<Deck?> GetById(long deckId)
    {
        await using var db = GetDb;
        return await db.Decks
            .AsNoTracking()
            .SingleOrDefaultAsync(d => d.Id == deckId);
    }

    public async Task<Deck> GetFromCard(long cardId)
    {
        await using var db = GetDb;
        return await db.Cards
            .Where(c => c.Id == cardId)
            .Include(c => c.Deck)
            .Select(c => c.Deck)
            .SingleAsync();
    }
    public async Task<ILookup<long?, Deck>> ParentIdChildrenLookup(long userId)
    {
        await using var db = GetDb;
        return (
            await AllDecksQuery(db, userId)
            .ToArrayAsync())
            .ToLookup(d => d.ParentDeckId);
    }

    public static async Task<ILookup<long?, Deck>> ParentIdChildrenLookup(long userId, AppDbContext db)
    {
        return (
            await AllDecksQuery(db, userId)
            .ToArrayAsync())
            .ToLookup(d => d.ParentDeckId
        );
    }

    public static async Task<IEnumerable<long>> GetChildrenIds(long deckId, AppDbContext db)
    {
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

    public async Task<bool> Exists(long id)
    {
        await using var db = GetDb;
        return await db.Decks
            .AnyAsync(d => d.Id == id);
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
