using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Services;
public class CardQueryBuilder(IDeckRepo dr): ICardQueryBuilder
{
    private readonly IDeckRepo deckRepo = dr;

    public static CardsByStateQ GroupByStateQ(IQueryable<CardEntity> baseQuery)
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
    public async Task<IQueryable<CardEntity>> AllCardsInDeckQAsync(long deckId, AppDbContext db)
    {
        var deckIds = await deckRepo.GetChildrenIds(deckId);

        return db.Cards
            .AsNoTracking()
            .Where(c => 
                deckIds.Contains(c.DeckId));
    }
}