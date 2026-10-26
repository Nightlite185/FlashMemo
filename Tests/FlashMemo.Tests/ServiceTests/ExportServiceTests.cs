using System.Globalization;
using CsvHelper;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FluentAssertions;
using Moq;

namespace FlashMemo.Tests.ServiceTests;

public class ExportServiceTests : IDisposable
{
    private readonly DirectoryInfo directory = Directory.CreateTempSubdirectory("FlashMemo-ExportServiceTests-");

    [Theory]
    [InlineData('\t')]
    [InlineData('\n')]
    [InlineData(' ')]
    [InlineData(';')]
    public void PrepareTxt_UsesSerializedNotesAndRequestedSeparators(char noteSeparator)
    {
        CardEntity[] cards = [new(), new()];
        var serializer = new Mock<INoteSerializer>(MockBehavior.Strict);
        serializer.Setup(service => service.Serialize(cards)).Returns(new (CardEntity, Note)[]
        {
            (cards[0], StandardNote.Create("żółw", "turtle")),
            (cards[1], StandardNote.Create("", "empty front"))
        });
        var service = new ExportService(serializer.Object);

        string content = service.PrepareTxt(cards, " | ", noteSeparator);

        content.Should().Be($"żółw | turtle{noteSeparator} | empty front{noteSeparator}");
        serializer.Verify(s => s.Serialize(cards), Times.Once);
        serializer.VerifyNoOtherCalls();
    }

    [Fact]
    public void PrepareCsv_MapsSelectedMetadataAndEscapesSerializedContent()
    {
        var card = new CardEntity
        {
            DeckId = 2,
            Deck = new Deck { Id = 2, Name = "Child" },
            Tags = [new Tag { Name = "first" }, new Tag { Name = "second" }],
            Interval = TimeSpan.FromDays(12),
            Due = new DateTime(2026, 10, 25),
            Created = new DateTime(2026, 9, 1),
            IsSuspended = true
        };
        CardEntity[] cards = [card];
        var serializer = new Mock<INoteSerializer>(MockBehavior.Strict);
        serializer.Setup(service => service.Serialize(cards)).Returns(new (CardEntity, Note)[]
        {
            (card, StandardNote.Create("Quoted \"front\", żółw\nnext line", "back"))
        });
        var service = new ExportService(serializer.Object);

        string content = service.PrepareCsv(cards, new Dictionary<long, string> { [2] = "Root/Child" },
        [
            new("Front", dto => dto.FrontContent), new("Deck", dto => dto.Deck),
            new("Tags", dto => dto.Tags), new("Interval", dto => dto.IntervalDays),
            new("Due", dto => dto.DueDate), new("Suspended", dto => dto.IsSuspended),
            new("Last reviewed", dto => dto.LastReviewedDate)
        ]);

        using var reader = new CsvReader(new StringReader(content), CultureInfo.InvariantCulture);
        reader.Read().Should().BeTrue();
        reader.ReadHeader();
        reader.HeaderRecord.Should().Equal("Front", "Deck", "Tags", "Interval", "Due", "Suspended", "Last reviewed");
        reader.Read().Should().BeTrue();
        reader.GetField("Front").Should().Be("Quoted \"front\", żółw\nnext line");
        reader.GetField("Deck").Should().Be("Root/Child");
        reader.GetField("Tags").Should().Be("first,second");
        reader.GetField("Interval").Should().Be("12");
        DateTime.ParseExact(reader.GetField("Due")!, "dd/MM/yyyy", CultureInfo.CurrentCulture)
            .Should().Be(new DateTime(2026, 10, 25));
        reader.GetField("Suspended").Should().Be("True");
        reader.GetField("Last reviewed").Should().Be("N/A");
        reader.Read().Should().BeFalse();
        serializer.Verify(s => s.Serialize(cards), Times.Once);
        serializer.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PrepareCsv_MapsAllColumnsIncludingNullableMetadata(bool hasReviewHistory)
    {
        var card = new CardEntity
        {
            DeckId = 2,
            Deck = new Deck { Id = 2, Name = "Child" },
            Note = StandardNote.Create("front", "back"),
            Tags = [new Tag { Name = "żółw" }, new Tag { Name = "tag, with comma" }],
            Interval = TimeSpan.FromDays(12),
            State = CardState.Learning,
            IsSuspended = true,
            IsBuried = false,
            LearningStage = hasReviewHistory ? LearningStage.II : null,
            Created = new DateTime(2026, 9, 1),
            Due = hasReviewHistory ? new DateTime(2026, 10, 29) : null,
            LastModified = hasReviewHistory ? new DateTime(2026, 10, 26) : null,
            LastReviewed = hasReviewHistory ? new DateTime(2026, 10, 25) : null
        };
        CardEntity[] cards = [card];
        var serializer = new Mock<INoteSerializer>(MockBehavior.Strict);
        serializer.Setup(service => service.Serialize(cards)).Returns(new[] { (card, card.Note) });
        CsvExportColumn[] columns =
        [
            new("Front", dto => dto.FrontContent), new("Back", dto => dto.BackContent),
            new("Deck", dto => dto.Deck), new("Due", dto => dto.DueDate),
            new("Tags", dto => dto.Tags), new("Interval", dto => dto.IntervalDays),
            new("State", dto => dto.State), new("Suspended", dto => dto.IsSuspended),
            new("Buried", dto => dto.IsBuried), new("Stage", dto => dto.LearningStage),
            new("Created", dto => dto.CreatedDate), new("Modified", dto => dto.LastModifiedDate),
            new("Reviewed", dto => dto.LastReviewedDate)
        ];

        string content = new ExportService(serializer.Object).PrepareCsv(
            cards, new Dictionary<long, string> { [2] = "Root/Child" }, columns);

        using var reader = new CsvReader(new StringReader(content), CultureInfo.InvariantCulture);
        reader.Read().Should().BeTrue();
        reader.ReadHeader();
        reader.HeaderRecord.Should().Equal(columns.Select(column => column.Name));
        reader.Read().Should().BeTrue();
        reader.GetField("Front").Should().Be("front");
        reader.GetField("Back").Should().Be("back");
        reader.GetField("Deck").Should().Be("Root/Child");
        reader.GetField("Tags").Should().Be("żółw,tag, with comma");
        reader.GetField("Interval").Should().Be("12");
        reader.GetField("State").Should().Be("Learning");
        reader.GetField("Suspended").Should().Be("True");
        reader.GetField("Buried").Should().Be("False");
        reader.GetField("Stage").Should().Be(hasReviewHistory ? "II" : "N/A");
        DateTime.ParseExact(reader.GetField("Created")!, "dd/MM/yyyy", CultureInfo.CurrentCulture)
            .Should().Be(new DateTime(2026, 9, 1));
        foreach (var (column, expected) in new[]
                 { ("Due", card.Due), ("Modified", card.LastModified), ("Reviewed", card.LastReviewed) })
        {
            if (expected is DateTime date)
                DateTime.ParseExact(reader.GetField(column)!, "dd/MM/yyyy", CultureInfo.CurrentCulture).Should().Be(date);
            else
                reader.GetField(column).Should().Be("N/A");
        }
        reader.Read().Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveAsync_WritesCompleteContentAndLeavesNoTemporaryFile(bool destinationExists)
    {
        string path = Path.Combine(directory.FullName, "export.txt");
        if (destinationExists)
            await File.WriteAllTextAsync(path, "previous export");
        var service = new ExportService(Mock.Of<INoteSerializer>());

        await service.SaveAsync("żółw\ncomplete export", path);

        (await File.ReadAllTextAsync(path)).Should().Be("żółw\ncomplete export");
        directory.GetFiles().Should().ContainSingle(file => file.FullName == path);
    }

    [Fact]
    public async Task SaveAsync_LockedDestinationPreservesOriginalAndCleansTemporaryFile_ThenCanBeRetried()
    {
        string path = Path.Combine(directory.FullName, "export.txt");
        await File.WriteAllTextAsync(path, "original content");
        var service = new ExportService(Mock.Of<INoteSerializer>());
        using (File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Func<Task> save = () => service.SaveAsync("replacement", path);

            var failure = await save.Should().ThrowAsync<Exception>();
            Assert.True(failure.Which is IOException or UnauthorizedAccessException);

            directory.GetFiles().Should().ContainSingle(file => file.FullName == path);
        }
        (await File.ReadAllTextAsync(path)).Should().Be("original content");

        await service.SaveAsync("replacement", path);

        (await File.ReadAllTextAsync(path)).Should().Be("replacement");
        directory.GetFiles().Should().ContainSingle(file => file.FullName == path);
    }

    [Fact]
    public async Task SaveAsync_MissingDirectoryPropagatesFailureWithoutCreatingOutput()
    {
        string path = Path.Combine(directory.FullName, "missing", "export.txt");
        var service = new ExportService(Mock.Of<INoteSerializer>());

        Func<Task> save = () => service.SaveAsync("content", path);

        await save.Should().ThrowAsync<DirectoryNotFoundException>();
        directory.GetFileSystemInfos().Should().BeEmpty();
    }

    public void Dispose() => directory.Delete(recursive: true);
}
