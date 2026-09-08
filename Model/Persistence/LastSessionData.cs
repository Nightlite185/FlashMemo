namespace FlashMemo.Model.Persistence;

public sealed class AppSessionData
{
    public int Id { get; set; }
    public long? LastLoadedUserId { get; set; }
}

public sealed class UserSessionCache
{
    public long UserId { get; set; }
    public long? LastUsedDeckId { get; set; }
    public Filters? LastFilters { get; set; }
}
