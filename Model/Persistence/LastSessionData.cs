namespace FlashMemo.Model.Persistence;

public sealed class LastSessionData
{
    public int Id { get; set; }
    public long? LastLoadedUserId { get; set; }
    public long? LastUsedDeckId { get; set; }
    public Filters? LastFilters { get; set; }
    
    //? maybe some window size stuff later too
}