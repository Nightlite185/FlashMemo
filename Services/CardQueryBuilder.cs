using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Services;
public static class CardQueryBuilder
{
    public static CardsByStateQ GroupByStateQ(this IQueryable<CardEntity> baseQuery)
    {
        return new CardsByStateQ()
        {
            Lessons = baseQuery
                .Where(c => c.State == CardState.New),

            Learning = baseQuery
                .Where(c => c.State == CardState.Learning),

            Reviews = baseQuery
                .Where(c => c.State == CardState.Review),
        };
    }

    public static IQueryable<CardEntity> ForStudy(this IQueryable<CardEntity> baseQuery)
    {
        var today = DateTime.Today;

        return baseQuery.Where(c =>
            !c.IsSuspended && !c.IsBuried
            && (!c.Due.HasValue || c.Due.Value.Date <= today));
    }

    public static IEnumerable<CardEntity> ForStudy(this IEnumerable<CardEntity> cards)
    {
        var today = DateTime.Today;

        return cards.Where(c =>
            !c.IsSuspended && !c.IsBuried
            && (!c.Due.HasValue || c.Due.Value.Date <= today));
    }
    
    public static async Task<IQueryable<CardEntity>> AllCardsInDeckQAsync(this AppDbContext db, long deckId)
    {
        var deckIds = await DeckRepo
            .GetChildrenIds(deckId, db);

        return db.Cards
            .AsNoTracking()
            .Where(c => 
                deckIds.Contains(c.DeckId));
    }
}