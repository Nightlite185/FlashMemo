using System.Globalization;
using CsvHelper;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.Tests.Fakes.Model;
using FlashMemo.ViewModel.Factories;
using FlashMemo.ViewModel.Popups;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlashMemo.Tests.ServiceTests;

public class ExportIntegrationTests : IDisposable
{
    private readonly FakeDbFactory factory = new();
    private readonly DirectoryInfo directory = Directory.CreateTempSubdirectory("FlashMemo-ExportIntegrationTests-");

    [Theory]
    [InlineData(ExportFormat.txt)]
    [InlineData(ExportFormat.csv)]
    public Task Export_SelectedSubtreeUsesStoredXamlAndTagsWithoutChangingDatabase(ExportFormat format)
        => Helpers.RunOnSta(async () =>
    {
        await new TestsDbSeeder(factory).SeedDefault();
        var rootCard = CreateCard(101, 7, 10, "Root front");
        var childCard = CreateCard(102, 7, 11, "Żółw, \"quoted\" 🐢");
        childCard.IsBuried = true;
        childCard.IsSuspended = true;
        string originalFront = ((StandardNote)childCard.Note).FrontContent;
        string originalBack = ((StandardNote)childCard.Note).BackContent;
        await using (var db = factory.CreateDbContext())
        {
            db.Users.AddRange(
                new UserEntity { Id = 7, Name = "Export user", Options = UserOptions.CreateDefault() },
                new UserEntity { Id = 8, Name = "Other user", Options = UserOptions.CreateDefault() });
            db.Decks.AddRange(
                CreateDeck(10, 7, "Root"), CreateDeck(11, 7, "Child", parentId: 10),
                CreateDeck(20, 7, "Unselected"), CreateDeck(30, 8, "Other user's deck"));
            childCard.Tags.Add(new Tag { Id = 41, UserId = 7, Name = "język, polski" });
            db.Cards.AddRange(rootCard, childCard,
                CreateCard(103, 7, 20, "Excluded deck"), CreateCard(104, 8, 30, "Excluded user"));
            await db.SaveChangesAsync();
        }
        var options = new UserOptionsService(factory);
        var counter = new CountingService(factory, null!, options);
        var tree = await new DeckTreeBuilder(new DeckRepo(factory), counter).BuildAsync(7);
        var root = tree.Single(deck => deck.Id == 10);
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);
        bool closed = false;
        var vm = await new ExportVMF(
            new CardQueryService(factory, counter, options), new ExportService(new XamlNoteSerializer()),
            counter, dialogs.Object,
            NullLogger<ExportVM>.Instance).CreateAsync(7, [root], () => closed = true);
        vm.Should().NotBeNull();
        vm!.Format = format;
        vm.ExportPath = Path.Combine(directory.FullName, $"export.{format}");
        if (format == ExportFormat.csv)
            vm.CsvColumns.Single(column => column.Name == "Tags").IsSelected = true;

        await vm.ConfirmCommand.ExecuteAsync(null);

        closed.Should().BeTrue();
        dialogs.VerifyNoOtherCalls();
        directory.GetFiles().Should().ContainSingle(file => file.FullName == vm.ExportPath);
        if (format == ExportFormat.txt)
        {
            (await File.ReadAllTextAsync(vm.ExportPath)).Should().Be(
                $"Root front - answer{Environment.NewLine}second line\n"
                + $"Żółw, \"quoted\" 🐢 - answer{Environment.NewLine}second line\n");
        }
        else
        {
            using var reader = new CsvReader(new StreamReader(vm.ExportPath), CultureInfo.InvariantCulture);
            reader.Read().Should().BeTrue();
            reader.ReadHeader();
            reader.HeaderRecord.Should().Equal("Front content", "Back content", "Deck", "Due date", "Tags");
            reader.Read().Should().BeTrue();
            reader.GetField("Front content").Should().Be("Root front");
            reader.GetField("Deck").Should().Be("Root");
            reader.GetField("Tags").Should().BeEmpty();
            reader.Read().Should().BeTrue();
            reader.GetField("Front content").Should().Be("Żółw, \"quoted\" 🐢");
            reader.GetField("Back content").Should().Be($"answer{Environment.NewLine}second line");
            reader.GetField("Deck").Should().Be("Root/Child");
            reader.GetField("Tags").Should().Be("język, polski");
            reader.Read().Should().BeFalse();
        }
        await using var verifyDb = factory.CreateDbContext();
        var stored = await verifyDb.Cards.AsNoTracking().Include(card => card.Tags)
            .SingleAsync(card => card.Id == childCard.Id);
        var note = stored.Note.Should().BeOfType<StandardNote>().Subject;
        note.FrontContent.Should().Be(originalFront);
        note.BackContent.Should().Be(originalBack);
        stored.IsBuried.Should().BeTrue();
        stored.IsSuspended.Should().BeTrue();
        stored.Tags.Should().ContainSingle(tag => tag.Name == "język, polski");
        (await verifyDb.Cards.CountAsync()).Should().Be(4);
    });

    private static Deck CreateDeck(long id, long userId, string name, long? parentId = null)
        => new() { Id = id, UserId = userId, Name = name, ParentDeckId = parentId, OptionsId = DeckOptions.DefaultId };

    private static CardEntity CreateCard(long id, long userId, long deckId, string front)
        => new()
        {
            Id = id, UserId = userId, DeckId = deckId,
            Created = new DateTime(2026, 10, 1).AddMinutes(id),
            Note = StandardNote.Create(
                $"<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph><Bold>{front}</Bold></Paragraph></FlowDocument>",
                "<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph>answer<LineBreak/>second line</Paragraph></FlowDocument>")
        };

    public void Dispose()
    {
        factory.Dispose();
        directory.Delete(recursive: true);
    }
}
