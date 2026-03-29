using System.Collections.Immutable;
using FlashMemo.Helpers;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Services;
public static class QueryExtensions
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
    public static IQueryable<CardEntity> ForStudy(this IQueryable<CardEntity> baseQuery, byte dayStartOffset)
    {
        dayStartOffset.ThrowInvalidOffset();

        var adjustedNow = DateTime.Now
            .AddHours(-dayStartOffset);

        return baseQuery.Where(c =>
            !c.IsSuspended && !c.IsBuried && (!c.Due.HasValue
            || c.Due.Value.Date <= adjustedNow));
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
    public static async Task<ICollection<long>> DeckIdsFromUser(this IQueryable<Deck> query, long userId)
    {
        return await query
            .Where(d => d.UserId == userId)
            .Select(d => d.Id)
            .ToArrayAsync();
    }
}