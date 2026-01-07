using FlashMemo.Model.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Repositories
{
    public sealed class CardRepo(IDbContextFactory<AppDbContext> dbFactory) : RepoBase(dbFactory)
    {
        ///<summary>Updates scalars and syncs tags collection. DOES NOT WORK FOR NAV PROPERTIES LIKE Deck</summary>
        public async Task UpdateCard(CardEntity detached)
        {
            var db = GetDb;

            var tracked = await db.Cards
                .FirstAsync(c => c.Id == detached.Id);

            db.Entry(tracked)
                .CurrentValues
                .SetValues(detached);

            tracked.SyncTagsFrom(detached);

            await db.SaveChangesAsync();
        }

        public async Task DeleteCard(CardEntity card)
        {
            var db = GetDb;

            db.Cards.Remove(card);

            await db.SaveChangesAsync();
        }

        public async Task AddCard(CardEntity card)
        {
            var db = GetDb;

            await db.Cards.AddAsync(card);

            await db.SaveChangesAsync();
        }

        public async Task<CardEntity> GetCard(long cardId)
        {
            var db = GetDb;

            return await db.Cards.FindAsync(cardId)
                ?? throw new ArgumentException(IdNotFoundMsg("Card"), nameof(cardId));
        }

        public async Task<IEnumerable<CardEntity>> GetFromDeck(long deckId)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<CardEntity>> GetAllCards()
        {
            throw new NotImplementedException();
        }
    }
}