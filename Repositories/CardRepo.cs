using FlashMemo.Model.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Repositories;

public sealed class CardRepo(IDbContextFactory<AppDbContext> dbFactory) : DbDependentClass(dbFactory), ICardRepo
{
    public async Task DeleteCards(IEnumerable<long> cardIds)
    {
        await GetDb.Cards
            .Where(c => cardIds.Contains(c.Id))
            .ExecuteDeleteAsync();
    }

    public async Task AddCard(CardEntity card)
    {
        var db = GetDb;

        await db.Cards.AddAsync(card);
        await db.SaveChangesAsync();
    }

    public async Task<CardEntity> GetById(long cardId) => await GetDb.Cards
        .Include(c => c.Deck)
        .SingleAsync(c => c.Id == cardId);

    public async Task<IEnumerable<CardEntity>> GetByIds(IEnumerable<long> cardIds) 
        => await GetDb.Cards
        .Include(c => c.Deck)
        .Where(c => cardIds.Contains(c.Id))
        .ToArrayAsync();
}