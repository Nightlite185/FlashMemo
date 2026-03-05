using FlashMemo.Helpers;

namespace FlashMemo.Model.Persistence;

public enum NoteTypes { Standard, Cloze, List }

public abstract class Note
{
    public long Id { get; set; }
}

public class StandardNote: Note
{
    public string FrontContent { get; set; } = null!;
    public string BackContent { get; set; } = null!;

    public static StandardNote Create(string front, string back)
    {
        return new()
        {
            Id = IdGetter.Next(),
            
            FrontContent = front,
            BackContent = back
        };
    }
}