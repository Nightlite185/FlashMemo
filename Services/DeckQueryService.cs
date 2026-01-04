using FlashMemo.Model;
using FlashMemo.Model.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Services
{
    public class DeckQueryService(IDbContextFactory<AppDbContext> factory)
    {
        private readonly IDbContextFactory<AppDbContext> dbFactory = factory;
        public async Task<IEnumerable<CardEntity>> GetFilteredCards(Filters filters)
        {
            var db = dbFactory.CreateDbContext();
            var query = filters.ToExpression();

            var cards = await db.Cards
                .Where(query)
                .ToListAsync();

            db.Dispose();
            return cards;
        }
    }
}