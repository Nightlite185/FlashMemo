namespace FlashMemo.Model.Persistence;

public interface IDeckMeta
{
    long Id { get; }
    string Name { get; }
    long UserId { get; }
    long OptionsId { get; }
    long? ParentDeckId { get; }

    Deck ToEntity();
}