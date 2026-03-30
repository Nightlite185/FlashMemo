using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;

namespace FlashMemo.Services;

public class XamlNoteComparer: INoteComparer
{
    public bool AreEqual(NoteComparable first, NoteComparable second)
    {
        return (first, second) switch
        {
            (StandardNoteComparable a, StandardNoteComparable b) =>
                string.Equals(a.Front, b.Front, StringComparison.Ordinal)
                && string.Equals(a.Back, b.Back, StringComparison.Ordinal),

            _ when first.Type != second.Type => false,
            _ => throw new NotSupportedException(
                $"Comparison for note type '{first.Type}' is not supported yet.")
        };
    }

    public NoteComparable FromModel(Note savedNote)
    {
        return savedNote switch
        {
            StandardNote note => new StandardNoteComparable(
                ExtractComparableText(note.FrontContent),
                ExtractComparableText(note.BackContent)),

            _ => throw new NotSupportedException(
                $"Note type '{savedNote.GetType().Name}' is not supported yet.")
        };
    }

    public NoteComparable FromEditor(NoteTypes noteType, string frontText, string backText)
    {
        return noteType switch
        {
            NoteTypes.Standard => new StandardNoteComparable(
                NormalizeText(frontText),
                NormalizeText(backText)),

            _ => throw new NotSupportedException(
                $"Note type '{noteType}' is not supported yet.")
        };
    }

    private static string NormalizeText(string? value)
        => value?.Trim() ?? string.Empty;

    private static string ExtractComparableText(string? xamlOrText)
    {
        if (string.IsNullOrWhiteSpace(xamlOrText))
            return string.Empty;

        try
        {
            return XamlSerializer.GetPlainText(xamlOrText);
        }
        catch
        {
            // If stored data is malformed XAML, fall back to plain text comparison.
            return NormalizeText(xamlOrText);
        }
    }
}
