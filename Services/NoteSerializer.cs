using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;

namespace FlashMemo.Services;

public interface INoteSerializer
{
    /// <summary>
    /// WARNING: this mutates card.Note for every given card, serializes it in place.
    /// </summary>
    public void Serialize(IEnumerable<CardEntity> cards);
}

public class XamlNoteSerializer: INoteSerializer
{
    
    public void Serialize(IEnumerable<CardEntity> cards)
    {
        foreach(var card in cards)
        {
            if (card.Note is not StandardNote sn)
                throw new NotSupportedException(
                "Only standard notes supported at the moment.");

            sn.FrontContent = XamlSerializer.GetPlainText(sn.FrontContent);
            sn.BackContent = XamlSerializer.GetPlainText(sn.BackContent);
        };
    }
}