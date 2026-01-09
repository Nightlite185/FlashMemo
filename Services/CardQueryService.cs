using FlashMemo.Model;
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
            var db = GetDb;
            var allDecks = await db.Decks.ToListAsync();
            var rootDeck = await db.Decks.FindAsync(deckId)
                ?? throw new ArgumentException(IdNotFoundMsg("Deck"), nameof(deckId));

            // I know this is ugly as hell, but the only way to put parentDeckId as a key in dict. 
            // It wont believe me its not null otherwise.
            var deckGroups = (IEnumerable<IGrouping<long, DeckEntity>>) allDecks
                .Where(d => d.ParentDeckId is not null)
                .GroupBy(x => x.ParentDeckId);
                
            var parentChildrenMap = deckGroups
                .ToDictionary(d => d.Key, d => d.AsEnumerable());

            var deckIds = new HashSet<long>();
            GetChildrenIds(deckId, parentChildrenMap, deckIds);

            var forStudy = await db.Cards
                .AsNoTracking()
                .Where(c => 
                       !c.IsBuried 
                    && !c.IsSuspended 
                    && deckIds.Contains(c.DeckId) 
                    && c.NextReview <= DateTime.Now)
                .ToListAsync();

            return forStudy.OrderCards(rootDeck.Options.Sorting);
        }
        private static void GetChildrenIds(long deckId, Dictionary<long, IEnumerable<DeckEntity>> lookup, HashSet<long> result)
        {
            result.Add(deckId);

            if (!lookup.TryGetValue(deckId, out var children))
                return;

            foreach (var deck in children)
                GetChildrenIds(deck.Id, lookup, result);
        }
    }
}