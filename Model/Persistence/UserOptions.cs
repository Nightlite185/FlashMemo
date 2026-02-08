namespace FlashMemo.Model.Persistence;

public record class UserOptions
{
    public long UserId { get; init; }

    // review timer on/off globally?

    // include lessons in reviews daily limit?

    internal static UserOptions CreateDefault()
    {
        throw new NotImplementedException();
    }
}