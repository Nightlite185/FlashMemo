using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FluentAssertions;

namespace FlashMemo.Tests.ServiceTests;

public class XamlNoteSerializerTests
{
    [Fact]
    public Task Serialize_ConvertsFormattedMultilineUnicodeNotesWithoutMutatingCards() => Helpers.RunOnSta(() =>
    {
        const string front = """
            <FlowDocument xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
                <Paragraph><Bold>Żółw 🐢</Bold><LineBreak/><Italic>line &amp; two</Italic></Paragraph>
            </FlowDocument>
            """;
        const string back = """
            <FlowDocument xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
                <Paragraph><Run Text="Quoted &quot;answer&quot;, 日本語"/></Paragraph>
                <Paragraph>second paragraph</Paragraph>
            </FlowDocument>
            """;
        var originalNote = new StandardNote { Id = 10, FrontContent = front, BackContent = back };
        var first = new CardEntity { Id = 1, NoteId = 10, Note = originalNote };
        var second = new CardEntity { Id = 2, Note = StandardNote.Create(back, front) };

        var result = new XamlNoteSerializer().Serialize([first, second]).ToArray();

        result.Select(pair => pair.card).Should().Equal(first, second);
        var firstNote = result[0].note.Should().BeOfType<StandardNote>().Subject;
        firstNote.Should().NotBeSameAs(originalNote);
        firstNote.FrontContent.Should().Be($"Żółw 🐢{Environment.NewLine}line & two");
        firstNote.BackContent.Should().Be($"Quoted \"answer\", 日本語{Environment.NewLine}second paragraph");
        var secondNote = result[1].note.Should().BeOfType<StandardNote>().Subject;
        secondNote.FrontContent.Should().Be(firstNote.BackContent);
        secondNote.BackContent.Should().Be(firstNote.FrontContent);
        first.Note.Should().BeSameAs(originalNote);
        first.NoteId.Should().Be(10);
        originalNote.Id.Should().Be(10);
        originalNote.FrontContent.Should().Be(front);
        originalNote.BackContent.Should().Be(back);
        ((StandardNote)second.Note).FrontContent.Should().Be(back);
        ((StandardNote)second.Note).BackContent.Should().Be(front);
    });

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n ")]
    [InlineData("<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph/></FlowDocument>")]
    public Task Serialize_BlankFieldsBecomeEmptyText(string? content) => Helpers.RunOnSta(() =>
    {
        var card = new CardEntity { Note = StandardNote.Create(content!, content!) };

        var result = new XamlNoteSerializer().Serialize([card]);

        var note = result.Should().ContainSingle().Subject.note.Should().BeOfType<StandardNote>().Subject;
        note.FrontContent.Should().BeEmpty();
        note.BackContent.Should().BeEmpty();
    });

    [Fact]
    public Task Serialize_RejectsUnsupportedNoteTypes() => Helpers.RunOnSta(() =>
    {
        var card = new CardEntity { Note = new UnsupportedNote() };

        Action serialize = () => new XamlNoteSerializer().Serialize([card]);

        serialize.Should().Throw<NotSupportedException>();
    });

    private sealed class UnsupportedNote : Note { }
}
