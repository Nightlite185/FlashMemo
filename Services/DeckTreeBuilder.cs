using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.Services;

public class DeckTreeBuilder(IDeckRepo dr, ICountingService cs): IDeckTreeBuilder
{
    private readonly ICountingService counter = cs;
    private readonly IDeckRepo deckRepo = dr;

    public async Task<IEnumerable<DeckNode>> BuildAsync(long userId)
    {
        var decksLookup = await deckRepo
            .ParentIdChildrenLookup(userId);

        return BuildDeckLevel(
            parent: null, decksLookup);
    }

    public async Task<IEnumerable<DeckNode>> BuildCountedAsync(long userId)
    {
        var decksLookup = await deckRepo
            .ParentIdChildrenLookup(userId);

        var cardsCount = await counter
            .CardsByState(userId, onlyForStudy: true);

        return BuildDeckLevelCounted(
            parent: null, decksLookup, cardsCount);
    }
    private static IEnumerable<DeckNode> BuildDeckLevelCounted(DeckNode? parent, ILookup<long?, Deck> 
                                                                lookup, IDictionary<long, CardsCount> deckToCCMap)
    {
        foreach (var deck in lookup[parent?.Id])
        {
            if (!deckToCCMap.TryGetValue(deck.Id, out var cc))
                throw new InvalidOperationException("Missing card count");

            // create this node FIRST
            var node = new DeckNode(deck, [], parent, cc);

            // build children using THIS node as parent
            var children = BuildDeckLevelCounted(
                node, lookup, deckToCCMap);

            foreach (var child in children)
                node.AddChild(child);

            // return node
            yield return node;
        }
    }

    private static IEnumerable<DeckNode> BuildDeckLevel(DeckNode? parent, ILookup<long?, Deck> lookup)
    {
        foreach (var deck in lookup[parent?.Id])
        {
            // create this node first
            var node = new DeckNode(deck, [], parent);

            // build children using this node as parent
            var children = BuildDeckLevel(node, lookup);

            foreach (var child in children)
                node.AddChild(child);

            // return node
            yield return node;
        }
    }
}