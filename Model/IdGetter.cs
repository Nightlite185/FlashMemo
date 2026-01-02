namespace FlashMemo.Model
{
    public static class IdGetter
    {
        public static long Next() 
            => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}