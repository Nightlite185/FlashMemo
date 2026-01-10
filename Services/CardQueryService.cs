using FlashMemo.Model;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Services
{
    public class CardQueryService(IDbContextFactory<AppDbContext> factory): DbDependentClass(factory)
    {
        public async Task<IEnumerable<CardEntity>> GetFilteredCards(Filters filters)
        {
            var db = GetDb;
            var query = filters.ToExpression();

            var cards = await db.Cards
                .Where(query)
                .ToListAsync();

            return cards;
        }
        public async Task<IEnumerable<CardEntity>> GetForStudy(long deckId)
        {
            #region base query
            var db = GetDb;
            var allDecks = await db.Decks.ToListAsync();
            var rootDeck = await db.Decks.FindAsync(deckId)
                ?? throw new ArgumentException(IdNotFoundMsg("Deck"), nameof(deckId));

            // I know this is ugly as hell, but the only way to put parentDeckId as a key in dict. 
            // It wont believe me its not null otherwise.
            var deckGroups = (IEnumerable<IGrouping<long, Deck>>) allDecks
                .Where(d => d.ParentDeckId is not null)
                .GroupBy(x => x.ParentDeckId);
                
            var parentChildrenMap = deckGroups
                .ToDictionary(d => d.Key, d => d.AsEnumerable());

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
            #endregion
            
            #region final return
            return sortOpt.CardStateOrder switch
            {
                CardStateOrder.NewThenReviews
                    => learning.Concat(lessons).Concat(reviews),

                CardStateOrder.ReviewsThenNew
                    => learning.Concat(reviews).Concat(lessons),

                CardStateOrder.Mix
                    => learning.Concat(reviews.Concat(lessons).Shuffle()),

                _ => throw new ArgumentException(
                    $"Invalid {nameof(CardStateOrder)} enum value: {sortOpt.CardStateOrder}")
            };
            #endregion
        }
        private static void GetChildrenIds(long deckId, Dictionary<long, IEnumerable<Deck>> lookup, HashSet<long> result)
        {
            result.Add(deckId);

            if (!lookup.TryGetValue(deckId, out var children))
                return;

            foreach (var deck in children)
                GetChildrenIds(deck.Id, lookup, result);
        }
    }
}