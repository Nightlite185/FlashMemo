namespace FlashMemo.Model.Persistence;

public class Note; // would make abstract but EF hates it, and this is an entity

public class StandardNote : Note
{
    public string FrontContent { get; set; } = null!;
    public string BackContent { get; set; } = null!;

    public static StandardNote Create(string front, string back)
    {
        return new()
        {
            FrontContent = front,
            BackContent = back
        };
    }
}