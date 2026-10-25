using System.Globalization;
using System.IO;
using CsvHelper;
using FlashMemo.Model;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.ViewModel.Factories;
using FlashMemo.ViewModel.Popups;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;
using Moq;

namespace FlashMemo.Tests.VMTests;

public class ExportVMTests : IDisposable
{
    private readonly DirectoryInfo exportDirectory = Directory.CreateTempSubdirectory("FlashMemo-ExportTests-");

    [Fact]
    public async Task DefaultFilename_MatchesTextFormat()
    {
        var vm = await CreateViewModel();

        Path.GetExtension(vm.ExportPath).Should().Be(".txt");
        vm.HasErrors.Should().BeFalse();
        vm.ConfirmCommand.CanExecute(null).Should().BeTrue();
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("export", false)]
    [InlineData("export.csv", false)]
    [InlineData("export.txt.csv", false)]
    [InlineData("invalid?.txt", false)]
    [InlineData("export.txt", true)]
    [InlineData("export.TXT", true)]
    public async Task ExportPath_ReportsErrorsAndControlsConfirmation(string fileName, bool valid)
    {
        var vm = await CreateViewModel();
        vm.ExportPath = string.IsNullOrWhiteSpace(fileName)
            ? fileName
            : Path.Combine(Path.GetTempPath(), fileName);

        vm.GetErrors(nameof(ExportVM.ExportPath)).Any().Should().Be(!valid);
        vm.ConfirmCommand.CanExecute(null).Should().Be(valid);
    }

    [Fact]
    public async Task FolderPath_IsRejected()
    {
        var vm = await CreateViewModel();
        vm.ExportPath = Path.GetTempPath();

        vm.GetErrors(nameof(ExportVM.ExportPath)).Should().NotBeEmpty();
        vm.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task CorrectingPath_ClearsErrorsAndNotifiesCommand()
    {
        var vm = await CreateViewModel();
        int commandChanges = 0;
        vm.ConfirmCommand.CanExecuteChanged += (_, _) => commandChanges++;

        vm.ExportPath = Path.Combine(Path.GetTempPath(), "export.csv");
        vm.ExportPath = Path.Combine(Path.GetTempPath(), "export.txt");

        vm.HasErrors.Should().BeFalse();
        vm.ConfirmCommand.CanExecute(null).Should().BeTrue();
        commandChanges.Should().BeGreaterThanOrEqualTo(2);
    }

    [Theory]
    [InlineData(NoteSeparator.Tab, "\t")]
    [InlineData(NoteSeparator.Enter, "\n")]
    [InlineData(NoteSeparator.Space, " ")]
    [InlineData(NoteSeparator.SemiColon, ";")]
    public async Task MatchingSeparators_AreReportedOnBothInputs_AndClearWhenNoteSeparatorChanges(
        NoteSeparator separator, string text)
    {
        var vm = await CreateViewModel();
        vm.NoteSep = separator;
        vm.FieldSeparator = text;

        vm.GetErrors(nameof(ExportVM.FieldSeparator)).Should().NotBeEmpty();
        vm.GetErrors(nameof(ExportVM.NoteSep)).Should().NotBeEmpty();
        vm.ConfirmCommand.CanExecute(null).Should().BeFalse();

        vm.NoteSep = separator == NoteSeparator.Space ? NoteSeparator.Enter : NoteSeparator.Space;

        vm.HasErrors.Should().BeFalse();
        vm.ConfirmCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task ChangingFieldSeparator_ClearsBothSeparatorErrors()
    {
        var vm = await CreateViewModel();
        vm.FieldSeparator = ";";
        vm.NoteSep = NoteSeparator.SemiColon;

        vm.HasErrors.Should().BeTrue();

        vm.FieldSeparator = " - ";

        vm.HasErrors.Should().BeFalse();
        vm.ConfirmCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task EmptyFieldSeparator_IsRejected()
    {
        var vm = await CreateViewModel();
        vm.FieldSeparator = "";

        vm.GetErrors(nameof(ExportVM.FieldSeparator)).Should().NotBeEmpty();
        vm.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task ChangingFormat_UpdatesTheExtensionAndClearsUnsupportedFormatErrors()
    {
        var vm = await CreateViewModel();
        vm.Format = (ExportFormat)99;
        vm.ExportPath = Path.Combine(Path.GetTempPath(), "export.csv");

        vm.GetErrors(nameof(ExportVM.Format)).Should().NotBeEmpty();
        vm.ConfirmCommand.CanExecute(null).Should().BeFalse();

        vm.Format = ExportFormat.txt;

        vm.GetErrors(nameof(ExportVM.Format)).Should().BeEmpty();
        Path.GetExtension(vm.ExportPath).Should().Be(".txt");
        vm.GetErrors(nameof(ExportVM.ExportPath)).Should().BeEmpty();

        vm.ExportPath = Path.Combine(Path.GetTempPath(), "export.txt");

        vm.HasErrors.Should().BeFalse();
        vm.ConfirmCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task SwitchingFormats_PreservesOptionsAndOnlyValidatesVisibleSeparators()
    {
        var vm = await CreateViewModel();
        vm.FieldSeparator = ";";
        vm.NoteSep = NoteSeparator.SemiColon;
        vm.HasErrors.Should().BeTrue();
        var tags = vm.CsvColumns.Single(col => col.Name == "Tags");
        tags.IsSelected = true;

        vm.Format = ExportFormat.csv;

        Path.GetExtension(vm.ExportPath).Should().Be(".csv");
        vm.HasErrors.Should().BeFalse();
        vm.ConfirmCommand.CanExecute(null).Should().BeTrue();

        vm.Format = ExportFormat.txt;

        vm.FieldSeparator.Should().Be(";");
        vm.NoteSep.Should().Be(NoteSeparator.SemiColon);
        vm.HasErrors.Should().BeTrue();
        tags.IsSelected.Should().BeTrue();
    }

    [Fact]
    public async Task CsvFormat_DoesNotRequireATextFieldSeparator()
    {
        var vm = await CreateViewModel();
        vm.FieldSeparator = "";
        vm.Format = ExportFormat.csv;
        vm.Validate();

        vm.HasErrors.Should().BeFalse();

        vm.Format = ExportFormat.txt;

        vm.GetErrors(nameof(ExportVM.FieldSeparator)).Should().NotBeEmpty();
    }

    [Fact]
    public async Task ResetCsvColumns_RestoresDefaultsAfterSelectingAll()
    {
        var vm = await CreateViewModel();
        vm.CsvColumns.Where(col => col.IsDefault).Select(col => col.Name)
            .Should().Equal("Front content", "Back content", "Deck", "Due date");

        vm.SelectAllCsvColumnsCommand.Execute(null);
        vm.CsvColumns.Should().OnlyContain(col => col.IsSelected);

        vm.ResetCsvColumnsCommand.Execute(null);

        vm.CsvColumns.Should().OnlyContain(col => col.IsSelected == col.IsDefault);
    }

    [Fact]
    public async Task InvalidNoteSeparator_IsRejected()
    {
        var vm = await CreateViewModel();
        vm.NoteSep = (NoteSeparator)99;

        vm.GetErrors(nameof(ExportVM.NoteSep)).Should().NotBeEmpty();
        vm.ConfirmCommand.CanExecute(null).Should().BeFalse();

        vm.NoteSep = NoteSeparator.Enter;

        vm.HasErrors.Should().BeFalse();
    }

    [Fact]
    public async Task ExecutingInvalidExport_DoesNotQueryCardsOrClosePopup()
    {
        var query = new Mock<ICardQueryService>(MockBehavior.Strict);
        var serializer = new Mock<INoteSerializer>(MockBehavior.Strict);
        bool closed = false;
        var vm = await CreateViewModel(query.Object, serializer.Object, () => closed = true);
        vm.FieldSeparator = "\n";

        await vm.ConfirmCommand.ExecuteAsync(null);

        query.VerifyNoOtherCalls();
        serializer.VerifyNoOtherCalls();
        closed.Should().BeFalse();
    }

    [Fact]
    public async Task CsvExport_WritesHierarchicalPathsForEveryBranchAndRoot()
    {
        var root = CreateDeck(1, "Languages, \"daily\"");
        var child = CreateDeck(2, "Spanish");
        var grandchild = CreateDeck(3, "Verbs");
        var sibling = CreateDeck(4, "French");
        var otherRoot = CreateDeck(5, "Science");
        var otherChild = CreateDeck(6, "Verbs");
        root.AddChild(child);
        child.AddChild(grandchild);
        root.AddChild(sibling);
        otherRoot.AddChild(otherChild);
        CardEntity[] cards = [
            CreateCard(root), CreateCard(child), CreateCard(grandchild),
            CreateCard(sibling), CreateCard(otherRoot), CreateCard(otherChild)
        ];
        int closed = 0;
        var (vm, query) = await CreateExportViewModel([root, otherRoot], cards, () => closed++);
        vm.Format = ExportFormat.csv;

        await vm.ConfirmCommand.ExecuteAsync(null);

        var records = ReadCsv(vm.ExportPath);
        records[0].Should().Equal("Front content", "Back content", "Deck", "Due date");
        records.Skip(1).Select(row => row[2]).Should().Equal(
            "Languages, \"daily\"",
            "Languages, \"daily\"/Spanish",
            "Languages, \"daily\"/Spanish/Verbs",
            "Languages, \"daily\"/French",
            "Science",
            "Science/Verbs");
        records.Skip(1).Select(row => row[0]).Should().Equal(cards.Select(card => $"front {card.Id}"));
        records.Skip(1).Select(row => row[1]).Should().Equal(cards.Select(card => $"back {card.Id}"));
        records.Skip(1).Select(row => row[3]).Should().OnlyContain(due => due == "N/A");
        query.Verify(service => service.GetCardsWhere(
            It.Is<Filters>(filters => filters.UserId == 7
                && filters.DeckIds.SequenceEqual(new long[] { 1, 2, 3, 4, 5, 6 })
                && filters.IncludeChildrenDecks),
            CardsOrder.Created, SortingDirection.Ascending), Times.Once);
        query.VerifyNoOtherCalls();
        closed.Should().Be(1);
        exportDirectory.GetFiles().Should().ContainSingle(file => file.FullName == vm.ExportPath);
    }

    [Fact]
    public async Task CsvExport_SelectedSubtreeStartsPathsAtSelectedDeck()
    {
        var ancestor = CreateDeck(1, "Unselected ancestor");
        var selected = CreateDeck(2, "Selected");
        var child = CreateDeck(3, "Child");
        ancestor.AddChild(selected);
        selected.AddChild(child);
        var (vm, query) = await CreateExportViewModel(
            [selected], [CreateCard(selected), CreateCard(child)]);
        vm.Format = ExportFormat.csv;

        await vm.ConfirmCommand.ExecuteAsync(null);

        ReadCsv(vm.ExportPath).Skip(1).Select(row => row[2])
            .Should().Equal("Selected", "Selected/Child");
        query.Verify(service => service.GetCardsWhere(
            It.Is<Filters>(filters => filters.DeckIds.SequenceEqual(new long[] { 2, 3 })),
            CardsOrder.Created, SortingDirection.Ascending), Times.Once);
        query.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CsvExport_EmptyDeckSelectionExportsAllUserCardsWithDeckNameFallback()
    {
        var deck = CreateDeck(1, "Standalone");
        IList<CardEntity> cards = new[] { CreateCard(deck) };
        var query = new Mock<ICardQueryService>(MockBehavior.Strict);
        query.Setup(service => service.GetAllFromUser(7)).ReturnsAsync(cards);
        var serializer = CreateSerializer(cards);
        bool closed = false;
        var vm = new ExportVM(7, query.Object, serializer.Object, () => closed = true, [], []);
        vm.Format = ExportFormat.csv;
        vm.ExportPath = Path.Combine(exportDirectory.FullName, "all-cards.csv");

        await vm.ConfirmCommand.ExecuteAsync(null);

        ReadCsv(vm.ExportPath)[1].Should().Equal("front 1", "back 1", "Standalone", "N/A");
        query.Verify(service => service.GetAllFromUser(7), Times.Once);
        query.VerifyNoOtherCalls();
        serializer.Verify(service => service.Serialize(cards), Times.Once);
        closed.Should().BeTrue();
    }

    [Fact]
    public async Task CsvExport_OnlyWritesSelectedColumns()
    {
        var root = CreateDeck(1, "Root");
        var child = CreateDeck(2, "Child");
        root.AddChild(child);
        var (vm, _) = await CreateExportViewModel([root], [CreateCard(child)]);
        vm.Format = ExportFormat.csv;
        foreach (var column in vm.CsvColumns)
            column.IsSelected = column.Name == "Deck";

        await vm.ConfirmCommand.ExecuteAsync(null);

        var records = ReadCsv(vm.ExportPath);
        records.Should().HaveCount(2);
        records[0].Should().Equal("Deck");
        records[1].Should().Equal("Root/Child");
    }

    [Fact]
    public async Task TextExport_SelectedTreeUsesConfiguredSeparatorsWithoutDeckPaths()
    {
        var root = CreateDeck(1, "Root");
        var child = CreateDeck(2, "Child");
        root.AddChild(child);
        var (vm, query) = await CreateExportViewModel(
            [root], [CreateCard(root), CreateCard(child)]);
        vm.FieldSeparator = " | ";
        vm.NoteSep = NoteSeparator.SemiColon;

        await vm.ConfirmCommand.ExecuteAsync(null);

        (await File.ReadAllTextAsync(vm.ExportPath)).Should().Be("front 1 | back 1;front 2 | back 2;");
        query.Verify(service => service.GetCardsWhere(
            It.Is<Filters>(filters => filters.UserId == 7
                && filters.DeckIds.SequenceEqual(new long[] { 1, 2 })),
            CardsOrder.Created, SortingDirection.Ascending), Times.Once);
        query.VerifyNoOtherCalls();
    }

    private async Task<(ExportVM VM, Mock<ICardQueryService> Query)> CreateExportViewModel(
        ICollection<DeckNode> deckTree, ICollection<CardEntity> cards, Action? cancel = null)
    {
        var query = new Mock<ICardQueryService>(MockBehavior.Strict);
        query.Setup(service => service.GetCardsWhere(
            It.IsAny<Filters>(), CardsOrder.Created, SortingDirection.Ascending)).ReturnsAsync(cards);
        var serializer = CreateSerializer(cards);
        var vm = await CreateViewModel(query.Object, serializer.Object, cancel, deckTree, userId: 7);
        vm.ExportPath = Path.Combine(exportDirectory.FullName, "export.txt");
        return (vm, query);
    }

    private static Mock<INoteSerializer> CreateSerializer(IEnumerable<CardEntity> cards)
    {
        var serializer = new Mock<INoteSerializer>(MockBehavior.Strict);
        serializer.Setup(service => service.Serialize(cards))
            .Returns(cards.Select(card => (card, card.Note)).ToArray());
        return serializer;
    }

    private static DeckNode CreateDeck(long id, string name)
        => new(new Deck { Id = id, UserId = 7, Name = name }, []);

    private static CardEntity CreateCard(DeckNode deck)
        => new()
        {
            Id = deck.Id,
            UserId = 7,
            DeckId = deck.Id,
            Deck = deck.ToEntity(),
            Note = StandardNote.Create($"front {deck.Id}", $"back {deck.Id}")
        };

    private static List<string[]> ReadCsv(string path)
    {
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var records = new List<string[]>();
        while (csv.Read())
            records.Add(Enumerable.Range(0, csv.Parser.Count).Select(index => csv.GetField(index)!).ToArray());
        return records;
    }

    private static async Task<ExportVM> CreateViewModel(
        ICardQueryService? cardQuery = null, INoteSerializer? serializer = null, Action? cancel = null,
        IEnumerable<DeckNode>? deckTree = null, long userId = 1)
    {
        var counter = new Mock<ICountingService>();
        counter.Setup(c => c.AllCardsInDecks(It.IsAny<IEnumerable<long>>())).ReturnsAsync(1);

        var vm = await new ExportVMF(
                cardQuery ?? Mock.Of<ICardQueryService>(),
                serializer ?? Mock.Of<INoteSerializer>(),
                counter.Object)
            .CreateAsync(userId, deckTree ?? [CreateDeck(1, "Deck")], cancel ?? (() => { }));

        vm.Should().NotBeNull();
        return vm!;
    }

    public void Dispose() => exportDirectory.Delete(recursive: true);
}
