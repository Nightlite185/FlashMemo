using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;

namespace FlashMemo.Services;

public interface INoteSerializer
{
    public ICollection<Note> Serialize(IEnumerable<Note> notes);
}

public class XamlNoteSerializer: INoteSerializer
{
    public ICollection<Note> Serialize(IEnumerable<Note> notes)
    {
        var result = new List<Note>();

        foreach(var note in notes)
        {
            if (note is not StandardNote sn) 
                throw new NotSupportedException(
                "Only standard notes supported at the moment.");

            var serialized = new StandardNote()
            {
                Id = sn.Id, 
                FrontContent = XamlSerializer.GetPlainText(sn.FrontContent),
                BackContent = XamlSerializer.GetPlainText(sn.BackContent)
            };

            result.Add(serialized);
        }

        return result;
    }
}