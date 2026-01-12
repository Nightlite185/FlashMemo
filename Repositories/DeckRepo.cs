using FlashMemo.Model.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Repositories
{
    public sealed class DeckRepo(IDbContextFactory<AppDbContext> dbFactory) : DbDependentClass(dbFactory)
    {
        ///<summary>ONLY UPDATES SCALARS, does not touch navs.</summary>
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
        public async Task DeleteDeck(long deckId)
        {
            var db = GetDb;

            var deck = await db.Decks.FindAsync(deckId)
                ?? throw new ArgumentException("deck with this id was not found in database.", nameof(deckId));

            db.Decks.Remove(deck);
            await db.SaveChangesAsync();
        }
        public async Task<Deck> LoadDeck(long id)
        {
            var db = GetDb;

            var deckEntity = await db.Decks
                .AsNoTracking()
                .SingleAsync(d => d.Id == id);

            return deckEntity;
        }
    }
}