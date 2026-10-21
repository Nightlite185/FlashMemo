using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;

namespace FlashMemo.Services;

public interface INoteSerializer
{
    public ICollection<(CardEntity card, Note note)> Serialize(IEnumerable<CardEntity> cards);
}

public class XamlNoteSerializer: INoteSerializer
{
    public ICollection<(CardEntity card, Note note)> Serialize(IEnumerable<CardEntity> cards)
    {
        var pairs = new List<(CardEntity, Note)>();

        foreach(var card in cards)
        {
            if (card.Note is not StandardNote sn)
                throw new NotSupportedException(
                "Only standard notes supported at the moment.");

            var note = new StandardNote
            {
                FrontContent = XamlSerializer.GetPlainText(sn.FrontContent),
                BackContent = XamlSerializer.GetPlainText(sn.BackContent)
            };

            pairs.Add((card, note));
        };

        return pairs;
    }
}