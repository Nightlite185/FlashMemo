/*
using FlashMemo.Model.Persistence;

namespace FlashMemo.Model;

public readonly struct DeckMeta(long id, string name, long userId, 
                                long optionsId, long? parentDeckId): IDeckMeta
{
    public long Id { get; } = id;
    public string Name { get; } = name;
    public long UserId { get; } = userId;
    public long OptionsId { get; } = optionsId;
    public long? ParentDeckId { get; } = parentDeckId;

    public readonly Deck ToEntity()
    {
        var deck = Deck.CreateNew(
            Name, UserId, ParentDeckId);

        deck.Id = Id;
        deck.OptionsId = OptionsId;

        return deck;
    }
}
*/