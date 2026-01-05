using FlashMemo.Model;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Repositories
{
    public class DeckRepo(IDbContextFactory<AppDbContext> factory)
    {
        private const string idNotFoundMessage = "deck with this id was not found in database.";
        private readonly IDbContextFactory<AppDbContext> dbFactory = factory;
        private DbContext? currentCtx;
        private DeckEntity? loadedDeck = null;
        private AppDbContext GetDb()
        {
            if (currentCtx != null)
                throw new InvalidOperationException("Cannot create a second DbContext since there is already one which hasn't been disposed.");
            
            var db = dbFactory.CreateDbContext();
            currentCtx = db;
            return db;
        }
        
        public async Task RenameDeck(long deckId, string newName)
        {
            var db = GetDb();
            
            var deck = await db.Decks.FindAsync(deckId) // hmm does it even track it if I just do find??? do I need to include sth here??? TO DO
                ?? throw new ArgumentException(idNotFoundMessage, nameof(newName));
            
            deck.Name = newName;
            await db.SaveChangesAsync();
        }
        public async Task AddNewDeck(DeckEntity deck)
        {
            var db = GetDb();
            
            await db.Decks.AddAsync(deck);
            await db.SaveChangesAsync();
        }
        public async Task DeleteDeck(long deckId) // TO DO: cascade delete cards later too and all fk entries too.
        {                                         // as well as cardlogs with same ids.
            var db = GetDb();

            var deck = await db.Decks.FindAsync(deckId) 
                ?? throw new ArgumentException("deck with this id was not found in database.", nameof(deckId));

            db.Decks.Remove(deck);
            await db.SaveChangesAsync();
        }
        public async Task<DeckEntity> LoadDeck(long id)
        {
            var db = GetDb();

            var deckEntity = await db.Decks
                .Include(d => d.Cards)
                .SingleAsync(d => d.Id == id);

            loadedDeck = deckEntity;
            return deckEntity;
        }
    }
}