namespace FlashMemo.Model.Persistence;

public interface IDeckMeta
{
    long Id { get; }
    string Name { get; }
    public long UserId { get; }
    public long OptionsId { get; }
}