namespace FlashMemo.Helpers;

public static class IdGetter
{
    public static long Next() 
        => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
