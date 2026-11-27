using System.Globalization;
using CsvHelper;
using CsvHelper.TypeConversion;
using FlashMemo.Helpers;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FluentAssertions;
using Moq;

namespace FlashMemo.Tests.ModelTests;

public class CsvImportMapTests
{
    [Theory]
    [InlineData("en-US", false)]
    [InlineData("en-US", true)]
    [InlineData("pl-PL", true)]
    public void ExportedColumns_AreReadIntoTypedProperties(string cultureName, bool hasHistory)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            var card = new CardEntity
            {
                DeckId = 2,
                Deck = new Deck { Id = 2, Name = "Child" },
                Note = StandardNote.Create("Quoted \"front\", text\nnext line", "N/A"),
                Tags = [new Tag { Name = "first" }, new Tag { Name = "second" }],
                Due = hasHistory ? new DateTime(2026, 10, 29) : null,
                Interval = TimeSpan.FromDays(12),
                State = hasHistory ? CardState.Learning : CardState.New,
                IsSuspended = true,
                IsBuried = false,
                LearningStage = hasHistory ? LearningStage.II : null,
                Created = new DateTime(2026, 9, 1),
                LastModified = hasHistory ? new DateTime(2026, 10, 26) : null,
                LastReviewed = hasHistory ? new DateTime(2026, 10, 25) : null
            };
            CardEntity[] cards = [card];
            var serializer = new Mock<INoteSerializer>();
            serializer.Setup(service => service.Serialize(cards)).Returns(new[] { (card, card.Note) });
            CsvExportColumn[] columns =
            [
                new("Front content", dto => dto.FrontContent), new("Back content", dto => dto.BackContent),
                new("Deck", dto => dto.Deck), new("Due date", dto => dto.DueDate),
                new("Tags", dto => dto.Tags), new("Interval days", dto => dto.IntervalDays),
                new("State", dto => dto.State), new("Suspended?", dto => dto.IsSuspended),
                new("Buried?", dto => dto.IsBuried), new("Learning stage", dto => dto.LearningStage),
                new("Created date", dto => dto.CreatedDate), new("Last modified", dto => dto.LastModifiedDate),
                new("Last reviewed", dto => dto.LastReviewedDate)
            ];
            string content = new ExportService(serializer.Object).PrepareCsv(
                cards, new Dictionary<long, string> { [2] = "Root/Child" }, columns);

            var row = Read(content).Should().ContainSingle().Subject;

            row.FrontContent.Should().Be(((StandardNote)card.Note).FrontContent);
            row.BackContent.Should().Be("N/A");
            row.DeckPath.Should().Be("Root/Child");
            row.Tags.Should().Equal("first", "second");
            row.Due.Should().Be(card.Due);
            row.Interval.Should().Be(card.Interval);
            row.State.Should().Be(card.State);
            row.IsSuspended.Should().BeTrue();
            row.IsBuried.Should().BeFalse();
            row.LearningStage.Should().Be(card.LearningStage);
            row.Created.Should().Be(card.Created);
            row.LastModified.Should().Be(card.LastModified);
            row.LastReviewed.Should().Be(card.LastReviewed);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void RequiredColumnsOnly_LeaveOptionalPropertiesNull()
    {
        var row = Read("Back content,Front content\r\nback,front\r\n")
            .Should().ContainSingle().Subject;

        row.Should().BeEquivalentTo(new CsvImportDTO { FrontContent = "front", BackContent = "back" });
    }

    [Theory]
    [InlineData("")]
    [InlineData("N/A")]
    public void EmptyOrUnavailableMetadata_IsReadAsNull(string value)
    {
        string content = "Front content,Back content,Due date,Interval days,State,Suspended?,Buried?,"
            + "Learning stage,Created date,Last modified,Last reviewed\r\n"
            + "front,back," + string.Join(",", Enumerable.Repeat(value, 9)) + "\r\n";

        var row = Read(content).Should().ContainSingle().Subject;

        row.Should().BeEquivalentTo(new CsvImportDTO { FrontContent = "front", BackContent = "back" });
    }

    [Theory]
    [InlineData("Front content")]
    [InlineData("Back content")]
    public void MissingContentColumn_IsRejected(string onlyColumn)
    {
        Action read = () => Read($"{onlyColumn}\r\ncontent\r\n");

        read.Should().Throw<HeaderValidationException>();
    }

    [Theory]
    [InlineData("Due date", "31/02/2026")]
    [InlineData("Interval days", "not-a-duration")]
    [InlineData("State", "Unknown")]
    [InlineData("Suspended?", "not-a-boolean")]
    [InlineData("Learning stage", "Unknown")]
    public void InvalidTypedMetadata_IsRejected(string column, string value)
    {
        Action read = () => Read($"Front content,Back content,{column}\r\nfront,back,{value}\r\n");

        read.Should().Throw<TypeConverterException>();
    }

    [Theory]
    [InlineData("first,second")]
    [InlineData("first, second")]
    [InlineData(" first , second ,, ")]
    public void Tags_AreSplitAndTrimmed(string value)
    {
        var row = Read($"Front content,Back content,Tags\r\nfront,back,\"{value}\"\r\n")
            .Should().ContainSingle().Subject;

        row.Tags.Should().Equal("first", "second");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void EmptyTags_AreReadAsAnEmptyArray(string value)
    {
        var row = Read($"Front content,Back content,Tags\r\nfront,back,\"{value}\"\r\n")
            .Should().ContainSingle().Subject;

        row.Tags.Should().BeEmpty();
    }

    private static List<CsvImportDTO> Read(string content)
    {
        using var reader = new StringReader(content);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<CsvImportMap>();
        return csv.GetRecords<CsvImportDTO>().ToList();
    }
}