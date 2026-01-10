using FlashMemo.Model.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Repositories
{
    public sealed class DeckRepo(IDbContextFactory<AppDbContext> dbFactory) : DbDependentClass(dbFactory)
    {
        public async Task RenameDeck(long deckId, string newName)
        {
            var db = GetDb;
            
            var deck = await db.Decks.FindAsync(deckId)
                ?? throw new ArgumentException(IdNotFoundMsg("Deck"), nameof(newName));
            
            deck.Name = newName;
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