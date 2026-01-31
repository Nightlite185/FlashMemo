using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.ViewModel.WrapperVMs;

namespace FlashMemo.Services;

public class DeckTreeBuilder(IDeckRepo dr, ICardQueryService cqs)
{
    private readonly ICardQueryService cardQueryS = cqs;
    private readonly IDeckRepo deckRepo = dr;

    public async Task<IEnumerable<DeckNode>> BuildAsync(long userId)
    {
        var decksLookup = await deckRepo
            .BuildDeckLookupAsync(userId);

        return BuildDeckLevel(parentId: null, decksLookup);
    }
    public async Task<IEnumerable<DeckNode>> BuildCountedAsync(long userId)
    {
        var decksLookup = await deckRepo
            .BuildDeckLookupAsync(userId);

        var cardsCount = await cardQueryS
            .CardsByState(
                userId, 
                countOnlyStudyable: true
            );

        return BuildDeckLevelWithCount(parentId: null, decksLookup, cardsCount);
    }

    private static IEnumerable<DeckNode> BuildDeckLevelWithCount(long? parentId, ILookup<long?, Deck> decksLookup, 
                                                                IDictionary<long, CardsCount> deckToCCMap)
    {
        foreach (var deck in decksLookup[parentId])
        {
            // build children recursively
            var children = BuildDeckLevelWithCount(deck.Id, decksLookup, deckToCCMap);

            // get card counts for this deck
            if (!deckToCCMap.TryGetValue(deck.Id, out var cc))
            {
                throw new InvalidOperationException(
                    $"{nameof(deckToCCMap)} deck ids don't match the ones in {nameof(decksLookup)}");
            }

            // create the VM
            yield return new DeckNode(deck, children, cc);
        }
    }

    private static IEnumerable<DeckNode> BuildDeckLevel(long? parentId, ILookup<long?, Deck> decksLookup)
    {
        foreach (var deck in decksLookup[parentId])
        {
            // build children recursively
            var children = BuildDeckLevel(deck.Id, decksLookup);

            // create the VM
            yield return new DeckNode(deck, children);
        }
    }
}
