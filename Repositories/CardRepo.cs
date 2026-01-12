using FlashMemo.Model.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Repositories
{
    public sealed class CardRepo(IDbContextFactory<AppDbContext> dbFactory) : DbDependentClass(dbFactory)
    {
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
    }
}