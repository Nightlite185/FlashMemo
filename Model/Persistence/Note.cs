using FlashMemo.Helpers;

namespace FlashMemo.Model.Persistence;

public enum NoteTypes { Standard, Cloze, List }

public abstract class Note
{
    public long Id { get; set; }
    public void MapTo(Note other)
    {
        switch (this)
        {
            case StandardNote sn1 when other is StandardNote sn2:
                if (this.Id != sn2.Id) 
                    throw new InvalidOperationException("Cant map to an entity with diff Id.");

                sn2.FrontContent = sn1.FrontContent;
                sn2.BackContent = sn1.BackContent;
                break;
        }
    }
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