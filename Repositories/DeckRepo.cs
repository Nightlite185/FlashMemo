using FlashMemo.Model;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Repositories
{
    public class DeckRepo(IDbContextFactory<AppDbContext> factory)
    {
        private readonly IDbContextFactory<AppDbContext> dbFactory = factory;
        private AppDbContext GetDb()
        {
            if (currentCtx != null)
                throw new InvalidOperationException("Cannot create a second DbContext since there is already one which hasn't been disposed.");
            
            var db = dbFactory.CreateDbContext();
            currentCtx = db;
            return db;
        }
        private DbContext? currentCtx;
        public async Task<Deck> LoadDeck(long id)
        {
            var db = GetDb();

            var deck = await db.Decks
                .Include(d => d.Cards)
                .SingleAsync(d => d.Id == id);

            return deck.MapToDomain(new());
        }
        public void SaveChanges()
        {
            if (currentCtx is DbContext db)
            {
                currentCtx = null;
                db.SaveChanges();
                db.Dispose();
            }

            else throw new InvalidOperationException(
                "Cannot save changes because there is no currently active DbContext");
        }
    }
}