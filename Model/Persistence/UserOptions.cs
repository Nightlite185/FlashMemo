namespace FlashMemo.Model.Persistence;

public record class UserOptions
{
    // public long UserId { get; init; }

    // review timer on/off globally?
    
    // review timer stops on answer reveal or actually answering?

    // include lessons in reviews daily limit?

    internal static UserOptions CreateDefault()
    {
        return new()
        {
            //UserId = userId
        };
    }
}