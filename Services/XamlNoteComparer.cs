using System.Windows.Controls;
using System.Windows.Documents;
using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;

namespace FlashMemo.Services;

public class XamlNoteComparer: INoteComparer
{
    /* Design tradeoff: this singleton reuses one scratch editor to normalize WPF's
    markup rewrites and editor-added properties without constructing a control per
    comparison. It is created on first normalization and must stay on the UI thread.
    Keeping the existing editor/storage avoids WebView2's browser runtime, JS bridge
    and a content-format migration. XML comparison libraries still need WPF-specific
    normalization; other document engines would add dependencies for this one check.
    Each call still parses/serializes a fresh FlowDocument, and equality remains
    structural: visually equivalent markup can still compare differently. */
    private RichTextBox? normalizationEditor;

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
                NormalizeContent(note.FrontContent),
                NormalizeContent(note.BackContent)),

            _ => throw new NotSupportedException(
                $"Note type '{savedNote.GetType().Name}' is not supported yet.")
        };
    }

    public NoteComparable FromEditor(NoteTypes noteType, string frontContent, string backContent)
    {
        return noteType switch
        {
            NoteTypes.Standard => new StandardNoteComparable(
                NormalizeContent(frontContent),
                NormalizeContent(backContent)),

            _ => throw new NotSupportedException(
                $"Note type '{noteType}' is not supported yet.")
        };
    }

    private string NormalizeContent(string? xamlOrText)
    {
        // Retain compatibility with plain-text notes without trimming meaningful whitespace.
        if (!string.IsNullOrWhiteSpace(xamlOrText) && !xamlOrText.TrimStart().StartsWith('<'))
            return xamlOrText;

        try
        {
            // RichTextBox adds editor properties (such as AllowDrop), and XamlWriter
            // rewrites markup. Round-trip both sides through the same editor first.
            var editor = normalizationEditor ??= new RichTextBox();
            editor.Document = XamlSerializer.FromXaml(xamlOrText ?? string.Empty);
            
            if (editor.Document.Blocks.Count == 0)
                editor.Document.Blocks.Add(new Paragraph());

            return XamlSerializer.ToXaml(editor.Document);
        }

        catch
        {
            // Keep malformed stored content intact; display/recovery is handled separately.
            return xamlOrText ?? string.Empty;
        }
    }
}