using FlashMemo.Model;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Services
{
    public class CardQueryService(IDbContextFactory<AppDbContext> factory): DbDependentClass(factory)
    {
        public async Task<IList<CardEntity>> GetCardsWhere(Filters filters, CardsOrder order, SortingDirection dir)
        {
            var db = GetDb;
            var query = filters.ToExpression();

            var cards = await db.Cards
                .Where(query)
                .SortAnyCards(order, dir)
                .ToListAsync();

            return cards;
        }
        public async Task<IList<CardEntity>> GetForStudy(long deckId)
        {
            #region base query
            var db = GetDb;
            var allDecks = await db.Decks.ToListAsync();
            var rootDeck = await db.Decks.FindAsync(deckId)
                ?? throw new ArgumentException(IdNotFoundMsg("Deck"), nameof(deckId));

            var parentChildrenMap = allDecks
                .ToLookup(d => d.ParentDeckId);

            var deckIds = new HashSet<long>();
            GetChildrenIds(deckId, parentChildrenMap, deckIds);

            var baseQuery = db.Cards
                .AsNoTracking()
                .Where(c =>
                   !c.IsBuried 
                && !c.IsSuspended 
                && deckIds.Contains(c.DeckId));
            #endregion
            
            #region branching into 3 subsets and ordering
            var sortOpt = rootDeck.Options.Sorting;
            var limitsOpt = rootDeck.Options.DailyLimits;

            var learning = await baseQuery
                .Where(c => c.State == CardState.Learning)
                .OrderBy(c => c.Due)
                .ToListAsync();

            var lessons = await baseQuery
                .Where(c => c.State == CardState.New)
                .SortLessons(sortOpt)
                .Take(limitsOpt.DailyLessonsLimit)
                .ToListAsync();
            
            var reviews = await baseQuery
                .Where(c => c.State == CardState.Review)
                .SortReviews(sortOpt)
                .Take(limitsOpt.DailyReviewsLimit)
                .ToListAsync();

            lessons.ShuffleIf(sortOpt.LessonsOrder == LessonOrder.Random);
            reviews.ShuffleIf(sortOpt.ReviewsOrder == ReviewOrder.Random);
            #endregion
            
            #region final return
            return sortOpt.CardStateOrder switch
            {
                CardStateOrder.NewThenReviews
                    => [..learning
                        .Concat(lessons)
                        .Concat(reviews)],

                CardStateOrder.ReviewsThenNew
                    => [..learning
                        .Concat(reviews)
                        .Concat(lessons)],

                CardStateOrder.Mix
                    => [..learning.Concat(
                            reviews.Concat(lessons).Shuffle())],

                _ => throw new ArgumentException(
                    $"Invalid {nameof(CardStateOrder)} enum value: {sortOpt.CardStateOrder}")
            };
            #endregion
        }
        private static void GetChildrenIds(long deckId, ILookup<long?, Deck> lookup, HashSet<long> result)
        {
            result.Add(deckId);

            var children = lookup[deckId];
            
            if (!children.Any()) return;
            
            foreach (var deck in children)
                GetChildrenIds(deck.Id, lookup, result);
        }
    }
}