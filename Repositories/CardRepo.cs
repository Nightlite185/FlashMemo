using FlashMemo.Model.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Repositories;

public sealed class CardRepo(IDbContextFactory<AppDbContext> dbFactory) : DbDependentClass(dbFactory), ICardRepo
{
    public async Task DeleteCards(IEnumerable<CardEntity> cards)
    {
        var db = GetDb;
        db.Cards.RemoveRange(cards);

        await db.SaveChangesAsync();
    }

    public async Task AddCard(CardEntity card)
    {
        var db = GetDb;

        await db.Cards.AddAsync(card);
        await db.SaveChangesAsync();
    }

    public async Task<CardEntity> GetById(long cardId)
        => await GetDb.Cards.SingleAsync(c => c.Id == cardId);
}