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

        await AttachTags(card, db);

        await db.Cards.AddAsync(card);
        await db.SaveChangesAsync();
    }

    private async Task AttachTags(CardEntity card, AppDbContext db)
    {
        var tagIds = card.Tags
            .Select(t => t.Id)
            .Distinct()
            .ToArray();

        if (tagIds.Length > 0)
        {
            var existingTags = await db.Tags
                .Where(t => tagIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id);

            var missingIds = tagIds
                .Except(existingTags.Keys)
                .ToArray();

            if (missingIds.Length > 0)
                throw new InvalidOperationException(
                    $"Cannot create card: missing tags in DB: {string.Join(", ", missingIds)}");

            card.ReplaceTagsWith(tagIds.Select(
                id => existingTags[id]));
        }
    }

    public async Task<CardEntity?> GetById(long cardId) => await GetDb.Cards
        .Include(c => c.Deck)
        .SingleOrDefaultAsync(c => c.Id == cardId);

    public async Task<IEnumerable<CardEntity>> GetByIds(IEnumerable<long> cardIds) 
        => await GetDb.Cards
        .Include(c => c.Deck)
        .Where(c => cardIds.Contains(c.Id))
        .ToArrayAsync();
}