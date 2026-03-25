namespace FlashMemo.Model.Persistence;

public interface IDeckMeta
{
    long Id { get; }
    string Name { get; }
    long UserId { get; }
    long OptionsId { get; }

    Deck ToEntity();
}